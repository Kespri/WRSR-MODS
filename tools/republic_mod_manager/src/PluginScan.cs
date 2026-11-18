// Republic Mod Manager - the plugin DLLs TesmioLoader will load, found the way the loader finds
// them. This file mirrors tesmio_discovery.h of the loader rule for rule: the manager must never
// list a plugin the loader passes over, or miss one it takes.
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace TesmioAutoload
{
    // One plugin DLL as the loader sees it.
    public sealed class FoundPlugin
    {
        // Key is the file name without ".dll": the name of the switch in tesmioloader.ini [plugins].
        // Ini is the file beside the DLL that the loader hands to the plugin, present or not.
        public string Path="",File="",Key="",Ini="";
        // True for a DLL in <loader>\plugins, false for one below the Workshop folder.
        public bool Local;
        // Workshop only: the item folder (<workshop>\<number>) the DLL lies in.
        public string Item="";
        // The loader skips a plugin whose INI path does not fit into MAX_PATH.
        public bool PathTooLong;
        // Other DLLs with the same key, which the loader passes over, in the order it ranks them.
        public readonly List<FoundPlugin> Shadowed=new List<FoundPlugin>();
    }

    public static class PluginScan
    {
        public const string WorkshopSubdir=@"workshop\content\784150";
        public const string ExportApiVersion="TsmPluginApiVersion",ExportInit="TsmPluginInit";
        const int MaxPath=260;

        // The loader compares keys with _wcsicmp in the "C" locale: only A-Z fold.
        static char Fold(char c){return c>='A'&&c<='Z'?(char)(c+32):c;}
        public static int CompareKeys(string a,string b)
        {
            int n=Math.Min(a.Length,b.Length);
            for(int i=0;i<n;i++){int d=Fold(a[i])-Fold(b[i]);if(d!=0)return d;}
            return a.Length-b.Length;
        }
        public static bool SameKey(string a,string b){return CompareKeys(a??"",b??"")==0;}

        static string Trimmed(string dir){return (dir??"").Replace('/','\\').TrimEnd('\\');}
        static string Parent(string path){int end=path.LastIndexOf('\\');return end<0?"":path.Substring(0,end);}
        static string Leaf(string path){int end=path.LastIndexOf('\\');return end<0?path:path.Substring(end+1);}

        // From the loader's folder upwards to "steamapps", then workshop\content\784150. Empty when
        // the loader does not lie below a Steam library.
        public static string WorkshopDefault(string loaderDir)
        {
            string dir=Trimmed(loaderDir);
            while(dir.Length>0)
            {
                if(SameKey(Leaf(dir),"steamapps"))return dir+"\\"+WorkshopSubdir;
                dir=Parent(dir);
            }
            return "";
        }

        // The export table read as file data, exactly as the loader reads it: a 64-bit DLL that
        // exports both plugin entry points by name. Nothing of the file is executed.
        public static bool HasPluginExports(string path)
        {
            byte[] data;
            try
            {
                using(var stream=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.Read|FileShare.Delete))
                {
                    if(stream.Length<=0||stream.Length>int.MaxValue)return false;
                    data=new byte[(int)stream.Length];
                    int done=0;
                    while(done<data.Length){int got=stream.Read(data,done,data.Length-done);if(got<=0)return false;done+=got;}
                }
            }
            catch(IOException){return false;}
            catch(UnauthorizedAccessException){return false;}
            return IsPluginImage(data);
        }

        sealed class Image
        {
            public byte[] Data;
            public long Sections;
            public int SectionCount;
            public bool Has(long offset,long count,int size){return offset>=0&&offset<=Data.Length&&count<=(Data.Length-offset)/size;}
            public uint U32(long offset){return BitConverter.ToUInt32(Data,(int)offset);}
            public ushort U16(long offset){return BitConverter.ToUInt16(Data,(int)offset);}
            // File offset of `count` items of `size` bytes at an RVA, or -1. The first section that
            // holds the range decides, as in the loader.
            public long FromRva(uint rva,long count,int size)
            {
                if(rva==0||count==0)return -1;
                for(int i=0;i<SectionCount;i++)
                {
                    long section=Sections+40L*i;
                    uint address=U32(section+12),rawSize=U32(section+16),rawAt=U32(section+20);
                    if(rva<address)continue;
                    uint delta=rva-address;
                    if(delta>=rawSize||count>(rawSize-delta)/size)continue;
                    long at=(long)rawAt+delta;
                    return Has(at,count,size)?at:-1;
                }
                return -1;
            }
            public bool Matches(uint rva,string name)
            {
                long at=FromRva(rva,name.Length+1,1);
                if(at<0)return false;
                for(int i=0;i<name.Length;i++)if(Data[at+i]!=(byte)name[i])return false;
                return Data[at+name.Length]==0;
            }
        }

        public static bool IsPluginImage(byte[] data)
        {
            if(data==null)return false;
            var image=new Image{Data=data};
            // IMAGE_DOS_HEADER: 64 bytes, e_magic "MZ", e_lfanew at 60.
            if(!image.Has(0,1,64)||image.U16(0)!=0x5A4D)return false;
            int lfanew=BitConverter.ToInt32(data,60);
            if(lfanew<0)return false;
            long nt=lfanew;
            // IMAGE_NT_HEADERS64: signature 4, file header 20, optional header 240.
            if(!image.Has(nt,1,264)||image.U32(nt)!=0x00004550)return false;
            ushort machine=image.U16(nt+4),sectionCount=image.U16(nt+6),optionalSize=image.U16(nt+20),characteristics=image.U16(nt+22);
            if(machine!=0x8664||(characteristics&0x2000)==0||optionalSize<240)return false;
            if(image.U16(nt+24)!=0x20B||image.U32(nt+132)<=0)return false;
            image.SectionCount=sectionCount;
            image.Sections=nt+24+optionalSize;
            if(sectionCount==0||!image.Has(image.Sections,sectionCount,40))return false;
            uint exportRva=image.U32(nt+136),exportSize=image.U32(nt+140);
            // IMAGE_EXPORT_DIRECTORY: 40 bytes.
            long exports=image.FromRva(exportRva,1,40);
            if(exports<0||exportSize<40)return false;
            uint functionCount=image.U32(exports+20),nameCount=image.U32(exports+24);
            long functions=image.FromRva(image.U32(exports+28),functionCount,4);
            long names=image.FromRva(image.U32(exports+32),nameCount,4);
            long ordinals=image.FromRva(image.U32(exports+36),nameCount,2);
            if(names<0||ordinals<0||functions<0)return false;
            bool version=false,init=false;
            for(long i=0;i<nameCount;i++)
            {
                ushort ordinal=image.U16(ordinals+2*i);
                if(ordinal>=functionCount||image.U32(functions+4L*ordinal)==0)continue;
                uint name=image.U32(names+4*i);
                if(image.Matches(name,ExportApiVersion))version=true;
                if(image.Matches(name,ExportInit))init=true;
                if(version&&init)return true;
            }
            return false;
        }

        static void ScanDirectory(string root,bool recursive,bool local,string workshop,List<FoundPlugin> plugins)
        {
            root=Trimmed(root);
            if(root.Length==0||!Directory.Exists(root))return;
            var pending=new Stack<string>();
            pending.Push(root);
            while(pending.Count>0)
            {
                string dir=pending.Pop();
                FileSystemInfo[] entries;
                try{entries=new DirectoryInfo(dir).GetFileSystemInfos();}
                catch(IOException){continue;}
                catch(UnauthorizedAccessException){continue;}
                foreach(var entry in entries)
                {
                    // Neither a linked folder nor a linked file is followed.
                    if((entry.Attributes&FileAttributes.ReparsePoint)!=0)continue;
                    if((entry.Attributes&FileAttributes.Directory)!=0)
                    {
                        if(recursive)pending.Push(dir+"\\"+entry.Name);
                        continue;
                    }
                    string name=entry.Name;
                    int dot=name.LastIndexOf('.');
                    if(dot<=0||!SameKey(name.Substring(dot),".dll"))continue;
                    string path=dir+"\\"+name;
                    if(!HasPluginExports(path))continue;
                    var found=new FoundPlugin{Path=path,File=name,Key=name.Substring(0,dot),Local=local};
                    found.Ini=path.Substring(0,path.Length-4)+".ini";
                    found.PathTooLong=found.Ini.Length>=MaxPath;
                    if(!local)found.Item=ItemOf(workshop,path);
                    plugins.Add(found);
                }
            }
        }

        // <workshop>\<item>\... -> <workshop>\<item>; "" for a file directly in the Workshop folder.
        public static string ItemOf(string workshop,string path)
        {
            string root=Trimmed(workshop);
            if(root.Length==0||path.Length<=root.Length+1||!path.StartsWith(root+"\\",StringComparison.OrdinalIgnoreCase))return "";
            int end=path.IndexOf('\\',root.Length+1);
            return end<0?"":path.Substring(0,end);
        }

        static int ByKeyThenPath(FoundPlugin a,FoundPlugin b)
        {
            int key=CompareKeys(a.Key,b.Key);
            return key!=0?key:String.CompareOrdinal(a.Path,b.Path);
        }

        // Every plugin DLL the loader will consider, in the order it loads them: by key, a DLL in
        // <loader>\plugins before one of the same key in the Workshop folder, and among Workshop
        // DLLs of one key the smallest full path. The losers of a key hang on the winner.
        public static List<FoundPlugin> Discover(string local,string workshop)
        {
            var localFound=new List<FoundPlugin>();
            var workshopFound=new List<FoundPlugin>();
            ScanDirectory(local,false,true,"",localFound);
            ScanDirectory(workshop,true,false,workshop,workshopFound);
            localFound.Sort(ByKeyThenPath);
            workshopFound.Sort(ByKeyThenPath);
            var merged=new List<FoundPlugin>();
            int l=0,w=0;
            while(l<localFound.Count||w<workshopFound.Count)
            {
                bool takeLocal=w>=workshopFound.Count||(l<localFound.Count&&CompareKeys(localFound[l].Key,workshopFound[w].Key)<=0);
                merged.Add(takeLocal?localFound[l++]:workshopFound[w++]);
            }
            var result=new List<FoundPlugin>();
            foreach(var plugin in merged)
            {
                if(result.Count>0&&SameKey(result[result.Count-1].Key,plugin.Key))result[result.Count-1].Shadowed.Add(plugin);
                else result.Add(plugin);
            }
            return result;
        }
    }
}
