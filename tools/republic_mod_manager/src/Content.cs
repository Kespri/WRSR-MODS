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
        public static readonly string[] Keys={"resources","deposits","needs","buildings"};
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
            if(key=="deposits"||key=="buildings")
            {
                foreach(string section in doc.SectionNames()){string s=section.Trim();if(s.Length==0||s.Equals("deposits",StringComparison.OrdinalIgnoreCase)||s.Equals("deposits_plus",StringComparison.OrdinalIgnoreCase)||s.Equals("buildings",StringComparison.OrdinalIgnoreCase)||s.Equals("buildings_plus",StringComparison.OrdinalIgnoreCase)||s.Equals("list",StringComparison.OrdinalIgnoreCase))continue;result.Add(s);}
                return result;
            }
            foreach(var pair in doc.Entries("list"))result.Add(pair.Key);
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
                if(f.Added.Count>0)result.Add(f);
            }
            return result;
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
            ReadReceipt();PendingOn=Provided;
            foreach(string key in ContentTargets.Keys)
            {
                if(!package.ContentFragments.ContainsKey(key))continue;
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
            Watch();
        }
        void Watch()
        {
            Before.Clear();Before[ReceiptPath]=SafeFiles.HashFile(ReceiptPath);
            foreach(string key in ContentTargets.Keys){string path=Path.Combine(StateDir,key+".ini");Before[path]=SafeFiles.HashFile(path);}
            string vfs=LocalEditorSpec.VfsRoot(Build);
            foreach(string relative in CopiedAssets.Concat(Package.AssetFiles).Distinct(StringComparer.OrdinalIgnoreCase)){string path=SafeFiles.Child(vfs,relative);Before[path]=SafeFiles.HashFile(path);}
        }
        void ReadReceipt()
        {
            Provided=false;ProvidedVersion="";ProvidedHash="";CopiedAssets.Clear();Skipped.Clear();Added.Clear();RecordedTargets.Clear();
            if(!File.Exists(ReceiptPath))return;
            LooseIni r;try{r=new LooseIni(SafeFiles.Text(ReceiptPath));}catch(Exception){return;}
            Provided=(r.Get("content","provided")??"")=="1";ProvidedVersion=r.Get("content","version")??"";ProvidedHash=r.Get("content","hash")??"";
            foreach(string key in ContentTargets.Keys){string t=r.Get("content","target."+key)??"";if(t.Length>0)RecordedTargets[key]=t;}
            foreach(var pair in r.Entries("assets"))CopiedAssets.Add(pair.Value);
            foreach(var pair in r.Entries("skipped"))Skipped[pair.Key]=pair.Value.Split('|').Select(x=>x.Trim()).Where(x=>x.Length>0).ToList();
            foreach(var pair in r.Entries("added"))Added[pair.Key]=pair.Value.Split('|').Select(x=>x.Trim()).Where(x=>x.Length>0).ToList();
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
        public bool NeedsWrite{get{return PendingOn!=Provided||(PendingOn&&(UpdatePending||TargetChanged));}}
        public void AssertUnchanged(){foreach(var pair in Before)if(SafeFiles.HashFile(pair.Key)!=pair.Value)throw new IOException(Msg.Key("err_datei_inzwischen_geaendert_neu",pair.Key));}
        // Deposit types must be unique among everything the deposits plugin reads: the fragment
        // keeps its own numbers when they are free, a taken or missing one becomes the next free
        // number (10..127), and a number once assigned stays with its section across updates.
        internal string AssignDepositTypes(string text,string plugin)
        {
            LooseIni doc;try{doc=new LooseIni(text);}catch(Exception){return text;}
            var taken=new HashSet<int>();var own=new HashSet<string>(doc.SectionNames().Select(x=>x.Trim()),StringComparer.OrdinalIgnoreCase);
            string effective=SafeFiles.Child(Build,"plugins\\"+plugin+".ini");
            // The effective INI may already carry this package's sections from the last time; their
            // numbers are not "taken by somebody else".
            if(File.Exists(effective))try{var e=new LooseIni(SafeFiles.Text(effective));foreach(string s in e.SectionNames()){if(own.Contains(s.Trim()))continue;int t;if(Int32.TryParse((e.Get(s,"type")??"").Trim(),out t))taken.Add(t);}}catch(Exception){}
            foreach(ContentFragment other in ContentLayer.Fragments(Build,plugin))
            {
                if(other.PackageId.Equals(Package.Id,StringComparison.OrdinalIgnoreCase))continue;
                try{var o=new LooseIni(other.Text);foreach(string s in o.SectionNames()){int t;if(Int32.TryParse((o.Get(s,"type")??"").Trim(),out t))taken.Add(t);}}catch(Exception){}
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
            foreach(var pair in Package.ContentFragments)
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
                var clash=ContentTargets.IdsOf(pair.Key,pair.Value)
                    .Where(id=>known.Contains(id)&&!ours.Contains(id))
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
            string vfs=LocalEditorSpec.VfsRoot(Build);var assets=new List<string>();
            foreach(string old in CopiedAssets)writes[SafeFiles.Child(vfs,old)]=null;
            foreach(string key in ContentTargets.Keys)writes[Path.Combine(StateDir,key+".ini")]=null;
            var skippedAssets=new List<string>();
            if(on)
            {
                foreach(var pair in Package.ContentFragments)
                {
                    string text=pair.Value;string plugin;
                    if(pair.Key=="deposits"&&Targets.TryGetValue("deposits",out plugin)&&plugin.Length>0)text=AssignDepositTypes(text,plugin);
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
            bool buildingsWritten=false;
            foreach(string key in ContentTargets.Keys)
            {
                string plugin;if(!Targets.TryGetValue(key,out plugin)||plugin.Length==0)continue;
                LocalResourceSession session=sessionFor(plugin);if(session==null)continue;
                // The folders of this package's building sections are parked below, in its own state folder.
                session.ParkRemoved=false;
                try{session.Commit(guard);}finally{session.ParkRemoved=true;}
                if(key=="buildings")buildingsWritten=true;
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
            if(leaving.Count>0)
            {
                Parked.AddRange(WipBuildings.Park(Build,leaving,ParkDir,ParkFailed));
                if(ParkFailed.Count>0)Notes.Add(Msg.Key("content_building_left",String.Join(", ",ParkFailed)));
            }
            ReadReceipt();PendingOn=Provided;Watch();
            return backup;
        }
        string RenderReceipt(bool on,List<string> assets,Dictionary<string,List<string>> added,Dictionary<string,List<string>> skipped)
        {
            var text=new StringBuilder();
            text.Append("; Written by Republic Mod Manager: what this content package currently provides.\r\n[content]\r\nid = ").Append(Package.Id).Append("\r\nname = ").Append(Package.Name).Append("\r\nprovided = ").Append(on?"1":"0").Append("\r\nversion = ").Append(Package.Version).Append("\r\nhash = ").Append(CurrentHash).Append("\r\n");
            foreach(var pair in Targets)if(pair.Value.Length>0)text.Append("target.").Append(pair.Key).Append(" = ").Append(pair.Value).Append("\r\n");
            text.Append("\r\n[assets]\r\n");int n=0;if(on)foreach(string relative in assets)text.Append(n++).Append(" = ").Append(relative).Append("\r\n");
            // The ids are written for a package that is being switched off as well: its receipt
            // names them until the editors have written (see Commit and ContentLayer.Recorded).
            text.Append("\r\n[added]\r\n");foreach(var pair in added)if(pair.Value.Count>0)text.Append(pair.Key).Append(" = ").Append(String.Join(" | ",pair.Value)).Append("\r\n");
            text.Append("\r\n[skipped]\r\n");if(on)foreach(var pair in skipped)if(pair.Value.Count>0)text.Append(pair.Key).Append(" = ").Append(String.Join(" | ",pair.Value)).Append("\r\n");
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
                string local=SafeFiles.Child(root,"plugins\\"+original+".ini");
                source=File.Exists(local)?local:BesideDll(root,original);
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
