// UI discovery and preferences only. No DLL loading, config deployment or game changes.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace TesmioAutoload
{
    public sealed class CatalogEntry
    {
        public string Root, Id = "", Name, Version = "", Problem = "";
        public bool Supported, LocalEditor;
        // Where the entry comes from, for the list icon: "steam" (a Workshop folder of
        // a Steam library), "loader" (the loader's own plugins folder), else "unknown".
        public string Origin = "unknown";
        // "plugin" (a DLL the loader finds) or "content" (fragments and files without a DLL).
        public string Kind = "plugin";
        // Installed: the DLL lies in <loader>\plugins and has no package (Root is the DLL path).
        public bool Installed;
        // Saved once and changed in the Workshop since (see UpdateCheck).
        public bool Updated; public string PreviousVersion = "";
        public string Target = "";
        // The plugin's key - the DLL's file name without ".dll", the name of its switch in
        // tesmioloader.ini - and the DLL the loader takes for it. Empty for a content package.
        // A keyed editor carries the key of the plugin it edits here as well.
        public string Key = "", Dll = "";
        // The key of the plugin this one takes the place of ("" = none).
        public string Replaces = "";
        // Other DLLs with the same key, which the loader passes over.
        public readonly List<string> PassedOver = new List<string>();
        public readonly List<string> Hints = new List<string>();
        public readonly List<Dependency> Dependencies = new List<Dependency>();
        public string Status { get { return Problem.Length > 0 ? "Hinweis" : !Supported ? "Nicht unterstützt" : Kind == "content" ? "Inhalt" : Installed ? "Installiert" : "Verfügbar"; } }
        public override string ToString() { return Name + (Problem.Length > 0 ? " [Hinweis]" : !Supported ? " [nicht unterstützt]" : Kind == "content" ? " [Inhalt]" : Installed ? " [installiert]" : ""); }
    }

    public static class Catalog
    {
        // Folder with local launcher schemas (settings_schemas beside the exe): the
        // second schema tier for plugins without a package schema, and home of the keyed editors.
        public static string SchemaRoot = "";
        // Marks every Workshop package that was saved once and has changed
        // since; returns the names of those, for the startup summary.
        public static List<string> MarkUpdates(IEnumerable<CatalogEntry> entries, string build)
        {
            var result = new List<string>();
            if (String.IsNullOrWhiteSpace(build)) return result;
            foreach (var e in entries.Where(x => x.Supported && !x.Installed && !x.LocalEditor && (x.Kind == "plugin" || x.Kind == "content") && x.Problem.Length == 0))
            {
                e.Updated = false; e.PreviousVersion = "";
                try
                {
                    if (e.Kind == "content")
                    {
                        // A provided content package whose fragments or assets changed in the Workshop.
                        var content = new ContentSession(Package.Load(e.Root), build);
                        e.Updated = content.UpdatePending || content.Incomplete; e.PreviousVersion = content.ProvidedVersion;
                        if (e.Updated) result.Add(e.Name + (content.ProvidedVersion.Length > 0 && content.ProvidedVersion != e.Version ? " (" + content.ProvidedVersion + " -> " + e.Version + ")" : ""));
                        continue;
                    }
                    UpdateState u = UpdateCheck.Of(Package.Load(e.Root), build);
                    e.Updated = u.Pending; e.PreviousVersion = u.PreviousVersion;
                    if (u.Pending) result.Add(e.Name + (u.PreviousVersion.Length > 0 && u.PreviousVersion != e.Version ? " (" + u.PreviousVersion + " -> " + e.Version + ")" : ""));
                }
                catch (Exception) { }
            }
            return result;
        }
        // Plugins whose key exists more than once: the loader takes one DLL - a local one before
        // any Workshop one, else the smallest path - and passes the others over. One line per
        // key, naming the file that runs and the ones that do not.
        public static List<string> DuplicateDlls(IEnumerable<CatalogEntry> entries, string build)
        {
            var result = new List<string>();
            foreach (var e in entries.Where(x => x.PassedOver.Count > 0))
                result.Add(e.Name + ": " + e.Dll + " <- " + String.Join(", ", e.PassedOver));
            return result;
        }
        // settings_schemas\replacements.ini: [replaces] <key> = <key of the plugin it replaces>,
        // for replacement plugins that are found without a manifest. A manifest's own
        // [mod] replaces wins.
        public static Dictionary<string, string> Replacements(string schemaRoot)
        {
            var table = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            try
            {
                string path = Path.Combine(schemaRoot ?? "", "replacements.ini");
                if (!File.Exists(path)) return table;
                foreach (var pair in new Ini(SafeFiles.Text(path)).Values)
                    if (pair.Key.StartsWith("replaces/", StringComparison.OrdinalIgnoreCase) && pair.Value.Trim().Length > 0) table[pair.Key.Substring(9)] = pair.Value.Trim();
            }
            catch (Exception) { }
            return table;
        }
        // The entry that answers for a plugin key: the plugin itself or its keyed editor.
        public static CatalogEntry ByKey(IEnumerable<CatalogEntry> entries, string key)
        { return String.IsNullOrEmpty(key) ? null : entries.FirstOrDefault(e => e.Supported && e.Kind != "content" && e.Key.Equals(key, StringComparison.OrdinalIgnoreCase)); }
        // Pairs (replacement, original) where both DLLs are found and both are switched on. The
        // brake inside a replacement idles it while its original loads, so left alone such a pair
        // runs the original; the replacement is what the player installed on purpose.
        public static List<KeyValuePair<CatalogEntry, CatalogEntry>> OpenReplacements(IEnumerable<CatalogEntry> entries, string build)
        {
            var result = new List<KeyValuePair<CatalogEntry, CatalogEntry>>();
            var list = entries.Where(e => e.Supported && e.Kind != "content" && e.Key.Length > 0).ToList();
            if (String.IsNullOrWhiteSpace(build) || list.Count == 0) return result;
            var switches = LoaderFiles.Switches(build, list.Select(e => e.Key));
            foreach (var replacement in list.Where(e => e.Replaces.Length > 0 && switches[e.Key]))
            {
                CatalogEntry original = ByKey(list, replacement.Replaces);
                if (original != null && original != replacement && switches[original.Key]) result.Add(new KeyValuePair<CatalogEntry, CatalogEntry>(replacement, original));
            }
            return result;
        }
        // Switches the original of every open pair off in tesmioloader.ini; returns the pairs.
        public static List<KeyValuePair<CatalogEntry, CatalogEntry>> SettleReplacements(IEnumerable<CatalogEntry> entries, string build, Action guard)
        {
            var open = OpenReplacements(entries, build);
            if (open.Count == 0) return open;
            var switches = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
            foreach (var pair in open) switches[pair.Value.Key] = false;
            string path = LoaderFiles.IniPath(build);
            var before = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { { path, SafeFiles.HashFile(path) } };
            var writes = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase) { { path, LoaderFiles.Render(build, switches) } };
            Transaction.Apply(build, "replacements", writes, before, guard);
            return open;
        }
        // Matches every [dependencies] line of a package against the catalog and checks the
        // version constraint. A line names a mod id or a plugin key; an id whose last part is a
        // plugin key matches that plugin (tesmio.localization -> localization.dll), and a plugin
        // that replaces the named one stands in for it. Among several candidates one that is
        // switched on wins. Pure bookkeeping: Startup.Unmet decides what it means for a start.
        public static void ResolveDependencies(List<Dependency> dependencies, IEnumerable<CatalogEntry> entries, string build = null)
        {
            var list = entries.Where(e => e.Supported && e.Id.Length > 0).ToList();
            Dictionary<string, bool> switches = null;
            if (!String.IsNullOrWhiteSpace(build)) { try { switches = LoaderFiles.Switches(build, list.Where(e => e.Key.Length > 0).Select(e => e.Key)); } catch (Exception) { switches = null; } }
            Func<CatalogEntry, bool> on = e => e.Kind == "content" || switches == null || e.Key.Length == 0 || switches[e.Key];
            foreach (Dependency d in dependencies)
            {
                string tail = d.Id.Substring(d.Id.LastIndexOf('.') + 1);
                var candidates = list.Where(e => e.Id.Equals(d.Id, StringComparison.OrdinalIgnoreCase)).ToList();
                bool byId = candidates.Count > 0;
                foreach (string key in new[] { d.Id, tail }.Distinct(StringComparer.OrdinalIgnoreCase))
                {
                    candidates.AddRange(list.Where(e => e.Kind != "content" && e.Key.Equals(key, StringComparison.OrdinalIgnoreCase) && !candidates.Contains(e)));
                    candidates.AddRange(list.Where(e => e.Kind != "content" && e.Replaces.Equals(key, StringComparison.OrdinalIgnoreCase) && !candidates.Contains(e)));
                }
                CatalogEntry match = candidates.FirstOrDefault(on) ?? candidates.FirstOrDefault();
                d.Found = match != null; d.Target = ""; d.Kind = ""; d.Version = ""; d.VersionOk = true; d.Root = ""; d.Name = "";
                if (match == null) { d.Note = Msg.Key("dep_missing", d.Id + (d.Constraint.Length > 0 ? " (" + d.Constraint + ")" : "")); continue; }
                d.Target = match.Key; d.Kind = match.Kind; d.Version = match.Version; d.Root = match.Root ?? ""; d.Name = String.IsNullOrEmpty(match.Name) ? d.Id : match.Name;
                // A version rule belongs to the package the id names. A plugin found by its key
                // only - no manifest, or a replacement - has no version to hold against it.
                bool versioned = byId && match.Id.Equals(d.Id, StringComparison.OrdinalIgnoreCase);
                d.VersionOk = !versioned || VersionRule.Satisfied(d.Constraint, match.Version);
                d.Note = !d.VersionOk ? Msg.Key("dep_wrong_version", d.Name, match.Version, d.Constraint)
                    : !versioned ? Msg.Key("dep_found_installed", d.Id, match.Key)
                    : Msg.Key(match.Kind == "content" ? "dep_found_content" : "dep_found", d.Name, match.Version);
            }
        }
        // The dependency notes of every entry, resolved against the complete list.
        public static void ResolveAll(List<CatalogEntry> entries, string build = null)
        {
            foreach (var entry in entries.Where(e => e.Dependencies.Count > 0))
            {
                ResolveDependencies(entry.Dependencies, entries, build);
                entry.Hints.RemoveAll(h => h.StartsWith("\u0001dep_", StringComparison.Ordinal));
                foreach (Dependency d in entry.Dependencies.Where(x => !x.Found || !x.VersionOk)) entry.Hints.Add(d.Note);
            }
        }
        public static string NormalizeRoot(string path)
        {
            if (String.IsNullOrWhiteSpace(path)) return "";
            string full = Path.GetFullPath(path);
            if (full.Length > Path.GetPathRoot(full).Length) full = full.TrimEnd('\\', '/');
            // A single package path becomes its parent.
            if (Package.FindManifest(full) != null) return Path.GetDirectoryName(full);
            return full;
        }
        public static string WorkshopForBuild(string build)
        {
            if (String.IsNullOrWhiteSpace(build)) return "";
            return PluginScan.WorkshopDefault(Path.GetFullPath(build));
        }
        public static string InitialRoot(string build, string legacyPackage)
        {
            string steam = WorkshopForBuild(build);
            if (!String.IsNullOrWhiteSpace(legacyPackage))
            {
                string legacy = Path.GetFullPath(legacyPackage).TrimEnd('\\', '/');
                // Prefer the subscribed copy when this formerly private WIP item
                // is now present in Steam. Otherwise keep the WIP collection usable.
                if (steam.Length > 0 && Package.FindManifest(Path.Combine(steam, Path.GetFileName(legacy))) != null) return steam;
                return NormalizeRoot(legacy);
            }
            return steam;
        }
        static List<string> steamWorkshopRoots;
        static bool UnderSteamWorkshop(string path)
        {
            try
            {
                if (steamWorkshopRoots == null) steamWorkshopRoots = Discovery.SteamLibraries().Select(l => Path.Combine(l, "steamapps\\workshop") + "\\").ToList();
                string full = Path.GetFullPath(path);
                return steamWorkshopRoots.Any(r => full.StartsWith(r, StringComparison.OrdinalIgnoreCase));
            }
            catch (Exception) { return false; }
        }
        // A package that is rejected still shows under its own name: [mod] of its manifest is
        // read on its own, as far as it can be read at all.
        static void Peek(CatalogEntry entry, string folder)
        {
            try
            {
                string manifest = Package.FindManifest(folder); if (manifest == null) return;
                var mod = Manifest.Parse(SafeFiles.Text(manifest));
                string id = mod.Get("mod", "id", ""), name = mod.Get("mod", "name", ""), version = mod.Get("mod", "version", "");
                if (id.Length > 0 && id.Length <= 160) entry.Id = id;
                if (name.Length > 0 && name.Length <= 200 && !name.Any(Char.IsControl)) entry.Name = name;
                if (version.Length > 0 && version.Length <= 80) entry.Version = version;
            }
            catch (Exception) { }
        }
        static void Fill(CatalogEntry entry, Package package)
        {
            entry.Supported = true; entry.Id = package.Id; entry.Name = package.Name; entry.Version = package.Version; entry.Kind = package.Kind; entry.Target = package.Target;
            entry.Key = package.Target; entry.Dll = package.DllPath ?? ""; entry.Replaces = package.Replaces;
            entry.Hints.AddRange(package.Hints); entry.Dependencies.AddRange(package.Dependencies);
        }
        // Everything the list shows, from what the loader will find on its next start:
        //   - plugin DLLs in <loader>\plugins and, in any depth, below the Workshop folder, one
        //     entry per key (the DLL the loader takes; the ones it passes over are named on it);
        //   - keyed editors from settings_schemas, in place of the plain entry of their plugin
        //     and only when that plugin's DLL is found;
        //   - content packages: Workshop folders with a manifest and no plugin DLL.
        public static List<CatalogEntry> ScanAll(string build, string workshop, string schemaRoot, List<string> diagnostics)
        {
            var entries = new List<CatalogEntry>();
            string loader = String.IsNullOrWhiteSpace(build) ? "" : Path.GetFullPath(build);
            string root = String.IsNullOrWhiteSpace(workshop) ? "" : NormalizeRoot(workshop);
            if (root.Length > 0 && !Directory.Exists(root)) { diagnostics.Add("Workshop-Ordner nicht vorhanden: " + root); root = ""; }
            List<FoundPlugin> found;
            try { found = PluginScan.Discover(loader.Length > 0 ? Path.Combine(loader, "plugins") : "", root); }
            catch (Exception e) { diagnostics.Add("Plugins konnten nicht gesucht werden: " + e.Message); found = new List<FoundPlugin>(); }
            if (loader.Length > 0) LoaderFiles.Remember(loader, root, found);
            var replaces = Replacements(schemaRoot);
            Dictionary<string, string> versions = loader.Length > 0 ? InstalledPlugins.LoaderVersions(loader) : new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            // Keyed editors first: they stand for their plugin.
            var editors = ScanLocalEditors(schemaRoot, diagnostics);
            var covered = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var editor in editors)
            {
                string plugin; try { plugin = LocalEditorSpec.Load(editor.Root).Plugin; } catch (Exception) { continue; }
                FoundPlugin dll = found.FirstOrDefault(f => PluginScan.SameKey(f.Key, plugin));
                if (dll == null) continue;
                covered.Add(plugin); editor.Key = dll.Key; editor.Dll = dll.Path; editor.Origin = dll.Local ? "loader" : UnderSteamWorkshop(dll.Path) ? "steam" : "unknown";
                editor.PassedOver.AddRange(dll.Shadowed.Select(s => s.Path));
                string original; if (replaces.TryGetValue(dll.Key, out original)) editor.Replaces = original;
                entries.Add(editor);
            }
            // Package folders that hold more than one plugin DLL are named by DLL path.
            var rootOf = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var f in found.Where(x => !x.Local)) { try { rootOf[f.Path] = Package.RootOf(f.Path); } catch (Exception) { rootOf[f.Path] = Path.GetDirectoryName(f.Path); } }
            var crowded = new HashSet<string>(rootOf.Values.GroupBy(x => x, StringComparer.OrdinalIgnoreCase).Where(g => g.Count() > 1).Select(g => g.Key), StringComparer.OrdinalIgnoreCase);
            foreach (var f in found)
            {
                if (covered.Contains(f.Key)) continue;
                CatalogEntry entry;
                if (f.Local)
                {
                    entry = new CatalogEntry { Root = f.Path, Name = f.Key, Target = f.Key, Key = f.Key, Dll = f.Path, Installed = true, Origin = "loader" };
                    try { Fill(entry, InstalledPlugins.Load(loader, f.Key, schemaRoot)); }
                    catch (Exception e) { entry.Problem = Msg.Key("catalog_installed_rejected", Msg.Of(e)); entry.Supported = false; }
                }
                else
                {
                    string packageRoot = rootOf[f.Path];
                    entry = new CatalogEntry { Root = crowded.Contains(packageRoot) ? f.Path : packageRoot, Name = f.Key, Target = f.Key, Key = f.Key, Dll = f.Path, Origin = UnderSteamWorkshop(f.Path) ? "steam" : "unknown" };
                    try
                    {
                        SafeFiles.NoLinks(packageRoot);
                        Package package = Package.Load(entry.Root); Fill(entry, package);
                        // A package without a version of its own shows what the loader reported.
                        string seen; if (package.ManifestPath.Length == 0 && versions.TryGetValue(f.Key, out seen)) entry.Version = seen;
                    }
                    catch (Exception e) { Peek(entry, packageRoot); entry.Problem = Msg.Key("catalog_manifest_rejected", Msg.Of(e)); entry.Supported = false; }
                }
                if (f.PathTooLong) { entry.Problem = Msg.Key("catalog_path_too_long", f.Path); }
                entry.PassedOver.AddRange(f.Shadowed.Select(s => s.Path));
                if (entry.Replaces.Length == 0) { string original; if (replaces.TryGetValue(f.Key, out original)) entry.Replaces = original; }
                entries.Add(entry);
            }
            // Content packages: a manifest and no plugin DLL of their own.
            if (root.Length > 0)
            {
                try
                {
                    SafeFiles.NoLinks(root);
                    var withDll = new HashSet<string>(rootOf.Values, StringComparer.OrdinalIgnoreCase);
                    foreach (var f in found.Where(x => !x.Local)) foreach (var s in f.Shadowed) { try { withDll.Add(Package.RootOf(s.Path)); } catch (Exception) { } }
                    foreach (string child in Directory.GetDirectories(root).OrderBy(s => s, StringComparer.OrdinalIgnoreCase))
                    {
                        if (Package.FindManifest(child) == null || withDll.Contains(child)) continue;
                        var entry = new CatalogEntry { Root = child, Name = Path.GetFileName(child), Origin = UnderSteamWorkshop(child) ? "steam" : "unknown" };
                        try { SafeFiles.NoLinks(child); Fill(entry, Package.Load(child)); }
                        catch (Exception e) { Peek(entry, child); entry.Problem = Msg.Key("catalog_manifest_rejected", Msg.Of(e)); entry.Supported = false; }
                        entries.Add(entry);
                    }
                }
                catch (Exception e) { diagnostics.Add("Workshop konnte nicht vollständig gelesen werden: " + e.Message); }
            }
            foreach (var duplicates in entries.Where(e => e.Id.Length > 0).GroupBy(e => e.Id, StringComparer.OrdinalIgnoreCase).Where(g => g.Count() > 1))
                foreach (var entry in duplicates) entry.Problem = Msg.Key("catalog_duplicate_id", entry.Id);
            foreach (var entry in entries.Where(e => e.PassedOver.Count > 0)) entry.Hints.Add(Msg.Key("hint_same_key", entry.Dll, String.Join(", ", entry.PassedOver)));
            foreach (var entry in entries.Where(e => e.Replaces.Length > 0 && ByKey(entries, e.Replaces) != null)) entry.Hints.Add(Msg.Key("hint_replaces", ByKey(entries, entry.Replaces).Name));
            ResolveAll(entries, loader.Length > 0 ? loader : null);
            foreach (var entry in entries.Where(e => e.Problem.Length > 0)) diagnostics.Add(entry.Root + ": " + entry.Problem);
            return entries.OrderBy(e => e.Name, StringComparer.CurrentCultureIgnoreCase).ThenBy(e => e.Root, StringComparer.OrdinalIgnoreCase).ToList();
        }
        public static List<CatalogEntry> ScanLocalEditors(string schemaRoot,List<string> diagnostics)
        {
            var entries=new List<CatalogEntry>();
            try
            {
                if(String.IsNullOrWhiteSpace(schemaRoot)||!Directory.Exists(schemaRoot))return entries;SafeFiles.NoLinks(schemaRoot);
                foreach(string path in Directory.GetFiles(schemaRoot,"*.launcher.ini").OrderBy(x=>x,StringComparer.OrdinalIgnoreCase))
                {
                    // Plain launcher schemas in this folder are the second schema tier for
                    // plugins without a package schema; only keyed editors are entries here.
                    try{if(new Ini(SafeFiles.Text(path)).Get("launcher","editor_type","").Length==0)continue;}catch(Exception){continue;}
                    try{LocalEditorSpec spec=LocalEditorSpec.Load(path);entries.Add(new CatalogEntry{Root=path,Id=spec.Id,Name=spec.Name,Version=spec.Version,Supported=true,LocalEditor=true,Origin="loader",Key=spec.Plugin});}
                    catch(Exception e){diagnostics.Add(path+": Lokaler Editor abgewiesen: "+e.Message);}
                }
            }
            catch(Exception e){diagnostics.Add("Lokale Editor-Schemas konnten nicht gelesen werden: "+e.Message);}
            return entries;
        }
        public static CatalogEntry RestoreSelection(List<CatalogEntry> entries, string id, string source)
        {
            var exact = entries.FirstOrDefault(e => String.Equals(e.Root, source, StringComparison.OrdinalIgnoreCase));
            if (exact != null) return exact;
            var sameId = entries.Where(e => id.Length > 0 && e.Id.Equals(id, StringComparison.OrdinalIgnoreCase)).ToList();
            if (sameId.Count == 1) return sameId[0];
            return entries.FirstOrDefault(e => e.Supported && e.Problem.Length == 0) ?? entries.FirstOrDefault();
        }
    }

    public sealed class UiState
    {
        public string Build = "", WorkshopRoot = "", SelectedId = "", SelectedSource = "", SelectedTab = "", Language = "auto";
        // 0.4.93: what the options window set. Empty means "not set here" - then the rmm.ini value counts.
        public string LauncherWindow = "", WatchSeconds = "", VersionCheck = "", KeepOpen = "";
        // Last window size in physical pixels (0 = not saved yet) and whether it was maximized.
        public int WindowWidth, WindowHeight; public bool WindowMaximized;
        public UiState Copy() { return new UiState { Build = Build, WorkshopRoot = WorkshopRoot, SelectedId = SelectedId, SelectedSource = SelectedSource, SelectedTab = SelectedTab, Language = Language, WindowWidth = WindowWidth, WindowHeight = WindowHeight, WindowMaximized = WindowMaximized, LauncherWindow = LauncherWindow, WatchSeconds = WatchSeconds, VersionCheck = VersionCheck, KeepOpen = KeepOpen }; }
    }
    public sealed class UiStateStore
    {
        public readonly string PathName;
        string expected;
        bool readable = true;
        public UiStateStore(string path) { PathName = Path.GetFullPath(path); }
        public UiState Load(UiState defaults, List<string> notes)
        {
            var state = defaults.Copy();
            try
            {
                SafeFiles.NoLinks(PathName); expected = SafeFiles.HashFile(PathName);
                if (expected == "absent") return state;
                var ini = new Ini(SafeFiles.Text(PathName));
                if (ini.Get("ui", "format") != "1") throw new FormatException(Msg.Key("err_unbekanntes_ui_speicherformat"));
                state.Build = ini.Get("paths", "loader_build", state.Build); state.WorkshopRoot = ini.Get("paths", "workshop_root", state.WorkshopRoot);
                state.SelectedId = ini.Get("selection", "id", ""); state.SelectedSource = ini.Get("selection", "source", "");
                state.SelectedTab = ini.Get("selection", "tab", "");
                state.Language = ini.Get("ui", "language", "auto");
                state.LauncherWindow = ini.Get("ui", "tesmiolauncher_window", ""); state.WatchSeconds = ini.Get("ui", "launch_watch_seconds", ""); state.VersionCheck = ini.Get("ui", "version_check", ""); state.KeepOpen = ini.Get("ui", "keep_open", "");
                int w, h; Int32.TryParse(ini.Get("ui", "window_width", "0"), out w); Int32.TryParse(ini.Get("ui", "window_height", "0"), out h);
                state.WindowWidth = w > 0 ? w : 0; state.WindowHeight = h > 0 ? h : 0; state.WindowMaximized = ini.Get("ui", "window_maximized", "0") == "1";
            }
            catch (Exception e) { readable = false; notes.Add(Msg.Key("view_restore_failed", Msg.Of(e))); }
            return state;
        }
        public void Save(UiState state)
        {
            if (!readable) throw new IOException(Msg.Key("err_besch_digter_ansichtsspeicher_wird", PathName));
            var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { { "ui/format", "1" } };
            values.Add("ui/language", state.Language);
            if (state.LauncherWindow.Length > 0) values.Add("ui/tesmiolauncher_window", state.LauncherWindow);
            if (state.WatchSeconds.Length > 0) values.Add("ui/launch_watch_seconds", state.WatchSeconds);
            if (state.VersionCheck.Length > 0) values.Add("ui/version_check", state.VersionCheck);
            if (state.KeepOpen.Length > 0) values.Add("ui/keep_open", state.KeepOpen);
            if (state.WindowWidth > 0 && state.WindowHeight > 0) { values.Add("ui/window_width", state.WindowWidth.ToString(System.Globalization.CultureInfo.InvariantCulture)); values.Add("ui/window_height", state.WindowHeight.ToString(System.Globalization.CultureInfo.InvariantCulture)); values.Add("ui/window_maximized", state.WindowMaximized ? "1" : "0"); }
            if (state.Build.Length > 0) values.Add("paths/loader_build", state.Build);
            if (state.WorkshopRoot.Length > 0) values.Add("paths/workshop_root", state.WorkshopRoot);
            if (state.SelectedId.Length > 0) values.Add("selection/id", state.SelectedId);
            if (state.SelectedSource.Length > 0) values.Add("selection/source", state.SelectedSource);
            if (state.SelectedTab.Length > 0) values.Add("selection/tab", state.SelectedTab);
            foreach (string value in values.Values) if (value.Any(Char.IsControl)) throw new FormatException(Msg.Key("err_ung_ltiger_wert_im"));
            SafeFiles.NoLinks(PathName);
            if (SafeFiles.HashFile(PathName) != expected) throw new IOException(Msg.Key("err_ansicht_wurde_in_einem"));
            byte[] bytes = SafeFiles.Utf8.GetBytes(Ini.Sparse(values)); string hash = SafeFiles.Hash(bytes);
            if (hash == expected) return;
            Directory.CreateDirectory(Path.GetDirectoryName(PathName));
            string temp = PathName + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                using (var file = new FileStream(temp, FileMode.CreateNew, FileAccess.Write, FileShare.None)) { file.Write(bytes, 0, bytes.Length); file.Flush(true); }
                SafeFiles.NoLinks(PathName);
                if (SafeFiles.HashFile(PathName) != expected) throw new IOException(Msg.Key("err_ansicht_w_hrend_des"));
                if (File.Exists(PathName)) File.Replace(temp, PathName, null); else File.Move(temp, PathName);
                expected = hash;
            }
            finally { if (File.Exists(temp)) File.Delete(temp); } // own uniquely-created staging file only
        }
        static string Identity(string executableDirectory)
        { return SafeFiles.Hash(SafeFiles.Utf8.GetBytes(System.IO.Path.GetFullPath(executableDirectory).TrimEnd('\\').ToUpperInvariant())).Substring(0, 20); }
        public static string DefaultPath(string executableDirectory)
        {
            return System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "RepublicModManager", "profiles", Identity(executableDirectory) + ".ini");
        }
        // Before 0.17 the state lived under %LOCALAPPDATA%\TesmioAutoload. Copied once.
        public static void Migrate(string executableDirectory)
        {
            try
            {
                string fresh = DefaultPath(executableDirectory);
                string old = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "TesmioAutoload", "profiles", Identity(executableDirectory) + ".ini");
                if (File.Exists(fresh) || !File.Exists(old)) return;
                Directory.CreateDirectory(System.IO.Path.GetDirectoryName(fresh)); File.Copy(old, fresh);
            }
            catch (Exception) { }
        }
    }
}
