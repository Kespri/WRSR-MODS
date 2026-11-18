using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace TesmioAutoload
{
    public sealed class ResourceCleanupPlan
    {
        public readonly List<string> Plugins=new List<string>();
        public readonly Dictionary<string,byte[]> Writes=new Dictionary<string,byte[]>(StringComparer.OrdinalIgnoreCase);
    }

    // Cross-plugin checks use only collection metadata declared by launcher schemas.
    // Unknown INI keys are never guessed to be resource references.
    public static class ResourceConsistency
    {
        static IEnumerable<Package> Packages(IEnumerable<CatalogEntry> entries,string build)
        {
            var seen=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach(var entry in entries.Where(x=>x.Supported&&!x.LocalEditor&&x.Problem.Length==0))
            {
                Package package;
                try{package=RuntimeStatus.Load(entry,build);}catch{continue;}
                // A content package brings no DLL and no INI of its own, so it has neither a target nor a
                // config name; building plugins\<name> from an empty name would be an invalid path.
                if(package.Kind=="content")continue;
                if(seen.Add(package.Id))yield return package;
            }
        }
        static IEnumerable<CollectionSpec> ResourceLists(Package package)
        { return package.Collections.Where(x=>x.SourcePlugin.Equals("resources",StringComparison.OrdinalIgnoreCase)&&x.SourceSection.Equals("list",StringComparison.OrdinalIgnoreCase)); }
        // What the plugin reads: plugins\<name>.ini for a DLL in the loader folder, and for a
        // Workshop plugin its own INI with the personal values of user_config laid over it. null
        // when there is nothing to read.
        static Ini Effective(Package package,string root)
        {
            if(package.Installed)
            {
                string local=SafeFiles.Child(root,"plugins\\"+package.ConfigName);
                return File.Exists(local)?new Ini(SafeFiles.Text(local)):null;
            }
            if(!package.HasConfig)return null;
            string user=SafeFiles.Child(root,"user_config\\"+package.ConfigName);
            var personal=File.Exists(user)?new Ini(SafeFiles.Text(user)).Values:new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase);
            return new Ini(package.Defaults.Render(personal));
        }
        // The ids the resources plugin publishes: the list of whichever plugin takes resources
        // today (the fork before the original), read from its file in plugins\.
        public static HashSet<string> ResourceIds(string build)
        {
            string root=Path.GetFullPath(build),plugin=ContentTargets.Resolve(root,"resources");
            if(plugin.Length==0)plugin="resources";
            string path=SafeFiles.Child(root,"plugins\\"+plugin+".ini");
            if(!File.Exists(path))throw new IOException(Msg.Key("err_plugins_resources_ini_fehlt"));
            var result=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach(var pair in new LooseIni(SafeFiles.Text(path)).Entries("list"))
                if(!CollectionRules.SafeItem(pair.Key)||!result.Add(pair.Key))throw new FormatException(Msg.Key("err_ungueltige_oder_doppelte_ressource", pair.Key));
            return result;
        }
        // Whether the list editor of this plugin holds the resource list the game will read: only
        // the plugin that takes resources today (the fork before the original). The unsaved list
        // of that editor may replace the file on disk in ValidateReferences; no other editor's may.
        public static bool SpeaksForResources(string build,string editorPlugin)
        {
            string provider=ContentTargets.Resolve(Path.GetFullPath(build),"resources");
            if(provider.Length==0)provider="resources";
            return provider.Equals(editorPlugin??"",StringComparison.OrdinalIgnoreCase);
        }
        public static List<string> ValidateReferences(string build,IEnumerable<CatalogEntry> entries,ISet<string> resourceIds=null)
        {
            var issues=new List<string>();string root=Path.GetFullPath(build);
            HashSet<string> ids=resourceIds==null?null:new HashSet<string>(resourceIds,StringComparer.OrdinalIgnoreCase);
            foreach(Package package in Packages(entries,root))
            {
                if(!ResourceLists(package).Any()||!File.Exists(package.DllPath)||!LoaderFiles.Enabled(root,package.Target))continue;
                try
                {
                    Ini config=Effective(package,root);if(config==null)continue;
                    foreach(CollectionSpec collection in ResourceLists(package))
                    {
                        var names=CollectionRules.Names(collection,config);if(names.Count==0)continue;
                        // The list is only read when a plugin really names a resource.
                        if(ids==null)ids=ResourceIds(root);
                        foreach(string id in names.Where(x=>!ids.Contains(x)))
                            issues.Add(Msg.Key("consistency_resource_missing",package.Name,id,collection.Id));
                    }
                }
                catch(Exception e){issues.Add(Msg.Key("consistency_check_failed",package.Name,e.Message));}
            }
            return issues;
        }
        public static ResourceCleanupPlan PlanRemoval(string build,IEnumerable<CatalogEntry> entries,string resourceId)
        {
            var plan=new ResourceCleanupPlan();string root=Path.GetFullPath(build);
            foreach(Package package in Packages(entries,root))
            {
                Ini current;
                try{current=Effective(package,root);}catch{continue;}
                if(current==null)continue;
                bool changed=false;
                foreach(CollectionSpec collection in ResourceLists(package))
                {
                    if(!CollectionRules.Names(collection,current).Any(x=>x.Equals(resourceId,StringComparison.OrdinalIgnoreCase)))continue;
                    if(!CollectionRules.CanRemove(package,collection,resourceId))throw new IOException(Msg.Key("err_enthaelt_als_nicht_loeschbaren", package.Name, resourceId));
                    current=CollectionRules.Remove(package,collection,current,resourceId);changed=true;
                }
                if(!changed)continue;
                plan.Plugins.Add(package.Name);
                var overrides=current.Values.Where(x=>!package.IsDefault(x.Key,x.Value)).ToDictionary(x=>x.Key,x=>x.Value,StringComparer.OrdinalIgnoreCase);
                string userIni=SafeFiles.Child(root,"user_config\\"+package.ConfigName);
                string personal=File.Exists(userIni)?new Ini(SafeFiles.Text(userIni)).RewriteValues(overrides):Ini.Sparse(overrides);
                plan.Writes[userIni]=SafeFiles.Utf8.GetBytes(personal);
                // A plugin in the loader folder reads plugins\<name>.ini: that file and its
                // receipt follow, so the next session does not take the change for a foreign one.
                if(package.Installed)
                {
                    string localIni=SafeFiles.Child(root,"plugins\\"+package.ConfigName);
                    plan.Writes[localIni]=SafeFiles.Utf8.GetBytes(current.RewriteValues(current.Values));
                    string receipt=SafeFiles.Child(root,"user_config\\.autoload\\"+package.Target+".receipt.ini");
                    plan.Writes[receipt]=SafeFiles.Utf8.GetBytes("[state]\r\nid = "+package.Id+"\r\nversion = "+package.Version+"\r\nmode = installed\r\ndll_hash = "+SafeFiles.Hash(package.Dll)+"\r\nini_hash = "+SafeFiles.Hash(plan.Writes[localIni])+"\r\ndefaults_hash = "+package.DefaultsHash+"\r\n");
                }
            }
            return plan;
        }
    }
}
