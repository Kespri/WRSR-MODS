// Content packages (0.4.80): a Workshop package with [content] in its manifest and no DLL -
// the Soviet Mod Loader convention (resources, deposits, needs, buildings as INI fragments plus
// assets under assets\media_soviet\...). RMM provides such a package by copying the fragments to
// user_config\.autoload\content\<id>\ and the assets into the loader's vfs; the keyed editors of
// the target plugins (Resources, Deposits Plus, Needs, Buildings Plus) merge every provided
// fragment into their effective INI as original entries. Removing the package takes the
// fragments and the copied assets away again, and the editors write their INIs without them.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace TesmioAutoload
{
    public static class ContentTargets
    {
        public static readonly string[] Keys={"resources","deposits","needs","buildings","rules"};
        // The plugins that read a fragment kind, in order of preference; the first one whose
        // effective INI exists in plugins\ takes the fragment.
        public static string[] PluginsFor(string key)
        {
            switch((key??"").ToLowerInvariant())
            {
                // 0.5.10: Resources Plus and Needs Plus are forks with files of their own
                // (plugins\resources_plus.ini, plugins\needs_plus.ini), like Deposits Plus.
                case "resources":return new[]{"resources_plus","resources"};
                case "deposits":return new[]{"deposits_plus","deposits"};
                case "needs":return new[]{"needs_plus","needs"};
                case "buildings":return new[]{"buildings_plus","buildings"};
                // 0.8.0: rule sets for Vanilla Buildings, one section per rule set.
                case "rules":return new[]{"vanilla_buildings"};
            }
            return new string[0];
        }
        public static string KeyForPlugin(string plugin)
        {foreach(string key in Keys)if(PluginsFor(key).Any(x=>x.Equals(plugin,StringComparison.OrdinalIgnoreCase)))return key;return "";}
        // The ONE rule for "who takes this fragment kind today": the first plugin of PluginsFor
        // that the loader finds and has switched on, else the first one it finds at all. The
        // receipt only records what it was last time; a fork that appeared since (resources_plus
        // next to resources) takes the fragment over at the next save.
        public static string Resolve(string build,string key)
        {
            try
            {
                string root=Path.GetFullPath(build),found="";
                foreach(string plugin in PluginsFor(key))
                {
                    if(!LoaderFiles.Present(root,plugin))continue;
                    if(LoaderFiles.Enabled(root,plugin))return plugin;
                    if(found.Length==0)found=plugin;
                }
                return found;
            }
            catch(Exception){}
            return "";
        }
        public static string StateRoot(string build){return SafeFiles.Child(Path.GetFullPath(build),"user_config\\.autoload\\content");}
        public static string FolderName(string packageId)
        {
            if(Regex.IsMatch(packageId??"","^[A-Za-z0-9._-]{1,80}$"))return packageId;
            return "content_"+SafeFiles.Hash(SafeFiles.Utf8.GetBytes(packageId??"")).Substring(0,16);
        }
        public static string StateDir(string build,string packageId){return Path.Combine(StateRoot(build),FolderName(packageId));}
        // The ids a fragment declares: section names for deposits and buildings, [list] keys for
        // resources and needs. Reserved switch sections of the plugins are not entries.
        public static List<string> IdsOf(string key,string text)
        {
            var result=new List<string>();LooseIni doc;try{doc=new LooseIni(text??"");}catch(Exception){return result;}
            if(key=="deposits"||key=="buildings"||key=="rules")
            {
                foreach(string section in doc.SectionNames()){string s=section.Trim();if(s.Length==0||s.Equals("general",StringComparison.OrdinalIgnoreCase)||s.Equals("vanilla_buildings",StringComparison.OrdinalIgnoreCase)||s.Equals("deposits",StringComparison.OrdinalIgnoreCase)||s.Equals("deposits_plus",StringComparison.OrdinalIgnoreCase)||s.Equals("buildings",StringComparison.OrdinalIgnoreCase)||s.Equals("buildings_plus",StringComparison.OrdinalIgnoreCase)||s.Equals("list",StringComparison.OrdinalIgnoreCase))continue;result.Add(s);}
                return result;
            }
            foreach(var pair in doc.Entries("list"))result.Add(pair.Key);
            return result;
        }
    }

    // 0.8.0: the part of a content package that gives vehicle production an extra material. It
    // is not merged into anybody's INI: when the player switches it on, the package's lines
    // become one file in plugins\vehicle_materials.d, which Vehicle Materials reads at start.
    public static class VehicleParts
    {
        public const string Plugin="vehicle_materials",Folder="vehicle_materials.d";
        // [materials] with lines `<resource> = <road>, <rail>, <ship>, <airplane>`: four numbers
        // from 0 up, at least one above 0, no resource twice. Returns the text as it is installed.
        public static string Normalize(string text,List<string> names)
        {
            var doc=new LooseIni(text??"");var result=new StringBuilder("[materials]\r\n");
            foreach(var pair in doc.Entries("materials"))
            {
                string name=pair.Key.Trim(),value=pair.Value??"";int comment=value.IndexOf(';');if(comment>=0)value=value.Substring(0,comment);
                string[] parts=value.Split(',').Select(x=>x.Trim()).ToArray();
                if(!Regex.IsMatch(name,"^[A-Za-z0-9_]{1,31}$")||(parts.Length!=4&&parts.Length!=5)||names.Contains(name,StringComparer.OrdinalIgnoreCase))throw new FormatException(name);
                double sum=0;
                foreach(string part in parts){double v;if(!Double.TryParse(part,System.Globalization.NumberStyles.Float,System.Globalization.CultureInfo.InvariantCulture,out v)||Double.IsNaN(v)||v<0||v>1000000)throw new FormatException(name);sum+=v;}
                if(sum<=0)throw new FormatException(name);
                names.Add(name);result.Append(name).Append(" = ").Append(String.Join(", ",parts)).Append("\r\n");
            }
            if(names.Count==0)throw new FormatException("materials");
            return result.ToString();
        }
        // 0.8.1: optional captions in the vehicles file - [names] for every language,
        // [names:German] for the German RMM. Keys of the dictionary: the id, or "German:"+id.
        public static void Captions(string text,Dictionary<string,string> captions)
        {
            try
            {
                var doc=new LooseIni(text??"");
                foreach(var pair in doc.Entries("names")){string v=(pair.Value??"").Trim();if(v.Length>0)captions[pair.Key.Trim()]=v;}
                foreach(var pair in doc.Entries("names:German")){string v=(pair.Value??"").Trim();if(v.Length>0)captions["German:"+pair.Key.Trim()]=v;}
            }
            catch(Exception){}
        }
        // The resources by the name a player reads: the package's own caption for the game
        // language of the RMM language (German for de), else the caption of its [list] line,
        // else the id.
        public static string Names(Package package,string languageCode)
        {
            var result=new List<string>();LooseIni doc=null;string text;
            if(package.ContentFragments.TryGetValue("resources",out text))try{doc=new LooseIni(text);}catch(Exception){doc=null;}
            foreach(string id in package.VehicleResources)
            {
                string name="";
                if(doc!=null)
                {
                    try{if((languageCode??"").Equals("de",StringComparison.OrdinalIgnoreCase))name=(doc.Get("names:German",id)??"").Trim();}catch(Exception){}
                    if(name.Length==0)try{string line=doc.Get("list",id)??"";int comma=line.IndexOf(',');if(comma>=0)name=line.Substring(comma+1).Trim();}catch(Exception){}
                }
                // 0.8.1: a package that brings a material without declaring the resource itself
                // names it in the vehicles file.
                if(name.Length==0){string caption;if((languageCode??"").Equals("de",StringComparison.OrdinalIgnoreCase)&&package.VehicleCaptions.TryGetValue("German:"+id,out caption))name=caption;else if(package.VehicleCaptions.TryGetValue(id,out caption))name=caption;}
                result.Add(name.Length>0?name:id);
            }
            return String.Join(", ",result);
        }
        public static string FilePath(string build,string packageId){return SafeFiles.Child(Path.GetFullPath(build),"plugins\\"+Folder+"\\"+ContentTargets.FolderName(packageId)+".ini");}
        // 0.8.4: the vehicle part of a package is an ordinary entry in the player's own list of
        // Vehicle Materials (user_config\vehicle_materials.ini). ON adds it with the package's
        // numbers, OFF takes it out again - the same thing the trash button on the plugin's
        // page does - and the player may change the numbers there like those of any entry.
        // The file in vehicle_materials.d of 0.8.0 to 0.8.3 is gone; one that is left is turned
        // into such an entry with the next save.
        public static string OverlayPath(string build){return SafeFiles.Child(Path.GetFullPath(build),"user_config\\"+Plugin+".ini");}
        // 0.8.11: the fifth class, rocket parts (Vehicle Materials 0.4.13); a package's line may
        // leave it out.
        static readonly string[] Classes={"road","rail","ship","airplane","rocket"};
        static List<string> ListOf(LooseIni doc)
        {
            var result=new List<string>();
            foreach(var pair in doc.Entries("resources").Where(p=>Regex.IsMatch(p.Key.Trim(),"^resource[0-9]{1,4}$")).OrderBy(p=>Int32.Parse(p.Key.Trim().Substring(8)))){string v=(pair.Value??"").Trim();if(v.Length>0&&!result.Contains(v,StringComparer.OrdinalIgnoreCase))result.Add(v);}
            return result;
        }
        // The list as one block resource0..n-1 with its count. An empty list leaves no count
        // and no "enabled" of the player's behind: the plugin is then as its author ships it.
        static void WriteList(LooseIni doc,List<string> list)
        {
            foreach(var pair in doc.Entries("resources").ToList())if(Regex.IsMatch(pair.Key.Trim(),"^resource[0-9]{1,4}$"))doc.Remove("resources",pair.Key);
            if(list.Count==0){doc.Remove("resources","count");doc.Remove("general","enabled");return;}
            doc.Set("resources","count",list.Count.ToString(System.Globalization.CultureInfo.InvariantCulture));
            for(int i=0;i<list.Count;i++)doc.Set("resources","resource"+i.ToString(System.Globalization.CultureInfo.InvariantCulture),list[i]);
        }
        // True when the player's list has every one of these resources.
        public static bool Listed(string build,IEnumerable<string> resources)
        {
            var own=OwnList(build);var wanted=resources.ToList();
            return wanted.Count>0&&wanted.All(x=>own.Contains(x));
        }
        // The personal values with the materials of a package's vehicles file added. A resource
        // the player lists already keeps his numbers. Adding one switches the plugin on.
        public static string WithMaterials(string build,string overlay,string fragment){return WithMaterials(build,overlay,fragment,null);}
        // fill: materials the package entered itself and that stay listed. A class the entry has
        // no value for yet (a class that came later, as rocket parts did) gets the package's
        // value; a value that is there - the player's or an older one - stays.
        public static string WithMaterials(string build,string overlay,string fragment,IEnumerable<string> fill)
        {
            var names=new List<string>();string body=Normalize(fragment,names);
            var own=OwnList(build);var missing=names.Where(x=>!own.Contains(x)).ToList();
            var filling=(fill??Enumerable.Empty<string>()).Where(x=>own.Contains(x)&&names.Contains(x,StringComparer.OrdinalIgnoreCase)).ToList();
            if(missing.Count==0&&filling.Count==0)return overlay??"";
            var doc=new LooseIni(overlay??"");var list=ListOf(doc);
            if(list.Count==0)
            {
                // A list in an INI of the player's in plugins is carried on, not cut off.
                try{string local=SafeFiles.Child(Path.GetFullPath(build),"plugins\\"+Plugin+".ini");if(File.Exists(local))list=ListOf(new LooseIni(SafeFiles.Text(local)));}catch(Exception){list=new List<string>();}
            }
            var values=new LooseIni(body);
            foreach(string name in missing)
            {
                list.Add(name);string[] numbers=(values.Get("materials",name)??"").Split(',').Select(x=>x.Trim()).ToArray();
                for(int i=0;i<Classes.Length&&i<numbers.Length;i++)doc.Set(Classes[i],name,numbers[i]);
            }
            bool filled=false;
            foreach(string name in filling)
            {
                if(!ListOf(doc).Contains(name,StringComparer.OrdinalIgnoreCase))continue;
                string[] numbers=(values.Get("materials",name)??"").Split(',').Select(x=>x.Trim()).ToArray();
                for(int i=0;i<Classes.Length&&i<numbers.Length;i++)if(doc.Get(Classes[i],name)==null&&numbers[i].Length>0){doc.Set(Classes[i],name,numbers[i]);filled=true;}
            }
            if(missing.Count==0)return filled?doc.Render():(overlay??"");
            WriteList(doc,list);doc.Set("general","enabled","1");
            return doc.Render();
        }
        // The personal values without these resources: what the trash button does.
        public static string WithoutMaterials(string overlay,IEnumerable<string> resources)
        {
            var doc=new LooseIni(overlay??"");var list=ListOf(doc);var gone=resources.ToList();
            var kept=list.Where(x=>!gone.Contains(x,StringComparer.OrdinalIgnoreCase)).ToList();
            if(kept.Count==list.Count)return overlay??"";
            foreach(string name in gone)foreach(string c in Classes)doc.Remove(c,name);
            WriteList(doc,kept);
            return doc.Render();
        }
        // 0.8.2: what the files in vehicle_materials.d hold right now, for the page of Vehicle
        // Materials: one text per file, "<resource> <road> / <rail> / <ship> / <airplane> (<package>)".
        public static List<string> Installed(string build)
        {
            var result=new List<string>();
            try
            {
                string folder=SafeFiles.Child(Path.GetFullPath(build),"plugins\\"+Folder);
                if(!Directory.Exists(folder))return result;
                foreach(string path in Directory.GetFiles(folder,"*.ini").OrderBy(x=>x,StringComparer.OrdinalIgnoreCase))
                {
                    string text,body;var names=new List<string>();
                    try{text=SafeFiles.Text(path);body=Normalize(text,names);}catch(Exception){continue;}
                    Match m=Regex.Match(text,"content package (.+?):");
                    var lines=new LooseIni(body).Entries("materials").Select(p=>p.Key.Trim()+" "+(p.Value??"").Trim().Replace(", "," / "));
                    result.Add(String.Join("; ",lines)+" ("+(m.Success?m.Groups[1].Value.Trim():Path.GetFileNameWithoutExtension(path))+")");
                }
            }
            catch(Exception){}
            return result;
        }
        // 0.8.2: the resources the player lists himself for Vehicle Materials (personal values, or
        // an INI of his in plugins). Such a resource keeps his coefficients whatever a package says.
        public static HashSet<string> OwnList(string build)
        {
            var result=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            try
            {
                string root=Path.GetFullPath(build);
                foreach(string relative in new[]{"user_config\\"+Plugin+".ini","plugins\\"+Plugin+".ini"})
                {
                    string path=SafeFiles.Child(root,relative);if(!File.Exists(path))continue;
                    LooseIni doc;try{doc=new LooseIni(SafeFiles.Text(path));}catch(Exception){continue;}
                    foreach(var pair in doc.Entries("resources")){string key=pair.Key.Trim(),value=(pair.Value??"").Trim();if(Regex.IsMatch(key,"^resource[0-9]+$")&&value.Length>0)result.Add(value);}
                }
            }
            catch(Exception){}
            return result;
        }
        // 0.8.3: a package's rule set names every building it can serve, also those of a DLC or a
        // Workshop item this player does not have. Vanilla Buildings reports a target it cannot
        // read as an error, so only the targets that exist here are provided; a rule set without
        // any is left out. Without a game folder (tests) the text stays as it is.
        public static string PresentTargets(string build,string text)
        {
            try
            {
                if(GameBuildings.GameRoot(build)==null)return text;
                var doc=new LooseIni(text??"");bool changed=false;
                foreach(string section in doc.SectionNames().ToList())
                {
                    bool had=false,any=false;
                    foreach(string key in new[]{"target","target1","target2","target3"})
                    {
                        var all=doc.GetAll(section,key);if(all.Count==0)continue;had=true;
                        var kept=all.Where(t=>GameBuildings.TargetFile(build,null,t)!=null).ToList();
                        if(kept.Count>0)any=true;
                        if(kept.Count==all.Count)continue;
                        changed=true;if(kept.Count>0)doc.SetAll(section,key,kept);else doc.Remove(section,key);
                    }
                    if(had&&!any){doc.RemoveSection(section);changed=true;}
                }
                return changed?doc.Render():text;
            }
            catch(Exception){return text;}
        }
        // 0.8.3: true when the rule sets of a vehicle part would not reach the game - Vanilla
        // Buildings is missing, switched off in the loader, or switched off in its own INI.
        public static bool RulesBlocked(string build)
        {
            try
            {
                string root=Path.GetFullPath(build),plugin=ContentTargets.PluginsFor("rules")[0];
                if(!LoaderFiles.Present(root,plugin)||!LoaderFiles.Enabled(root,plugin))return true;
                string path=SafeFiles.Child(root,"plugins\\"+plugin+".ini");
                if(File.Exists(path)){string value=(new LooseIni(SafeFiles.Text(path)).Get("general","enabled")??"").Trim();if(value=="0")return true;}
            }
            catch(Exception){}
            return false;
        }
        // Why an installed file would not be consumed in the game: "" = nothing in the way, else
        // "missing" (no Vehicle Materials), "off" (switched off in the loader) or "disabled"
        // (the player's own enabled = 0). The plugin judges the same way: enabled counts as the
        // player's own when it stands in user_config or in an INI of his in plugins.
        public static string Blocker(string build)
        {
            try
            {
                string root=Path.GetFullPath(build);
                if(!LoaderFiles.Present(root,Plugin))return "missing";
                if(!LoaderFiles.Enabled(root,Plugin))return "off";
                foreach(string relative in new[]{"user_config\\"+Plugin+".ini","plugins\\"+Plugin+".ini"})
                {
                    string path=SafeFiles.Child(root,relative);if(!File.Exists(path))continue;
                    string value;try{value=(new LooseIni(SafeFiles.Text(path)).Get("general","enabled")??"").Trim();}catch(Exception){continue;}
                    if(value.Length==0)continue;
                    return value=="0"?"disabled":"";
                }
            }
            catch(Exception){}
            return "";
        }
        // 0.8.17: the player's list names materials, but "enabled" is nowhere his own, and the
        // plugin ships it as 0 - then it only tidies up old storages and asks for nothing. The
        // listed resources in that case, else an empty list. An explicit 0 is his choice.
        public static List<string> ListedButOff(string build)
        {
            var result=new List<string>();
            try
            {
                string root=Path.GetFullPath(build);
                if(!LoaderFiles.Present(root,Plugin)||!LoaderFiles.Enabled(root,Plugin))return result;
                foreach(string relative in new[]{"user_config\\"+Plugin+".ini","plugins\\"+Plugin+".ini"})
                {
                    string path=SafeFiles.Child(root,relative);if(!File.Exists(path))continue;
                    string value;try{value=(new LooseIni(SafeFiles.Text(path)).Get("general","enabled")??"").Trim();}catch(Exception){continue;}
                    if(value.Length>0)return result;
                }
                result.AddRange(OwnList(root).OrderBy(x=>x,StringComparer.OrdinalIgnoreCase));
            }
            catch(Exception){}
            return result;
        }
    }

    // Added: the ids this fragment actually contributed the last time its editor wrote (from the
    // receipt); only those are ever stripped from an effective INI that becomes a new base.
    public sealed class ContentFragment{public string PackageId="",PackageName="",Key="",Text="";public readonly List<string> Added=new List<string>();}
    // What one package contributed to one editor: the ids taken over and the ids that were
    // already there (an existing entry always wins, whether original or personal).
    public sealed class ContentContribution{public string PackageId="",PackageName="";public readonly List<string> Added=new List<string>(),Skipped=new List<string>();}

    public static class ContentLayer
    {
        // The provided fragments for one target plugin, read from the receipts.
        public static List<ContentFragment> Fragments(string build,string plugin)
        {
            var result=new List<ContentFragment>();string key=ContentTargets.KeyForPlugin(plugin);if(key.Length==0)return result;
            string root=ContentTargets.StateRoot(build);if(!Directory.Exists(root))return result;
            // The order of the resources is part of every save: packages always come in the same
            // order, by the name of their state folder.
            foreach(string dir in Directory.GetDirectories(root).OrderBy(x=>x,StringComparer.OrdinalIgnoreCase))
            {
                string receipt=Path.Combine(dir,"receipt.ini"),fragment=Path.Combine(dir,key+".ini");
                if(!File.Exists(receipt)||!File.Exists(fragment))continue;
                LooseIni r;try{r=new LooseIni(SafeFiles.Text(receipt));}catch(Exception){continue;}
                if((r.Get("content","provided")??"")!="1")continue;
                // The fragment belongs to whoever takes the kind TODAY, not to the plugin the
                // receipt named last time - otherwise a fork that appeared since never sees it.
                string target=ContentTargets.Resolve(build,key);
                if(target.Length==0)target=r.Get("content","target."+key)??"";
                if(target.Length>0&&!target.Equals(plugin,StringComparison.OrdinalIgnoreCase))continue;
                var f=new ContentFragment{PackageId=r.Get("content","id")??Path.GetFileName(dir),PackageName=r.Get("content","name")??Path.GetFileName(dir),Key=key,Text=SafeFiles.Text(fragment)};
                f.Added.AddRange((r.Get("added",key)??"").Split('|').Select(x=>x.Trim()).Where(x=>x.Length>0));
                result.Add(f);
            }
            return result;
        }
        // The ids every receipt still names for one target plugin, whether the package is
        // provided or not. Strip works with these: a package that is being switched off has
        // lost its fragment already, but its receipt keeps the ids until the editors have
        // written, and a hand-edited effective INI must not keep them as base entries.
        public static List<ContentFragment> Recorded(string build,string plugin)
        {
            var result=new List<ContentFragment>();string key=ContentTargets.KeyForPlugin(plugin);if(key.Length==0)return result;
            string root=ContentTargets.StateRoot(build);if(!Directory.Exists(root))return result;
            foreach(string dir in Directory.GetDirectories(root).OrderBy(x=>x,StringComparer.OrdinalIgnoreCase))
            {
                string receipt=Path.Combine(dir,"receipt.ini");
                if(!File.Exists(receipt))continue;
                LooseIni r;try{r=new LooseIni(SafeFiles.Text(receipt));}catch(Exception){continue;}
                string target=ContentTargets.Resolve(build,key);
                if(target.Length==0)target=r.Get("content","target."+key)??"";
                if(target.Length>0&&!target.Equals(plugin,StringComparison.OrdinalIgnoreCase))continue;
                var f=new ContentFragment{PackageId=r.Get("content","id")??Path.GetFileName(dir),PackageName=r.Get("content","name")??Path.GetFileName(dir),Key=key,Text=""};
                f.Added.AddRange((r.Get("added",key)??"").Split('|').Select(x=>x.Trim()).Where(x=>x.Length>0));
                // 0.8.9: a shared section came from RMM as well, whichever package supplies it now.
                foreach(string id in (r.Get("shared",key)??"").Split('|').Select(x=>x.Trim()).Where(x=>x.Length>0))if(!f.Added.Contains(id,StringComparer.OrdinalIgnoreCase))f.Added.Add(id);
                if(f.Added.Count>0)result.Add(f);
            }
            return result;
        }
        // 0.8.9: the provided packages that share one section of one kind, in the fixed order of
        // their state folders - the first of them supplies it. { package id, name, state folder }.
        public static List<string[]> SharedBy(string build,string key,string id)
        {
            var result=new List<string[]>();
            string root=ContentTargets.StateRoot(build);if(!Directory.Exists(root))return result;
            foreach(string dir in Directory.GetDirectories(root).OrderBy(x=>x,StringComparer.OrdinalIgnoreCase))
            {
                string receipt=Path.Combine(dir,"receipt.ini");if(!File.Exists(receipt))continue;
                LooseIni r;try{r=new LooseIni(SafeFiles.Text(receipt));}catch(Exception){continue;}
                if((r.Get("content","provided")??"")!="1")continue;
                if(!(r.Get("shared",key)??"").Split('|').Select(x=>x.Trim()).Contains(id,StringComparer.OrdinalIgnoreCase))continue;
                result.Add(new[]{r.Get("content","id")??Path.GetFileName(dir),r.Get("content","name")??Path.GetFileName(dir),dir});
            }
            return result;
        }
        // 0.8.9: one section of a fragment as comparable text - its keys and values in order.
        public static string SectionText(string fragment,string section)
        {
            try
            {
                var doc=new LooseIni(fragment??"");string found=doc.SectionNames().FirstOrDefault(x=>x.Trim().Equals(section,StringComparison.OrdinalIgnoreCase));
                if(found==null)return "";
                return String.Join("\n",doc.Entries(found).Select(x=>x.Key.Trim().ToLowerInvariant()+"="+(x.Value??"").Trim()));
            }
            catch(Exception){return "";}
        }
        static void CopySection(LooseIni from,LooseIni to,string section)
        {
            to.EnsureSection(section);
            foreach(string key in from.Entries(section).Select(x=>x.Key).Distinct(StringComparer.OrdinalIgnoreCase))to.SetAll(section,key,from.GetAll(section,key));
        }
        // baseText plus every fragment: sections or list entries that the base does not have yet
        // are appended; ids the base already knows are skipped and reported.
        public static string Merge(LocalEditorSpec spec,string baseText,List<ContentFragment> fragments,List<ContentContribution> contributions)
        {
            if(fragments==null||fragments.Count==0)return baseText;
            var doc=new LooseIni(baseText);
            foreach(ContentFragment fragment in fragments)
            {
                var c=new ContentContribution{PackageId=fragment.PackageId,PackageName=fragment.PackageName};
                LooseIni part;try{part=new LooseIni(fragment.Text);}catch(Exception){c.Skipped.Add("*");contributions.Add(c);continue;}
                if(spec.IsSections)
                {
                    foreach(string section in part.SectionNames())
                    {
                        string name=section.Trim();if(name.Length==0||spec.IsReserved(name))continue;
                        if(doc.SectionNames().Any(x=>x.Trim().Equals(name,StringComparison.OrdinalIgnoreCase))){c.Skipped.Add(name);continue;}
                        CopySection(part,doc,name);c.Added.Add(name);
                    }
                }
                else
                {
                    string list=spec.ListSection;
                    foreach(var pair in part.Entries(list))
                    {
                        if(doc.Get(list,pair.Key)!=null){c.Skipped.Add(pair.Key);continue;}
                        doc.Set(list,pair.Key,pair.Value);c.Added.Add(pair.Key);
                        if(spec.IsList)continue;
                        // Resources: the [custom:<id>] section and the per-resource keys of the
                        // other sections (prices) travel with the list entry.
                        foreach(string section in part.SectionNames())
                        {
                            string name=section.Trim();
                            if(name.Equals(list,StringComparison.OrdinalIgnoreCase))continue;
                            if(name.Equals(spec.ItemSectionPrefix+pair.Key,StringComparison.OrdinalIgnoreCase)){if(!doc.HasSection(name))CopySection(part,doc,name);continue;}
                            if(name.StartsWith(spec.ItemSectionPrefix,StringComparison.OrdinalIgnoreCase))continue;
                            string value=part.Get(name,pair.Key);
                            if(value!=null&&doc.Get(name,pair.Key)==null)doc.Set(name,pair.Key,value);
                        }
                    }
                }
                contributions.Add(c);
            }
            return doc.Render();
        }
        // The opposite of Merge, for an effective INI that is taken as a new base while fragments
        // are provided: the entries a fragment contributed came from RMM and must not become part
        // of the base. Only the ids recorded as added are taken out - an entry that was there before
        // the package (and made the package's own copy skip) is the user's and stays.
        public static string Strip(LocalEditorSpec spec,string text,List<ContentFragment> fragments)
        {
            if(fragments==null||fragments.Count==0)return text;
            var doc=new LooseIni(text);
            foreach(ContentFragment fragment in fragments)
            {
                if(spec.IsSections)
                {
                    foreach(string id in fragment.Added){if(spec.IsReserved(id))continue;foreach(string present in doc.SectionNames().Where(x=>x.Trim().Equals(id,StringComparison.OrdinalIgnoreCase)).ToList())doc.RemoveSection(present);}
                    continue;
                }
                string list=spec.ListSection;
                foreach(string id in fragment.Added)
                {
                    if(doc.Get(list,id)==null)continue;
                    doc.Remove(list,id);
                    if(spec.IsList)continue;
                    foreach(string section in doc.SectionNames().ToList())
                    {
                        string name=section.Trim();
                        if(name.Equals(spec.ItemSectionPrefix+id,StringComparison.OrdinalIgnoreCase))doc.RemoveSection(section);
                        else if(!name.Equals(list,StringComparison.OrdinalIgnoreCase)&&!name.StartsWith(spec.ItemSectionPrefix,StringComparison.OrdinalIgnoreCase)&&doc.Get(name,id)!=null)doc.Remove(name,id);
                    }
                }
            }
            return doc.Render();
        }
    }

    // One content package against one loader folder: its receipt, what it would provide, and the
    // switch. Commit writes fragments, assets and receipt in one transaction and then lets every
    // target editor write its effective INI again.
    public sealed class ContentSession
    {
        public readonly Package Package;public readonly string Build,StateDir,ReceiptPath;
        public bool Provided,PendingOn;public string ProvidedVersion="",ProvidedHash="",CurrentHash="";
        // fragment key -> plugin that takes it ("" = no such plugin has an INI in plugins\)
        public readonly Dictionary<string,string> Targets=new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase);
        // 0.5.12: what the receipt recorded as the target last time, per fragment key.
        public readonly Dictionary<string,string> RecordedTargets=new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase);
        public readonly List<string> CopiedAssets=new List<string>();
        public readonly Dictionary<string,List<string>> Skipped=new Dictionary<string,List<string>>(StringComparer.OrdinalIgnoreCase),Added=new Dictionary<string,List<string>>(StringComparer.OrdinalIgnoreCase);
        public readonly List<string> Notes=new List<string>();
        // 0.8.9: what the receipt names as shared, and the notes about shared buildings (info, not warnings).
        public readonly List<string> RecordedShared=new List<string>(),SharedNotes=new List<string>();
        // 0.8.0: "vehicles need this resource" - "" = the player never decided, "on", "off".
        public string Vehicles="",PendingVehicles="";
        // 0.8.10: the materials this package entered into the list of Vehicle Materials itself.
        // Only these leave it again; one the player had listed before stays his. A receipt of
        // 0.8.4 to 0.8.9 has no such key: then every listed material of the package counts as
        // the package's, as it did then.
        public readonly List<string> VehiclesAdded=new List<string>();public bool VehiclesAddedKnown;
        List<string> pendingVehiclesAdded=new List<string>();
        // 0.8.10: materials of the package the player lists himself - OFF here does not take them out.
        public List<string> VehiclesOwn{get{if(!HasVehicles)return new List<string>();var own=VehicleParts.OwnList(Build);return Package.VehicleResources.Where(x=>own.Contains(x)&&!AddedByPackage(x)).ToList();}}
        bool AddedByPackage(string resource){return VehiclesAddedKnown?VehiclesAdded.Contains(resource,StringComparer.OrdinalIgnoreCase):Provided;}
        public bool HasVehicles{get{return Package.VehicleFragment.Length>0;}}
        public string VehiclesPath{get{return VehicleParts.FilePath(Build,Package.Id);}}
        // What the player should know about the vehicle part of a provided package: "" = nothing,
        // "undecided", or with the switch on why nothing is consumed ("missing", "off", "disabled").
        // 0.8.3: "nostorage" - the material would be asked for, but Vanilla Buildings, which gives
        // the game's vehicle factories a storage for it, is not there or not switched on.
        public string VehicleTrouble
        {
            get
            {
                if(!HasVehicles||!Provided)return "";
                if(Vehicles.Length==0)return "undecided";
                if(Vehicles!="on")return "";
                string blocker=VehicleParts.Blocker(Build);
                if(blocker.Length>0||Package.VehicleRules.Length==0)return blocker;
                return VehicleParts.RulesBlocked(Build)?"nostorage":"";
            }
        }
        // 0.8.3: one kind of fragment as it is provided. Rule sets that belong to the vehicle
        // part count only while the vehicle part is on.
        public bool HasKind(string key){return Package.ContentFragments.ContainsKey(key)||(key=="rules"&&Package.VehicleRules.Length>0);}
        string FragmentFor(string key,string vehicles)
        {
            string text;Package.ContentFragments.TryGetValue(key,out text);text=text??"";
            if(key=="rules"&&vehicles=="on"&&Package.VehicleRules.Length>0)text=text.Trim().Length>0?text.TrimEnd()+"\r\n\r\n"+Package.VehicleRules:Package.VehicleRules;
            return text;
        }
        // What the page shows: the fragment with the choice that is staged right now.
        public string Fragment(string key){return FragmentFor(key,PendingVehicles);}
        // 0.6.11: ids of the generated building folders the last Commit took out of workshop_wip,
        // and of the ones it could not move.
        public readonly List<string> Parked=new List<string>(),ParkFailed=new List<string>();
        public string ParkDir{get{return Path.Combine(StateDir,"parked_buildings");}}
        readonly Dictionary<string,string> Before=new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase);
        public ContentSession(Package package,string build)
        {
            if(package==null||package.Kind!="content")throw new InvalidOperationException(Msg.Key("err_kein_inhaltspaket"));
            Package=package;Build=Path.GetFullPath(build);StateDir=ContentTargets.StateDir(Build,package.Id);ReceiptPath=Path.Combine(StateDir,"receipt.ini");
            CurrentHash=Hash(package);
            ReadReceipt();DeriveVehicles();PendingOn=Provided;PendingVehicles=Vehicles;
            foreach(string key in ContentTargets.Keys)
            {
                if(!HasKind(key))continue;
                // 0.5.3: under SML its own embedded component is the target - it writes
                // plugins\buildings.ini, so naming buildings_plus here would be wrong.
                string found=ContentTargets.Resolve(Build,key);
                Targets[key]=found;
                if(found.Length==0)Notes.Add(Msg.Key("content_missing_target",key,String.Join(", ",ContentTargets.PluginsFor(key))));
                // 0.5.12: the receipt names another plugin than the one that takes the kind today
                // (resources_plus appeared next to resources): the entries have to move at the next
                // save, and the page says so.
                string recorded;
                if(Provided&&found.Length>0&&RecordedTargets.TryGetValue(key,out recorded)&&recorded.Length>0&&!recorded.Equals(found,StringComparison.OrdinalIgnoreCase))
                    Notes.Add(Msg.Key("content_target_changed",key,recorded,found));
            }
            // 0.8.17: a kind the package provided last time and no longer brings at all (an update
            // that dropped its rule sets, say). Its editor still has to write once, so that the
            // entries the receipt names as added leave the effective INI; without a target here
            // nobody opened that page and the entries stayed for good.
            if(Provided)foreach(string key in ContentTargets.Keys)
            {
                List<string> had;if(HasKind(key)||Targets.ContainsKey(key)||!Added.TryGetValue(key,out had)||had.Count==0)continue;
                string found=ContentTargets.Resolve(Build,key),recorded;
                if(found.Length==0&&RecordedTargets.TryGetValue(key,out recorded))found=recorded;
                if(found.Length>0)Targets[key]=found;
            }
            // 0.7.6: said on the page, because the switch alone looks finished.
            if(Incomplete)Notes.Add(Msg.Key("content_incomplete"));
            NoteShared();
            Watch();
        }
        // 0.8.9: for every shared building, who supplies it now - another provided package that
        // comes first in the fixed order - and whether that package brings another version of it.
        void NoteShared()
        {
            SharedNotes.Clear();
            string own;Package.ContentFragments.TryGetValue("buildings",out own);
            foreach(string id in Package.SharedBuildings)
            {
                string[] first=ContentLayer.SharedBy(Build,"buildings",id).FirstOrDefault(x=>!x[0].Equals(Package.Id,StringComparison.OrdinalIgnoreCase)&&(!Provided||String.Compare(Path.GetFileName(x[2]),ContentTargets.FolderName(Package.Id),StringComparison.OrdinalIgnoreCase)<0));
                if(first==null)continue;
                SharedNotes.Add(Msg.Key("content_shared_from",id,first[1]));
                string theirs="";try{string path=Path.Combine(first[2],"buildings.ini");if(File.Exists(path))theirs=SafeFiles.Text(path);}catch(Exception){}
                if(theirs.Length>0&&ContentLayer.SectionText(theirs,id)!=ContentLayer.SectionText(own,id))SharedNotes.Add(Msg.Key("content_shared_differs",id,first[1]));
            }
        }
        void Watch()
        {
            Before.Clear();Before[ReceiptPath]=SafeFiles.HashFile(ReceiptPath);
            foreach(string key in ContentTargets.Keys){string path=Path.Combine(StateDir,key+".ini");Before[path]=SafeFiles.HashFile(path);}
            if(HasVehicles)Before[VehiclesPath]=SafeFiles.HashFile(VehiclesPath);
            string vfs=LocalEditorSpec.VfsRoot(Build);
            foreach(string relative in CopiedAssets.Concat(Package.AssetFiles).Distinct(StringComparer.OrdinalIgnoreCase)){string path=SafeFiles.Child(vfs,relative);Before[path]=SafeFiles.HashFile(path);}
        }
        // 0.8.4: the list of Vehicle Materials decides. A package whose resources stand there is
        // ON, whoever entered them; ON in the receipt with the entry gone means the player
        // deleted it on the plugin's page - that is OFF. A file of the earlier kind that is
        // still there counts as ON until the next save has turned it into an entry.
        public bool LegacyVehicleFile;
        void DeriveVehicles()
        {
            LegacyVehicleFile=false;if(!HasVehicles)return;
            try{LegacyVehicleFile=File.Exists(VehiclesPath);}catch(Exception){}
            bool listed=false;try{listed=VehicleParts.Listed(Build,Package.VehicleResources);}catch(Exception){}
            if(listed)Vehicles="on";
            else if(Vehicles=="on"&&!(Provided&&LegacyVehicleFile))Vehicles="off";
        }
        void ReadReceipt()
        {
            Provided=false;ProvidedVersion="";ProvidedHash="";Vehicles="";VehiclesAdded.Clear();VehiclesAddedKnown=false;CopiedAssets.Clear();Skipped.Clear();Added.Clear();RecordedTargets.Clear();RecordedShared.Clear();
            if(!File.Exists(ReceiptPath))return;
            LooseIni r;try{r=new LooseIni(SafeFiles.Text(ReceiptPath));}catch(Exception){return;}
            Provided=(r.Get("content","provided")??"")=="1";ProvidedVersion=r.Get("content","version")??"";ProvidedHash=r.Get("content","hash")??"";
            Vehicles=(r.Get("content","vehicles")??"").Trim().ToLowerInvariant();if(Vehicles!="on"&&Vehicles!="off")Vehicles="";
            {string added=r.Get("content","vehicles_added");VehiclesAddedKnown=added!=null;if(added!=null)VehiclesAdded.AddRange(added.Split('|').Select(x=>x.Trim()).Where(x=>x.Length>0));}
            foreach(string key in ContentTargets.Keys){string t=r.Get("content","target."+key)??"";if(t.Length>0)RecordedTargets[key]=t;}
            foreach(var pair in r.Entries("assets"))CopiedAssets.Add(pair.Value);
            foreach(var pair in r.Entries("skipped"))Skipped[pair.Key]=pair.Value.Split('|').Select(x=>x.Trim()).Where(x=>x.Length>0).ToList();
            foreach(var pair in r.Entries("added"))Added[pair.Key]=pair.Value.Split('|').Select(x=>x.Trim()).Where(x=>x.Length>0).ToList();
            RecordedShared.AddRange((r.Get("shared","buildings")??"").Split('|').Select(x=>x.Trim()).Where(x=>x.Length>0));
        }
        public static bool IsProvided(string build,string packageId)
        {
            try{string receipt=Path.Combine(ContentTargets.StateDir(build,packageId),"receipt.ini");return File.Exists(receipt)&&(new LooseIni(SafeFiles.Text(receipt)).Get("content","provided")??"")=="1";}
            catch(Exception){return false;}
        }
        // Fragment texts plus the assets' names, sizes and write times: enough to notice a
        // Workshop update without hashing every mesh at each start.
        public static string Hash(Package package)
        {
            var text=new StringBuilder();
            foreach(string key in ContentTargets.Keys){string fragment;if(package.ContentFragments.TryGetValue(key,out fragment))text.Append(key).Append('\n').Append(fragment).Append('\n');}
            if(package.VehicleFragment.Length>0)text.Append("vehicles").Append('\n').Append(package.VehicleFragment).Append('\n');
            if(package.VehicleRules.Length>0)text.Append("vehicle_rules").Append('\n').Append(package.VehicleRules).Append('\n');
            if(package.SharedBuildings.Count>0)text.Append("shared_buildings").Append('\n').Append(String.Join("|",package.SharedBuildings)).Append('\n');
            foreach(string relative in package.AssetFiles){var info=new FileInfo(Path.Combine(package.AssetsDir,relative));text.Append(relative.ToLowerInvariant()).Append('|').Append(info.Length).Append('|').Append(info.LastWriteTimeUtc.Ticks).Append('\n');}
            return SafeFiles.Hash(SafeFiles.Utf8.GetBytes(text.ToString()));
        }
        public bool UpdatePending{get{return Provided&&ProvidedHash!=CurrentHash;}}
        // 0.5.12: a fragment kind is taken by another plugin than the receipt names.
        public bool TargetChanged
        {
            get
            {
                if(!Provided)return false;
                foreach(var pair in Targets){string recorded;if(pair.Value.Length>0&&RecordedTargets.TryGetValue(pair.Key,out recorded)&&recorded.Length>0&&!recorded.Equals(pair.Value,StringComparison.OrdinalIgnoreCase))return true;}
                return false;
            }
        }
        // 0.7.6: a fragment with entries whose target plugin exists, but that no editor took in at
        // the last save - the page of that plugin could not be opened then. The package is not
        // complete, and the next save has to write it again.
        public bool Incomplete
        {
            get
            {
                if(!Provided)return false;
                foreach(var pair in Targets)
                {
                    if(pair.Value.Length==0)continue;
                    string fragment=FragmentFor(pair.Key,Vehicles);
                    if(pair.Key=="rules")fragment=VehicleParts.PresentTargets(Build,fragment);
                    if(ContentTargets.IdsOf(pair.Key,fragment).Count==0)continue;
                    if(!Added.ContainsKey(pair.Key)&&!Skipped.ContainsKey(pair.Key))return true;
                }
                return false;
            }
        }
        // 0.7.6: gives the INI a target plugin ships in its Workshop package ("" when it has
        // none): what the plugin reads as long as nothing has written plugins\<name>.ini. Set by
        // whoever commits; the deposit numbers of that INI are taken like any other.
        public Func<string,string> ShippedIni;
        string Shipped(string plugin){try{return ShippedIni==null?"":(ShippedIni(plugin)??"");}catch(Exception){return "";}}
        public bool NeedsWrite{get{return PendingOn!=Provided||(HasVehicles&&PendingVehicles!=Vehicles)||(HasVehicles&&Provided&&LegacyVehicleFile)||(PendingOn&&(UpdatePending||TargetChanged||Incomplete));}}
        public void AssertUnchanged(){foreach(var pair in Before)if(SafeFiles.HashFile(pair.Key)!=pair.Value)throw new IOException(Msg.Key("err_datei_inzwischen_geaendert_neu",pair.Key));}
        // Deposit types must be unique among everything the deposits plugin reads: the fragment
        // keeps its own numbers when they are free, a taken or missing one becomes the next free
        // number (10..127), and a number once assigned stays with its section across updates.
        internal string AssignDepositTypes(string text,string plugin){return AssignDepositTypes(text,plugin,null);}
        // repaired: the fragments of other packages that RepairDepositTypes renumbered, by package id.
        internal string AssignDepositTypes(string text,string plugin,Dictionary<string,string> repaired)
        {
            LooseIni doc;try{doc=new LooseIni(text);}catch(Exception){return text;}
            var taken=new HashSet<int>();var own=new HashSet<string>(doc.SectionNames().Select(x=>x.Trim()),StringComparer.OrdinalIgnoreCase);
            // 0.7.6: the deposits of the plugin's own package INI take numbers as well (copper, sand,
            // clay and gas of Deposits Plus have 10 to 13). Without them a package got 10, the page of
            // the plugin refused to open over the double number, and no deposit of any package arrived.
            foreach(int t in TypesOf(Shipped(plugin),own))taken.Add(t);
            string effective=SafeFiles.Child(Build,"plugins\\"+plugin+".ini");
            // The effective INI may already carry this package's sections from the last time; their
            // numbers are not "taken by somebody else".
            if(File.Exists(effective))try{var e=new LooseIni(SafeFiles.Text(effective));foreach(string s in e.SectionNames()){if(own.Contains(s.Trim()))continue;int t;if(Int32.TryParse((e.Get(s,"type")??"").Trim(),out t))taken.Add(t);}}catch(Exception){}
            foreach(ContentFragment other in ContentLayer.Fragments(Build,plugin))
            {
                if(other.PackageId.Equals(Package.Id,StringComparison.OrdinalIgnoreCase))continue;
                string otherText;if(repaired==null||!repaired.TryGetValue(other.PackageId,out otherText))otherText=other.Text;
                foreach(int t in TypesOf(otherText,null))taken.Add(t);
            }
            var previous=new Dictionary<string,int>(StringComparer.OrdinalIgnoreCase);
            string stored=Path.Combine(StateDir,"deposits.ini");
            if(File.Exists(stored))try{var p=new LooseIni(SafeFiles.Text(stored));foreach(string s in p.SectionNames()){int t;if(Int32.TryParse((p.Get(s,"type")??"").Trim(),out t))previous[s.Trim()]=t;}}catch(Exception){}
            // The number provided last time comes first, then the one the fragment asks for.
            foreach(string section in doc.SectionNames())
            {
                string name=section.Trim();if(name.Length==0||name.Equals("deposits",StringComparison.OrdinalIgnoreCase)||name.Equals("deposits_plus",StringComparison.OrdinalIgnoreCase))continue;
                int want;
                if(!previous.TryGetValue(name,out want)&&!Int32.TryParse((doc.Get(section,"type")??"").Trim(),out want))want=0;
                if(want<10||want>127||taken.Contains(want)){want=10;while(want<=127&&taken.Contains(want))want++;if(want>127)throw new FormatException(Msg.Key("err_keine_freie_vorkommensnummer",name));}
                taken.Add(want);doc.Set(section,"type",want.ToString());
                // The map channel is the plugin's business, never the package's.
                doc.Remove(section,"component");doc.Set(section,"map","auto");
            }
            return doc.Render();
        }
        // The deposit type numbers an INI text carries; sections named in `skip` are left out.
        static List<int> TypesOf(string text,HashSet<string> skip)
        {
            var found=new List<int>();if(String.IsNullOrEmpty(text))return found;
            try{var doc=new LooseIni(text);foreach(string s in doc.SectionNames()){if(skip!=null&&skip.Contains(s.Trim()))continue;int t;if(Int32.TryParse((doc.Get(s,"type")??"").Trim(),out t))found.Add(t);}}catch(Exception){}
            return found;
        }
        // 0.7.6: the staged deposit fragments of the OTHER provided packages, renumbered where a
        // number is taken by the plugin's own INI or twice among them - state files written before
        // 0.7.6 carry such numbers, and one of them is enough to keep the plugin's page shut. A
        // number that is free stays. Returns package id -> new fragment text for the ones that change.
        internal Dictionary<string,string> RepairDepositTypes(string plugin)
        {
            var changed=new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase);
            var taken=new HashSet<int>(TypesOf(Shipped(plugin),null));
            foreach(ContentFragment other in ContentLayer.Fragments(Build,plugin))
            {
                if(other.PackageId.Equals(Package.Id,StringComparison.OrdinalIgnoreCase))continue;
                LooseIni doc;try{doc=new LooseIni(other.Text);}catch(Exception){continue;}
                bool touched=false;
                foreach(string name in ContentTargets.IdsOf("deposits",other.Text))
                {
                    int have;bool valid=Int32.TryParse((doc.Get(name,"type")??"").Trim(),out have)&&have>=10&&have<=127&&!taken.Contains(have);
                    if(!valid){have=10;while(have<=127&&taken.Contains(have))have++;if(have>127)throw new FormatException(Msg.Key("err_keine_freie_vorkommensnummer",name));doc.Set(name,"type",have.ToString());touched=true;}
                    taken.Add(have);
                }
                if(touched)changed[other.PackageId]=doc.Render();
            }
            return changed;
        }
        // 0.4.87: ids of this package that a target plugin already knows from somewhere else -
        // the player's own entry, the plugin's shipped INI, another package. Those never lose:
        // an id that is in the way stops the whole package from being provided, so nothing is
        // written and no file lands in the vfs. The player removes his own entry and switches
        // the package on again. Ids this package provided itself (the receipt knows them) are
        // not conflicts, otherwise a package could never be saved twice.
        public Dictionary<string,List<string>> Conflicts(Func<string,LocalResourceSession> sessionFor)
        {
            var result=new Dictionary<string,List<string>>(StringComparer.OrdinalIgnoreCase);
            if(sessionFor==null)return result;
            foreach(var pair in ContentTargets.Keys.Where(HasKind).Select(k=>new KeyValuePair<string,string>(k,FragmentFor(k,PendingVehicles))))
            {
                string plugin;if(!Targets.TryGetValue(pair.Key,out plugin)||plugin.Length==0)continue;
                LocalResourceSession session;try{session=sessionFor(plugin);}catch(Exception){continue;}
                if(session==null)continue;
                List<string> mine;Added.TryGetValue(pair.Key,out mine);
                // 0.5.12: what the editor lists only because it merged THIS package's fragment from
                // the state folder is ours as well - the receipt may not know it yet when the target
                // has just moved to a fork.
                var ours=new HashSet<string>(mine??new List<string>(),StringComparer.OrdinalIgnoreCase);
                foreach(ContentContribution c in session.Content)if(c.PackageId.Equals(Package.Id,StringComparison.OrdinalIgnoreCase))foreach(string id in c.Added)ours.Add(id);
                var known=new HashSet<string>(session.Items().Select(x=>x.Id),StringComparer.OrdinalIgnoreCase);
                // 0.8.9: a building both packages share is no clash when another provided package
                // supplies it as shared as well.
                var clash=ContentTargets.IdsOf(pair.Key,pair.Value)
                    .Where(id=>known.Contains(id)&&!ours.Contains(id))
                    .Where(id=>!(pair.Key=="buildings"&&Package.SharedBuildings.Contains(id,StringComparer.OrdinalIgnoreCase)&&ContentLayer.SharedBy(Build,"buildings",id).Any(x=>!x[0].Equals(Package.Id,StringComparison.OrdinalIgnoreCase))))
                    .ToList();
                if(clash.Count>0)result[pair.Key]=clash;
            }
            return result;
        }
        // Writes the switch state. sessionFor(plugin) opens the keyed editor of a target plugin,
        // or returns null when it is not available; every editor that gets a session writes its
        // effective INI once more, with or without this package's entries.
        public string Commit(Action guard,Func<string,LocalResourceSession> sessionFor)
        {
            guard();AssertUnchanged();
            if(PendingOn)
            {
                foreach(var clash in Conflicts(sessionFor))
                {
                    string plugin;Targets.TryGetValue(clash.Key,out plugin);
                    throw new RuleException("content_conflict",Package.Name,String.Join(", ",clash.Value),plugin??"",clash.Key);
                }
            }
            bool on=PendingOn;var writes=new Dictionary<string,byte[]>(StringComparer.OrdinalIgnoreCase);
            // 0.6.11: the building sections this package had provided, as the receipt recorded them.
            var hadBuildings=new List<string>();{List<string> recorded;if(Provided&&Added.TryGetValue("buildings",out recorded))hadBuildings.AddRange(recorded);}
            // 0.8.9: a shared section may be supplied by this package now although its receipt says skipped.
            if(Provided)foreach(string id in RecordedShared)if(!hadBuildings.Contains(id,StringComparer.OrdinalIgnoreCase))hadBuildings.Add(id);
            string vfs=LocalEditorSpec.VfsRoot(Build);var assets=new List<string>();
            foreach(string old in CopiedAssets)writes[SafeFiles.Child(vfs,old)]=null;
            foreach(string key in ContentTargets.Keys)writes[Path.Combine(StateDir,key+".ini")]=null;
            // 0.8.4: the vehicle part is an entry in the player's list of Vehicle Materials, there
            // while the package is provided AND the player switched it on. It is read anew here:
            // the plugin's page may have saved since this session was opened. The file of the
            // earlier kind goes in any case.
            if(HasVehicles)
            {
                writes[VehiclesPath]=null;
                string overlayPath=VehicleParts.OverlayPath(Build),overlayBefore=File.Exists(overlayPath)?SafeFiles.Text(overlayPath):"";
                // 0.8.10: what this package entered itself; only that leaves the list again.
                var own=VehicleParts.OwnList(Build);
                pendingVehiclesAdded=Package.VehicleResources.Where(x=>own.Contains(x)&&AddedByPackage(x)).ToList();
                string overlayAfter;
                if(on&&PendingVehicles=="on")
                {
                    foreach(string name in Package.VehicleResources)if(!own.Contains(name)&&!pendingVehiclesAdded.Contains(name,StringComparer.OrdinalIgnoreCase))pendingVehiclesAdded.Add(name);
                    overlayAfter=VehicleParts.WithMaterials(Build,overlayBefore,Package.VehicleFragment,pendingVehiclesAdded.Where(x=>own.Contains(x)).ToList());
                }
                else{overlayAfter=VehicleParts.WithoutMaterials(overlayBefore,pendingVehiclesAdded);pendingVehiclesAdded=new List<string>();}
                if(overlayAfter!=overlayBefore)writes[overlayPath]=SafeFiles.Utf8.GetBytes(overlayAfter);
            }
            var skippedAssets=new List<string>();
            if(on)
            {
                foreach(var pair in ContentTargets.Keys.Where(HasKind).Select(k=>new KeyValuePair<string,string>(k,FragmentFor(k,PendingVehicles))))
                {
                    string text=pair.Value;string plugin;
                    // 0.8.3: only the targets this player has; a rule file left empty is no file.
                    if(pair.Key=="rules"){text=VehicleParts.PresentTargets(Build,text);if(ContentTargets.IdsOf("rules",text).Count==0)continue;}
                    if(pair.Key=="deposits"&&Targets.TryGetValue("deposits",out plugin)&&plugin.Length>0)
                    {
                        // The other packages' numbers are put right in the same transaction: the
                        // plugin's page reads every fragment and stays shut over one double number.
                        Dictionary<string,string> repaired=RepairDepositTypes(plugin);
                        foreach(var fix in repaired)writes[Path.Combine(ContentTargets.StateDir(Build,fix.Key),"deposits.ini")]=SafeFiles.Utf8.GetBytes(fix.Value);
                        text=AssignDepositTypes(text,plugin,repaired);
                    }
                    writes[Path.Combine(StateDir,pair.Key+".ini")]=SafeFiles.Utf8.GetBytes(text);
                }
                // A file that was already in the vfs before this package is somebody else's - it is
                // neither overwritten nor, later, removed. Only files this package created are ours.
                foreach(string relative in Package.AssetFiles)
                {
                    string target=SafeFiles.Child(vfs,relative);
                    bool ours=CopiedAssets.Contains(relative,StringComparer.OrdinalIgnoreCase);
                    if(!ours&&File.Exists(target)){skippedAssets.Add(relative);writes.Remove(target);continue;}
                    writes[target]=SafeFiles.Read(Path.Combine(Package.AssetsDir,relative),64*1024*1024);assets.Add(relative);
                }
            }
            // The receipt keeps the ids added last time until the editors have written again, so an
            // editor that has to take its effective INI as a new base strips exactly those.
            writes[ReceiptPath]=SafeFiles.Utf8.GetBytes(RenderReceipt(on,assets,Added,Skipped));
            foreach(string path in writes.Keys.ToList())if(!Before.ContainsKey(path))Before[path]=SafeFiles.HashFile(path);
            string backup=Transaction.Apply(Build,"content_"+ContentTargets.FolderName(Package.Id),writes,Before,guard);
            // The editors read the fragments from disk, so they are opened only now.
            Dictionary<string,List<string>> added=new Dictionary<string,List<string>>(StringComparer.OrdinalIgnoreCase),skipped=new Dictionary<string,List<string>>(StringComparer.OrdinalIgnoreCase);
            bool buildingsWritten=false;string buildingsIni="";
            foreach(string key in ContentTargets.Keys)
            {
                string plugin;if(!Targets.TryGetValue(key,out plugin)||plugin.Length==0)continue;
                LocalResourceSession session=sessionFor(plugin);if(session==null)continue;
                // The folders of this package's building sections are parked below, in its own state folder.
                session.ParkRemoved=false;
                try{session.Commit(guard);}finally{session.ParkRemoved=true;}
                if(key=="buildings"){buildingsWritten=true;buildingsIni=session.LocalIni;}
                if(on)foreach(ContentContribution c in session.Content)if(c.PackageId.Equals(Package.Id,StringComparison.OrdinalIgnoreCase)){if(c.Added.Count>0)added[key]=c.Added;if(c.Skipped.Count>0)skipped[key]=c.Skipped;}
            }
            if(on&&skippedAssets.Count>0)skipped["assets"]=skippedAssets;
            Directory.CreateDirectory(StateDir);File.WriteAllText(ReceiptPath,RenderReceipt(on,assets,added,skipped),SafeFiles.Utf8);
            // 0.6.11: the declarations are out of the buildings INI now, but the folders Buildings
            // Plus generated from them would stay in workshop_wip (prune is off by default) and the
            // game would go on loading them - with the package's resources gone it stops at start.
            // They are parked in the state folder of the package; a folder that cannot be moved is
            // named on the page.
            // 0.6.13: the same for an update that stays switched on - a section the package had
            // provided and no longer brings (dropped or renamed in its new version) has left the
            // buildings INI with this save. Only when the buildings editor really wrote, and never
            // a section that is now skipped because the player declares it himself.
            Parked.Clear();ParkFailed.Clear();
            var leaving=new List<string>();
            if(!on)leaving.AddRange(hadBuildings);
            else if(buildingsWritten)
            {
                List<string> now,own;added.TryGetValue("buildings",out now);skipped.TryGetValue("buildings",out own);
                leaving.AddRange(hadBuildings.Where(x=>(now==null||!now.Contains(x,StringComparer.OrdinalIgnoreCase))&&(own==null||!own.Contains(x,StringComparer.OrdinalIgnoreCase))));
            }
            // 0.8.9: a section another package still supplies (shared) stays where it is, and so
            // does its folder - the generated building keeps its number for the saved games.
            if(leaving.Count>0)
            {
                var still=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                try{if(buildingsWritten&&File.Exists(buildingsIni))foreach(string section in new LooseIni(SafeFiles.Text(buildingsIni)).SectionNames())still.Add(section.Trim());}catch(Exception){}
                if(!buildingsWritten)foreach(string id in leaving)if(ContentLayer.SharedBy(Build,"buildings",id).Any(x=>!x[0].Equals(Package.Id,StringComparison.OrdinalIgnoreCase)))still.Add(id);
                leaving.RemoveAll(x=>still.Contains(x));
            }
            if(leaving.Count>0)
            {
                Parked.AddRange(WipBuildings.Park(Build,leaving,ParkDir,ParkFailed));
                if(ParkFailed.Count>0)Notes.Add(Msg.Key("content_building_left",String.Join(", ",ParkFailed)));
            }
            ReadReceipt();DeriveVehicles();PendingOn=Provided;PendingVehicles=Vehicles;Watch();
            return backup;
        }
        string RenderReceipt(bool on,List<string> assets,Dictionary<string,List<string>> added,Dictionary<string,List<string>> skipped)
        {
            var text=new StringBuilder();
            text.Append("; Written by Republic Mod Manager: what this content package currently provides.\r\n[content]\r\nid = ").Append(Package.Id).Append("\r\nname = ").Append(Package.Name).Append("\r\nprovided = ").Append(on?"1":"0").Append("\r\nversion = ").Append(Package.Version).Append("\r\nhash = ").Append(CurrentHash).Append("\r\n");
            if(HasVehicles&&PendingVehicles.Length>0)text.Append("vehicles = ").Append(PendingVehicles).Append("\r\n");
            if(HasVehicles)text.Append("vehicles_added = ").Append(String.Join(" | ",pendingVehiclesAdded)).Append("\r\n");
            foreach(var pair in Targets)if(pair.Value.Length>0&&HasKind(pair.Key))text.Append("target.").Append(pair.Key).Append(" = ").Append(pair.Value).Append("\r\n");
            text.Append("\r\n[assets]\r\n");int n=0;if(on)foreach(string relative in assets)text.Append(n++).Append(" = ").Append(relative).Append("\r\n");
            // The ids are written for a package that is being switched off as well: its receipt
            // names them until the editors have written (see Commit and ContentLayer.Recorded).
            text.Append("\r\n[added]\r\n");foreach(var pair in added)if(pair.Value.Count>0)text.Append(pair.Key).Append(" = ").Append(String.Join(" | ",pair.Value)).Append("\r\n");
            text.Append("\r\n[skipped]\r\n");if(on)foreach(var pair in skipped)if(pair.Value.Count>0)text.Append(pair.Key).Append(" = ").Append(String.Join(" | ",pair.Value)).Append("\r\n");
            // 0.8.9: the buildings this package shares; other packages read it to step in.
            text.Append("\r\n[shared]\r\n");if(on&&Package.SharedBuildings.Count>0)text.Append("buildings = ").Append(String.Join(" | ",Package.SharedBuildings)).Append("\r\n");
            return text.ToString();
        }
    }

    // A keyed editor works on plugins\<config>, the file its plugin reads. When that file is
    // missing it is made once, from the first of:
    //   1. for a fork (Resources Plus, Needs Plus), the list of the plugin it replaces:
    //      plugins\<original>.ini, else the INI beside the original's DLL in the Workshop folder;
    //   2. the INI beside the plugin's own DLL - where a Workshop plugin ships its settings.
    //      MaxLegend's resources, deposits and needs look for their INI in plugins\ only, so for
    //      them this copy is what makes the subscribed plugin work at all;
    //   3. plugins\<plugin>.template.ini.
    // A fork does the first step itself at its first game start, for the files it can see; this
    // does the same when the page is opened before that, so the editor and the game agree on
    // the file. The source is only read; the copy gets a header that names it. The order of
    // [list] is kept, it is part of every save.
    public static class ForkSeed
    {
        public static string OriginalOf(string plugin)
        {
            switch((plugin??"").ToLowerInvariant())
            {
                case "resources_plus":return "resources";
                case "needs_plus":return "needs";
            }
            return "";
        }
        // 0.6.7: the plugin whose own entries a section editor takes over once. Deposits Plus keeps
        // its shipped INI as the original (a Steam update is the new base at once), so nothing is
        // copied; the deposits the player added to the original's deposits.ini become his entries.
        public static string SectionOriginalOf(string plugin)
        {
            return (plugin??"").Equals("deposits_plus",StringComparison.OrdinalIgnoreCase)?"deposits":"";
        }
        // That plugin's INI: in plugins\, else beside the DLL the loader takes from the Workshop.
        public static string OriginalIni(string build,string plugin)
        {
            string original=SectionOriginalOf(plugin);if(original.Length==0)return "";
            string root=Path.GetFullPath(build),local=SafeFiles.Child(root,"plugins\\"+original+".ini");
            return File.Exists(local)?local:BesideDll(root,original);
        }
        // The INI beside the DLL the loader takes for a key, "" when there is none or the DLL
        // lies in plugins\ itself (then that INI is the target, not a source).
        static string BesideDll(string root,string key)
        {
            FoundPlugin found=LoaderFiles.Find(root,key);
            return found!=null&&!found.Local&&File.Exists(found.Ini)?found.Ini:"";
        }
        // 0.8.17: true when the file says within its first 200 bytes that Soviet Mod Loader
        // generated it (the plugins test the same).
        public static bool SmlGenerated(string path)
        {
            try{byte[] head=SafeFiles.Read(path,4*1024*1024);string text=System.Text.Encoding.GetEncoding(28591).GetString(head,0,Math.Min(head.Length,400));int at=text.IndexOf("generated by Soviet Mod Loader",StringComparison.Ordinal);return at>=0&&at<200;}
            catch(Exception){return false;}
        }
        // 0.8.17: <state>\base\<original>.ini of Soviet Mod Loader - [loader] state_dir of
        // plugins\soviet_mod_loader.ini, by default the folder soviet_mod_loader in the loader folder.
        public static string SmlBase(string root,string original)
        {
            string dir="soviet_mod_loader";
            try{string ini=SafeFiles.Child(root,"plugins\\soviet_mod_loader.ini");if(File.Exists(ini)){string v=(new LooseIni(SafeFiles.Text(ini)).Get("loader","state_dir")??"").Trim();if(v.Length>0)dir=v;}}catch(Exception){}
            try{return Path.Combine(Path.IsPathRooted(dir)?dir:Path.Combine(root,dir),"base",original+".ini");}catch(Exception){return "";}
        }
        // Creates plugins\<configName> when it is missing; returns the source it was made from,
        // "" when the file exists already or nothing could seed it.
        public static string Ensure(string build,string plugin,string configName)
        {
            if(String.IsNullOrWhiteSpace(plugin)||String.IsNullOrWhiteSpace(configName))return "";
            string root=Path.GetFullPath(build),target=SafeFiles.Child(root,"plugins\\"+configName);
            if(File.Exists(target))return "";
            string original=OriginalOf(plugin),source="";
            if(original.Length>0)
            {
                // 0.8.17: the same order as the plugin's ResolveIniPath. A plugins\<original>.ini that
                // Soviet Mod Loader generated already holds every package's entries, which RMM
                // provides again on its own - SML's baseline (<state>\base\<original>.ini) is the
                // player's own list then; without one the generated file is passed over.
                string local=SafeFiles.Child(root,"plugins\\"+original+".ini"),smlBase=SmlBase(root,original);
                if(File.Exists(local)&&!SmlGenerated(local))source=local;
                else if(smlBase.Length>0&&File.Exists(smlBase))source=smlBase;
                else source=BesideDll(root,original);
            }
            if(source.Length==0)source=BesideDll(root,plugin);
            string template=SafeFiles.Child(root,"plugins\\"+plugin+".template.ini");
            if(source.Length==0&&File.Exists(template))source=template;
            if(source.Length==0)return "";
            byte[] body=SafeFiles.Read(source,4*1024*1024);
            string header="; "+configName+" - created by Republic Mod Manager on "+DateTime.Now.ToString("yyyy-MM-dd HH:mm")+"\r\n; from "+source+". The source was not changed. From now on this file is the one\r\n; that counts; the plugin reads it and Republic Mod Manager edits it.\r\n\r\n";
            byte[] head=SafeFiles.Utf8.GetBytes(header);
            var all=new byte[head.Length+body.Length];Buffer.BlockCopy(head,0,all,0,head.Length);Buffer.BlockCopy(body,0,all,head.Length,body.Length);
            Directory.CreateDirectory(Path.GetDirectoryName(target));
            string tmp=target+".tmp";File.WriteAllBytes(tmp,all);
            if(File.Exists(target))File.Delete(target);
            File.Move(tmp,target);
            return source;
        }
    }
}
