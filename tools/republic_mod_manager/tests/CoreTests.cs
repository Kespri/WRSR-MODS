using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using TesmioAutoload;

static class CoreTests
{
    static int passed,failed;
    static void Test(string name,Action action){try{action();passed++;Console.WriteLine("PASS "+name);}catch(Exception e){failed++;Console.WriteLine("FAIL "+name+": "+e);}}
    static void Check(bool value){if(!value)throw new Exception("Assertion failed");}
    static void Check(bool value,string what){if(!value)throw new Exception("Assertion failed: "+what);}
    static void Reject(Action action){bool rejected=false;try{action();}catch(Exception){rejected=true;}Check(rejected);}
    static void Write(string path,string text){Directory.CreateDirectory(Path.GetDirectoryName(path));File.WriteAllText(path,text,SafeFiles.Utf8);}
    static string MakeBuild(string root,string name){string path=Path.Combine(root,name);Directory.CreateDirectory(Path.Combine(path,"plugins"));return path;}
    static void CopyTree(string source,string target){foreach(string file in Directory.GetFiles(source,"*",SearchOption.AllDirectories)){string destination=Path.Combine(target,file.Substring(source.Length+1));Directory.CreateDirectory(Path.GetDirectoryName(destination));File.Copy(file,destination,true);}}
    static void NoGame(){}
    // What the list shows for a Workshop folder alone, and the plugins in a loader folder alone.
    static List<CatalogEntry> ScanWorkshop(string workshop,List<string> notes){LoaderFiles.Forget();return Catalog.ScanAll("",workshop,"",notes);}
    static List<CatalogEntry> ScanInstalled(string build,IEnumerable<CatalogEntry> known,List<string> notes){LoaderFiles.Forget();return Catalog.ScanAll(build,"",Catalog.SchemaRoot,notes).Where(e=>e.Installed).ToList();}

    static string MakeSimplePackage(string root,Package dllSource)
    {
        string package=Path.Combine(root,"simple-package");Directory.CreateDirectory(Path.Combine(package,"hooks"));Directory.CreateDirectory(Path.Combine(package,"config"));
        File.WriteAllBytes(Path.Combine(package,"hooks","sample_plugin.dll"),dllSource.Dll);
        Write(Path.Combine(package,"hooks","sample_plugin.ini"),"[general]\nenabled = 1\nlimit = 5\nmode = normal\n");
        Write(Path.Combine(package,"config","sample.launcher.ini"),
            "[launcher]\nlayout_version=1\nvisible=1\nid=example.sample\nconfig=sample_plugin.ini\nenabled_field=general/enabled\nicon=builtin:gear\n"+
            "[field:enabled]\nsection=general\nkey=enabled\nlabel=Enabled\ntype=boolean\ngroup=main\n"+
            "[field:limit]\nsection=general\nkey=limit\nlabel=Limit\ntype=integer\nminimum=0\nmaximum=20\ngroup=main\n"+
            "[field:mode]\nsection=general\nkey=mode\nlabel=Mode\ntype=choice\nchoices=normal|strict\ngroup=main\n"+
            "[tab:settings]\nlabel=Settings\n[group:main]\ntab=settings\nlabel=Main\n");
        Write(Path.Combine(package,"soviet.mod.ini"),
            "[mod]\nid=example.sample\nname=Sample Plugin\nversion=2.0-beta\nenabled=1\ntesmio_api_min=4\ntesmio_api_max=4\n"+
            "[hooks]\ndll=hooks\\sample_plugin.dll\n[configuration]\ndefaults=hooks\\sample_plugin.ini\nlauncher_schema=config\\sample.launcher.ini\nuser_config=sample_plugin.ini\n"+
            "[autoload]\nformat=1\nkind=plugin\ntarget=sample_plugin\n");
        return package;
    }

    static int Main(string[] args)
    {
        try{return Run(args);}catch(Exception e){Console.Error.WriteLine("TEST SETUP FAILED: "+e);return 1;}
    }
    static int Run(string[] args)
    {
        if(args.Length==3&&args[2]=="--inspect-only")
        {var package=Package.Load(args[0]);Catalog.SchemaRoot=Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"..","settings_schemas"));var inspectKnown=ScanWorkshop(Catalog.NormalizeRoot(args[0]),new List<string>());inspectKnown.AddRange(ScanInstalled(args[1],inspectKnown,new List<string>()));Catalog.ResolveDependencies(package.Dependencies,inspectKnown);foreach(Dependency d in package.Dependencies)Console.WriteLine("dependency "+d.Id+": "+Msg.Plain(d.Note));var inspection=new Session(package,args[1]);RuntimeGuard.Check(inspection);inspection.AssertUnchanged();Console.WriteLine("PASS read-only runtime inspection; effective config valid; no deployment.");return 0;}
        if(args.Length==3&&args[2]=="--inspect-resources")
        {var spec=LocalEditorSpec.Load(args[0]);var inspection=new LocalResourceSession(spec,args[1]);inspection.ValidateAll();inspection.AssertUnchanged();Console.WriteLine("PASS read-only Resources inspection; "+inspection.Items().Count+" entries; no deployment.");return 0;}
        if(args.Length!=2)return 2;
        string root=Path.GetFullPath(args[1]);Directory.CreateDirectory(root);Package p=Package.Load(args[0]);CollectionSpec collection=p.Collections.Single();

        Test("package is generic manifest/schema model",()=>Check(p.Id=="tesmio.vehicle_materials"&&p.Target=="vehicle_materials"&&p.ConfigName=="vehicle_materials.ini"&&p.Fields.Count==20&&p.Collections.Count==1));
        Test("collection empty notice is schema-driven",()=>Check(collection.EmptyNoticeKey=="vehicles.empty_notice"&&collection.EmptyNoticeStyle=="warning"));
        Test("shipped Vehicle Materials starts with no resources",()=>Check(p.Defaults.Get("general","enabled")=="0"&&CollectionRules.Names(collection,p.Defaults).Count==0));
        Test("default config validates through generic rules",()=>ConfigRules.Validate(p,p.Defaults));
        Test("PE validation rejects non-DLL",()=>Reject(()=>SafeFiles.RequireX64Dll(new byte[256])));

        Test("provider catalogue exposes identifiers and display names",()=>
        {var registry=ResourceRegistry.Parse("[list]\nglass=aluminium, Glass\nsand=61, bauxite, Sand\nraw_salt=rawgravel\n[resources]\nhook=2\n","fixture",collection);Check(registry.Ready&&registry.Options.Count==3&&registry.Options[1].Display=="Sand");});
        Test("provider catalogue rejects duplicate identifiers",()=>Reject(()=>ResourceRegistry.Parse("[list]\nsand=x\nSAND=y\n[resources]\nhook=2","fixture",collection)));
        Test("provider readiness condition comes from schema",()=>Check(!ResourceRegistry.Parse("[list]\nsand=x\n[resources]\nhook=1","fixture",collection).Ready));

        var coefficients=new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase){{"road","0.01"},{"rail","0.02"},{"ship","0"},{"airplane","0.003"}};
        Ini sand=CollectionRules.Add(p,collection,p.Defaults,"sand",coefficients);
        Test("generic collection addition updates count list and every target",()=>Check(sand.Get("resources","count")=="1"&&sand.Get("resources","resource0")=="sand"&&sand.Get("rail","sand")=="0.02"));
        Test("enabled collection accepts a positive dynamic value",()=>ConfigRules.Validate(p,new Ini(sand.Render(new Dictionary<string,string>{{"general/enabled","1"}}))));
        Test("enabled collection rejects only-zero dynamic values",()=>Reject(()=>ConfigRules.Validate(p,new Ini(CollectionRules.Add(p,collection,p.Defaults,"sand",collection.TargetSections.ToDictionary(x=>x,x=>"0")).Render(new Dictionary<string,string>{{"general/enabled","1"}})))));
        Test("positive collection failure exposes a language-neutral rule key",()=>{RuleException found=null;try{ConfigRules.Validate(p,new Ini(p.Defaults.Render(new Dictionary<string,string>{{"general/enabled","1"}})));}catch(RuleException e){found=e;}Check(found!=null&&found.TranslationKey=="error_positive_collection_required"&&Convert.ToString(found.TranslationArguments.Single())=="materials");});
        Test("duplicate collection item is rejected",()=>Reject(()=>CollectionRules.Add(p,collection,sand,"SAND",coefficients)));
        Ini copper=CollectionRules.Add(p,collection,sand,"copper",collection.TargetSections.ToDictionary(x=>x,x=>"0.04"));
        Test("collection removal compacts list and removes all target keys",()=>{Ini removed=CollectionRules.Remove(p,collection,copper,"sand");Check(removed.Get("resources","count")=="1"&&removed.Get("resources","resource0")=="copper"&&removed.Get("road","sand")==null);});
        Test("unknown key rejected precisely",()=>Reject(()=>ConfigRules.Validate(p,new Ini(p.Defaults.Render(new Dictionary<string,string>{{"general/unknown","1"}})))));
        Test("bad fixed choice rejected",()=>Reject(()=>ConfigRules.Validate(p,new Ini(p.Defaults.Render(new Dictionary<string,string>{{"mapping/type0","4"}})))));
        var decimalField=new Field{Section="road",Key="sand",Label="Sand",Type="decimal",Minimum=0,Maximum=1000000};
        Test("numeric comparison ignores trailing zeroes",()=>Check(decimalField.Equivalent("0.030","0.03")&&decimalField.Normalize("0.0301234")=="0.0301234"));
        Test("decimal comma rejected by core invariant parser",()=>Reject(()=>decimalField.Normalize("0,030")));

        string simpleRoot=MakeSimplePackage(root,p);Package simple=Package.Load(simpleRoot);
        Test("second unrelated plugin loads without code changes",()=>Check(simple.Id=="example.sample"&&simple.Target=="sample_plugin"&&simple.Fields.Count==3&&simple.Collections.Count==0));
        Test("second plugin validation uses only its schema",()=>{ConfigRules.Validate(simple,simple.Defaults);Reject(()=>ConfigRules.Validate(simple,new Ini(simple.Defaults.Render(new Dictionary<string,string>{{"general/limit","21"}}))));});
        string simpleBuild=MakeBuild(root,"simple-build");Session simpleSession=new Session(simple,simpleBuild);
        Test("a package session points at the package's own DLL and at user_config",()=>Check(simpleSession.LocalDll==simple.DllPath&&simple.UserOverlay&&simpleSession.UserIni.EndsWith("user_config\\sample_plugin.ini")&&simpleSession.LoaderSwitchApplies&&simpleSession.SwitchMode=="loader"));
        Test("a Workshop plugin saves its values to user_config and writes nothing into plugins",()=>{string before=SafeFiles.HashFile(simple.DefaultsPath);simpleSession.Set(simple.Fields.Single(x=>x.Id=="general/limit"),"7");simpleSession.Commit(true,NoGame);Check(new Ini(SafeFiles.Text(simpleSession.UserIni)).Get("general","limit")=="7"&&File.Exists(simpleSession.Receipt),"values and receipt");Check(Directory.GetFiles(Path.Combine(simpleBuild,"plugins")).Length==0,"no DLL and no INI in plugins");Check(SafeFiles.HashFile(simple.DefaultsPath)==before,"the package INI is untouched");Check(new Ini(SafeFiles.Text(simpleSession.Receipt)).Get("state","mode")=="workshop");Check(new Session(simple,simpleBuild).Overrides["general/limit"]=="7","the value is read back");});

        Test("package and deployment roots may not overlap",()=>{Reject(()=>new Session(p,p.Root));Reject(()=>new Session(p,Path.Combine(p.Root,"build")));});
        Test("INI comments and newlines are retained",()=>{string text="; header\r\n[x]\r\na = 1\r\n# note\r\n";Check(new Ini(text).Render(new Dictionary<string,string>{{"x/a","2"}})==text.Replace("a = 1","a = 2"));});
        Test("INI repeated keys and sections are rejected",()=>{Reject(()=>new Ini("[x]\na=1\na=2"));Reject(()=>new Ini("[x]\na=1\n[X]\nb=2"));});
        string localSchema=Path.GetFullPath(Path.Combine(args[0],"..","..","settings_schemas","resources.launcher.ini"));LocalEditorSpec resourceSpec=LocalEditorSpec.Load(localSchema);string resourceBuild=MakeBuild(root,"resource-editor");File.WriteAllBytes(Path.Combine(resourceBuild,"plugins","resources.dll"),p.Dll);string resourceText="; upstream comment\r\n[list]\r\nsand = bauxite, Sand\r\ncopper = aluminium, Copper\r\n\r\n[custom:sand]\r\ncargo = bulk\r\n\r\n[base_price]\r\n; optional\r\n\r\n[price]\r\n\r\n[resources]\r\nhook = 2\r\n";Write(Path.Combine(resourceBuild,"plugins","resources.ini"),resourceText);Write(Path.Combine(resourceBuild,"tesmioloader.ini"),"[tesmioloader]\nplugins=1\n[plugins]\nresources=1\n");LocalResourceSession resources=new LocalResourceSession(resourceSpec,resourceBuild);
        Test("local Resources schema defines dynamic custom and keyed price fields",()=>Check(resourceSpec.Fields.Any(x=>x.Scope=="custom"&&x.Key=="transport")&&resourceSpec.Fields.Any(x=>x.Scope=="keyed"&&x.Section=="base_price")&&resourceSpec.Fields.Any(x=>x.Scope=="keyed"&&x.Section=="price")));
        Test("upstream Resources entries are fixed and retain comments",()=>Check(resources.Items().Count==2&&resources.Items().All(x=>!x.Owned)&&resources.Effective().Contains("; upstream comment")));
        Test("fixed Resources entries accept personal property overrides",()=>{resources.SetField("sand",resourceSpec.Fields.Single(x=>x.Id=="pinned_base_price"),"12.0, 10.0");string effective=resources.Effective();var doc=new LooseIni(effective);Check(doc.Get("base_price","sand")=="12, 10"&&doc.Get("custom:sand","cargo")=="bulk");});
        Test("personal custom resource creates list custom base and price entries",()=>{resources.Add("hydrogen","custom","Hydrogen","oil");resources.SetField("hydrogen",resourceSpec.Fields.Single(x=>x.Id=="kind"),"1");resources.SetField("hydrogen",resourceSpec.Fields.Single(x=>x.Id=="pinned_base_price"),"180, 150");resources.SetField("hydrogen",resourceSpec.Fields.Single(x=>x.Id=="forced_price"),"200, 175");var doc=new LooseIni(resources.Effective());Check(resources.Items().Single(x=>x.Id=="hydrogen").Owned&&doc.Get("list","hydrogen")=="custom, Hydrogen"&&doc.Get("custom:hydrogen","transport")=="oil"&&doc.Get("custom:hydrogen","kind")=="1"&&doc.Get("base_price","hydrogen")=="180, 150"&&doc.Get("price","hydrogen")=="200, 175");});
        Test("custom resource without transport is rejected without leaving an item",()=>{Reject(()=>resources.Add("broken","custom","Broken",""));Check(!resources.Items().Any(x=>x.Id=="broken"));});
        Test("original Resources entry cannot be removed",()=>Reject(()=>resources.Remove("sand")));
        // ---- 0.18.0: keyed_list editor (needs) ----
        string needsSchema=Path.GetFullPath(Path.Combine(args[0],"..","..","settings_schemas","needs.launcher.ini"));LocalEditorSpec needsSpec=LocalEditorSpec.Load(needsSchema);string needsBuild=MakeBuild(root,"needs-editor");File.WriteAllBytes(Path.Combine(needsBuild,"plugins","needs.dll"),p.Dll);File.WriteAllBytes(Path.Combine(needsBuild,"plugins","resources.dll"),p.Dll);Write(Path.Combine(needsBuild,"plugins","resources.ini"),"[list]\r\nfurniture = eletronics, Furniture\r\nmedicine = eletronics, Medicine\r\nbooks = clothes, Books\r\n\r\n[resources]\r\nhook = 2\r\n");
        string needsText="; needs upstream\r\n[list]\r\nfurniture = eletronics, 1.0, advanced, 0.35, 0.010\r\nmedicine  = eletronics, 0.5, none, 0.30, 0.008\r\n\r\n[needs]\r\nenabled = 1\r\ndemand = 1\r\nstorage = 1\r\nmax_demands = 7\r\nwhen_full = skip\r\nprobe = 1\r\nlog_seconds = 60\r\n";Write(Path.Combine(needsBuild,"plugins","needs.ini"),needsText);Write(Path.Combine(needsBuild,"tesmioloader.ini"),"[tesmioloader]\nplugins=1\n[plugins]\nresources=1\nneeds=1\n");
        LocalResourceSession needs=new LocalResourceSession(needsSpec,needsBuild);LocalDetailField needsProbe=needsSpec.Fields.Single(x=>x.Id=="probe"),needsMax=needsSpec.Fields.Single(x=>x.Id=="max_demands"),needsWhen=needsSpec.Fields.Single(x=>x.Id=="when_full");
        Test("needs schema is a keyed_list editor with five columns and global switches",()=>Check(needsSpec.IsList&&needsSpec.Columns.Count==5&&needsSpec.Columns[0].Id=="donor"&&needsSpec.Columns[0].Required&&needsSpec.Columns[2].AllowOther&&needsSpec.Fields.Count(x=>x.Scope=="global")==7&&needsSpec.SourcePlugin=="resources"&&needsSpec.MaximumItems==8&&needsSpec.HasGlobals));
        Test("needs list lines are shown column by column with defaults",()=>{var items=needs.Items();Check(items.Count==2&&!items.Any(x=>x.Owned)&&items[0].Id=="furniture"&&items[0].Subtitle=="eletronics · 1.0 · advanced"&&needsSpec.TupleColumns("clothes")[1]=="1.0"&&needsSpec.TupleColumns("clothes")[2]=="auto"&&needsSpec.TupleColumns("clothes")[4]=="0");});
        Test("tuple normalisation validates each column and fills gaps with defaults",()=>{Check(needsSpec.NormalizeTuple("food")=="food"&&needsSpec.NormalizeTuple(" Food ,, none")=="food, 1, none"&&needsSpec.NormalizeTuple("meat, 0.5, 12, 0.3, 0.01")=="meat, 0.5, 12, 0.3, 0.01");Reject(()=>needsSpec.NormalizeTuple("bread"));Reject(()=>needsSpec.NormalizeTuple("food, 1, auto, 1.5"));Reject(()=>needsSpec.NormalizeTuple("food, 1, auto, 1, 0, extra"));Reject(()=>needsSpec.NormalizeTuple(""));Reject(()=>needsSpec.NormalizeTuple("food, x"));});
        Test("personal need is added as a full line and rendered into the effective INI",()=>{needs.AddRaw("books","clothes, 2, medium, 0.5, 0.01");var doc=new LooseIni(needs.Effective());Check(doc.Get("list","books")=="clothes, 2, medium, 0.5, 0.01"&&needs.Items().Single(x=>x.Id=="books").Owned&&needs.Effective().Contains("; needs upstream")&&needs.Dirty);Reject(()=>needs.AddRaw("books","food"));Reject(()=>needs.AddRaw("furniture","food"));Reject(()=>needs.AddRaw("bad id","food"));Reject(()=>needs.AddRaw("nodonor",""));Check(needs.Items().Count==3);});
        Test("original need columns can be overridden and return to the original",()=>{needs.SetListRaw("furniture","eletronics, 1.0, advanced, 0.5, 0.010");Check(new LooseIni(needs.Effective()).Get("list","furniture")=="eletronics, 1, advanced, 0.5, 0.01"&&needs.Overrides.Items.ContainsKey("furniture"));needs.SetListRaw("furniture","eletronics, 1.0, advanced, 0.35, 0.010");Check(!needs.Overrides.Items.ContainsKey("furniture")&&new LooseIni(needs.Effective()).Get("list","furniture")=="eletronics, 1.0, advanced, 0.35, 0.010");Reject(()=>needs.SetListRaw("furniture","food, 1, auto, 2"));});
        Test("original need can be hidden and shown again",()=>{needs.Suppress("medicine");var doc=new LooseIni(needs.Effective());Check(doc.Get("list","medicine")==null&&needs.Items().All(x=>x.Id!="medicine")&&needs.SuppressedIds().SequenceEqual(new[]{"medicine"}));Reject(()=>needs.AddRaw("medicine","food"));needs.Unsuppress("medicine");Check(needs.Items().Any(x=>x.Id=="medicine")&&!needs.Overrides.Items.ContainsKey("medicine"));Reject(()=>needs.Suppress("books"));Reject(()=>needs.Remove("furniture"));});
        Test("global plugin switches are overridden per field and validated",()=>{needs.SetGlobal(needsProbe,"0");needs.SetGlobal(needsMax,"5");var doc=new LooseIni(needs.Effective());Check(doc.Get("needs","probe")=="0"&&doc.Get("needs","max_demands")=="5"&&doc.Get("needs","enabled")=="1"&&needs.GlobalValue(needsProbe)=="0"&&needs.GlobalBaseline(needsProbe)=="1");Reject(()=>needs.SetGlobal(needsMax,"9"));Reject(()=>needs.SetGlobal(needsWhen,"maybe"));needs.SetGlobal(needsProbe,"1");Check(!needs.Overrides.Globals.ContainsKey("probe")&&needs.Overrides.Globals["max_demands"]=="5");});
        Test("maximum of eight needs is enforced",()=>{for(int i=4;i<=8;i++)needs.AddRaw("need"+i,"food");Check(needs.Items().Count==8);Reject(()=>needs.AddRaw("need9","food"));for(int i=4;i<=8;i++)needs.Remove("need"+i);Check(needs.Items().Count==3);});
        Test("needs overrides are stored separately and survive commit and reopen",()=>{needs.Commit(NoGame);var needsStore=ResourceOverrideStore.Parse(SafeFiles.Text(needs.UserIni));Check(needsStore.Globals["max_demands"]=="5"&&needsStore.Items["books"].Owned&&needsStore.Items["books"].ListValue=="clothes, 2, medium, 0.5, 0.01");var reopened=new LocalResourceSession(needsSpec,needsBuild);var doc=new LooseIni(SafeFiles.Text(reopened.LocalIni));Check(doc.Get("list","books")=="clothes, 2, medium, 0.5, 0.01"&&doc.Get("needs","max_demands")=="5"&&reopened.Items().Single(x=>x.Id=="books").Owned&&reopened.GlobalValue(needsMax)=="5"&&!reopened.Dirty&&File.Exists(reopened.UpstreamFile)&&SafeFiles.Text(reopened.UpstreamFile)==needsText);});
        Test("hidden original is left out of the effective file but kept in the upstream copy",()=>{var s=new LocalResourceSession(needsSpec,needsBuild);s.Suppress("medicine");s.Commit(NoGame);Check(new LooseIni(SafeFiles.Text(s.LocalIni)).Get("list","medicine")==null&&new LooseIni(SafeFiles.Text(s.UpstreamFile)).Get("list","medicine")!=null);var again=new LocalResourceSession(needsSpec,needsBuild);Check(again.SuppressedIds().Contains("medicine")&&again.Items().Count==2);again.Reset();Check(again.Items().Count==2&&again.Items().All(x=>!x.Owned)&&again.SuppressedIds().Count==0&&again.GlobalValue(needsMax)=="7");again.Commit(NoGame);});
        Test("needs picker offers exactly the Resources-registered ids",()=>{var registry=ResourceRegistry.Load(needsBuild,needsSpec.SourcePlugin,needsSpec.SourceSection,needsSpec.SourceReadySection,needsSpec.SourceReadyKey,needsSpec.SourceReadyValue);Check(registry.Ready&&registry.Options.Count==3&&registry.Find("books")!=null);Check(LocalResourceRuntime.Active(new CatalogEntry{Root=needsSchema},needsBuild));});
        Test("keyed_list schemas without columns or with bad columns are rejected",()=>{string bad=Path.Combine(root,"bad-list");Write(Path.Combine(bad,"bad.launcher.ini"),"[launcher]\neditor_type=keyed_list\nlayout_version=1\nid=x\nname=X\n[editor]\nplugin=x\nconfig=x.ini\nlist_section=list\nmaximum_items=3\n");Reject(()=>LocalEditorSpec.Load(Path.Combine(bad,"bad.launcher.ini")));Write(Path.Combine(bad,"bad2.launcher.ini"),"[launcher]\neditor_type=keyed_list\nlayout_version=1\nid=x\nname=X\n[editor]\nplugin=x\nconfig=x.ini\nlist_section=list\nmaximum_items=3\n[column:a]\ntype=choice\n");Reject(()=>LocalEditorSpec.Load(Path.Combine(bad,"bad2.launcher.ini")));});
        // ---- 0.20.0: keyed_sections editor (deposits) ----
        string depositsSchema=Path.GetFullPath(Path.Combine(args[0],"..","..","settings_schemas","deposits.launcher.ini"));LocalEditorSpec depositsSpec=LocalEditorSpec.Load(depositsSchema);string depositsBuild=MakeBuild(root,"deposits-editor");File.WriteAllBytes(Path.Combine(depositsBuild,"plugins","deposits.dll"),p.Dll);File.WriteAllBytes(Path.Combine(depositsBuild,"plugins","resources.dll"),p.Dll);Write(Path.Combine(depositsBuild,"plugins","resources.ini"),"[list]\r\ncopper_ore = rawiron, Copper Ore\r\nglass = aluminium, Glass\r\nsand = bauxite, Sand\r\n\r\n[resources]\r\nhook = 2\r\n");
        string depositsText="; deposits upstream\r\n[deposits]\r\ncode_patch = 1\r\nminimap = 1\r\neditor = 1\r\n\r\n; the copper deposit\r\n[copper]\r\ntoken         = $TYPE_MINE_COPPER\r\ntype          = 10\r\nmap           = resourcemap2\r\ncomponent     = 3\r\nradius        = ore\r\nicon          = copper_ore\r\nminimap       = 1\r\neditor        = copper\r\n\r\n[sand]\r\ntoken         = $TYPE_MINE_SAND\r\ntype          = 11\r\nmap           = terrain\r\ncomponent     = 1\r\nradius        = gravel\r\nicon          = sand\r\nminimap       = 1\r\neditor        = sand\r\n";Write(Path.Combine(depositsBuild,"plugins","deposits.ini"),depositsText);Write(Path.Combine(depositsBuild,"tesmioloader.ini"),"[tesmioloader]\nplugins=1\n[plugins]\nresources=1\ndeposits=1\n");
        LocalResourceSession deposits=new LocalResourceSession(depositsSpec,depositsBuild);LocalDetailField depositType=depositsSpec.Fields.Single(x=>x.Id=="type"),depositEditor=depositsSpec.Fields.Single(x=>x.Id=="editor"),depositMap=depositsSpec.Fields.Single(x=>x.Id=="map"),depositIcon=depositsSpec.Fields.Single(x=>x.Id=="icon");
        Test("deposits schema is a keyed_sections editor with item fields, dialog fields and two tabs",()=>Check(depositsSpec.IsSections&&depositsSpec.HidesOriginals&&depositsSpec.IsReserved("deposits")&&depositsSpec.Fields.Count(x=>x.Scope=="item")==10&&depositsSpec.Fields.Count(x=>x.Scope=="item"&&x.InDialog)==7&&depositsSpec.Fields.Count(x=>x.Scope=="global")==3&&depositType.Unique&&depositType.AutoIncrement&&depositEditor.MaximumLength==31&&depositEditor.LengthRuleKey==""&&depositIcon.ChoicesSource=="registry"&&depositsSpec.Tabs.Count==2&&depositsSpec.DefaultTab=="deposits"&&depositsSpec.ProposedToken("glass")=="$TYPE_MINE_GLASS"&&depositsSpec.SummaryKeys.SequenceEqual(new[]{"type","map"})));
        Test("deposit sections are listed with their type and map, the reserved section is not",()=>{var items=deposits.Items();Check(items.Count==2&&items[0].Id=="copper"&&items[0].Subtitle=="10 · resourcemap2"&&items[1].Id=="sand"&&!items.Any(x=>x.Owned)&&deposits.Value("copper",depositsSpec.Fields.Single(x=>x.Id=="token"))=="$TYPE_MINE_COPPER"&&deposits.NextValue(depositType)=="12"&&deposits.SwitchOn);});
        Test("a personal deposit is added as a new section in schema order",()=>{deposits.AddSection("glass",new Dictionary<string,string>{{"token","$TYPE_MINE_GLASS"},{"type","12"},{"map","auto"},{"radius","ore"},{"icon","glass"},{"minimap","1"},{"editor","glass"}});var doc=new LooseIni(deposits.Effective());Check(doc.Get("glass","token")=="$TYPE_MINE_GLASS"&&doc.Get("glass","type")=="12"&&doc.Get("glass","map")=="auto"&&doc.Get("copper","type")=="10"&&deposits.Effective().Contains("; the copper deposit")&&deposits.Items().Single(x=>x.Id=="glass").Owned&&deposits.NextValue(depositType)=="13");string text=deposits.Effective();Check(text.IndexOf("[glass]")>text.IndexOf("[sand]")&&text.IndexOf("token = $TYPE_MINE_GLASS")<text.IndexOf("type = 12"));});
        Test("duplicate type numbers, reserved names and existing names are rejected",()=>{Reject(()=>deposits.AddSection("dup",new Dictionary<string,string>{{"token","$TYPE_MINE_DUP"},{"type","12"}}));Reject(()=>deposits.AddSection("deposits",new Dictionary<string,string>{{"token","x"},{"type","20"}}));Reject(()=>deposits.AddSection("copper",new Dictionary<string,string>{{"token","x"},{"type","20"}}));Reject(()=>deposits.AddSection("!!!",new Dictionary<string,string>{{"token","x"},{"type","20"}}));Check(deposits.Items().Count==3);});   // 0.4.25: "bad name" would now become bad_name and be accepted
        // 0.4.7 of Deposits Plus writes a generated key into the tool descriptor, so the brush name
        // is only the two file names and may be as long as the INI field allows.
        Test("item fields validate ranges and the brush name length",()=>{Reject(()=>deposits.SetField("glass",depositType,"9"));Reject(()=>deposits.SetField("glass",depositType,"128"));Reject(()=>deposits.SetField("glass",depositEditor,new String('x',32)));deposits.SetField("glass",depositEditor,"rocksalt_and_more_than_seven");deposits.ValidateAll();deposits.SetField("glass",depositEditor,"");deposits.SetField("sand",depositEditor,"sandy");deposits.ValidateAll();deposits.SetField("sand",depositEditor,"");Check(deposits.Value("sand",depositEditor)=="sand");deposits.ValidateAll();deposits.SetField("glass",depositType,"10");Reject(()=>deposits.Validate());deposits.SetField("glass",depositType,"12");deposits.ValidateAll();});
        Test("an original deposit's keys can be overridden and reset",()=>{deposits.SetField("copper",depositMap,"resourcemap3");Check(new LooseIni(deposits.Effective()).Get("copper","map")=="resourcemap3"&&deposits.Overrides.Items.ContainsKey("copper"));deposits.SetField("copper",depositMap,"resourcemap2");Check(!deposits.Overrides.Items.ContainsKey("copper"));deposits.SetField("copper",depositMap,"");Check(deposits.Value("copper",depositMap)=="resourcemap2");});
        Test("hiding an original deposit drops its whole section and keeps the rest",()=>{deposits.Suppress("copper");string text=deposits.Effective();var doc=new LooseIni(text);Check(!doc.HasSection("copper")&&doc.Get("sand","type")=="11"&&doc.Get("deposits","code_patch")=="1"&&doc.Get("glass","type")=="12"&&deposits.SuppressedIds().SequenceEqual(new[]{"copper"})&&deposits.NextValue(depositType)=="13");deposits.Unsuppress("copper");Check(new LooseIni(deposits.Effective()).Get("copper","token")=="$TYPE_MINE_COPPER");});
        Test("deposit overrides survive commit and reopen and the loader switch is written",()=>{deposits.SetGlobal(depositsSpec.Fields.Single(x=>x.Id=="editor_all"),"0");deposits.SetLoaderEnabled(false);deposits.Commit(NoGame);var reopened=new LocalResourceSession(depositsSpec,depositsBuild);var doc=new LooseIni(SafeFiles.Text(reopened.LocalIni));Check(doc.Get("glass","token")=="$TYPE_MINE_GLASS"&&doc.Get("deposits","editor")=="0"&&reopened.Items().Single(x=>x.Id=="glass").Owned&&!reopened.LoaderEnabled&&!reopened.Dirty&&SafeFiles.Text(reopened.UpstreamFile)==depositsText&&new Ini(SafeFiles.Text(reopened.LoaderIni)).Get("plugins","deposits")=="0");reopened.SetLoaderEnabled(true);reopened.Remove("glass");reopened.Commit(NoGame);Check(!new LooseIni(SafeFiles.Text(reopened.LocalIni)).HasSection("glass"));});
        Test("LooseIni section helpers keep comments of the neighbours",()=>{var doc=new LooseIni("; head\r\n[a]\r\nx = 1\r\n; about b\r\n[b]\r\ny = 2\r\n[c]\r\nz = 3\r\n");doc.RemoveSection("b");Check(doc.SectionNames().SequenceEqual(new[]{"a","c"})&&doc.Render().Contains("; head")&&!doc.Render().Contains("about b")&&doc.Get("c","z")=="3");doc.EnsureSection("d");doc.Set("d","k","v");Check(doc.HasSection("d")&&doc.Get("d","k")=="v"&&doc.SectionNames().Last()=="d");});
        // ---- 0.19.0: single plugin switch, tabs in local editors, launcher arguments ----
        // 0.5.18: a missing activity key reads as the schema default, the way the plugin reads it (buildings_plus, needs_plus).
        Test("a missing activity key counts as the schema default and the header switch writes nothing then",()=>{string b=MakeBuild(root,"activity-default");Write(Path.Combine(b,"plugins","needs.ini"),"[list]\r\nfurniture = eletronics, 1.0, advanced, 0.35, 0.010\r\n");File.WriteAllBytes(Path.Combine(b,"plugins","needs.dll"),p.Dll);Write(Path.Combine(b,"tesmioloader.ini"),"[tesmioloader]\nplugins=1\n[plugins]\nneeds=1\n");var s=new LocalResourceSession(needsSpec,b);Check(s.ActivityOn);LocalDetailField en=needsSpec.ActivityField;Check(en!=null&&en.Default=="1");s.SetGlobal(en,"0");Check(!s.ActivityOn);s.SetLoaderEnabled(true);Check(s.ActivityOn&&new LooseIni(s.Effective()).Get("needs","enabled")=="1");});
        Test("needs schema declares two tabs and the single switch drives loader entry and INI key",()=>{Check(needsSpec.Tabs.Count==2&&needsSpec.Tabs[0].Id=="general"&&needsSpec.Tabs[1].Id=="needs"&&needsSpec.GroupTab=="needs"&&needsSpec.GlobalTab=="general"&&needsSpec.DefaultTab=="needs"&&needsSpec.ActivityField!=null&&needsSpec.ActivityField.Id=="enabled");var s=new LocalResourceSession(needsSpec,needsBuild);Check(s.LoaderEnabled&&s.SwitchOn&&!s.Dirty);s.SetLoaderEnabled(false);Check(s.LoaderChanged&&s.Dirty&&!s.SwitchOn);s.Commit(NoGame);Check(new Ini(SafeFiles.Text(s.LoaderIni)).Get("plugins","needs")=="0"&&new Ini(SafeFiles.Text(s.LoaderIni)).Get("plugins","resources")=="1"&&!LocalResourceRuntime.Active(new CatalogEntry{Root=needsSchema},needsBuild));var again=new LocalResourceSession(needsSpec,needsBuild);Check(!again.LoaderEnabled&&!again.Dirty);LocalDetailField enabledField=needsSpec.Fields.Single(x=>x.Id=="enabled");again.SetGlobal(enabledField,"0");Check(!again.ActivityOn);again.SetLoaderEnabled(true);Check(again.SwitchOn&&again.GlobalValue(enabledField)=="1");again.Commit(NoGame);Check(new Ini(SafeFiles.Text(again.LoaderIni)).Get("plugins","needs")=="1"&&new LooseIni(SafeFiles.Text(again.LocalIni)).Get("needs","enabled")=="1"&&LocalResourceRuntime.Active(new CatalogEntry{Root=needsSchema},needsBuild));});
        Test("resources schema keeps a single tab and has no INI activity key",()=>Check(resourceSpec.Tabs.Count==1&&resourceSpec.GroupTab==resourceSpec.Tabs[0].Id&&resourceSpec.DefaultTab==resourceSpec.Tabs[0].Id&&resourceSpec.ActivityField==null&&new LocalResourceSession(resourceSpec,resourceBuild).SwitchOn));
        Test("a disabled loader entry no longer blocks saving a local editor",()=>{Write(Path.Combine(resourceBuild,"tesmioloader.ini"),"[tesmioloader]\nplugins=1\n[plugins]\nresources=0\n");Write(Path.Combine(resourceBuild,"tesmioloader.dll"),"fake");Write(Path.Combine(resourceBuild,"tesmiolauncher.exe"),"fake");var s=new LocalResourceSession(resourceSpec,resourceBuild);Check(!s.LoaderEnabled&&!s.SwitchOn);LocalResourceGuard.Check(s);Write(Path.Combine(resourceBuild,"tesmioloader.ini"),"[tesmioloader]\nplugins=1\n[plugins]\nresources=1\n");});
        Test("unknown tab references in a local schema are rejected",()=>{string bad=Path.Combine(root,"bad-tabs");Write(Path.Combine(bad,"bad.launcher.ini"),"[launcher]\neditor_type=keyed_list\nlayout_version=1\nid=x\nname=X\ndefault_tab=missing\n[editor]\nplugin=x\nconfig=x.ini\nlist_section=list\nmaximum_items=3\n[tab:a]\nlabel=A\n[column:a]\ntype=text\n");Reject(()=>LocalEditorSpec.Load(Path.Combine(bad,"bad.launcher.ini")));});
        Test("launcher arguments skip the window unless rmm.ini asks for it",()=>{Check(LauncherOptions.Arguments=="--nogui");LauncherOptions.ShowWindow=true;Check(LauncherOptions.Arguments=="");LauncherOptions.ShowWindow=false;});
        string resourceDllHash=SafeFiles.HashFile(Path.Combine(resourceBuild,"plugins","resources.dll"));
        Test("Resources overlay commits transactionally without replacing the DLL",()=>{resources.Commit(NoGame);Check(File.Exists(resources.UserIni)&&File.Exists(resources.UpstreamFile)&&new LooseIni(SafeFiles.Text(resources.LocalIni)).Get("price","hydrogen")=="200, 175"&&SafeFiles.HashFile(resources.LocalDll)==resourceDllHash);});
        Test("external Resources update becomes the new upstream and keeps personal entries",()=>{string updated=resourceText.Replace("copper = aluminium, Copper","copper = aluminium, Copper\r\nclay = bauxite, Clay");Write(resources.LocalIni,updated);var rebased=new LocalResourceSession(resourceSpec,resourceBuild);Check(rebased.Items().Any(x=>x.Id=="clay"&&!x.Owned)&&rebased.Items().Any(x=>x.Id=="hydrogen"&&x.Owned)&&rebased.Notes.Any(x=>Msg.KeyOf(x)=="res_note_external_update"));});
        Test("manual additions become locked external entries without absorbing personal entries",()=>{var rebased=new LocalResourceSession(resourceSpec,resourceBuild);string edited=rebased.Effective().Replace("clay = bauxite, Clay","clay = bauxite, Clay\r\nquartz = bauxite, Quartz");Write(rebased.LocalIni,edited);var detected=new LocalResourceSession(resourceSpec,resourceBuild);Check(detected.Items().Single(x=>x.Id=="quartz").Owned==false&&detected.Items().Single(x=>x.Id=="hydrogen").Owned);});
        Test("disappeared external resources are reported",()=>{var current=new LocalResourceSession(resourceSpec,resourceBuild);Write(current.LocalIni,current.Effective().Replace("copper = aluminium, Copper\r\n","").Replace("copper = aluminium, Copper\n",""));var detected=new LocalResourceSession(resourceSpec,resourceBuild);Check(detected.DisappearedExternal.Contains("copper"));});
        Test("resource deletion plans and transactionally removes declared plugin references",()=>
        {
            var current=new LocalResourceSession(resourceSpec,resourceBuild);current.Commit(NoGame);
            var vehicle=new Session(p,resourceBuild);var configured=CollectionRules.Add(p,collection,new Ini(vehicle.Effective()),"hydrogen",new Dictionary<string,string>{{"road","0.02"},{"rail","0.01"},{"ship","0.005"},{"airplane","0.003"}});vehicle.Overrides.Clear();foreach(var pair in configured.Values.Where(x=>!p.IsDefault(x.Key,x.Value)))vehicle.Overrides[pair.Key]=pair.Value;vehicle.Commit(true,NoGame);
            var resourceCatalog=new[]{new CatalogEntry{Root=p.Root,Id=p.Id,Name=p.Name,Version=p.Version,Supported=true}};var plan=ResourceConsistency.PlanRemoval(resourceBuild,resourceCatalog,"hydrogen");Check(plan.Plugins.Contains(p.Name)&&plan.Writes.Count==1&&plan.Writes.Keys.Single().EndsWith("user_config\\vehicle_materials.ini"),"a Workshop plugin's reference is taken out of its personal values");
            var editor=new LocalResourceSession(resourceSpec,resourceBuild);editor.Remove("hydrogen");editor.StageDependencyWrites(plan.Writes);editor.Commit(NoGame);
            Check(!CollectionRules.Names(collection,new Ini(new Session(p,resourceBuild).Effective())).Contains("hydrogen")&&!File.Exists(Path.Combine(resourceBuild,"plugins","vehicle_materials.ini"))&&ResourceConsistency.ValidateReferences(resourceBuild,resourceCatalog).Count==0);
        });
        foreach(string path in new[]{"..\\escape","C:\\escape","\\\\server\\file","hooks\\..\\file","hooks\\file. "}){string candidate=path;Test("reject path "+path,()=>Reject(()=>SafeFiles.Child(root,candidate)));}

        string first=MakeBuild(root,"first");Ini existing=CollectionRules.Add(p,collection,p.Defaults,"sand",coefficients);Write(Path.Combine(first,"plugins","vehicle_materials.ini"),existing.Render(new Dictionary<string,string>{{"general/enabled","1"}}));
        Test("existing local INI is imported without overwrite",()=>{var s=new Session(p,first);Check(CollectionRules.Names(collection,new Ini(s.Effective())).Single()=="sand"&&!File.Exists(s.UserIni));});
        Test("first deployment preserves imported collection",()=>{var s=new Session(p,first);s.Commit(true,NoGame);Check(CollectionRules.Names(collection,new Ini(SafeFiles.Text(s.LocalIni))).Single()=="sand"&&File.Exists(s.Receipt));});
        Test("no-op deployment creates no additional backup",()=>Check(new Session(p,first).Commit(true,NoGame).StartsWith("Keine Datei")));
        Test("a local INI beside a Workshop plugin is named and left alone",()=>{var s=new Session(p,first);string before=SafeFiles.HashFile(s.LocalIni);Check(s.Notes.Any(n=>Msg.KeyOf(n)=="note_local_ini_shadows"));s.Commit(true,NoGame);Check(SafeFiles.HashFile(s.LocalIni)==before);});
        Test("an INI that an earlier version deployed into plugins is taken out on the next save",()=>{string b=MakeBuild(root,"stale-ini");Write(Path.Combine(b,"plugins","sample_plugin.ini"),"[general]\nenabled = 1\nlimit = 9\nmode = normal\n");Write(Path.Combine(b,"user_config",".autoload","sample_plugin.receipt.ini"),"[state]\r\nid = "+simple.Id+"\r\nversion = 1.0\r\nmode = package\r\ndll_hash = x\r\nini_hash = "+SafeFiles.HashFile(Path.Combine(b,"plugins","sample_plugin.ini"))+"\r\ndefaults_hash = x\r\n");var s=new Session(simple,b);Check(s.Notes.Any(n=>Msg.KeyOf(n)=="note_local_ini_removed")&&s.Overrides["general/limit"]=="9","its values become personal values");s.Commit(true,NoGame);Check(!File.Exists(s.LocalIni)&&new Ini(SafeFiles.Text(s.UserIni)).Get("general","limit")=="9");});


        string statusBuild=MakeBuild(root,"status");File.WriteAllBytes(Path.Combine(statusBuild,"plugins","sample_plugin.dll"),simple.Dll);Write(Path.Combine(statusBuild,"plugins","sample_plugin.ini"),simple.Defaults.Render(new Dictionary<string,string>()));Write(Path.Combine(statusBuild,"tesmioloader.ini"),"[tesmioloader]\nplugins=1\n[plugins]\nsample_plugin=1\n");
        var statusEntry=new CatalogEntry{Id=simple.Id,Name=simple.Name,Root=simple.Root,Supported=true};
        Test("activity status works for unrelated plugin target",()=>Check(RuntimeStatus.ConfiguredActive(statusEntry,statusBuild)));
        Write(Path.Combine(statusBuild,"tesmioloader.ini"),"[tesmioloader]\nplugins=1\n[plugins]\nsample_plugin=0\n");Test("activity status follows generic loader key",()=>Check(!RuntimeStatus.ConfiguredActive(statusEntry,statusBuild)));

        string catalogRoot=Path.Combine(root,"catalog");CopyTree(p.Root,Path.Combine(catalogRoot,"1"));CopyTree(simple.Root,Path.Combine(catalogRoot,"2"));Write(Path.Combine(catalogRoot,"3","soviet.mod.ini"),"[mod]\nid=broken\nname=Broken\n");Write(Path.Combine(catalogRoot,"4","soviet.mod.ini"),"broken file");
        var diagnostics=new List<string>();var catalog=ScanWorkshop(catalogRoot,diagnostics);
        Test("catalog accepts every complete Autoload package",()=>Check(catalog.Count==4&&catalog.Count(x=>x.Supported)==2));
        Test("catalog gives exact rejection reasons",()=>Check(catalog.Where(x=>!x.Supported).All(x=>x.Problem.Contains("catalog_manifest_rejected"))));
        Test("catalog chooses a compatible package",()=>Check(Catalog.RestoreSelection(catalog,"","").Supported));
        string duplicate=Path.Combine(catalogRoot,"5");CopyTree(simple.Root,duplicate);Test("a second copy of the same plugin is passed over and named on the one that loads",()=>{var twice=ScanWorkshop(catalogRoot,new List<string>()).Where(x=>x.Id==simple.Id).ToList();Check(twice.Count==1&&twice[0].Problem.Length==0&&twice[0].Dll.Contains("\\2\\")&&twice[0].PassedOver.Count==1&&twice[0].PassedOver[0].Contains("\\5\\")&&twice[0].Hints.Any(h=>Msg.KeyOf(h)=="hint_same_key"));Check(Catalog.DuplicateDlls(new[]{twice[0]},"").Single().Contains("\\5\\"));});

        string fakeLibrary=Path.Combine(root,"steamlib","steamapps"),fakeBuild=Path.Combine(fakeLibrary,"common","SovietRepublic","tesmioloader","build"),fakeWip=Path.Combine(fakeLibrary,"common","SovietRepublic","media_soviet","workshop_wip","123");Write(Path.Combine(fakeWip,"soviet.mod.ini"),"[mod]\nid=test\nname=Test");
        Test("private WIP path remains usable",()=>Check(Catalog.InitialRoot(fakeBuild,fakeWip)==Path.GetDirectoryName(fakeWip)));
        string profile=Path.Combine(root,"ui","profile.ini");var ui=new UiState{Build=fakeBuild,WorkshopRoot=catalogRoot,SelectedId=simple.Id,SelectedSource=simple.Root};var store=new UiStateStore(profile);store.Load(new UiState(),new List<string>());store.Save(ui);
        Test("UI state is independent and reloadable",()=>Check(new UiStateStore(profile).Load(new UiState(),new List<string>()).SelectedId==simple.Id));

        // ---- 0.9.0: convention over declaration ----
        string plainRoot=Path.Combine(root,"plain-package");Directory.CreateDirectory(Path.Combine(plainRoot,"hooks"));
        File.WriteAllBytes(Path.Combine(plainRoot,"hooks","plain_plugin.dll"),simple.Dll);
        Write(Path.Combine(plainRoot,"hooks","plain_plugin.ini"),"; Plain plugin - header comment.\n; Second header line.\n\n[plain]\n\n; 0 leaves the game alone.\nenabled = 1\n\n; How far, in metres.\n; Second paragraph.\ndistance = 480   ; (stock 480)\n\n; A ratio.\nfactor = 1.5\n\n; auto or a number.\nday_scale = auto\n");
        Write(Path.Combine(plainRoot,"soviet.mod.ini"),"[mod]\nid=example.plain\nname=Plain Plugin\nversion=1.0\nenabled=1\ntesmio_api_min=4\ntesmio_api_max=4\n\n[dependencies]\n; none\n\n[content]\n\n[hooks]\ndll=hooks\\plain_plugin.dll\n");
        Package plain=null;
        Test("manifest without autoload metadata derives target, defaults and user_config",()=>{plain=Package.Load(plainRoot);Check(plain.Target=="plain_plugin"&&plain.ConfigName=="plain_plugin.ini"&&plain.Generic&&plain.HasConfig&&plain.Kind=="plugin"&&plain.SchemaPath==null&&plain.UserOverlay);});
        Test("generic schema infers types and the enabled field",()=>{Check(plain.Fields.Count==4);Check(plain.Fields.Single(f=>f.Key=="enabled").Type=="boolean");Check(plain.Fields.Single(f=>f.Key=="distance").Type=="integer");Check(plain.Fields.Single(f=>f.Key=="factor").Type=="decimal");Check(plain.Fields.Single(f=>f.Key=="day_scale").Type=="text");Check(plain.EnabledField=="plain/enabled"&&plain.Visible);});
        string switchRoot=Path.Combine(root,"switch-package");Directory.CreateDirectory(Path.Combine(switchRoot,"hooks"));File.WriteAllBytes(Path.Combine(switchRoot,"hooks","switchy.dll"),simple.Dll);
        Write(Path.Combine(switchRoot,"hooks","switchy.ini"),"[switchy]\n; Rebuild every connection each time a save is loaded.\nregen_on_load = 1\n; How many calendar days one cycle takes.\n;   1    one sunrise per day\n;   13   the base game\n;   2-12 in between\ncycle_days = 1\n; v1.6-beta: natural deposits inside the country.\ngeneration = 1\n; Frequency 1..6 -> whole regions.\ngeneration_frequency = 1\n; Optional read-only surface. 0 restores native visuals.\nsand_surface = 1\nretry_count = 0\n");
        Write(Path.Combine(switchRoot,"soviet.mod.ini"),"[mod]\nid=example.switchy\nname=Switchy\nversion=1\n[hooks]\ndll=hooks\\switchy.dll\n");
        Test("0/1 values become switches unless a quantity is implied",()=>{Package sw=Package.Load(switchRoot);Func<string,string> t=k=>sw.Fields.Single(f=>f.Key==k).Type;Check(t("regen_on_load")=="boolean"&&t("generation")=="boolean"&&t("sand_surface")=="boolean");Check(t("cycle_days")=="integer"&&t("generation_frequency")=="integer"&&t("retry_count")=="integer");});
        Test("generic schema takes descriptions from INI comments",()=>{Field d=plain.Fields.Single(f=>f.Key=="distance");Check(d.Description.Contains("How far")&&d.Description.Contains("Second paragraph")&&d.Description.Contains("stock 480"));Check(plain.Schema.Get("launcher","description","").Contains("header comment"));});
        Test("inline comments do not break generic defaults",()=>{Field d=plain.Fields.Single(f=>f.Key=="distance");Check(d.Normalize(d.DefaultValue)=="480"&&plain.IsDefault(d.Id,"480")&&!plain.IsDefault(d.Id,"481"));});
        string plainBuild=MakeBuild(root,"plain-build");
        Test("generic package saves through the same transaction",()=>{var s=new Session(plain,plainBuild);s.Set(plain.Fields.Single(f=>f.Key=="distance"),"900");s.Set(plain.Fields.Single(f=>f.Key=="day_scale"),"13");s.Commit(true,NoGame);var effective=new Ini(s.Effective());Check(effective.Get("plain","distance")=="900"&&effective.Get("plain","day_scale")=="13"&&effective.Get("plain","enabled")=="1");Check(s.LocalDll==plain.DllPath&&!File.Exists(s.LocalIni)&&SafeFiles.Text(s.UserIni).Contains("distance = 900")&&SafeFiles.Text(plain.DefaultsPath).Contains("How far"));});
        string plainBuild2=MakeBuild(root,"plain-build-2");Write(Path.Combine(plainBuild2,"plugins","plain_plugin.ini"),"[plain]\nenabled = 0\ndistance = 480\nfactor = 1.5\nday_scale = auto\n[extra]\nfoo = 1\n");
        Test("generic package imports a local INI with extra keys instead of rejecting it",()=>{var s=new Session(plain,plainBuild2);Check(s.Overrides["plain/enabled"]=="0"&&s.Notes.Any(n=>Msg.KeyOf(n)=="note_first_import"));});
        string overlayRoot=Path.Combine(root,"overlay-package");CopyTree(plainRoot,overlayRoot);
        Write(Path.Combine(overlayRoot,"soviet.mod.ini"),"[mod]\nid=example.overlay\nname=Overlay Plugin\nversion=1.0\n[hooks]\ndll=hooks\\plain_plugin.dll\n[configuration]\nuser_overlay=1\n");
        string overlayBuild=MakeBuild(root,"overlay-build");
        Test("the package's original INI stays byte-identical and no copy is made",()=>{Package o=Package.Load(overlayRoot);Check(o.UserOverlay);var s=new Session(o,overlayBuild);s.Set(o.Fields.Single(f=>f.Key=="distance"),"777");s.Commit(true,NoGame);Check(!File.Exists(s.LocalIni)&&File.ReadAllBytes(o.DefaultsPath).SequenceEqual(o.DefaultsBytes));Check(new Ini(SafeFiles.Text(s.UserIni)).Get("plain","distance")=="777");Check(s.Notes.Any(n=>Msg.KeyOf(n)=="note_overlay"));var again=new Session(o,overlayBuild);Check(again.Overrides["plain/distance"]=="777");});
        // 0.6.2: a plugin that opens its INI by path never sees user_config; its package says so and
        // gets the finished INI in <loader>\plugins, where such a plugin looks first.
        string effectiveRoot=Path.Combine(root,"effective-package");CopyTree(plainRoot,effectiveRoot);
        Write(Path.Combine(effectiveRoot,"soviet.mod.ini"),"[mod]\nid=example.effective\nname=Effective Plugin\nversion=1.0\n[hooks]\ndll=hooks\\plain_plugin.dll\n[configuration]\neffective_ini=1\n");
        string effectiveBuild=MakeBuild(root,"effective-build");
        Test("a package with an effective INI gets the finished INI in plugins and loses it with its last personal value",()=>{
            Package e=Package.Load(effectiveRoot);Check(e.EffectiveIni&&e.Hints.Any(h=>Msg.KeyOf(h)=="hint_effective")&&!e.Hints.Any(h=>Msg.KeyOf(h)=="hint_overlay"),"the manifest key is read");
            var s=new Session(e,effectiveBuild);Check(s.Notes.Any(n=>Msg.KeyOf(n)=="note_effective"));
            s.Commit(true,NoGame);Check(!File.Exists(s.LocalIni)&&!File.Exists(s.UserIni),"nothing is written without a personal value");
            s.Set(e.Fields.Single(f=>f.Key=="distance"),"777");s.Commit(true,NoGame);
            Check(File.Exists(s.LocalIni)&&new Ini(SafeFiles.Text(s.LocalIni)).Get("plain","distance")=="777"&&SafeFiles.Text(s.LocalIni).Contains("How far"),"the finished INI carries the value and the author's comments");
            Check(new Ini(SafeFiles.Text(s.UserIni)).Get("plain","distance")=="777"&&File.ReadAllBytes(e.DefaultsPath).SequenceEqual(e.DefaultsBytes),"the personal value is stored, the original untouched");
            Check(new Ini(SafeFiles.Text(s.Receipt)).Get("state","ini_hash","")==SafeFiles.HashFile(s.LocalIni)&&new Ini(SafeFiles.Text(s.Receipt)).Get("state","mode","")=="workshop","the receipt names the file it wrote");
            var again=new Session(Package.Load(effectiveRoot),effectiveBuild);
            Check(again.Overrides["plain/distance"]=="777"&&!again.Notes.Any(n=>{string k=Msg.KeyOf(n);return k=="note_local_ini_shadows"||k=="note_local_ini_removed"||k=="note_first_import";}),"its own file is no leftover");
            again.Overrides.Remove("plain/distance");again.Commit(true,NoGame);
            Check(!File.Exists(again.LocalIni),"without a personal value the plugin reads the author's INI again");
            Write(Path.Combine(effectiveRoot,"soviet.mod.ini"),"[mod]\nid=example.effective\nname=Effective Plugin\nversion=1.0\n[hooks]\ndll=hooks\\plain_plugin.dll\n[configuration]\neffective_ini=yes\n");
            bool refused=false;try{Package.Load(effectiveRoot);}catch(FormatException){refused=true;}Check(refused,"any value but 0 or 1 is refused");
        });
        string contentRoot=Path.Combine(root,"content-package");Write(Path.Combine(contentRoot,"tesmio","resources.ini"),"[list]\ncount = 0\n");Write(Path.Combine(contentRoot,"tesmio","deposits.ini"),"[gas]\ntoken = $TYPE_MINE_GAS\ntype = 20\n");Write(Path.Combine(contentRoot,"tesmio","buildings.ini"),"[gas_well]\ndonor = oil_well\n");Write(Path.Combine(contentRoot,"assets","media_soviet","resources","gas.png"),"png");
        Write(Path.Combine(contentRoot,"soviet.mod.ini"),"[mod]\nid = tesmioloader.naturalgas\nname = Natural Gas Industry\nversion = 1.0.1\nenabled = 1\npriority = 0\ntesmio_api_min = 4\ntesmio_api_max = 4\n\n; Dependencies are mod id = semantic-version constraint.\n[dependencies]\n; org.example.shared-library = >=1.2.0\n\n[content]\nresources = tesmio\\resources.ini\ndeposits = tesmio\\deposits.ini\nbuildings = tesmio\\buildings.ini\nassets = assets\n\n[hooks]\n; dll = hooks\\simple_hook.dll\n");
        // 0.4.83: a content package carries no DLL, so it has no target and no config name; the check has
        // to skip it instead of building the invalid relative path "plugins\" out of the empty name.
        Test("the consistency check skips content packages instead of building an invalid path",()=>{var mixed=new[]{new CatalogEntry{Root=contentRoot,Id="tesmioloader.naturalgas",Name="Natural Gas Industry",Version="1.0.1",Supported=true}};Check(ResourceConsistency.ValidateReferences(resourceBuild,mixed).Count==0);});
        // 0.6.3: the start check took the list of whatever list editor was open for the resource
        // list - with Buildings Plus or Needs Plus open, every resource a plugin names was "missing".
        Test("only the editor of the plugin that takes resources speaks for the resource list",()=>{
            string provider=ContentTargets.Resolve(resourceBuild,"resources");if(provider.Length==0)provider="resources";
            Check(ResourceConsistency.SpeaksForResources(resourceBuild,provider)&&ResourceConsistency.SpeaksForResources(resourceBuild,provider.ToUpperInvariant()),"the provider's own editor does");
            foreach(string other in new[]{"needs_plus","needs","deposits_plus","deposits","buildings_plus","traffic_rules","vanilla_buildings",""})
                if(!other.Equals(provider,StringComparison.OrdinalIgnoreCase)&&!(provider=="resources_plus"&&other=="resources_plus")) Check(!ResourceConsistency.SpeaksForResources(resourceBuild,other),"no other list editor does: ["+other+"]");
            Check(!ResourceConsistency.SpeaksForResources(resourceBuild,null),"and no editor at all does not either");
            Check(!ResourceConsistency.SpeaksForResources(resourceBuild,provider=="resources"?"resources_plus":"resources"),"the plugin that is not in charge does not");
        });
        string multiRoot=Path.Combine(root,"multi-package");CopyTree(plainRoot,multiRoot);Write(Path.Combine(multiRoot,"soviet.mod.ini"),"[mod]\nid=example.multi\nname=Multi\nversion=1\n[hooks]\ndll=hooks\\plain_plugin.dll\ndll=hooks\\second.dll\n");
        Test("repeated hook DLLs are refused with a precise reason",()=>{string reason="";try{Package.Load(multiRoot);}catch(Exception e){reason=Msg.Plain(e.Message);}Check(reason.Contains("err_mehrere_native_hooks"));});
        string mismatchRoot=Path.Combine(root,"mismatch-package");CopyTree(plainRoot,mismatchRoot);Write(Path.Combine(mismatchRoot,"soviet.mod.ini"),"[mod]\nid=example.mismatch\nname=Mismatch\nversion=1\n[hooks]\ndll=hooks\\plain_plugin.dll\n[autoload]\nformat=1\nkind=plugin\ntarget=other_name\n");
        string dllOnlyRoot=Path.Combine(root,"dllonly-package");Directory.CreateDirectory(Path.Combine(dllOnlyRoot,"hooks"));File.WriteAllBytes(Path.Combine(dllOnlyRoot,"hooks","bare_plugin.dll"),simple.Dll);Write(Path.Combine(dllOnlyRoot,"soviet.mod.ini"),"[mod]\nid=example.bare\nname=Bare\nversion=1\n[hooks]\ndll=hooks\\bare_plugin.dll\n");
        string bareBuild=MakeBuild(root,"bare-build");
        Test("DLL-only package deploys the DLL and no configuration files",()=>{Package bare=Package.Load(dllOnlyRoot);Check(!bare.HasConfig&&bare.Fields.Count==0&&bare.Visible);var s=new Session(bare,bareBuild);s.Commit(true,NoGame);Check(File.Exists(s.LocalDll)&&!File.Exists(s.LocalIni)&&!File.Exists(s.UserIni)&&File.Exists(s.Receipt));Check(new Session(bare,bareBuild).Notes.Any(n=>Msg.KeyOf(n)=="note_no_ini"));});
        Test("existing schema packages are unchanged by the convention rules",()=>Check(!simple.Generic&&simple.SchemaPath!=null&&simple.Fields.Count==3&&simple.HasConfig));
        Test("generic schema is a regular launcher schema on the application tab",()=>{Ini s=plain.Schema;Check(s.Get("launcher","enabled_field")=="plain/enabled"&&s.Get("launcher","visible")=="1"&&s.Get("launcher","config")=="plain_plugin.ini");Check(s.Sections.Contains("group:plain")&&s.Get("group:plain","tab")=="settings"&&!s.Sections.Any(x=>x.StartsWith("tab:")));Check(s.Sections.Count(x=>x.StartsWith("field:"))==4&&s.Get("field:g2","type")=="integer"&&s.Get("field:g2","group")=="plain");});

        // ---- 0.10.0: installed plugins without a package (protected base) ----
        string schemaRoot=Path.Combine(root,"schemas");Directory.CreateDirectory(schemaRoot);Catalog.SchemaRoot=schemaRoot;
        string instBuild=MakeBuild(root,"installed-build");string instPlugins=Path.Combine(instBuild,"plugins");
        File.WriteAllBytes(Path.Combine(instPlugins,"walkalike.dll"),simple.Dll);
        Write(Path.Combine(instPlugins,"walkalike.ini"),"; walkalike - how far.\n[walkalike]\n; 0 leaves the game alone.\nenabled = 1\n; metres\ndistance = 480\n");
        File.WriteAllBytes(Path.Combine(instPlugins,"orphan.dll"),simple.Dll);
        File.WriteAllBytes(Path.Combine(instPlugins,"sample_plugin.dll"),simple.Dll);Write(Path.Combine(instPlugins,"sample_plugin.ini"),"[general]\nenabled = 1\nlimit = 5\nmode = normal\n");
        Write(Path.Combine(instBuild,"tesmioloader.log"),"[01:00:00.000] plugin   walkalike        3.4      from walkalike.dll\n");
        var known=ScanWorkshop(catalogRoot,new List<string>());
        Test("installed plugins are discovered from build plugins without a package",()=>{var installed=ScanInstalled(instBuild,known,new List<string>());Check(installed.Count==3&&installed.All(e=>e.Installed&&e.Supported));var w=installed.Single(e=>e.Target=="walkalike");Check(w.Version=="3.4"&&w.Status=="Installiert"&&w.Id=="local.walkalike"&&w.Root.EndsWith("walkalike.dll")&&w.Key=="walkalike"&&w.Dll==w.Root);Check(installed.Any(e=>e.Target=="sample_plugin"),"a DLL in plugins is listed even when a package has the same key");});
        Test("orphan DLL without INI is listed with nothing to configure",()=>{Package o=InstalledPlugins.Load(instBuild,"orphan",schemaRoot);Check(!o.HasConfig&&o.Fields.Count==0&&o.Installed&&o.Version=="installiert");});
        Test("installed plugin first save protects the original and never writes the DLL",()=>{Package walk=InstalledPlugins.Load(instBuild,"walkalike",schemaRoot);Check(walk.Installed&&walk.Generic&&walk.RefreshUpstream&&walk.Fields.Count==2);string dllHash=SafeFiles.HashFile(Path.Combine(instPlugins,"walkalike.dll"));var s=new Session(walk,instBuild);s.Set(walk.Fields.Single(f=>f.Key=="distance"),"900");s.Commit(true,NoGame);Check(File.Exists(s.Upstream)&&File.ReadAllBytes(s.Upstream).SequenceEqual(walk.DefaultsBytes));Check(new Ini(SafeFiles.Text(s.LocalIni)).Get("walkalike","distance")=="900"&&SafeFiles.HashFile(s.LocalDll)==dllHash);Check(new Ini(SafeFiles.Text(s.Receipt)).Get("state","mode")=="installed");});
        Test("reopened installed plugin uses the protected original as its defaults",()=>{Package again=InstalledPlugins.Load(instBuild,"walkalike",schemaRoot);Check(!again.RefreshUpstream&&again.DefaultsPath.EndsWith("walkalike.upstream.ini"));var s=new Session(again,instBuild);Check(s.Overrides["walkalike/distance"]=="900"&&again.IsDefault("walkalike/distance","480"));});
        Test("external INI edit becomes the new original with personal values re-applied",()=>{Write(Path.Combine(instPlugins,"walkalike.ini"),"; walkalike v2.\n[walkalike]\n; 0 leaves the game alone.\nenabled = 1\n; metres\ndistance = 500\n; new key\ncar_distance = 2500\n");Package v2=InstalledPlugins.Load(instBuild,"walkalike",schemaRoot);Check(v2.RefreshUpstream&&v2.Fields.Count==3&&v2.IsDefault("walkalike/distance","500"));var s=new Session(v2,instBuild);Check(s.Overrides["walkalike/distance"]=="900");s.Commit(true,NoGame);Check(File.ReadAllBytes(s.Upstream).SequenceEqual(v2.DefaultsBytes));var local=new Ini(SafeFiles.Text(s.LocalIni));Check(local.Get("walkalike","distance")=="900"&&local.Get("walkalike","car_distance")=="2500");});
        Test("restore original writes the protected copy back and clears personal values",()=>{Package v2=InstalledPlugins.Load(instBuild,"walkalike",schemaRoot);var s=new Session(v2,instBuild);s.RestoreOriginal(NoGame);Check(File.ReadAllBytes(s.LocalIni).SequenceEqual(File.ReadAllBytes(s.Upstream))&&s.Overrides.Count==0&&!SafeFiles.Text(s.UserIni).Contains("distance"));Check(new Session(InstalledPlugins.Load(instBuild,"walkalike",schemaRoot),instBuild).Overrides.Count==0);});
        Write(Path.Combine(schemaRoot,"walkalike.launcher.ini"),"[launcher]\nlayout_version=1\nvisible=1\nid=local.walk.schema\nname=Walk Alike\nconfig=walkalike.ini\nenabled_field=walkalike/enabled\n[field:enabled]\nsection=walkalike\nkey=enabled\nlabel=Enabled\ntype=boolean\ngroup=main\n[field:distance]\nsection=walkalike\nkey=distance\nlabel=Distance\ntype=integer\nminimum=0\nmaximum=5000\ngroup=main\n[field:car]\nsection=walkalike\nkey=car_distance\nlabel=Car\ntype=integer\nminimum=0\nmaximum=9000\ngroup=main\n[tab:settings]\nlabel=Settings\n[group:main]\ntab=settings\nlabel=Main\n");
        Test("a local schema in settings_schemas takes precedence over the INI-derived one",()=>{Package w=InstalledPlugins.Load(instBuild,"walkalike",schemaRoot);Check(!w.Generic&&w.Id=="local.walk.schema"&&w.Name=="Walk Alike"&&w.Fields.Single(f=>f.Key=="distance").Maximum==5000);var s=new Session(w,instBuild);Reject(()=>s.Set(w.Fields.Single(f=>f.Key=="distance"),"6000"));});
        Test("keyed editor schemas are not mistaken for plugin schemas",()=>{Write(Path.Combine(schemaRoot,"orphan.launcher.ini"),"[launcher]\neditor_type=keyed_resources\nlayout_version=1\nid=x\nname=X\nconfig=orphan.ini\n[editor]\nplugin=orphan\nconfig=orphan.ini\n");Package o=InstalledPlugins.Load(instBuild,"orphan",schemaRoot);Check(o.Generic&&o.SchemaPath==null);Check(Catalog.ScanLocalEditors(schemaRoot,new List<string>()).Count(e=>e.Root.EndsWith("walkalike.launcher.ini"))==0);});
        // ---- 0.11.0: SML as a neighbour, dependencies ----
        string smlBuild=MakeBuild(root,"sml-build");File.WriteAllBytes(Path.Combine(smlBuild,"plugins","soviet_mod_loader.dll"),simple.Dll);Write(Path.Combine(smlBuild,"tesmioloader.ini"),"[tesmioloader]\nplugins=1\n[plugins]\nsoviet_mod_loader=1\n");
        Write(Path.Combine(smlBuild,"tesmioloader.dll"),"fake");Write(Path.Combine(smlBuild,"tesmiolauncher.exe"),"fake");
        string smlBuild2=MakeBuild(root,"sml-build-2");File.WriteAllBytes(Path.Combine(smlBuild2,"plugins","soviet_mod_loader.dll"),simple.Dll);Write(Path.Combine(smlBuild2,"tesmioloader.ini"),"[tesmioloader]\nplugins=1\n[plugins]\nsoviet_mod_loader=1\n");
        // 0.5.11: the fork carries the same service, so requires_local = resources is met by resources_plus.dll.
        Test("a needed plugin is also provided by the plugin that replaces it",()=>{string forkBuild=MakeBuild(root,"fork-requires");File.WriteAllBytes(Path.Combine(forkBuild,"plugins","resources_plus.dll"),simple.Dll);Write(Path.Combine(forkBuild,"plugins","resources_plus.ini"),"[resources]\nhook = 2\n[list]\nunobtainium = aluminium, Unobtainium\n");Write(Path.Combine(forkBuild,"tesmioloader.ini"),"[tesmioloader]\nplugins=1\n[plugins]\nresources_plus=1\n");Check(p.Dependencies.Any(d=>d.Id=="resources"),"an older manifest's requires_local is a dependency");LoaderFiles.Forget();var found=Catalog.ScanAll(forkBuild,"",Path.GetDirectoryName(localSchema),new List<string>());var fork=found.Single(e=>e.Key=="resources_plus");Check(fork.LocalEditor&&fork.Replaces=="resources","the keyed editor stands for the fork and knows what it replaces");var need=new Dependency{Id="resources"};Catalog.ResolveDependencies(new List<Dependency>{need},found,forkBuild);Check(need.Found&&need.Target=="resources_plus"&&need.VersionOk&&RuntimeGuard.Provisioned(forkBuild,need),"met by the replacement");Write(Path.Combine(forkBuild,"tesmioloader.ini"),"[tesmioloader]\nplugins=1\n[plugins]\nresources_plus=0\n");Check(!RuntimeGuard.Provisioned(forkBuild,need),"a switched-off replacement provides nothing");});
        // A Workshop folder of its own: the provider and a plugin that depends on it, each with its own DLL name.
        string depShop=Path.Combine(root,"dep-shop"),depRoot=Path.Combine(depShop,"dependent-package");CopyTree(plainRoot,Path.Combine(depShop,"plain-package"));CopyTree(plainRoot,depRoot);File.Move(Path.Combine(depRoot,"hooks","plain_plugin.dll"),Path.Combine(depRoot,"hooks","dependent_plugin.dll"));File.Move(Path.Combine(depRoot,"hooks","plain_plugin.ini"),Path.Combine(depRoot,"hooks","dependent_plugin.ini"));Write(Path.Combine(depRoot,"soviet.mod.ini"),"[mod]\nid=example.dependent\nname=Dependent\nversion=1.0\n[dependencies]\nexample.plain = >=1.0\nexample.missing = >=2.0\n[hooks]\ndll=hooks\\dependent_plugin.dll\n");
        Test("a dependency is satisfied by a plugin installed the classic way",()=>{var dep=new List<Dependency>{new Dependency{Id="tesmio.walkalike",Constraint=">=1.0"},new Dependency{Id="tesmio.nothere",Constraint=""}};var installed=ScanInstalled(instBuild,known,new List<string>());Catalog.ResolveDependencies(dep,installed);Check(dep[0].Found&&dep[0].Target=="walkalike"&&dep[0].Kind=="plugin"&&dep[0].VersionOk&&Msg.Plain(dep[0].Note).Contains("walkalike")&&!dep[1].Found);});
        Test("catalog re-resolution drops the missing hint once an installed plugin covers the dependency",()=>{var list=new List<CatalogEntry>{new CatalogEntry{Id="x.needs",Name="needs",Root="r",Supported=true}};list[0].Dependencies.Add(new Dependency{Id="tesmio.walkalike",Constraint=""});list[0].Hints.Add(Msg.Key("dep_missing","tesmio.walkalike"));list.AddRange(ScanInstalled(instBuild,known,new List<string>()));Catalog.ResolveAll(list);Check(list[0].Dependencies[0].Found&&!list[0].Hints.Any(h=>Msg.Plain(h).Contains("tesmio.walkalike")));});
        Test("version constraints compare dotted numbers and ignore suffixes",()=>{Check(VersionRule.Satisfied(">=1.2.0","1.10.0-beta")&&!VersionRule.Satisfied(">=1.2.0","1.1.9")&&VersionRule.Satisfied("=2.1","2.1.0")&&VersionRule.Satisfied("*","x")&&VersionRule.Satisfied("","1")&&VersionRule.Satisfied("<2","1.9.9")&&!VersionRule.Satisfied(">1.0","1.0")&&VersionRule.Satisfied("1.0","v1.0.1")&&!VersionRule.Satisfied(">=1","?"));});
        // ---- 0.12.0: loader switch, duplicate DLLs, game version ----
        string loaderBuild=MakeBuild(root,"loader-build");Write(Path.Combine(loaderBuild,"tesmioloader.ini"),"; launcher file with comments\n[tesmioloader]\ngame_exe=C:\\x\\SOVIET64.exe\nplugins=1\n[plugins]\nplain_plugin=1\nother=0\n");
        Test("loader switch is written into tesmioloader.ini preserving comments",()=>{var s=new Session(plain,loaderBuild);Check(s.LoaderSwitchApplies&&s.LoaderEnabled&&!s.LoaderChanged);s.SetLoaderEnabled(false);Check(s.LoaderChanged);s.Commit(false,NoGame);string text=SafeFiles.Text(Path.Combine(loaderBuild,"tesmioloader.ini"));Check(text.Contains("; launcher file with comments")&&text.Contains("other=0")&&text.Contains("game_exe=C:\\x\\SOVIET64.exe"));Check(new Ini(text).Get("plugins","plain_plugin")=="0"&&File.ReadAllBytes(Path.Combine(loaderBuild,"tesmioloader.ini"))[0]!=0xEF);var again=new Session(plain,loaderBuild);Check(!again.LoaderEnabled&&!again.LoaderChanged);again.SetLoaderEnabled(true);again.Commit(false,NoGame);Check(new Ini(SafeFiles.Text(again.LoaderIni)).Get("plugins","plain_plugin")=="1");});
        Test("loader switch creates tesmioloader.ini when absent and applies to installed plugins as well",()=>{string fresh=MakeBuild(root,"loader-build-2");var s=new Session(plain,fresh);Check(s.LoaderEnabled&&s.SwitchMode=="loader");s.SetLoaderEnabled(false);s.Commit(false,NoGame);Check(new Ini(SafeFiles.Text(s.LoaderIni)).Get("plugins","plain_plugin")=="0");File.WriteAllBytes(Path.Combine(fresh,"plugins","localonly.dll"),simple.Dll);var local=new Session(InstalledPlugins.Load(fresh,"localonly",schemaRoot),fresh);Check(local.LoaderSwitchApplies&&local.LoaderEnabled&&local.LocalDll.EndsWith("plugins\\localonly.dll"));});
        Test("game version comes from the PE time stamp and a known table",()=>{string vb=MakeBuild(root,"version-build");string exe=Path.Combine(root,"fake-game","SOVIET64.exe");Directory.CreateDirectory(Path.GetDirectoryName(exe));Func<uint,byte[]> pe=stamp=>{var b=new byte[0x100];b[0]=(byte)'M';b[1]=(byte)'Z';BitConverter.GetBytes(0x80).CopyTo(b,60);b[0x80]=(byte)'P';b[0x81]=(byte)'E';BitConverter.GetBytes(stamp).CopyTo(b,0x88);return b;};File.WriteAllBytes(exe,pe(0x6B05A574u));Write(Path.Combine(vb,"tesmioloader.ini"),"[tesmioloader]\ngame_exe="+exe+"\n");bool ok;string text=GameVersion.Describe(vb,out ok);Check(ok&&Msg.KeyOf(text)=="game_version_supported"&&Msg.Plain(text).Contains("1.2.0.9"));File.WriteAllBytes(exe,pe(0x6B0097B2u));text=GameVersion.Describe(vb,out ok);Check(!ok&&Msg.KeyOf(text)=="game_version_retired"&&Msg.Plain(text).Contains("1.2.0.8"));File.WriteAllBytes(exe,pe(0x6A8D4087u));text=GameVersion.Describe(vb,out ok);Check(!ok&&Msg.KeyOf(text)=="game_version_retired"&&Msg.Plain(text).Contains("1.1.1.13"));File.WriteAllBytes(exe,pe(0x6A3EB6ADu));text=GameVersion.Describe(vb,out ok);Check(!ok&&Msg.KeyOf(text)=="game_version_retired"&&Msg.Plain(text).Contains("1.1.1.9"));File.WriteAllBytes(exe,pe(0x12345678u));text=GameVersion.Describe(vb,out ok);Check(!ok&&Msg.KeyOf(text)=="game_version_unknown_stamp"&&Msg.Plain(text).Contains("0x12345678"));File.WriteAllBytes(exe,pe(0x69C4098Cu));text=GameVersion.Describe(vb,out ok);Check(!ok&&Msg.KeyOf(text)=="game_version_retired"&&Msg.Plain(text).Contains("1.1.1.7"));Write(Path.Combine(schemaRoot,"game_versions.ini"),"[supported]\n12345678 = 9.9.9\n");GameVersion.LoadTable(schemaRoot);File.WriteAllBytes(exe,pe(0x12345678u));text=GameVersion.Describe(vb,out ok);Check(ok&&Msg.Plain(text).Contains("9.9.9"));Check(GameVersion.Stamp(Path.Combine(root,"fake-game","missing.exe"))==0);string warnText;Check(!GameVersion.Warn(MakeBuild(root,"no-exe-build"),out warnText)&&Msg.KeyOf(warnText)=="game_exe_missing");File.WriteAllBytes(exe,pe(0x01020304u));Check(GameVersion.Warn(vb,out warnText)&&Msg.KeyOf(warnText)=="game_version_unknown_stamp");});
        Test("activity status works for an installed plugin",()=>{var e=ScanInstalled(instBuild,known,new List<string>()).Single(x=>x.Target=="walkalike");Check(RuntimeStatus.ConfiguredActive(e,instBuild));Write(Path.Combine(instBuild,"tesmioloader.ini"),"[tesmioloader]\nplugins=1\n[plugins]\nwalkalike=0\n");Check(!RuntimeStatus.ConfiguredActive(e,instBuild));});

        // ---- 0.14.0: workshop bridge ----
        string bridgeBuild=MakeBuild(root,"bridge-build");File.WriteAllBytes(Path.Combine(bridgeBuild,"plugins","workshop_bridge.dll"),simple.Dll);Write(Path.Combine(bridgeBuild,"plugins","workshop_bridge.ini"),"[bridge]\n; 0 = idle\nenabled = 1\n; list or all\npolicy = list\n[packages]\n");Write(Path.Combine(bridgeBuild,"tesmioloader.dll"),"fake");Write(Path.Combine(bridgeBuild,"tesmiolauncher.exe"),"fake");
        // ---- 0.22.0: editor schema shipped in a package, cards, links, origin icons ----
        Test("a package with a keyed_sections schema in config is editor-managed",()=>{string peRoot=Path.Combine(root,"pkg-editor");File.WriteAllBytes(Path.Combine(Directory.CreateDirectory(Path.Combine(peRoot,"hooks")).FullName,"pe_plugin.dll"),simple.Dll);Write(Path.Combine(peRoot,"hooks","pe_plugin.ini"),"; pe upstream\n[pe_plugin]\ncode_patch = 1\nsand = 1\n\n[copper]\ntoken = $TYPE_MINE_COPPER\ntype = 10\n");Write(Path.Combine(peRoot,"README_DE.md"),"# guide\n");Write(Path.Combine(peRoot,"soviet.mod.ini"),"[mod]\nid=example.pe\nname=Pkg Editor\nversion=2.0\nenabled=1\n[hooks]\ndll=hooks\\pe_plugin.dll\n");Write(Path.Combine(peRoot,"config","pe_plugin.launcher.ini"),"[launcher]\nlayout_version=1\neditor_type=keyed_sections\nid=example.pe\nname=Pkg Editor\ndefault_tab=deposits\n[editor]\nplugin=pe_plugin\nconfig=pe_plugin.ini\nlist_section=list\nreserved_sections=pe_plugin\nmaximum_items=20\n[tab:general]\nlabel=General\norder=10\n[tab:sand]\nlabel=Sand\norder=20\n[tab:deposits]\nlabel=Deposits\norder=30\n[group:deposits]\ntab=deposits\nlabel=Deposits\n[global]\ntab=general\nlabel=Plugin\n[card:surface]\ntab=sand\nlabel=Surface\n[links]\ntab=general\nroot=..\n[link:readme]\nfile=README_DE.md\nlabel=Readme\n[detail:code_patch]\nscope=global\nsection=pe_plugin\nkey=code_patch\ntype=boolean\n[detail:sand]\nscope=global\ncard=surface\nsection=pe_plugin\nkey=sand\ntype=boolean\n[detail:token]\nscope=item\nkey=token\ntype=text\ndialog=1\n[detail:type]\nscope=item\nkey=type\ntype=integer\nminimum=10\nmaximum=127\nunique=1\nauto_increment=1\ndialog=1\n");
            Package pe=Package.Load(peRoot);Check(pe.EditorManaged&&pe.EditorSchema.EndsWith("pe_plugin.launcher.ini")&&pe.SchemaPath==null&&pe.Generic&&pe.HasConfig);
            LocalEditorSpec peSpec=LocalEditorSpec.Load(pe.EditorSchema);Check(peSpec.Tabs.Count==3&&peSpec.Cards.Count==1&&peSpec.Cards[0].Tab=="sand"&&peSpec.AllCards().Count==2&&peSpec.Fields.Single(x=>x.Id=="sand").Card=="surface"&&peSpec.Links.Count==1&&peSpec.LinksTab=="general"&&File.Exists(peSpec.LinkPath(peSpec.Links[0])));
            string peBuild=MakeBuild(root,"pkg-editor-build");Write(Path.Combine(peBuild,"tesmioloader.dll"),"fake");Write(Path.Combine(peBuild,"tesmiolauncher.exe"),"fake");
            var pkg=new Session(pe,peBuild);var editor=new LocalResourceSession(peSpec,peBuild,pe);Check(editor.ExternalLoader&&editor.Items().Count==1&&editor.Items()[0].Id=="copper"&&!editor.Dirty&&editor.Notes.Any(n=>Msg.KeyOf(n)=="res_note_package_base"));
            editor.AddSection("glass",new Dictionary<string,string>{{"token","$TYPE_MINE_GLASS"},{"type","11"}});editor.SetGlobal(peSpec.Fields.Single(x=>x.Id=="sand"),"0");
            pkg.SetLoaderEnabled(true);pkg.Commit(true,NoGame);editor.Commit(NoGame);
            var doc=new LooseIni(SafeFiles.Text(Path.Combine(peBuild,"plugins","pe_plugin.ini")));Check(!File.Exists(Path.Combine(peBuild,"plugins","pe_plugin.dll"))&&doc.Get("glass","token")=="$TYPE_MINE_GLASS"&&doc.Get("pe_plugin","sand")=="0"&&doc.Get("copper","type")=="10"&&SafeFiles.Text(Path.Combine(peBuild,"plugins","pe_plugin.ini")).Contains("; pe upstream"));
            // Session never rewrites the editor's INI, and it reopens without complaining about it.
            var again=new Session(pe,peBuild);again.Commit(true,NoGame);Check(new LooseIni(SafeFiles.Text(Path.Combine(peBuild,"plugins","pe_plugin.ini"))).Get("glass","token")=="$TYPE_MINE_GLASS"&&!File.Exists(Path.Combine(peBuild,"user_config","pe_plugin.ini")));
            var reopened=new LocalResourceSession(peSpec,peBuild,pe);Check(reopened.Items().Count==2&&reopened.Items().Single(x=>x.Id=="glass").Owned&&reopened.GlobalValue(peSpec.Fields.Single(x=>x.Id=="sand"))=="0"&&!reopened.Dirty);
            var e=ScanWorkshop(root,new List<string>()).Single(x=>x.Root.Equals(peRoot,StringComparison.OrdinalIgnoreCase));Check(RuntimeStatus.ConfiguredActive(e,peBuild)&&e.Origin=="unknown");});
        Test("list icons follow the origin: loader folder, Steam workshop, unknown",()=>{Check(ScanInstalled(instBuild,known,new List<string>()).All(x=>x.Origin=="loader"));Check(Catalog.ScanLocalEditors(schemaRoot,new List<string>()).All(x=>x.Origin=="loader"));Check(ScanWorkshop(catalogRoot,new List<string>()).All(x=>x.Origin=="unknown"));});
        Test("a package editor schema whose plugin name differs from the DLL is rejected",()=>{string bad=Path.Combine(root,"pkg-editor-bad");File.WriteAllBytes(Path.Combine(Directory.CreateDirectory(Path.Combine(bad,"hooks")).FullName,"other.dll"),simple.Dll);Write(Path.Combine(bad,"hooks","other.ini"),"[x]\na = 1\n");Write(Path.Combine(bad,"soviet.mod.ini"),"[mod]\nid=example.bad2\nname=Bad\nversion=1\nenabled=1\n[hooks]\ndll=hooks\\other.dll\n");Write(Path.Combine(bad,"config","other.launcher.ini"),"[launcher]\nlayout_version=1\neditor_type=keyed_sections\nid=example.bad2\nname=Bad\n[editor]\nplugin=mismatch\nconfig=other.ini\nlist_section=list\nmaximum_items=5\n[detail:a]\nscope=item\nkey=a\ntype=text\n");Package b=Package.Load(bad);Check(b.EditorManaged);Reject(()=>new LocalResourceSession(LocalEditorSpec.Load(b.EditorSchema),MakeBuild(root,"pkg-editor-bad-build"),b));});
        Test("a second section group with its own prefix lists, adds and renders apart (0.4.29)",()=>{string dir=Path.Combine(root,"multi-group");string schema=Path.Combine(dir,"config","multi.launcher.ini");Write(schema,"[launcher]\nlayout_version=1\neditor_type=keyed_sections\nid=x.multi\nname=Multi\nversion=1\ndefault_tab=modify\n[editor]\nplugin=multi\nconfig=multi.ini\nreserved_sections=general\nsection_prefix=modify:\nmaximum_items=256\n[tab:general]\nlabel=General\norder=10\n[tab:modify]\nlabel=Edits\norder=20\n[tab:research]\nlabel=Research\norder=30\n[group:research]\ntab=research\nsection_prefix=research:\nlabel=New research\n[list:research]\nlabel=New research\nsummary=type|cost\nmaximum_items=2\n[new:research]\nname_label=Id\n[group:modify]\ntab=modify\nlabel=Edits\n[list]\nlabel=Edits\nsummary=add\n[new]\nname_label=Research id\n[global]\ntab=general\nlabel=Plugin\n[detail:enabled]\nscope=global\nsection=general\nkey=enabled\ntype=boolean\nlabel=Enabled\n[detail:add]\nscope=item\nkey=add\ntype=lines\nlabel=Add\norder=10\n[detail:r_type]\nscope=item\ngroup=research\nkey=type\ntype=choice\nchoices=technical|soviet|medical\nlabel=Type\ndialog=1\norder=10\n[detail:r_cost]\nscope=item\ngroup=research\nkey=cost\ntype=integer\nminimum=1\nmaximum=2147483647\nlabel=Cost\norder=20\n[detail:r_requires]\nscope=item\ngroup=research\nkey=requires\ntype=lines\npicker=game_research\nlabel=Requires\norder=30\n");var spec=LocalEditorSpec.Load(schema);Check(spec.ExtraGroups.Count==1&&spec.ExtraGroups[0].Prefix=="research:"&&spec.ExtraGroups[0].Tab=="research"&&spec.ExtraGroups[0].MaximumItems==2&&spec.GroupTab=="modify"&&spec.ItemFields(null).Count()==1&&spec.ItemFields(spec.ExtraGroups[0]).Count()==3);string build=MakeBuild(root,"multi-build");File.WriteAllBytes(Path.Combine(build,"plugins","multi.dll"),simple.Dll);Write(Path.Combine(build,"plugins","multi.ini"),"[general]\nenabled = 1\n\n[modify:faculty_geology]\nadd = $X 1\n\n[research:quartz]\ntype = technical\ncost = 5\n");var session=new LocalResourceSession(spec,build);ItemGroup research=spec.ExtraGroups[0];Check(session.Items().Count==2&&session.Items(null).Single().Id=="faculty_geology"&&session.Items(research).Single().Id=="research:quartz"&&session.Items(research).Single().Display=="quartz"&&session.Value("research:quartz",spec.Fields.Single(x=>x.Id=="r_cost"))=="5");session.AddSection("Crusher",new Dictionary<string,string>{{"r_type","soviet"},{"r_cost","10"},{"r_requires","quartz | before | x\r\nfaculty_geology"}},research);string effective=session.Effective();Check(session.Items(research).Count==2&&effective.Contains("[research:crusher]")&&effective.Contains("requires = quartz | before | x")&&effective.Contains("requires = faculty_geology")&&!effective.Contains("[research:crusher]\r\nadd"));Reject(()=>session.AddSection("third",new Dictionary<string,string>{{"r_type","medical"}},research));Reject(()=>session.AddSection("other",new Dictionary<string,string>{{"add","$Y"}},research));session.AddSection("new_edit",new Dictionary<string,string>{{"add","$Y 2"}},null);Check(session.Items(null).Count==2&&session.Effective().Contains("[modify:new_edit]"));session.Commit(NoGame);var reopened=new LocalResourceSession(spec,build);Check(reopened.Items(research).Count==2&&reopened.Items(null).Count==2);Write(schema,File.ReadAllText(schema)+"\n[group:bad]\ntab=research\nsection_prefix=modify:\n");string reason="";try{LocalEditorSpec.Load(schema);}catch(FormatException e){reason=Msg.Plain(e.Message);}Check(reason.Contains("group:bad"));});
        Test("the research tree is read with names and unlocks, deactivated blocks are skipped (0.4.29)",()=>{var entries=GameResearch.Parse(new[]{"$RESEARCH faculty_geology","$TYPE_TECHNICAL","$NAME 1001","$UNLOCK_RESEARCH uranium_study","-$UNLOCK_RESEARCH old_one","$UNLOCK_RESEARCH bauxite_study","$RESEARCH_ADD","","-$RESEARCH dead_one","-$NAME 1002","-$RESEARCH_ADD","$RESEARCH uranium_study","$NAME 1003","$RESEARCH_ADD"});Check(entries.Count==2&&entries[0].Id=="faculty_geology"&&entries[0].NameId==1001&&entries[0].Unlocks.Count==2&&entries[0].Unlocks[0]=="uranium_study"&&entries[1].Id=="uranium_study"&&entries[1].Unlocks.Count==0);});
        Test("a text pack is seeded, edited per language and staged as dependent writes (0.4.30)",()=>{string seed=Path.Combine(root,"textpack-seed");Write(Path.Combine(seed,"localization.ini"),"[localization]\nnamespace = research_expansion\nfallback = sovietEnglish\n");Write(Path.Combine(seed,"sovietEnglish.ini"),"[strings]\nquartz.name = Quartz\nquartz.desc = Crush it\n");Write(Path.Combine(seed,"sovietGerman.ini"),"[strings]\nquartz.name = Quarz\n");string local=Path.Combine(root,"textpack-local","research_expansion");Check(!new TextPackSession(local,0).Exists);var written=TextPackSession.Seed(local,seed,"research_expansion");Check(written.Count==3&&File.Exists(Path.Combine(local,"sovietGerman.ini")));var pack=new TextPackSession(local,1);Check(pack.Exists&&pack.Namespace=="research_expansion"&&pack.Fallback=="English"&&pack.Languages.Count==2&&pack.Get("English","quartz.desc")=="Crush it"&&pack.Get("German","quartz.desc")==""&&pack.Writes().Count==0);pack.Set("German","quartz.desc","Zerkleinert\r\nQuarz");pack.AddLanguage("French");pack.Set("French","quartz.name","Quartz FR");pack.SetFallback("German");var writes=pack.Writes();Check(writes.Count==3&&SafeFiles.Utf8.GetString(writes[pack.LanguageFile("German")]).Contains("quartz.desc = Zerkleinert\\nQuarz")&&SafeFiles.Utf8.GetString(writes[pack.LanguageFile("French")]).Contains("quartz.name = Quartz FR")&&SafeFiles.Utf8.GetString(writes[TextPackSession.ConfigPath(local)]).Contains("fallback = sovietGerman")&&pack.Keys().Count==2);pack.RemoveKey("quartz.name");Check(pack.Keys().Count==1&&pack.Get("English","quartz.name")==""&&pack.Get("French","quartz.name")==""&&pack.Writes().ContainsKey(pack.LanguageFile("English")));Reject(()=>pack.AddLanguage("German"));Reject(()=>pack.SetFallback("Polish"));Reject(()=>pack.Set("German","bad key","x"));string bare=Path.Combine(root,"textpack-bare","other");TextPackSession.Seed(bare,null,"other");var minimal=new TextPackSession(bare,0);Check(minimal.Exists&&minimal.Namespace=="other"&&minimal.Fallback=="English"&&minimal.Languages.Single()=="English");});
        Test("missing name/desc keys of own entries get the id as placeholder text (0.4.77)",()=>{string local=Path.Combine(root,"textpack-fill");Write(Path.Combine(local,"localization.ini"),"[localization]\nnamespace = research_expansion\nfallback = sovietEnglish\n");Write(Path.Combine(local,"sovietEnglish.ini"),"[strings]\nquartz.name = Quartz\n");Write(Path.Combine(local,"sovietGerman.ini"),"[strings]\nquartz.desc = Zerkleinert\n");var pack=new TextPackSession(local,0);
            Check(pack.EnsureKeys(new[]{"quartz","clay"},"English")==2&&pack.Get("English","clay.name")=="clay"&&pack.Get("English","clay.desc")=="clay"&&pack.Get("English","quartz.name")=="Quartz"&&pack.Get("English","quartz.desc")==""&&pack.Get("German","quartz.desc")=="Zerkleinert");
            Check(pack.EnsureKeys(new[]{"quartz","clay"},"English")==0&&pack.EnsureKeys(new[]{"x"},"French")==0&&pack.Writes().Count==1);});
        Test("a [textpack] section is parsed with its seed and key group (0.4.30)",()=>{string dir=Path.Combine(root,"textpack-schema");string schema=Path.Combine(dir,"config","tp.launcher.ini");Write(schema,File.ReadAllText(Path.Combine(root,"multi-group","config","multi.launcher.ini")).Replace("\n[group:bad]\ntab=research\nsection_prefix=modify:\n","")+"\n[tab:localization]\nlabel=Localization\norder=40\n[textpack]\ntab=localization\nfolder=build:plugins\\localization\\multi\nseed=dependency:tesmio.localization|hooks\\localization\\multi\nnamespace=multi\nkeys_from=research\n");var spec=LocalEditorSpec.Load(schema);Check(spec.TextPack!=null&&spec.TextPack.Tab=="localization"&&spec.TextPack.SeedDependency=="tesmio.localization"&&spec.TextPack.SeedPath=="hooks\\localization\\multi"&&spec.TextPack.KeysFrom=="research"&&spec.TextPack.Namespace=="multi");Check(!spec.TextPack.Required&&!spec.TextPackDue(dir));
            // 0.4.75: required = 1 is "due" while the local folder is missing, and satisfied once it exists.
            string requiredSchema=Path.Combine(dir,"config","tp-required.launcher.ini");Write(requiredSchema,File.ReadAllText(schema)+"required=1\nrequired_notice=First step\n");var required=LocalEditorSpec.Load(requiredSchema);Check(required.TextPack.Required&&required.TextPackDue(dir)&&required.TextPack.RequiredNotice=="First step");Directory.CreateDirectory(Path.Combine(dir,"plugins","localization","multi"));Check(!required.TextPackDue(dir));
            // 0.4.77: with own entries in the key group the missing pack blocks the save (ValidateAll); an existing folder lifts the block.
            string dueBuild=MakeBuild(root,"textpack-due");File.WriteAllBytes(Path.Combine(dueBuild,"plugins","multi.dll"),simple.Dll);Write(Path.Combine(dueBuild,"plugins","multi.ini"),"[general]\nenabled = 1\n\n[research:quartz]\ntype = technical\ncost = 5\n");
            var due=new LocalResourceSession(required,dueBuild);due.ValidateAll();due.AddSection("salt",new Dictionary<string,string>{{"r_type","soviet"},{"r_cost","10"}},required.GroupById("research"));
            string dueFailure="";try{due.ValidateAll();}catch(RuleException e){dueFailure=e.TranslationKey+": "+String.Join(" | ",e.TranslationArguments.Select(a=>Convert.ToString(a)));}Check(dueFailure.StartsWith("textpack_due:")&&dueFailure.Contains("Localization")&&dueFailure.Contains("Create locally"));
            Directory.CreateDirectory(Path.Combine(dueBuild,"plugins","localization","multi"));due.ValidateAll();Write(schema,File.ReadAllText(schema).Replace("keys_from=research","keys_from=nothere"));string reason="";try{LocalEditorSpec.Load(schema);}catch(FormatException e){reason=Msg.Plain(e.Message);}Check(reason.Length>0);});
        Test("a text field may carry a fixed key suffix, other types may not (0.4.31)",()=>{string schema=Path.Combine(root,"multi-group","config","multi.launcher.ini");string text=File.ReadAllText(schema).Replace("\n[group:bad]\ntab=research\nsection_prefix=modify:\n","");Write(schema,text+"\n[detail:r_name]\nscope=item\ngroup=research\nkey=name\ntype=text\nsuffix=.name\nlabel=Name\norder=70\n");var spec=LocalEditorSpec.Load(schema);Check(spec.Fields.Single(x=>x.Id=="r_name").Suffix==".name"&&spec.Fields.Single(x=>x.Id=="r_cost").Suffix=="");Write(schema,text+"\n[detail:r_bad]\nscope=item\ngroup=research\nkey=bad\ntype=integer\nsuffix=.name\nlabel=Bad\n");string reason="";try{LocalEditorSpec.Load(schema);}catch(FormatException e){reason=Msg.Plain(e.Message);}Check(reason.Contains("r_bad"));Write(schema,text);});
        Test("position = above_id, remove_label and [picture:] rows are parsed (0.4.35)",()=>{string schema=Path.Combine(root,"multi-group","config","multi.launcher.ini");string text=File.ReadAllText(schema);Write(schema,text+"\n[picture:icon]\ngroup=research\nfolder=vfs:media_soviet\\research\nfile={id}.png\nsize=128\nlabel=Picture\norder=15\n[detail:r_enabled]\nscope=item\ngroup=research\nkey=enabled\ntype=boolean\nposition=above_id\nlabel=Enabled\norder=5\n");var spec=LocalEditorSpec.Load(schema);Check(spec.Pictures.Count==1&&spec.Pictures[0].Group=="research"&&spec.Pictures[0].Size==128&&spec.PicturesFor(spec.ExtraGroups[0]).Count()==1&&spec.PicturesFor(null).Count()==0&&spec.Fields.Single(x=>x.Id=="r_enabled").AboveId&&!spec.Fields.Single(x=>x.Id=="r_cost").AboveId&&spec.ExtraGroups[0].RemoveLabel=="");Write(schema,text+"\n[picture:bad]\ngroup=research\nfolder=vfs:x\nfile=fixed.png\n");string reason="";try{LocalEditorSpec.Load(schema);}catch(FormatException e){reason=Msg.Plain(e.Message);}Check(reason.Contains("picture:bad"));Write(schema,text+"\n[detail:r_bad]\nscope=item\ngroup=research\nkey=bad\ntype=text\nposition=left\nlabel=Bad\n");reason="";try{LocalEditorSpec.Load(schema);}catch(FormatException e){reason=Msg.Plain(e.Message);}Check(reason.Contains("r_bad"));Write(schema,text);});
        Test("reference checks flag unknown research, building files and text ids (0.4.40)",()=>{
            string game=Path.Combine(root,"ref-game");string ms=Path.Combine(game,"media_soviet");
            Write(Path.Combine(ms,"research","research.ini"),"$RESEARCH faculty_geology\n$TYPE_TECHNICAL\n$UNLOCK_RESEARCH uranium_study\n$RESEARCH_ADD\n\n$RESEARCH uranium_study\n$TYPE_TECHNICAL\n$RESEARCH_ADD\n\n-$RESEARCH old_study\n$RESEARCH_ADD\n");
            Write(Path.Combine(ms,"buildings_types","plastics_factory.ini"),"$TYPE_FACTORY\n");
            File.WriteAllBytes(Path.Combine(ms,"sovietEnglish.btf"),GameTexts.Build(new List<KeyValuePair<int,string>>{new KeyValuePair<int,string>(1970,"Hello"),new KeyValuePair<int,string>(1971,"World")}));
            string build=MakeBuild(game,Path.Combine("tesmioloader","build"));File.WriteAllBytes(Path.Combine(build,"plugins","refs.dll"),simple.Dll);Write(Path.Combine(build,"plugins","refs.ini"),"[general]\nenabled = 1\n");
            string schema=Path.Combine(root,"ref-schema","config","refs.launcher.ini");
            Write(schema,"[launcher]\nlayout_version=1\neditor_type=keyed_sections\nid=x.refs\nname=Refs\nversion=1\n[editor]\nplugin=refs\nconfig=refs.ini\nreserved_sections=general\nsection_prefix=modify:\nmaximum_items=64\n[tab:main]\nlabel=Main\n[group:research]\ntab=main\nsection_prefix=research:\nlabel=New research\n[list:research]\nlabel=New research\n[new:research]\nname_label=Id\n[group:modify]\ntab=main\nlabel=Edits\nid_reference=game_research\n[list]\nlabel=Edits\n[new]\nname_label=Research id\n[detail:enabled]\nscope=global\nsection=general\nkey=enabled\ntype=boolean\nlabel=Enabled\n[detail:move]\nscope=item\nkey=move_before\ntype=lines\nlabel=Move\nreference=game_research\nreference_format=directive:$UNLOCK_RESEARCH\nreference_own=research\n[detail:r_enabled]\nscope=item\ngroup=research\nkey=enabled\ntype=boolean\nlabel=Active\n[detail:r_requires]\nscope=item\ngroup=research\nkey=requires\ntype=lines\nlabel=Parents\nreference=game_research\nreference_format=requires\nreference_own=research\n[detail:r_unlock]\nscope=item\ngroup=research\nkey=unlock\ntype=lines\nlabel=Unlocks\nreference=game_research\nreference_format=directive:$UNLOCK_RESEARCH\nreference_own=research\n[detail:r_target]\nscope=item\ngroup=research\nkey=target\ntype=lines\nlabel=Target\nreference=game_buildings\nreference_format=file\n");
            var spec=LocalEditorSpec.Load(schema);ItemGroup research=spec.ExtraGroups[0];
            LocalDetailField unlock=spec.Fields.Single(x=>x.Id=="r_unlock"),requires=spec.Fields.Single(x=>x.Id=="r_requires"),target=spec.Fields.Single(x=>x.Id=="r_target"),active=spec.Fields.Single(x=>x.Id=="r_enabled"),move=spec.Fields.Single(x=>x.Id=="move");
            Check(unlock.ReferenceFormat=="directive"&&unlock.ReferenceDirective=="$UNLOCK_RESEARCH"&&requires.ReferenceFormat=="requires"&&target.ReferenceFormat=="file"&&spec.IdReference=="game_research");
            var session=new LocalResourceSession(spec,build);session.References=new ReferenceSets(build,null,"en");
            Func<Action,string> failure=act=>{try{act();return "";}catch(RuleException e){return e.TranslationKey+": "+String.Join(" | ",e.TranslationArguments.Select(a=>Convert.ToString(a)));}};
            session.AddSection("clay",new Dictionary<string,string>{{"r_requires","faculty_geology | before | uranium_study"},{"r_unlock","$UNLOCK_BUILDING_PRODUCTION clay"},{"r_target","buildings_types\\plastics_factory.ini"}},research);session.ValidateAll();
            session.SetField("research:clay",unlock,"$UNLOCK_RESEARCH glass_study");string f1=failure(session.ValidateAll);Check(f1.StartsWith("reference_research:")&&f1.Contains("clay")&&f1.Contains("Unlocks")&&f1.Contains("glass_study"));
            // An own research makes the link valid, a switched-off one does not.
            session.AddSection("glass_study",new Dictionary<string,string>{{"r_unlock","$UNLOCK_BUILDING_PRODUCTION glass"}},research);session.ValidateAll();
            session.SetField("research:glass_study",active,"0");Check(failure(session.ValidateAll).Contains("glass_study"));session.SetField("research:glass_study",active,"1");session.ValidateAll();
            session.SetField("research:clay",requires,"faculty_geology | before | nowhere");Check(failure(session.ValidateAll).Contains("nowhere"));
            session.SetField("research:clay",requires,"old_study");Check(failure(session.ValidateAll).Contains("old_study"));
            session.SetField("research:clay",requires,"faculty_geology");session.ValidateAll();
            session.SetField("research:clay",target,"buildings_types\\missing.ini");string f2=failure(session.ValidateAll);Check(f2.StartsWith("reference_building:")&&f2.Contains("missing.ini"));
            session.SetField("research:clay",target,"buildings_types\\plastics_factory.ini");session.ValidateAll();
            string f3=failure(()=>{session.AddSection("not_a_research",new Dictionary<string,string>{{"move","$UNLOCK_RESEARCH uranium_study | $UNLOCK_RESEARCH faculty_geology"}},null);session.ValidateAll();});Check(f3.StartsWith("reference_research_id:")&&f3.Contains("not_a_research"));session.Remove("not_a_research");
            session.AddSection("faculty_geology",new Dictionary<string,string>{{"move","$UNLOCK_RESEARCH uranium_study | $UNLOCK_RESEARCH clay"}},null);session.ValidateAll();
            session.SetField("faculty_geology",move,"$UNLOCK_RESEARCH uranium_study | $UNLOCK_RESEARCH bauxite_study");Check(failure(session.ValidateAll).Contains("bauxite_study"));session.SetField("faculty_geology",move,"$UNLOCK_RESEARCH uranium_study");session.ValidateAll();
            // 0.4.74: an unresolved $UNLOCK_RESEARCH blocks the save, not the adding of the research it names (or any other).
            session.SetField("research:clay",unlock,"$UNLOCK_RESEARCH table_salt");session.AddSection("road_salt",new Dictionary<string,string>{{"r_unlock","$UNLOCK_BUILDING_PRODUCTION road_salt"}},research);Check(failure(session.ValidateAll).Contains("table_salt"));
            session.AddSection("table_salt",new Dictionary<string,string>{{"r_unlock","$UNLOCK_BUILDING_PRODUCTION table_salt"}},research);session.ValidateAll();
            // Text ids of a keyed_list are checked against the game language.
            string listSchema=Path.Combine(root,"ref-schema","config","texts.launcher.ini");
            Write(listSchema,"[launcher]\nlayout_version=1\neditor_type=keyed_list\nid=x.texts\nname=Texts\nversion=1\n[editor]\nplugin=texts\nconfig=texts.ini\nlist_section=text_ids\nmaximum_items=8\n[group:texts]\nlabel=Texts\nid_reference=game_texts\n[list]\nlabel=Texts\nid_suggestions=0\n[column:chars]\ntype=integer\nlabel=Chars\n");
            File.WriteAllBytes(Path.Combine(build,"plugins","texts.dll"),simple.Dll);Write(Path.Combine(build,"plugins","texts.ini"),"[text_ids]\n1970 = 0\n");
            var texts=new LocalResourceSession(LocalEditorSpec.Load(listSchema),build);texts.References=new ReferenceSets(build,null,"en");
            texts.AddRaw("1971","0");texts.ValidateAll();string f4=failure(()=>{texts.AddRaw("2500","0");texts.ValidateAll();});Check(f4.StartsWith("reference_text_id:")&&f4.Contains("2500"));
            // Without a game folder next to the loader the checks are skipped; without the sets too.
            string blindBuild=MakeBuild(root,"ref-blind");File.WriteAllBytes(Path.Combine(blindBuild,"plugins","refs.dll"),simple.Dll);Write(Path.Combine(blindBuild,"plugins","refs.ini"),"[general]\nenabled = 1\n");
            var blind=new LocalResourceSession(spec,blindBuild);blind.References=new ReferenceSets(blindBuild,null,"en");blind.AddSection("y",new Dictionary<string,string>{{"r_unlock","$UNLOCK_RESEARCH nothing"}},research);blind.ValidateAll();
            var silent=new LocalResourceSession(spec,build);silent.AddSection("z",new Dictionary<string,string>{{"r_unlock","$UNLOCK_RESEARCH nothing"}},research);silent.ValidateAll();
            // Schema errors: unknown set, format or own group.
            string text=File.ReadAllText(schema);
            Write(schema,text+"\n[detail:bad1]\nscope=item\ngroup=research\nkey=b1\ntype=lines\nlabel=B\nreference=game_planets\n");Reject(()=>LocalEditorSpec.Load(schema));
            Write(schema,text+"\n[detail:bad2]\nscope=item\ngroup=research\nkey=b2\ntype=lines\nlabel=B\nreference=game_research\nreference_format=magic\n");Reject(()=>LocalEditorSpec.Load(schema));
            Write(schema,text+"\n[detail:bad3]\nscope=item\ngroup=research\nkey=b3\ntype=lines\nlabel=B\nreference=game_research\nreference_own=nogroup\n");Reject(()=>LocalEditorSpec.Load(schema));
            Write(schema,text);});
        Test("resource names are checked against the base game and resources.ini (0.4.41)",()=>{
            string build=MakeBuild(root,"ref-res");File.WriteAllBytes(Path.Combine(build,"plugins","grit.dll"),simple.Dll);Write(Path.Combine(build,"plugins","grit.ini"),"[grit_materials]\nsand = 0.5\n");
            string schema=Path.Combine(root,"ref-schema","config","grit.launcher.ini");
            Write(schema,"[launcher]\nlayout_version=1\neditor_type=keyed_list\nid=x.grit\nname=Grit\nversion=1\n[editor]\nplugin=grit\nconfig=grit.ini\nlist_section=grit_materials\nmaximum_items=8\n[group:materials]\nlabel=Materials\nid_reference=resources\n[list]\nlabel=Materials\n[column:strength]\ntype=decimal\nminimum=0\nmaximum=1\nlabel=Strength\n");
            Func<Action,string> failure=act=>{try{act();return "";}catch(RuleException e){return e.TranslationKey+": "+String.Join(" | ",e.TranslationArguments.Select(a=>Convert.ToString(a)));}};
            var spec=LocalEditorSpec.Load(schema);var grit=new LocalResourceSession(spec,build);grit.References=new ReferenceSets(build,null,"en");
            grit.AddRaw("gravel","0.5");grit.ValidateAll();
            string f1=failure(()=>{grit.AddRaw("unobtainium","0.5");grit.ValidateAll();});Check(f1.StartsWith("reference_resource_id:")&&f1.Contains("unobtainium"));
            // A resource the Resources plugin registers (hook = 2) counts as known.
            File.WriteAllBytes(Path.Combine(build,"plugins","resources.dll"),simple.Dll);Write(Path.Combine(build,"plugins","resources.ini"),"[list]\nunobtainium=aluminium, Unobtainium\n[resources]\nhook=2\n");
            var again=new LocalResourceSession(spec,build);again.References=new ReferenceSets(build,null,"en");again.AddRaw("unobtainium","0.5");again.ValidateAll();
            // Without hook = 2 the registration does not count.
            Write(Path.Combine(build,"plugins","resources.ini"),"[list]\nunobtainium=aluminium, Unobtainium\n[resources]\nhook=0\n");
            var off=new LocalResourceSession(spec,build);off.References=new ReferenceSets(build,null,"en");Check(failure(()=>{off.AddRaw("unobtainium","0.5");off.ValidateAll();}).Contains("unobtainium"));});
        Test("research blocks keep their lines; block rows, research id pickers and line pickers are parsed (0.4.42)",()=>{
            var parsed=GameResearch.Parse(new[]{"$RESEARCH a","$TYPE_TECHNICAL","$COST 100","-$UNLOCK_RESEARCH old","$UNLOCK_RESEARCH b","$RESEARCH_ADD","","$RESEARCH b","$RESEARCH_ADD","-$RESEARCH gone","$COST 5","$RESEARCH_ADD"});
            Check(parsed.Count==2&&parsed[0].Lines.Count==6&&parsed[0].Lines[0]=="$RESEARCH a"&&parsed[0].Lines[5]=="$RESEARCH_ADD"&&parsed[1].Lines.Count==2);
            Check(GameResearch.EditableLines(parsed[0]).SequenceEqual(new[]{"$TYPE_TECHNICAL","$COST 100","$UNLOCK_RESEARCH b"}));
            string schema=Path.Combine(root,"block-schema","config","blk.launcher.ini");
            string text="[launcher]\nlayout_version=1\neditor_type=keyed_sections\nid=x.blk\nname=Blk\nversion=1\n[editor]\nplugin=blk\nconfig=blk.ini\nreserved_sections=general\nsection_prefix=modify:\nmaximum_items=64\n[tab:main]\nlabel=Main\n[group:modify]\ntab=main\nlabel=Edits\n[list]\nlabel=Edits\nid_picker=game_research\n[new]\nname_label=Research\n[detail:enabled]\nscope=global\nsection=general\nkey=enabled\ntype=boolean\nlabel=Enabled\n[detail:replace]\nscope=item\nkey=replace\ntype=lines\nlabel=Replace\npicker=research_lines\npicker_format=line_edit\norder=20\n[detail:remove]\nscope=item\nkey=remove\ntype=lines\nlabel=Remove\npicker=research_lines\norder=30\n[research_block:orig]\nlabel=Original\ndescription=As in the game\norder=5\n";
            Write(schema,text);var spec=LocalEditorSpec.Load(schema);
            Check(spec.ItemIdPicker=="game_research"&&spec.IdPickerOf(null)=="game_research"&&spec.Blocks.Count==1&&spec.BlocksFor(null).Single().Id=="orig"&&spec.BlocksFor(null).Single().Order==5);
            Check(spec.Fields.Single(x=>x.Id=="replace").PickerFormat=="line_edit"&&spec.Fields.Single(x=>x.Id=="remove").PickerFormat=="line");
            Write(schema,text+"[detail:bad]\nscope=item\nkey=bad\ntype=lines\nlabel=Bad\npicker=research_lines\npicker_format=magic\n");Reject(()=>LocalEditorSpec.Load(schema));
            Write(schema,text+"[research_block:bad]\ngroup=nogroup\nlabel=Bad\n");Reject(()=>LocalEditorSpec.Load(schema));
            Write(schema,text.Replace("id_picker=game_research","id_picker=game_planets"));Reject(()=>LocalEditorSpec.Load(schema));
            Write(schema,text);});
        Test("picker = files lists and checks files under the plugin's folders, DDS headers included (0.4.48)",()=>{
            string build=MakeBuild(root,"files-build");File.WriteAllBytes(Path.Combine(build,"plugins","tiles.dll"),simple.Dll);Write(Path.Combine(build,"plugins","tiles.ini"),"[general]\nenabled = 1\n\n[sand_tile:meadow]\nbase = tiles_normal/grass2.dds\ncolor = Vanilla/ok_color.dds\nnormal = Vanilla/ok_normal.dds\n");
            string assets=Path.Combine(build,"plugins","tiles","assets");Directory.CreateDirectory(Path.Combine(assets,"Vanilla"));Directory.CreateDirectory(Path.Combine(assets,"Set B"));
            Func<string,int,int,byte[]> dds=(fourcc,side,mips)=>{var h=new byte[256];h[0]=(byte)'D';h[1]=(byte)'D';h[2]=(byte)'S';h[3]=(byte)' ';BitConverter.GetBytes(124).CopyTo(h,4);BitConverter.GetBytes(side).CopyTo(h,12);BitConverter.GetBytes(side).CopyTo(h,16);BitConverter.GetBytes(mips).CopyTo(h,28);System.Text.Encoding.ASCII.GetBytes(fourcc).CopyTo(h,84);return h;};
            File.WriteAllBytes(Path.Combine(assets,"Vanilla","ok_color.dds"),dds("DXT1",1024,11));File.WriteAllBytes(Path.Combine(assets,"Vanilla","ok_normal.dds"),dds("DXT5",2048,12));
            File.WriteAllBytes(Path.Combine(assets,"Set B","raw_color.dds"),dds("\0\0\0\0",1024,11));File.WriteAllBytes(Path.Combine(assets,"Set B","nomips_color.dds"),dds("DXT1",1024,1));File.WriteAllBytes(Path.Combine(assets,"odd_color.dds"),dds("DXT1",1000,10));
            string schema=Path.Combine(root,"files-schema","config","tiles.launcher.ini");
            string text="[launcher]\nlayout_version=1\neditor_type=keyed_sections\nid=x.tiles\nname=Tiles\nversion=1\n[editor]\nplugin=tiles\nconfig=tiles.ini\nreserved_sections=general\nsection_prefix=modify:\nmaximum_items=64\n[tab:main]\nlabel=Main\n[group:tiles]\ntab=main\nsection_prefix=sand_tile:\nlabel=Tiles\n[list:tiles]\nlabel=Tiles\n[new:tiles]\nname_label=Id\n[group:modify]\ntab=main\nlabel=Edits\n[list]\nlabel=Edits\n[new]\nname_label=Id\n[detail:enabled]\nscope=global\nsection=general\nkey=enabled\ntype=boolean\nlabel=Enabled\n[detail:t_color]\nscope=item\ngroup=tiles\nkey=color\ntype=text\nmaximum_length=120\npicker=files\npicker_folders=build:plugins\\tiles\\assets\npicker_pattern=*.dds\nreference=files\nreference_format=dds_dxt1\nlabel=Colour\n[detail:t_normal]\nscope=item\ngroup=tiles\nkey=normal\ntype=text\nmaximum_length=120\npicker=files\npicker_folders=build:plugins\\tiles\\assets\npicker_pattern=*.dds\nreference=files\nreference_format=dds_dxt5\nlabel=Normal\n";
            Write(schema,text);var spec=LocalEditorSpec.Load(schema);ItemGroup tiles=spec.ExtraGroups[0];LocalDetailField color=spec.Fields.Single(x=>x.Id=="t_color"),normal=spec.Fields.Single(x=>x.Id=="t_normal");
            Check(color.Picker=="files"&&color.PickerFolders.Length==1&&color.PickerPattern=="*.dds"&&color.ReferenceFormat=="dds_dxt1"&&normal.ReferenceFormat=="dds_dxt5");
            var session=new LocalResourceSession(spec,build);session.References=new ReferenceSets(build,null,"en");
            Check(session.PickerFile(color,"Vanilla/ok_color.dds")!=null&&session.PickerFile(color,"Vanilla\\ok_color.dds")!=null&&session.PickerFile(color,"../tiles.ini")==null&&session.PickerFile(color,"missing.dds")==null);
            session.ValidateAll();   // the shipped entry names two valid files
            Func<Action,string> failure=act=>{try{act();return "";}catch(RuleException e){return e.TranslationKey+": "+String.Join(" | ",e.TranslationArguments.Select(a=>Convert.ToString(a)));}};
            session.AddSection("own",new Dictionary<string,string>{{"t_color","Vanilla/ok_color.dds"},{"t_normal","Vanilla/ok_normal.dds"}},tiles);session.ValidateAll();
            session.SetField("sand_tile:own",color,"Set B/missing.dds");Check(failure(session.ValidateAll).StartsWith("reference_file_missing:"));
            session.SetField("sand_tile:own",color,"Set B/raw_color.dds");Check(failure(session.ValidateAll).StartsWith("reference_file_dxt1:"));
            session.SetField("sand_tile:own",color,"Set B/nomips_color.dds");Check(failure(session.ValidateAll).StartsWith("reference_file_mips:"));
            session.SetField("sand_tile:own",color,"odd_color.dds");Check(failure(session.ValidateAll).StartsWith("reference_file_size:"));
            session.SetField("sand_tile:own",color,"Vanilla/ok_color.dds");session.SetField("sand_tile:own",normal,"Vanilla/ok_color.dds");Check(failure(session.ValidateAll).StartsWith("reference_file_dxt5:"));
            session.SetField("sand_tile:own",normal,"Vanilla/ok_normal.dds");session.ValidateAll();
            Check(ReferenceSets.DdsProblem(Path.Combine(assets,"Vanilla","ok_color.dds"),"exists")==null);
            Write(schema,text.Replace("picker=files\npicker_folders=build:plugins\\tiles\\assets\npicker_pattern=*.dds\nreference=files\nreference_format=dds_dxt1","picker=files\npicker_pattern=*.dds"));Reject(()=>LocalEditorSpec.Load(schema));
            Write(schema,text.Replace("type=text\nmaximum_length=120\npicker=files\npicker_folders=build:plugins\\tiles\\assets\npicker_pattern=*.dds\nreference=files\nreference_format=dds_dxt1","type=lines\npicker=files\npicker_folders=build:plugins\\tiles\\assets"));Reject(()=>LocalEditorSpec.Load(schema));
            Write(schema,text);});
        Test("number fields carry a step for the +/- buttons (0.4.39)",()=>{string schema=Path.Combine(root,"multi-group","config","multi.launcher.ini");string text=File.ReadAllText(schema);var spec=LocalEditorSpec.Load(schema);Check(spec.Fields.Single(x=>x.Id=="r_cost").Step==1m);Write(schema,text+"\n[detail:r_rate]\nscope=item\ngroup=research\nkey=rate\ntype=decimal\nminimum=0\nmaximum=10\nstep=0.25\nlabel=Rate\n");Check(LocalEditorSpec.Load(schema).Fields.Single(x=>x.Id=="r_rate").Step==0.25m);Write(schema,text+"\n[detail:r_bad]\nscope=item\ngroup=research\nkey=bad\ntype=integer\nstep=0\nlabel=Bad\n");string reason="";try{LocalEditorSpec.Load(schema);}catch(FormatException e){reason=Msg.Plain(e.Message);}Check(reason.Contains("r_bad"));Write(schema,text);});
        Test("folder and file rows resolve loader and package paths (0.4.28)",()=>{string dir=Path.Combine(root,"path-rows");string schema=Path.Combine(dir,"config","x.launcher.ini");Write(schema,File.ReadAllText(Path.GetFullPath(Path.Combine(args[0],"..","..","settings_schemas","deposits.launcher.ini")))+"\n[card:icons]\ntab = general\nlabel = Icons\n\n[folder:store]\ncard = icons\npath = vfs:media_soviet\\research\ncreate = 1\norder = 10\n\n[file:noimage]\ncard = icons\npaths = workshop=package:hooks\\icons\\noimage.png | local=build:plugins\\x\\noimage.png\norder = 20\n");var spec=LocalEditorSpec.Load(schema);Check(spec.PathRows.Count==2&&spec.PathRows[0].Kind=="folder"&&spec.PathRows[0].Create&&spec.PathRows[1].Candidates.Count==2&&spec.PathRows[1].Candidates[0].Key=="workshop"&&spec.PathRows[1].Candidates[1].Key=="local");string loader=Path.Combine(dir,"tesmioloader","build");Directory.CreateDirectory(loader);Check(LocalEditorSpec.VfsRoot(loader).Equals(Path.Combine(dir,"tesmioloader","vfs"),StringComparison.OrdinalIgnoreCase));Directory.CreateDirectory(Path.Combine(loader,"vfs"));Check(LocalEditorSpec.VfsRoot(loader).Equals(Path.Combine(loader,"vfs"),StringComparison.OrdinalIgnoreCase));Directory.CreateDirectory(Path.Combine(dir,"tesmioloader","vfs"));Check(LocalEditorSpec.VfsRoot(loader).Equals(Path.Combine(dir,"tesmioloader","vfs"),StringComparison.OrdinalIgnoreCase));Check(spec.ResolvePath("vfs:media_soviet\\research",loader).Equals(Path.Combine(dir,"tesmioloader","vfs","media_soviet","research"),StringComparison.OrdinalIgnoreCase)&&spec.ResolvePath("build:plugins\\x\\noimage.png",loader).Equals(Path.Combine(loader,"plugins","x","noimage.png"),StringComparison.OrdinalIgnoreCase)&&spec.ResolvePath("package:hooks\\icons\\noimage.png",loader).Equals(Path.Combine(dir,"config","hooks","icons","noimage.png"),StringComparison.OrdinalIgnoreCase));string reason="";try{spec.ResolvePath("vfs:..\\x",loader);}catch(FormatException e){reason=Msg.Plain(e.Message);}Check(reason.Length>0);Write(schema,File.ReadAllText(schema)+"\n[file:bad]\ncard = icons\npaths = steam=package:a.png\n");reason="";try{LocalEditorSpec.Load(schema);}catch(FormatException e){reason=Msg.Plain(e.Message);}Check(reason.Contains("steam=package:a.png"));});
        Test("a found dependency counts as provided while its plugin is switched on",()=>{Package d=Package.Load(depRoot);d.Dependencies.RemoveAll(x=>x.Id=="example.missing");var scan=ScanWorkshop(depShop,new List<string>());Catalog.ResolveDependencies(d.Dependencies,scan);Dependency dep=d.Dependencies.Single();Check(dep.Found&&dep.Target=="plain_plugin"&&dep.Name.Length>0&&Msg.Plain(dep.Note).Contains(dep.Name));string b6=MakeBuild(root,"provision-build");Check(RuntimeGuard.Provisioned(b6,dep),"no switch in tesmioloader.ini means on");Write(Path.Combine(b6,"tesmioloader.ini"),"[plugins]\n"+dep.Target+" = 0\n");Check(!RuntimeGuard.Provisioned(b6,dep),"switched off");Write(Path.Combine(b6,"tesmioloader.ini"),"[plugins]\n"+dep.Target+" = 1\n");Check(RuntimeGuard.Provisioned(b6,dep),"switched on");});

        // ---- 0.15.0: update detection ----
        string upBuild=MakeBuild(root,"update-build");Write(Path.Combine(upBuild,"tesmioloader.dll"),"fake");Write(Path.Combine(upBuild,"tesmiolauncher.exe"),"fake");
        string upShop=Path.Combine(root,"update-shop"),upRoot=Path.Combine(upShop,"update-package");CopyTree(plainRoot,upRoot);Write(Path.Combine(upRoot,"soviet.mod.ini"),"[mod]\nid=example.update\nname=Updatable\nversion=1.0\n[hooks]\ndll=hooks\\plain_plugin.dll\n");
        Test("a deployed package remembers its version and reports no update",()=>{Package p1=Package.Load(upRoot);var s=new Session(p1,upBuild);Check(!s.Update.Deployed&&!s.Update.Pending);s.Commit(true,NoGame);Check(new Ini(SafeFiles.Text(s.Receipt)).Get("state","version")=="1.0");var again=new Session(p1,upBuild);Check(again.Update.Deployed&&!again.Update.Pending&&!again.Notes.Any(n=>n.StartsWith("Paket aktualisiert")));});
        Test("a changed default INI and version show up as a pending update until saved",()=>{string ini=Path.Combine(upRoot,"hooks","plain_plugin.ini");Write(ini,SafeFiles.Text(ini)+"\n; updated by the author\n");Write(Path.Combine(upRoot,"soviet.mod.ini"),"[mod]\nid=example.update\nname=Updatable\nversion=1.1\n[hooks]\ndll=hooks\\plain_plugin.dll\n");Package p2=Package.Load(upRoot);var t=new Session(p2,upBuild);Check(t.Update.Pending&&t.Update.DefaultsChanged&&!t.Update.DllChanged&&t.Update.PreviousVersion=="1.0"&&t.Notes.Any(n=>Msg.KeyOf(n)=="note_update_pending"&&Msg.Plain(n).Contains("1.0 | 1.1")));var scan=ScanWorkshop(upShop,new List<string>());var marked=Catalog.MarkUpdates(scan,upBuild);Check(marked.Any(x=>x.StartsWith("Updatable")&&x.Contains("1.0 -> 1.1"))&&scan.Single(e=>e.Root.Equals(upRoot,StringComparison.OrdinalIgnoreCase)).Updated);t.Commit(true,NoGame);Check(!new Session(p2,upBuild).Update.Pending&&Catalog.MarkUpdates(scan,upBuild).Count==0&&!scan.Single(e=>e.Root.Equals(upRoot,StringComparison.OrdinalIgnoreCase)).Updated);});
        Test("a package that gained a schema key keeps the personal values, and a local INI is imported once",()=>{string gainRoot=Path.Combine(root,"gain-package");CopyTree(simpleRoot,gainRoot);string gainBuild=MakeBuild(root,"gain-build");Write(Path.Combine(gainBuild,"tesmioloader.dll"),"fake");Write(Path.Combine(gainBuild,"tesmiolauncher.exe"),"fake");Package g1=Package.Load(gainRoot);var gainFirst=new Session(g1,gainBuild);gainFirst.Set(g1.Fields.Single(x=>x.Id=="general/limit"),"9");gainFirst.Commit(true,NoGame);Check(new Ini(SafeFiles.Text(gainFirst.UserIni)).Get("general","limit")=="9"&&!File.Exists(gainFirst.LocalIni));
            Write(Path.Combine(gainRoot,"hooks","sample_plugin.ini"),"[general]\nenabled = 1\nlimit = 5\nmode = normal\nspacing = 18.0\n");Write(Path.Combine(gainRoot,"config","sample.launcher.ini"),SafeFiles.Text(Path.Combine(gainRoot,"config","sample.launcher.ini"))+"[field:spacing]\nsection=general\nkey=spacing\nlabel=Spacing\ntype=decimal\nminimum=8\nmaximum=40\ngroup=main\n");Package g2=Package.Load(gainRoot);Check(g2.Fields.Any(x=>x.Id=="general/spacing"));
            var gainSecond=new Session(g2,gainBuild);var gainEffective=new Ini(gainSecond.Effective());Check(gainSecond.Update.Pending&&gainSecond.Update.DefaultsChanged&&gainEffective.Get("general","spacing")=="18.0"&&gainEffective.Get("general","limit")=="9");gainSecond.Commit(true,NoGame);Check(!new Session(g2,gainBuild).Update.Pending&&new Session(g2,gainBuild).Overrides["general/limit"]=="9");
            Write(Path.Combine(gainBuild,"plugins","sample_plugin.ini"),"[general]\nenabled = 1\nlimit = 4\nmode = normal\nretired = yes\n");File.Delete(Path.Combine(gainBuild,"user_config","sample_plugin.ini"));File.Delete(Path.Combine(gainBuild,"user_config",".autoload","sample_plugin.receipt.ini"));var gainImported=new Session(g2,gainBuild);var gainImportedEffective=new Ini(gainImported.Effective());Check(gainImported.Overrides["general/limit"]=="4"&&gainImportedEffective.Get("general","limit")=="4"&&gainImportedEffective.Get("general","spacing")=="18.0"&&gainImportedEffective.Get("general","retired")==null&&gainImported.Notes.Any(n=>Msg.KeyOf(n)=="note_first_import"));});
        Test("game language files parse, measure and pick the default language",()=>{var texts=new List<KeyValuePair<int,string>>{new KeyValuePair<int,string>(5,"kurz"),new KeyValuePair<int,string>(1970,"Zeigt den Bereich an, in dem ein Problem besteht!\n(Das ist der Punkt)"),new KeyValuePair<int,string>(7,"Ä\r\nÖ")};byte[] file=GameTexts.Build(texts);var parsed=GameTexts.Parse(file);Check(parsed.Count==3&&parsed[0].Id==5&&parsed[0].Longest==4&&parsed[0].Lines==1);var hint=parsed[1];Check(hint.Id==1970&&hint.Lines==2&&hint.Longest==49&&hint.FirstLine=="Zeigt den Bereich an, in dem ein Problem besteht!"&&hint.Text.Contains("(Das ist der Punkt)"));Check(parsed[2].Text=="Ä\r\nÖ"&&parsed[2].Lines==2&&parsed[2].Longest==1);
            string game=Path.Combine(root,"text-game");Directory.CreateDirectory(Path.Combine(game,"media_soviet"));File.WriteAllBytes(Path.Combine(game,"media_soviet","sovietGerman.btf"),file);File.WriteAllBytes(Path.Combine(game,"media_soviet","sovietEnglish.btf"),file);File.WriteAllBytes(Path.Combine(game,"media_soviet","sovietComments.btf"),file);Write(Path.Combine(game,"media_soviet","config.ini"),"$TEXT LANGUAGE2 auto\n");
            var names=GameTexts.Languages(game);Check(names.Count==2&&names[0]=="English"&&names[1]=="German");Check(GameTexts.ConfiguredLanguage(game)==null&&GameTexts.DefaultLanguage(game,"de")=="German"&&GameTexts.DefaultLanguage(game,"en")=="English");
            Write(Path.Combine(game,"media_soviet","config.ini"),"$TEXT LANGUAGE2 English\n");Check(GameTexts.ConfiguredLanguage(game)=="English"&&GameTexts.DefaultLanguage(game,"de")=="English");Write(Path.Combine(game,"media_soviet","config.ini"),"$TEXT LANGUAGE2 Klingon\n");Check(GameTexts.DefaultLanguage(game,"de")=="German");
            Check(GameTexts.Load(game,"German").Count==3);Reject(()=>GameTexts.Load(game,"French"));Reject(()=>GameTexts.Parse(new byte[]{0,0,0,9,0,0,0,0,0,0,0,0}));Check(GameBuildings.GameRoot(Path.Combine(game,"tesmioloader","build"))==Path.GetFullPath(game));});
        Test("building scan lists the game's Steam Workshop items whatever the package root is",()=>{string lib=Path.Combine(root,"steamlib"),sgame=Path.Combine(lib,"steamapps","common","SovietRepublic"),content=Path.Combine(lib,"steamapps","workshop","content","784150");Write(Path.Combine(sgame,"media_soviet","buildings_types","flat.ini"),"$TYPE_LIVING\r\n");Write(Path.Combine(content,"4711","depot","building.ini"),"$TYPE_TECHNICAL_SERVICES\r\n");Write(Path.Combine(content,"4711","workshopconfig.ini"),"$ITEM_NAME \"Grit Depot\"\r\n");string sBuild=Path.Combine(sgame,"tesmioloader","build");
            Check(GameBuildings.SteamWorkshopFor(sgame)==content);var scan=GameBuildings.Scan(sBuild,"");Check(scan.Count==2&&scan.Any(e=>e.Target=="4711\\depot\\building.ini"&&e.Origin=="workshop:4711"&&e.OriginLabel=="Grit Depot"&&e.Type=="TECHNICAL_SERVICES"));
            Check(GameBuildings.Scan(sBuild,content).Count==2);string other=Path.Combine(root,"other-packages");Write(Path.Combine(other,"99","x","building.ini"),"$TYPE_FACTORY\r\n");var both=GameBuildings.Scan(sBuild,other);Check(both.Count==3&&both.Any(e=>e.Origin=="workshop:99"));Check(GameBuildings.SteamWorkshopFor(Path.Combine(root,"fake-game"))==null);});
        Test("typed names become safe section ids",()=>{Check(CollectionRules.IdFromName("Technical Service Storage")=="technical_service_storage");Check(CollectionRules.IdFromName("  Straßen-Öl  /  Test 2 ")=="strassen-oel_test_2");Check(CollectionRules.IdFromName("shared_grit")=="shared_grit"&&CollectionRules.IdFromName("a.b")=="a.b");Check(CollectionRules.IdFromName("[general]")=="general"&&CollectionRules.IdFromName("!!!")==""&&CollectionRules.IdFromName(null)=="");Check(CollectionRules.SafeItem(CollectionRules.IdFromName("Mein Regelsatz #1")));});

        // ---- 0.16.0: log viewer ----
        // 0.4.84: reads Steam's own login record. It must answer for whatever this
        // machine looks like and never throw; a missing or foreign entry is Unknown,
        // which never blocks a launch.
        Test("the Steam session check answers without throwing",()=>{var state=SteamSession.Check();Check(state==SteamSession.State.Ready||state==SteamSession.State.LoggedOut||state==SteamSession.State.Unknown);});
        // 0.4.91: a warning must be able to say what it found, and only a warning may have a reason -
        // "I could not look" is Unknown and never blocks a game start.
        Test("the Steam session check names its reason and its evidence",()=>{
            string reason;var state=SteamSession.Check(out reason);
            Check(state==SteamSession.State.LoggedOut?reason.Length>0:reason.Length==0);
            if(reason.Length>0)Check(reason=="steam_reason_user"||reason=="steam_reason_pid"||reason=="steam_reason_gone");
            string evidence=SteamSession.Evidence();Check(evidence.Length>0&&evidence.Contains("ActiveUser="));});
        // 0.4.92: the launch watch. Only "was there and is gone again" is worth a word; a game
        // that never shows up looks exactly like a slow disk, and one that stays is the normal case.
        Test("the launch watch only complains when the game disappears again",()=>{
            var died=new LaunchWatch(15);died.Tick(true,0.5);died.Tick(true,1.0);died.Tick(false,1.5);
            Check(died.Finished&&died.Died);
            var alive=new LaunchWatch(15);for(double t=0.5;t<=15.0;t+=0.5)alive.Tick(true,t);
            Check(alive.Finished&&!alive.Died);
            var never=new LaunchWatch(15);for(double t=0.5;t<=15.0;t+=0.5)never.Tick(false,t);
            Check(never.Finished&&!never.Died);
            var slow=new LaunchWatch(15);slow.Tick(false,0.5);slow.Tick(false,3.0);slow.Tick(true,4.0);
            Check(!slow.Finished&&!slow.Died);
            var settled=new LaunchWatch(15);settled.Tick(true,16.0);settled.Tick(false,16.5);
            Check(settled.Finished&&!settled.Died);});
        // 0.5.6: and only when it quits IMMEDIATELY. Both numbers are measured on the real
        // machine: the Steam failure lived 1.3 s, the user closing it on purpose 10.1 s.
        Test("a game the player closes himself is no longer reported as a failed start (0.5.6)",()=>{
            var steam=new LaunchWatch(15);
            for(double t=0.5;t<=1.5;t+=0.5)steam.Tick(true,t);
            steam.Tick(false,2.0);
            Check(steam.Finished&&steam.Died&&steam.Lived<2.0,"1.3 s is the Steam case and still warns");
            var byHand=new LaunchWatch(15);
            for(double t=0.5;t<=10.0;t+=0.5)byHand.Tick(true,t);
            byHand.Tick(false,10.5);
            Check(byHand.Finished&&!byHand.Died&&byHand.Lived>=10.0,"10.1 s is a person, and RMM says nothing");
            // Right on the line counts as the player, not as a fault.
            var edge=new LaunchWatch(15,5.0);edge.Tick(true,1.0);edge.Tick(false,6.0);
            Check(edge.Finished&&!edge.Died);});
        // 0.4.94: the reset. Level one must never reach anything the game reads; level two takes
        // back exactly what the receipts claim and names the saved games it costs.
        Test("the reset plans two levels and names the saved games it costs",()=>{
            string home=Path.Combine(root,"reset-plan"),build=Path.Combine(home,"tesmioloader","build");
            Directory.CreateDirectory(Path.Combine(build,"plugins"));
            Directory.CreateDirectory(Path.Combine(build,"user_config",".autoload","profiles","A_1234"));
            Directory.CreateDirectory(Path.Combine(build,"user_config",".autoload","backups","demo"));
            Directory.CreateDirectory(Path.Combine(build,"user_config",".autoload","content","tesmio.salt"));
            Directory.CreateDirectory(Path.Combine(build,"vfs","media_soviet","resources"));
            File.WriteAllText(Path.Combine(build,"tesmioloader.ini"),"[plugins]\r\nresources = 1\r\ndemo_plugin = 1\r\nkeyed = 1\r\nother = 1\r\n");
            File.WriteAllText(Path.Combine(build,"user_config","demo_plugin.ini"),"[demo]\r\nvalue = 2\r\n");
            File.WriteAllText(Path.Combine(build,"plugins","demo_plugin.ini"),"[demo]\r\nvalue = 2\r\n");
            File.WriteAllText(Path.Combine(build,"user_config",".autoload","demo_plugin.receipt.ini"),"[state]\r\nid = tesmio.demo_plugin\r\nversion = 1.0\r\nmode = installed\r\n");
            File.WriteAllText(Path.Combine(build,"user_config",".autoload","demo_plugin.upstream.ini"),"[demo]\r\nvalue = 1\r\n");
            File.WriteAllText(Path.Combine(build,"vfs","media_soviet","resources","rocksalt.png"),"x");
            File.WriteAllText(Path.Combine(build,"user_config",".autoload","content","tesmio.salt","receipt.ini"),
                "[content]\r\nid = tesmio.salt\r\nname = Salt\r\nprovided = 1\r\n[assets]\r\n0 = media_soviet\\resources\\rocksalt.png\r\n[added]\r\nresources = rocksalt | road_salt\r\n");
            // 0.4.99: a keyed editor writes "<plugin>.editor.receipt.ini" for "<plugin>.editor.ini".
            // The plugin behind it owns the loader entry and the upstream copy - both were missed.
            File.WriteAllText(Path.Combine(build,"user_config",".autoload","keyed.editor.receipt.ini"),"[state]\r\nid = tesmio.keyed\r\nversion = 1.0\r\n");
            File.WriteAllText(Path.Combine(build,"user_config",".autoload","keyed.upstream.ini"),"[keyed]\r\nvalue = original\r\n");
            File.WriteAllText(Path.Combine(build,"user_config","keyed.editor.ini"),"[keyed]\r\nvalue = mine\r\n");
            File.WriteAllText(Path.Combine(build,"plugins","keyed.ini"),"[keyed]\r\nvalue = mine\r\n");
            var save=new SaveGame{Name="Ostrava"}; save.Resources.Add("rocksalt");
            var other=new SaveGame{Name="Kladno"}; other.Resources.Add("steel");
            var saves=new List<SaveGame>{save,other};

            ResetPlan data=ResetTool.Plan(build,null,saves,false);
            Check(data.Folders.Count==2&&data.Files.Count==0&&data.Edited.Count==0&&data.Restored.Count==0&&data.Saves.Count==0);

            ResetPlan all=ResetTool.Plan(build,null,saves,true);
            Check(all.Folders.Count==1&&all.Folders[0]=="user_config\\.autoload");
            Check(all.Files.Contains("user_config\\demo_plugin.ini")&&all.Files.Contains("vfs\\media_soviet\\resources\\rocksalt.png"));
            Check(all.Restored.Contains("plugins\\demo_plugin.ini"));
            // the keyed editor: overlay by its own name, plugin INI and loader key by the name without ".editor"
            Check(all.Files.Contains("user_config\\keyed.editor.ini")&&all.Restored.Contains("plugins\\keyed.ini")&&all.Targets.Contains("keyed"));
            Check(all.Ids.Contains("rocksalt")&&all.Ids.Contains("road_salt"));
            Check(all.Saves.Count==1&&all.Saves[0]=="Ostrava");
            Check(all.Edited.Count==1&&all.Edited.Contains("tesmioloader.ini"));

            string report=ResetTool.Apply(build,all,null);
            Check(report.Contains("backup = ")&&Directory.Exists(Path.Combine(home,"tesmioloader","rmm_reset_backup")));
            Check(!File.Exists(Path.Combine(build,"user_config","demo_plugin.ini"))&&!File.Exists(Path.Combine(build,"vfs","media_soviet","resources","rocksalt.png")));
            Check(File.ReadAllText(Path.Combine(build,"plugins","demo_plugin.ini")).Contains("value = 1"));
            Check(!Directory.Exists(Path.Combine(build,"user_config",".autoload")));
            string loader=File.ReadAllText(Path.Combine(build,"tesmioloader.ini"));
            Check(loader.Contains("resources = 1")&&loader.Contains("other = 1")&&!loader.Contains("demo_plugin")&&!loader.Contains("keyed"));
            Check(File.ReadAllText(Path.Combine(build,"plugins","keyed.ini")).Contains("value = original")&&!File.Exists(Path.Combine(build,"user_config","keyed.editor.ini")));
            // A second run has nothing left to say - the state folder is gone and no loader
            // entry belongs to RMM any more.
            ResetPlan again=ResetTool.Plan(build,null,saves,true);
            Check(again.Count==0&&again.Folders.Count==0&&again.Edited.Count==0);});
        Test("the reset never strips a section it was not asked for",()=>{
            string text="[plugins]\r\na = 1\r\nb = 0\r\n\r\n[other]\r\na = 1\r\n";
            string stripped=ResetTool.Strip(text,"plugins",new List<string>{"tesmio.a"});
            Check(!stripped.Contains("a = 1\r\nb")&&stripped.Contains("b = 0")&&stripped.Contains("[other]\r\na = 1"));
            Check(ResetTool.Strip(text,"plugins",null).Contains("[plugins]")&&!ResetTool.Strip(text,"plugins",null).Contains("b = 0"));});
        Test("empty INI values are accepted and lenient text fields keep them",()=>{var blank=new Ini("[railspeed]\nspeed = 330\nspeed_concrete =\nspeed_121 =   ; (stock 121)\n");Check(blank.Get("railspeed","speed_concrete")==""&&blank.Get("railspeed","speed")=="330"&&blank.Get("railspeed","speed_121")=="; (stock 121)");bool emptyKeyRefused=false;try{new Ini("[a]\n = 1\n");}catch(FormatException){emptyKeyRefused=true;}Check(emptyKeyRefused);var lenientText=new Field{Section="railspeed",Key="speed_concrete",Label="Concrete",Type="text",Lenient=true};Check(lenientText.Normalize("")=="");bool strictRefused=false;try{new Field{Section="a",Key="b",Label="B",Type="text"}.Normalize("");}catch(FormatException){strictRefused=true;}Check(strictRefused);});
        Test("log lines are classified and attributed to their subject",()=>{Check(GameLogs.Classify("[19:58:53.317] vehicle_materials  INFO: - [status] Initialization was skipped because the plugin is disabled after 0 ms; 0 warning(s), 0 error(s), 0 fatal error(s)")==GameLogs.Kind.Plain);Check(GameLogs.Classify("[19:43:46.115] hook ok      C3DLog_PrintError      orig=00007FF8A7A5C8E0")==GameLogs.Kind.Plain);Check(GameLogs.Classify("[20:41:46.323] bridge   fixture_vehicle_materials   vehicle_materials.dll declined to install (1)")==GameLogs.Kind.Warning);Check(GameLogs.Classify("[19:43:46.914] game.ERROR setfocus")==GameLogs.Kind.Problem);
            // 0.4.78: the printed level decides; counters of zero never raise an INFO line, counters above zero do.
            Check(GameLogs.Classify("[04:24:35.315] weather_roads  INFO  Init [ready] duration_ms=0 warnings=0 errors=0 fatal=0 (this phase)")==GameLogs.Kind.Plain);
            Check(GameLogs.Classify("[04:24:35.499] research_expansion  INFO: - [status] Startup completed successfully after 16 ms; 12 warning(s), 0 error(s), 0 fatal error(s)")==GameLogs.Kind.Warning);
            Check(GameLogs.Classify("[04:24:35.499] research_expansion  INFO: - [status] Startup failed after 0 ms; 0 warning(s), 1 error(s), 1 fatal error(s)")==GameLogs.Kind.Problem);
            Check(GameLogs.Classify("[04:24:35.502] weather_roads  INFO  Startup [active] duration_ms=0 warnings=3 errors=0 fatal=0")==GameLogs.Kind.Warning);
            Check(GameLogs.Classify("[04:24:35.483] research_expansion  WARN: C:\\x\\icon.png [icon-invalid] wrong size")==GameLogs.Kind.Warning);
            Check(GameLogs.Classify("[04:24:36.181] game.WARN 1")==GameLogs.Kind.Warning);
            // 0.4.83: loader lines print no level; a word introduced by a zero is a counter, not a complaint.
            Check(GameLogs.Classify("[01:55:40.975] buildings_plus  2 generated, 1 up to date, 0 skipped, 0 failed, 0 pruned; 0 warning(s), 0 error(s)")==GameLogs.Kind.Plain);
            Check(GameLogs.Classify("[01:55:41.263] bridge   10 hook(s) loaded, 0 skipped, 10 package(s) with hooks")==GameLogs.Kind.Plain);
            Check(GameLogs.Classify("[01:55:40.975] buildings_plus  0 generated, 0 up to date, 1 skipped, 2 failed; 0 warning(s), 2 error(s)")==GameLogs.Kind.Problem);
            Check(GameLogs.Classify("[01:55:40.975] buildings_plus  3 generated, 1 skipped, 0 failed; 0 error(s)")==GameLogs.Kind.Warning);
            Check(GameLogs.Classify("[01:55:41.213] editor   FAILED  tool name: paint_rocksalt longer than paint_bauxite")==GameLogs.Kind.Problem);
            // 0.4.79: detail logs put the level right after the stamp, without a subject.
            Check(GameLogs.Classify("[2026-09-12 04:53:48.614] INFO  Init [ready] duration_ms=0 warnings=0 errors=0 fatal=0 (this phase)")==GameLogs.Kind.Plain);
            Check(GameLogs.Classify("[2026-09-12 04:53:48.614] INFO: - [status] Startup failed after 0 ms; 0 warning(s), 1 error(s), 1 fatal error(s)")==GameLogs.Kind.Problem);
            Check(GameLogs.Classify("[2026-09-12 04:53:48.614] WARN: plugins\\x.ini [legacy-key] verbose remains supported")==GameLogs.Kind.Warning);
            Check(GameLogs.Classify("[2026-09-12 04:53:48.614] ERROR: plugins\\x.ini [localization] key could not be resolved")==GameLogs.Kind.Problem);
            Check(GameLogs.Classify("[04:24:35.280] technical_service_storage  INFO: - [status] Duplicate policy: resources already rendered by the native Technical Services")==GameLogs.Kind.Plain);Check(GameLogs.Classify("[1] vehicle_materials  FATAL: vehicle_materials.ini [configuration] Configuration is invalid")==GameLogs.Kind.Problem);Check(GameLogs.SubjectOf("[19:43:46.117] plugin   accumulator      1.0      from accumulator.dll")=="plugin"&&GameLogs.SubjectOf("[20:41:46.323] bridge   Workshop C:\\x")=="bridge"&&GameLogs.SubjectOf("[1] vehicle_materials  INFO: x")=="vehicle_materials");var subjects=GameLogs.Subjects(new[]{"[1] plugin   a","[2] bridge   b","[3] plugin   c","[4] ---"});Check(subjects.Count==2&&subjects[0]=="bridge"&&subjects[1]=="plugin");Check(GameLogs.LastRun(new[]{"[1] plugin   10 loaded","[2] --- shutdown: 0 VFS redirects, 10 plugin(s) ---"})=="shutdown: 0 VFS redirects, 10 plugin(s)"&&GameLogs.LastRun(new[]{"[1] plugin   10 loaded"})=="plugin   10 loaded");});
        Test("log sources and loaded versions are read from the loader folder",()=>{string lb=MakeBuild(root,"log-build");Write(Path.Combine(lb,"tesmioloader.log"),"[1] plugin   walking          1.3      from C:\\Program Files (x86)\\Steam\\steamapps\\workshop\\content\\784150\\3773771138\\plugins\\walking.dll\r\n[2] plugin   vehicle_materials 1.2.0-beta from D:\\Games\\SovietRepublic\\tesmioloader\\plugins\\vehicle_materials.dll\n[2] plugin   resources.dll off in tesmioloader.ini [plugins]\n[2] plugin   service \"resources\" v1 from resources.dll\n[2] plugin   UI Layout Fixes  0.3.1    from D:\\Games\\SovietRepublic\\tesmioloader\\plugins\\ui_layout_fixes.dll\n[2] plugin   Technical Service Storage + Grit Spreader 0.3.4    from D:\\Games\\SovietRepublic\\tesmioloader\\plugins\\technical_service_storage.dll\n[3] plugin   10 loaded\n");Write(Path.Combine(lb,"tesmioloader.vehicle_materials.log"),"detail\n");Directory.CreateDirectory(Path.Combine(lb,"logs"));Write(Path.Combine(lb,"logs","tesmioloader.weather_roads.log"),"snow\n");Write(Path.Combine(lb,"logs","notes.txt"),"x");var sources=GameLogs.Sources(lb);Check(sources.Count==3&&sources[0].Name=="tesmioloader.log"&&sources[1].Name=="tesmioloader.vehicle_materials.log"&&GameLogs.Read(sources[1].Path)=="detail\n"&&sources[2].Name=="logs\\tesmioloader.weather_roads.log"&&GameLogs.Read(sources[2].Path)=="snow\n");var v=InstalledPlugins.LoaderVersions(lb);Check(v["walking"]=="1.3"&&v["vehicle_materials"]=="1.2.0-beta"&&!v.ContainsKey("resources")&&v.Count==4,"a path with spaces still gives the DLL's key");Check(v["ui_layout_fixes"]=="0.3.1"&&v["technical_service_storage"]=="0.3.4","a plugin that registers under a display name with spaces counts as loaded");Check(InstalledPlugins.LogTime(lb).HasValue&&!InstalledPlugins.LogTime(MakeBuild(root,"log-build-empty")).HasValue);});

        // ---- 0.16.0: resource pickers ----
        Test("base-game catalogue lists 20 classes and 61 donors with consistent class counts",()=>{Check(ResourceCatalogData.TransportClasses.Length==20&&ResourceCatalogData.TransportClasses[0]=="covered"&&ResourceCatalogData.TransportClasses[17]=="waste"&&ResourceCatalogData.TransportClasses[18]=="cryo"&&ResourceCatalogData.TransportClasses[19]=="hypergolic");Check(ResourceCatalogData.Templates.Length==61&&ResourceCatalogData.Templates.Select(t=>t.Name).Distinct().Count()==61&&ResourceCatalogData.Templates.All(t=>ResourceCatalogData.TransportClasses.Contains(t.Transport)&&ResourceCatalogData.Groups.Contains(t.Group)));Func<string,int> n=c=>ResourceCatalogData.Templates.Count(t=>t.Transport==c);Check(n("covered")==11&&n("gravel")==13&&n("open")==8&&n("oil")==5&&n("waste")==6&&n("nuclear2")==2&&n("cooler")==1&&n("cryo")==1&&n("hypergolic")==2);Check(ResourceCatalogData.Find("lox").Transport=="cryo"&&ResourceCatalogData.Find("kerosene").Transport=="oil"&&ResourceCatalogData.Find("udmh").Transport=="hypergolic"&&ResourceCatalogData.Find("n2o4").Transport=="hypergolic");Check(ResourceCatalogData.Find("eletronics").Transport=="covered"&&ResourceCatalogData.Find("nope")==null);Check(ResourceCatalogData.FamilyName("13")=="plastic"&&ResourceCatalogData.FamilyName("-1")=="none"&&ResourceCatalogData.FamilyName("gravel")=="gravel"&&ResourceCatalogData.Head("oil, 1, 5, 5, 0")=="oil"&&ResourceCatalogData.Head("gravel")=="gravel");});
        Test("transport and family details accept names, class lines and family numbers",()=>{LocalDetailField transport=resourceSpec.Fields.Single(x=>x.Key=="transport"),family=resourceSpec.Fields.Single(x=>x.Key=="family");Check(transport.Type=="choice"&&transport.ChoicePrefix&&transport.Choices.Length==20&&transport.Normalize("CRYO")=="cryo");Check(transport.Normalize("Oil")=="oil"&&transport.Normalize("oil, 1, 5, 5, 0")=="oil, 1, 5, 5, 0"&&transport.Normalize("GRAVEL, 2")=="gravel, 2");Reject(()=>transport.Normalize("bogus, 1"));Check(family.Choices.Length==11&&family.Normalize("13")=="plastic"&&family.Normalize("-1")=="none"&&family.Normalize("toxic")=="toxic");Reject(()=>family.Normalize("9"));});

        // ---- 0.23.0: profiles and restore points ----
        string pfBuild=MakeBuild(root,"profile-build");
        Write(Path.Combine(pfBuild,"tesmioloader.ini"),"[tesmioloader]\nplugins = 1\n[plugins]\nalpha = 1\n");Write(Path.Combine(pfBuild,"plugins","alpha.ini"),"[alpha]\nvalue = 1\n");Write(Path.Combine(pfBuild,"plugins","alpha.dll"),"not a dll");
        Write(Path.Combine(pfBuild,"user_config","alpha.ini"),"[alpha]\nvalue = 2\n");Write(Path.Combine(pfBuild,"user_config",".autoload","alpha.receipt.ini"),"[state]\nid = x\n");
        Test("profile names are validated",()=>{Check(Profiles.ValidName("Basis 1")&&Profiles.ValidName("Test (Sommer)")&&!Profiles.ValidName("")&&!Profiles.ValidName("a/b")&&!Profiles.ValidName("dot.")&&!Profiles.ValidName(new string('x',41)));});
        Test("a profile captures INI, overlay and receipt files but never a DLL",()=>{Profile saved=Profiles.Save(pfBuild,"Basis","erste Sicherung",false);Check(saved.Files.Count==4&&saved.Files.All(f=>!f.Key.EndsWith(".dll"))&&saved.Note=="erste Sicherung"&&File.Exists(saved.Path("user_config\\.autoload\\alpha.receipt.ini")));Check(Profiles.List(pfBuild).Single().Name=="Basis");Reject(()=>Profiles.Save(pfBuild,"Basis","",false));});
        Test("compare shows differing, missing and local-only files",()=>{Write(Path.Combine(pfBuild,"plugins","alpha.ini"),"[alpha]\nvalue = 9\n");File.Delete(Path.Combine(pfBuild,"user_config","alpha.ini"));Write(Path.Combine(pfBuild,"user_config","beta.ini"),"[beta]\nx = 1\n");Write(Path.Combine(pfBuild,"plugins","gamma.ini"),"[gamma]\nx = 1\n");
            var diff=Profiles.Compare(pfBuild,Profiles.Find(pfBuild,"Basis"));Check(diff.Single(d=>d.Relative=="plugins\\alpha.ini").State=="differs"&&diff.Single(d=>d.Relative=="user_config\\alpha.ini").State=="missing_local"&&diff.Single(d=>d.Relative=="user_config\\beta.ini").State=="only_local"&&diff.Single(d=>d.Relative=="plugins\\gamma.ini").State=="kept"&&diff.Single(d=>d.Relative=="tesmioloader.ini").State=="same");});
        Test("applying a profile restores its files, removes unknown overlays, keeps foreign plugin INIs and leaves a restore point",()=>{string backup=Profiles.Apply(pfBuild,Profiles.Find(pfBuild,"Basis"),NoGame);Check(backup.EndsWith("previous"));
            Check(SafeFiles.Text(Path.Combine(pfBuild,"plugins","alpha.ini")).Contains("value = 1")&&File.Exists(Path.Combine(pfBuild,"user_config","alpha.ini"))&&!File.Exists(Path.Combine(pfBuild,"user_config","beta.ini"))&&File.Exists(Path.Combine(pfBuild,"plugins","gamma.ini"))&&File.Exists(Path.Combine(pfBuild,"plugins","alpha.dll")));
            Check(Profiles.Compare(pfBuild,Profiles.Find(pfBuild,"Basis")).All(d=>d.State=="same"||d.State=="kept"));var point=RestorePoints.List(pfBuild).Single(x=>x.PluginId=="profile");Check(point.Complete&&point.Files.Count==5&&point.Changed==3);});
        Test("restoring the restore point brings back the state before the profile was applied",()=>{var point=RestorePoints.List(pfBuild).Single(x=>x.PluginId=="profile");RestorePoints.Restore(pfBuild,point,NoGame);
            Check(SafeFiles.Text(Path.Combine(pfBuild,"plugins","alpha.ini")).Contains("value = 9")&&!File.Exists(Path.Combine(pfBuild,"user_config","alpha.ini"))&&File.Exists(Path.Combine(pfBuild,"user_config","beta.ini")));var again=RestorePoints.List(pfBuild).Single(x=>x.PluginId=="profile");Check(again.Changed==3);});
        Test("a profile can be overwritten and deleted",()=>{Profile second=Profiles.Save(pfBuild,"Basis","",true);Check(second.Files.Count==5&&Profiles.List(pfBuild).Count==1);Profiles.Save(pfBuild,"Zweites",null,false);Check(Profiles.List(pfBuild).Count==2);Profiles.Delete(pfBuild,Profiles.Find(pfBuild,"Basis"));Check(Profiles.List(pfBuild).Single().Name=="Zweites");});
        Test("saving a package leaves a restore point that takes the written files back",()=>{string rpBuild=MakeBuild(root,"restore-build");Write(Path.Combine(rpBuild,"tesmioloader.ini"),"[tesmioloader]\nplugins = 1\n");Write(Path.Combine(rpBuild,"tesmioloader.dll"),"x");Write(Path.Combine(rpBuild,"tesmiolauncher.exe"),"x");
            var s=new Session(simple,rpBuild);s.Set(simple.Fields.Single(x=>x.Id=="general/limit"),"3");s.Commit(true,NoGame);Check(File.Exists(s.UserIni)&&File.Exists(s.Receipt)&&!File.Exists(s.LocalIni));var point=RestorePoints.List(rpBuild).Single();Check(point.PluginId==simple.Id&&point.Files.Any(f=>f.Path==s.UserIni&&f.Absent&&f.Differs));
            RestorePoints.Restore(rpBuild,point,NoGame);Check(!File.Exists(s.UserIni)&&!File.Exists(s.Receipt));var after=RestorePoints.List(rpBuild).Single();Check(after.Files.Any(f=>f.Path==s.UserIni&&!f.Absent&&f.Differs));Reject(()=>RestorePoints.Restore(rpBuild,new RestorePoint{Complete=false,PluginId="x"},NoGame));});


        // ---- 0.31.0: lines fields (repeated keys) for section editors ----
        Test("LooseIni reads and rewrites repeated keys as one block",()=>{var doc=new LooseIni("[general]\r\nenabled = 1\r\n\r\n[grit]\r\nenabled = 1\r\ntarget = a.ini\r\nadd = X 1\r\ntarget = b.ini\r\n\r\n[other]\r\nk = v\r\n");
            Check(doc.GetAll("grit","target").SequenceEqual(new[]{"a.ini","b.ini"})&&doc.GetAll("grit","missing").Count==0);Reject(()=>doc.Get("grit","target"));
            doc.SetAll("grit","target",new[]{"c.ini","d.ini","e.ini"});string text=doc.Render();Check(doc.GetAll("grit","target").SequenceEqual(new[]{"c.ini","d.ini","e.ini"})&&text.IndexOf("target = c.ini")<text.IndexOf("add = X 1")&&doc.Get("other","k")=="v");
            doc.SetAll("grit","remove",new[]{"Y 2"});Check(text.Length<doc.Render().Length&&doc.GetAll("grit","remove").SequenceEqual(new[]{"Y 2"})&&doc.Render().IndexOf("remove = Y 2")<doc.Render().IndexOf("[other]"));
            doc.SetAll("grit","target",new string[0]);Check(doc.GetAll("grit","target").Count==0&&doc.Get("grit","enabled")=="1");});
        string vbsBuild=MakeBuild(root,"vbs-editor");File.WriteAllBytes(Path.Combine(vbsBuild,"plugins","vanilla_buildings.dll"),new byte[]{7,7,7});
        Write(Path.Combine(vbsBuild,"plugins","vanilla_buildings.ini"),"[general]\r\nenabled = 1\r\ndebug = 0\r\n\r\n[example]\r\nenabled = 0\r\ntarget = buildings_types\\plastics_factory.ini\r\nreplace = $PRODUCTION plastics 0.11 | $PRODUCTION plastics 0.20\r\nadd = $PRODUCTION glass 0.45\r\n\r\n[grit]\r\nenabled = 1\r\ntarget = buildings_types\\technical_services.ini\r\ntarget = 2496571917\\utrzymanie1\\building.ini\r\nadd = $STORAGE_IMPORT_SPECIAL RESOURCE_TRANSPORT_GRAVEL 120 gravel\r\nadd = $STORAGE_IMPORT_SPECIAL RESOURCE_TRANSPORT_GRAVEL 120 sand\r\n");
        Write(Path.Combine(vbsBuild,"tesmioloader.ini"),"[plugins]\nvanilla_buildings=1\n");
        string vbsSchema=Path.Combine(root,"vbs-schema","vanilla_buildings.launcher.ini");Directory.CreateDirectory(Path.GetDirectoryName(vbsSchema));
        Write(vbsSchema,"[launcher]\nlayout_version=1\neditor_type=keyed_sections\nid=tesmio.vanilla_buildings\nname=Vanilla Buildings\nversion=1.3\ndefault_tab=buildings\n[editor]\nplugin=vanilla_buildings\nconfig=vanilla_buildings.ini\nlist_section=list\nreserved_sections=general\nmaximum_items=256\n[tab:general]\nlabel=General\norder=10\n[tab:buildings]\nlabel=Buildings\norder=20\n[group:buildings]\ntab=buildings\nlabel=Rule sets\n[list]\nlabel=Rule sets\nsummary=enabled|target\n[new]\nname_label=Name\n[global]\ntab=general\nlabel=Plugin\n[detail:enabled]\nscope=global\nsection=general\nkey=enabled\ntype=boolean\nlabel=Enabled\n[detail:rule_enabled]\nscope=item\nkey=enabled\ntype=boolean\nlabel=Rule enabled\norder=10\n[detail:target]\nscope=item\nkey=target\ntype=lines\ndialog=1\nlabel=Targets\norder=20\n[detail:add]\nscope=item\nkey=add\ntype=lines\nlabel=Add\norder=30\n[detail:replace]\nscope=item\nkey=replace\ntype=lines\nlabel=Replace\norder=40\n");
        LocalEditorSpec vbsSpec=LocalEditorSpec.Load(vbsSchema);LocalResourceSession vbs=new LocalResourceSession(vbsSpec,vbsBuild);LocalDetailField vbsTarget=vbsSpec.Fields.Single(x=>x.Id=="target"),vbsAdd=vbsSpec.Fields.Single(x=>x.Id=="add");
        Test("a keyed_sections schema without [source] and with lines fields loads",()=>Check(vbsSpec.IsSections&&vbsSpec.SourcePlugin.Length==0&&vbsTarget.Type=="lines"&&vbsTarget.InDialog&&vbsSpec.Fields.Count(x=>x.Scope=="item"&&x.Type=="lines")==3));
        // ---- 0.34.0: keyed_list without [source] (grit materials of Technical Service Storage) ----
        Test("a keyed_list schema without [source] accepts typed identifiers and keeps the other sections",()=>{
            string gritBuild=MakeBuild(root,"grit-editor");Write(Path.Combine(gritBuild,"grit.launcher.ini"),"[launcher]\nlayout_version=1\neditor_type=keyed_list\nid=t.grit\nname=Grit\nversion=1\n[editor]\nplugin=technical_service_storage\nconfig=technical_service_storage.ini\nlist_section=grit_materials\nmaximum_items=32\n[activity]\nsection=general\nkey=enabled\nvalues=1\n[column:strength]\ntype=decimal\nminimum=0\nmaximum=1\ndefault=0.5\nlabel=Strength\n[detail:debug]\nscope=global\nsection=general\nkey=debug\ntype=boolean\nlabel=Debug\n");
            var gritSpec=LocalEditorSpec.Load(Path.Combine(gritBuild,"grit.launcher.ini"));File.WriteAllBytes(Path.Combine(gritBuild,"plugins","technical_service_storage.dll"),new byte[]{1});
            Write(Path.Combine(gritBuild,"plugins","technical_service_storage.ini"),"; header\r\n[general]\r\nenabled = 1\r\ndebug = 0\r\n\r\n[grit_materials]\r\n; strengths\r\nsand = 0.30\r\ngravel = 0.45\r\n\r\n[sand_diagnostic]\r\nenabled = 1\r\n");Write(Path.Combine(gritBuild,"tesmioloader.ini"),"[tesmioloader]\nplugins=1\n[plugins]\ntechnical_service_storage=1\n");
            var grit=new LocalResourceSession(gritSpec,gritBuild);Check(gritSpec.IsList&&gritSpec.SourcePlugin.Length==0&&gritSpec.Columns.Count==1&&grit.Items().Count==2);
            grit.AddRaw("road_salt","0.80");Reject(()=>grit.AddRaw("bad name","0.5"));Reject(()=>grit.AddRaw("sand","0.5"));grit.Commit(NoGame);
            var doc=new LooseIni(SafeFiles.Text(grit.LocalIni));Check(doc.Get("grit_materials","road_salt")=="0.8"&&doc.Get("grit_materials","sand")=="0.30"&&doc.Get("sand_diagnostic","enabled")=="1"&&doc.Get("general","debug")=="0"&&SafeFiles.Text(grit.LocalIni).Contains("; strengths"));
        });
        Test("repeated keys are read as one multi-line value and summarised with a count",()=>{var items=vbs.Items();Check(items.Count==2&&items[1].Id=="grit"&&vbs.Value("grit",vbsTarget)=="buildings_types\\technical_services.ini\n2496571917\\utrzymanie1\\building.ini"&&items[1].Subtitle=="1 · buildings_types\\technical_services.ini (+1)"&&items[0].Subtitle=="0 · buildings_types\\plastics_factory.ini");});
        Test("editing a lines field rewrites the repeated key block and keeps the other keys",()=>{vbs.SetField("grit",vbsAdd,"$STORAGE_IMPORT_SPECIAL RESOURCE_TRANSPORT_GRAVEL 120 gravel\r\n\r\n$STORAGE_IMPORT_SPECIAL RESOURCE_TRANSPORT_GRAVEL 120 sand\r\n$STORAGE_IMPORT_SPECIAL RESOURCE_TRANSPORT_GRAVEL 120 road_salt");var doc=new LooseIni(vbs.Effective());
            Check(doc.GetAll("grit","add").Count==3&&doc.GetAll("grit","add")[2].EndsWith("road_salt")&&doc.GetAll("grit","target").Count==2&&doc.Get("grit","enabled")=="1"&&doc.Get("general","debug")=="0");
            vbs.SetField("grit",vbsAdd,"$STORAGE_IMPORT_SPECIAL RESOURCE_TRANSPORT_GRAVEL 120 gravel\n$STORAGE_IMPORT_SPECIAL RESOURCE_TRANSPORT_GRAVEL 120 sand");Check(!vbs.Overrides.Items.ContainsKey("grit"));
            Reject(()=>vbs.SetField("grit",vbsAdd,"[bad]"));});
        Test("a personal rule set with several targets survives commit and reopen",()=>{vbs.AddSection("mine",new Dictionary<string,string>{{"target","buildings_types\\a.ini\r\nbuildings_types\\b.ini"},{"add","$PRODUCTION glass 0.1"}});vbs.SetField("mine",vbsSpec.Fields.Single(x=>x.Id=="rule_enabled"),"1");vbs.Commit(NoGame);
            var reopened=new LocalResourceSession(vbsSpec,vbsBuild);var doc=new LooseIni(SafeFiles.Text(reopened.LocalIni));Check(doc.GetAll("mine","target").SequenceEqual(new[]{"buildings_types\\a.ini","buildings_types\\b.ini"})&&doc.GetAll("mine","add").SequenceEqual(new[]{"$PRODUCTION glass 0.1"})&&doc.Get("mine","enabled")=="1");
            Check(reopened.Value("mine",vbsTarget)=="buildings_types\\a.ini\nbuildings_types\\b.ini"&&SafeFiles.Text(reopened.UserIni).Contains("field.target.1 = buildings_types\\b.ini")&&reopened.Items().Count==3);});

        // ---- 0.32.0: building picker data and lines field attributes ----
        Test("game buildings are scanned from buildings_types, DLC folders and Workshop items",()=>{string game=Path.Combine(root,"fake-game");string ms=Path.Combine(game,"media_soviet");
            Write(Path.Combine(ms,"buildings_types","cement_plant.ini"),"$NAME 1\r\n$OBSOLETE\r\n$TYPE_FACTORY\r\n");Write(Path.Combine(ms,"buildings_types","cement_plant_v2.ini"),"$NAME 2\r\n$TYPE_FACTORY\r\n$STORAGE 1\r\n");Write(Path.Combine(ms,"buildings_types","flat.ini"),"$TYPE_LIVING\r\n");
            Write(Path.Combine(ms,"dlc3","buildings","technical_services_small","building.ini"),"$NAME 3\r\n$TYPE_GARBAGE_OFFICE\r\n");Write(Path.Combine(ms,"dlc1","other.txt"),"x");
            string ws=Path.Combine(root,"fake-workshop");Write(Path.Combine(ws,"2496571917","utrzymanie1","building.ini"),"$TYPE_GARBAGE_OFFICE\r\n");Write(Path.Combine(ws,"2496571917","workshopconfig.ini"),"$ITEM_ID 2496571917\r\n$ITEM_NAME \"Technical services\"\r\n");Write(Path.Combine(ws,"notanid","x","building.ini"),"$TYPE_LIVING\r\n");
            string gameBuild=Path.Combine(game,"tesmioloader","build");Directory.CreateDirectory(gameBuild);Check(GameBuildings.GameRoot(gameBuild)==game&&GameBuildings.GameRoot(root)==null);
            var list=GameBuildings.Scan(gameBuild,ws);Check(list.Count==5&&list[0].Target=="buildings_types\\cement_plant.ini"&&list[0].Obsolete&&list[0].Type=="FACTORY"&&list[0].Origin=="game"&&!list[1].Obsolete&&list[2].Type=="LIVING");
            var dlc=list.Single(x=>x.Origin=="dlc3");Check(dlc.Target=="dlc3\\buildings\\technical_services_small\\building.ini"&&dlc.Name=="technical_services_small"&&dlc.OriginLabel=="3"&&dlc.Type=="GARBAGE_OFFICE");
            var shop=list.Single(x=>x.Origin.StartsWith("workshop:"));Check(shop.Target=="2496571917\\utrzymanie1\\building.ini"&&shop.OriginLabel=="Technical services"&&shop.OriginRank==2&&dlc.OriginRank==1);});
        Test("lines fields accept picker, count_label and maximum_lines and enforce the cap",()=>{string schema=Path.Combine(root,"picker-schema","x.launcher.ini");Write(schema,"[launcher]\nlayout_version=1\neditor_type=keyed_sections\nid=t.x\nname=X\nversion=1\ndefault_tab=b\n[editor]\nplugin=x\nconfig=x.ini\nlist_section=list\nreserved_sections=general\nmaximum_items=9\n[tab:b]\nlabel=B\norder=10\n[group:b]\ntab=b\nlabel=B\n[list]\nlabel=B\n[global]\ntab=b\nlabel=P\n[detail:target]\nscope=item\nkey=target\ntype=lines\npicker=game_buildings\ncount_label=Buildings\ncount_label_key=x.count\nmaximum_lines=2\nlabel=T\n");
            var spec=LocalEditorSpec.Load(schema);var f=spec.Fields.Single();Check(f.Picker=="game_buildings"&&f.CountLabel=="Buildings"&&f.CountLabelKey=="x.count"&&f.MaximumLines==2&&f.Normalize("a\nb")=="a\nb");Reject(()=>f.Normalize("a\nb\nc"));
            Write(schema,"[launcher]\nlayout_version=1\neditor_type=keyed_sections\nid=t.x\nname=X\nversion=1\ndefault_tab=b\n[editor]\nplugin=x\nconfig=x.ini\nlist_section=list\nreserved_sections=general\nmaximum_items=9\n[tab:b]\nlabel=B\norder=10\n[group:b]\ntab=b\nlabel=B\n[list]\nlabel=B\n[global]\ntab=b\nlabel=P\n[detail:name]\nscope=item\nkey=name\ntype=text\npicker=game_buildings\nlabel=T\n");Reject(()=>LocalEditorSpec.Load(schema));});

        // ---- 0.33.0: section_prefix for keyed_sections (research_expansion [modify:<id>]) ----
        Test("a section prefix limits the items to prefixed sections and leaves free lines alone",()=>{string reBuild=MakeBuild(root,"re-editor");File.WriteAllBytes(Path.Combine(reBuild,"plugins","research_expansion.dll"),new byte[]{9,9,9});
            Write(Path.Combine(reBuild,"plugins","research_expansion.ini"),"[general]\r\nenabled = 1\r\ndebug = 0\r\n\r\n$RESEARCH quartz_smasher\r\n+faculty_geology\r\n$TYPE_TECHNICAL\r\n$COST 1800\r\n$RESEARCH_ADD\r\n\r\n[modify:faculty_geology]\r\nenabled = 1\r\nreplace = $COST 1500 | $COST 1800\r\nmove_before = $UNLOCK_RESEARCH uranium_study | $UNLOCK_RESEARCH bauxite_study\r\n\r\n[modify:engineering_1]\r\nenabled = 0\r\nadd = $UNLOCK_TOOLS forklift\r\n");
            Write(Path.Combine(reBuild,"tesmioloader.ini"),"[plugins]\nresearch_expansion=1\n");
            string schema=Path.Combine(root,"re-schema","research_expansion.launcher.ini");Write(schema,"[launcher]\nlayout_version=1\neditor_type=keyed_sections\nid=tesmio.research_expansion\nname=RE\nversion=1.5\ndefault_tab=m\n[editor]\nplugin=research_expansion\nconfig=research_expansion.ini\nlist_section=list\nreserved_sections=general\nsection_prefix=modify:\nmaximum_items=256\n[tab:m]\nlabel=M\norder=10\n[group:m]\ntab=m\nlabel=M\n[list]\nlabel=M\nsummary=enabled|replace\n[new]\nname_label=Id\n[global]\ntab=m\nlabel=P\n[detail:enabled]\nscope=global\nsection=general\nkey=enabled\ntype=boolean\nlabel=E\n[detail:rule_enabled]\nscope=item\nkey=enabled\ntype=boolean\nlabel=RE\norder=10\n[detail:replace]\nscope=item\nkey=replace\ntype=lines\nlabel=R\norder=20\n[detail:add]\nscope=item\nkey=add\ntype=lines\nlabel=A\norder=30\n");
            var spec=LocalEditorSpec.Load(schema);Check(spec.SectionPrefix=="modify:");var re=new LocalResourceSession(spec,reBuild);var items=re.Items();
            Check(items.Count==2&&items[0].Id=="faculty_geology"&&items[1].Id=="engineering_1"&&items[0].Subtitle=="1 · $COST 1500 | $COST 1800"&&re.Value("faculty_geology",spec.Fields.Single(x=>x.Id=="replace"))=="$COST 1500 | $COST 1800");
            re.AddSection("bauxite_study",new Dictionary<string,string>{{"add","$UNLOCK_TOOLS crane"}});re.SetField("engineering_1",spec.Fields.Single(x=>x.Id=="rule_enabled"),"1");re.Suppress("faculty_geology");
            var doc=new LooseIni(re.Effective());string text=re.Effective();
            Check(doc.HasSection("modify:bauxite_study")&&doc.GetAll("modify:bauxite_study","add").SequenceEqual(new[]{"$UNLOCK_TOOLS crane"})&&doc.Get("modify:engineering_1","enabled")=="1"&&!doc.HasSection("modify:faculty_geology")&&text.Contains("$RESEARCH quartz_smasher")&&text.Contains("+faculty_geology")&&doc.Get("general","debug")=="0");
            re.Unsuppress("faculty_geology");Check(re.Items().Count==3&&re.Items().Any(x=>x.Id=="bauxite_study"&&x.Owned));re.Commit(NoGame);var reopened=new LocalResourceSession(spec,reBuild);Check(reopened.Items().Count==3&&new LooseIni(SafeFiles.Text(reopened.LocalIni)).HasSection("modify:bauxite_study"));});

        // 0.4.80: content packages - fragments merged into the target editors as originals, assets
        // copied into the vfs, all of it taken back when the package is switched off.
        Test("a content package is provided through the target editors, updated and removed again (0.4.80)",()=>
        {
            Action<int,bool> C=(n,ok)=>{if(!ok)throw new Exception("content check "+n+" failed");};
            string pkg=Path.Combine(root,"content","salt_pack");
            Write(Path.Combine(pkg,"soviet.mod.ini"),"[mod]\nid=example.salt\nname=Salt Pack\nversion=1.0\nenabled=1\n[content]\nresources=tesmio\\resources.ini\ndeposits=tesmio\\deposits.ini\nbuildings=tesmio\\buildings.ini\nassets=assets\n");
            Write(Path.Combine(pkg,"tesmio","resources.ini"),"[list]\nraw_salt = rawgravel, Raw Salt\n\n[custom:raw_salt]\ncargo = bulk\nkind = 0\ntransport = gravel\n");
            Write(Path.Combine(pkg,"tesmio","deposits.ini"),"[rocksalt]\ntoken = $TYPE_MINE_ROCKSALT\ntype = 10\nmap = resourcemap2\ncomponent = 2\nradius = ore\nicon = raw_salt\nminimap = 1\neditor = salt\n");
            Write(Path.Combine(pkg,"tesmio","buildings.ini"),"; salt buildings\n[salt_mine]\ndonor = bauxite_mine\nname = Salt Mine\nline = $TYPE_MINE_ROCKSALT\nline = $PRODUCTION raw_salt 3.0\n");
            Write(Path.Combine(pkg,"assets","media_soviet","resources","raw_salt.png"),"png");
            Package cp=Package.Load(pkg);C(1,cp.Kind=="content"&&cp.ContentFragments.Count==3&&cp.AssetFiles.Count==1&&cp.AssetFiles[0]=="media_soviet\\resources\\raw_salt.png"&&cp.Hints.Any(h=>Msg.Plain(h).Contains("assets")));
            Reject(()=>Package.Load(Path.Combine(root,"content","empty")));
            Write(Path.Combine(root,"content","broken","soviet.mod.ini"),"[mod]\nid=example.broken\nname=Broken\nversion=1\nenabled=1\n[content]\nresources=tesmio\\missing.ini\n");Reject(()=>Package.Load(Path.Combine(root,"content","broken")));
            string build=MakeBuild(root,"content-build");
            File.WriteAllBytes(Path.Combine(build,"plugins","resources.dll"),p.Dll);Write(Path.Combine(build,"plugins","resources.ini"),"[list]\nsand = bauxite, Sand\n\n[base_price]\n\n[price]\n\n[resources]\nhook = 2\n");
            File.WriteAllBytes(Path.Combine(build,"plugins","deposits.dll"),p.Dll);Write(Path.Combine(build,"plugins","deposits.ini"),depositsText);
            File.WriteAllBytes(Path.Combine(build,"plugins","buildings_plus.dll"),p.Dll);Write(Path.Combine(build,"plugins","buildings_plus.ini"),"[buildings_plus]\nenabled = 1\nprune = 0\n");
            Write(Path.Combine(build,"tesmioloader.ini"),"[tesmioloader]\nplugins=1\n[plugins]\nresources=1\ndeposits=1\nbuildings_plus=1\n");
            LocalEditorSpec bpSpec=LocalEditorSpec.Load(Path.GetFullPath(Path.Combine(args[0],"..","..","settings_schemas","buildings_plus.launcher.ini")));
            Func<string,LocalResourceSession> sessions=plugin=>plugin=="resources"?new LocalResourceSession(resourceSpec,build):plugin=="deposits"?new LocalResourceSession(depositsSpec,build):plugin=="buildings_plus"?new LocalResourceSession(bpSpec,build):null;
            string resIni=Path.Combine(build,"plugins","resources.ini"),depIni=Path.Combine(build,"plugins","deposits.ini"),bpIni=Path.Combine(build,"plugins","buildings_plus.ini"),png=Path.Combine(LocalEditorSpec.VfsRoot(build),"media_soviet","resources","raw_salt.png");
            var content=new ContentSession(cp,build);
            C(2,!content.Provided&&!content.NeedsWrite&&content.Targets["resources"]=="resources"&&content.Targets["deposits"]=="deposits"&&content.Targets["buildings"]=="buildings_plus"&&content.Notes.Count==0);
            content.PendingOn=true;C(3,content.NeedsWrite);content.Commit(NoGame,sessions);
            C(4,content.Provided&&!content.NeedsWrite&&File.Exists(Path.Combine(content.StateDir,"resources.ini"))&&File.Exists(png)&&ContentSession.IsProvided(build,"example.salt"));
            string resText=SafeFiles.Text(resIni);C(5,resText.Contains("raw_salt = rawgravel, Raw Salt")&&resText.Contains("[custom:raw_salt]")&&!content.Skipped.ContainsKey("resources"));
            var dep=new LooseIni(SafeFiles.Text(depIni));C(6,dep.HasSection("rocksalt")&&dep.Get("rocksalt","type")=="12"&&dep.Get("rocksalt","map")=="auto"&&dep.Get("rocksalt","component")==null&&dep.Get("copper","type")=="10");
            var bp=new LooseIni(SafeFiles.Text(bpIni));C(7,bp.HasSection("salt_mine")&&bp.GetAll("salt_mine","line").SequenceEqual(new[]{"$TYPE_MINE_ROCKSALT","$PRODUCTION raw_salt 3.0"})&&bp.Get("buildings_plus","enabled")=="1");
            // The editors show the entries as originals, and their own save keeps them out of the base.
            var res=new LocalResourceSession(resourceSpec,build);C(8,res.Items().Any(x=>x.Id=="raw_salt"&&!x.Owned)&&res.Content.Count==1&&res.Content[0].Added.SequenceEqual(new[]{"raw_salt"})&&res.Content[0].Skipped.Count==0);
            res.Add("glass","aluminium","Glass","covered");res.Commit(NoGame);
            resText=SafeFiles.Text(resIni);C(9,resText.Contains("raw_salt = rawgravel")&&resText.Contains("glass = aluminium, Glass")&&!SafeFiles.Text(res.UpstreamFile).Contains("raw_salt"));
            // A hand edit of the effective INI does not turn the package's entries into base entries.
            File.AppendAllText(resIni,"\n[custom:sand]\ncargo = bulk\n",SafeFiles.Utf8);var edited=new LocalResourceSession(resourceSpec,build);
            C(10,System.Text.RegularExpressions.Regex.Matches(edited.Effective(),"raw_salt = rawgravel").Count==1&&edited.Items().Any(x=>x.Id=="glass"&&x.Owned));edited.Commit(NoGame);C(11,!SafeFiles.Text(edited.UpstreamFile).Contains("raw_salt")&&SafeFiles.Text(edited.UpstreamFile).Contains("[custom:sand]"));
            // A changed fragment in the Workshop is an update; saving takes it over and keeps the deposit's number.
            Write(Path.Combine(pkg,"tesmio","buildings.ini"),"[salt_mine]\ndonor = bauxite_mine\nname = Salt Mine\nline = $PRODUCTION raw_salt 4.0\n");Write(Path.Combine(pkg,"tesmio","deposits.ini"),"[rocksalt]\ntoken = $TYPE_MINE_ROCKSALT\ntype = 10\nmap = auto\nradius = ore\nicon = raw_salt\nminimap = 1\neditor = salt\n");
            cp=Package.Load(pkg);var again=new ContentSession(cp,build);C(12,again.Provided&&again.UpdatePending&&again.NeedsWrite&&again.PendingOn);again.Commit(NoGame,sessions);
            C(13,!again.UpdatePending&&SafeFiles.Text(bpIni).Contains("$PRODUCTION raw_salt 4.0")&&!SafeFiles.Text(bpIni).Contains("raw_salt 3.0")&&new LooseIni(SafeFiles.Text(depIni)).Get("rocksalt","type")=="12");
            // Switching off takes fragments, assets and entries away; personal entries stay.
            var off=new ContentSession(cp,build);off.PendingOn=false;C(14,off.NeedsWrite);off.Commit(NoGame,sessions);
            resText=SafeFiles.Text(resIni);C(15,!off.Provided&&!ContentSession.IsProvided(build,"example.salt")&&!File.Exists(Path.Combine(off.StateDir,"resources.ini"))&&!File.Exists(png)&&!resText.Contains("raw_salt")&&resText.Contains("glass = aluminium")&&resText.Contains("[custom:sand]")&&!new LooseIni(SafeFiles.Text(depIni)).HasSection("rocksalt")&&!new LooseIni(SafeFiles.Text(bpIni)).HasSection("salt_mine")&&new LooseIni(SafeFiles.Text(bpIni)).Get("buildings_plus","enabled")=="1");
            // A file that was in the vfs before the package is neither overwritten nor removed later.
            string own=Path.Combine(LocalEditorSpec.VfsRoot(build),"media_soviet","resources","raw_salt.png");
            Write(own,"mine");
            var keep=new ContentSession(cp,build);keep.PendingOn=true;keep.Commit(NoGame,sessions);
            C(16,keep.Provided&&SafeFiles.Text(own)=="mine"&&keep.Skipped.ContainsKey("assets")&&keep.Skipped["assets"].SequenceEqual(new[]{"media_soviet\\resources\\raw_salt.png"}));
            var drop=new ContentSession(cp,build);drop.PendingOn=false;drop.Commit(NoGame,sessions);
            C(17,!drop.Provided&&File.Exists(own)&&SafeFiles.Text(own)=="mine");
            File.Delete(own);
            // A package without any target plugin says so and provides only its files.
            string bare=MakeBuild(root,"content-bare");var lonely=new ContentSession(cp,bare);C(18,lonely.Notes.Count==3&&lonely.Targets.Values.All(x=>x.Length==0));lonely.PendingOn=true;lonely.Commit(NoGame,sessions);C(19,lonely.Provided&&File.Exists(Path.Combine(LocalEditorSpec.VfsRoot(bare),"media_soviet","resources","raw_salt.png")));
            // 0.4.87: an id somebody already has blocks the whole package - nothing is written and
            // no file lands in the vfs, so the player's own entry cannot be pushed aside quietly.
            string clashPkg=Path.Combine(root,"content","clash_pack");
            Write(Path.Combine(clashPkg,"soviet.mod.ini"),"[mod]\nid=example.clash\nname=Clash Pack\nversion=1.0\nenabled=1\n[content]\nresources=tesmio\\resources.ini\nassets=assets\n");
            Write(Path.Combine(clashPkg,"tesmio","resources.ini"),"[list]\nglass = steel, Glass Clash\nbrandnew = steel, Brand New\n\n[custom:brandnew]\ncargo = bulk\n");
            Write(Path.Combine(clashPkg,"assets","media_soviet","resources","brandnew.png"),"png");
            var clash=new ContentSession(Package.Load(clashPkg),build);
            var found=clash.Conflicts(sessions);
            C(20,found.ContainsKey("resources")&&found["resources"].SequenceEqual(new[]{"glass"}));
            clash.PendingOn=true;
            string reason="";try{clash.Commit(NoGame,sessions);}catch(RuleException e){reason=e.TranslationKey;}
            string afterClash=SafeFiles.Text(resIni);
            C(21,reason=="content_conflict"&&!clash.Provided&&!afterClash.Contains("brandnew")
                &&!File.Exists(Path.Combine(LocalEditorSpec.VfsRoot(build),"media_soviet","resources","brandnew.png"))
                &&!File.Exists(Path.Combine(clash.StateDir,"resources.ini")));
            // Once the entry in the way is gone, the same package goes in.
            var freed=new LocalResourceSession(resourceSpec,build);freed.Remove("glass");freed.Commit(NoGame);
            var retry=new ContentSession(Package.Load(clashPkg),build);
            C(22,retry.Conflicts(sessions).Count==0);
            retry.PendingOn=true;retry.Commit(NoGame,sessions);
            C(23,retry.Provided&&SafeFiles.Text(resIni).Contains("brandnew = steel, Brand New"));
            retry.PendingOn=false;retry.Commit(NoGame,sessions);
            // 0.6.5: translated names. A [names:<language>] section of the fragment travels with
            // the list entry, key by key; a key for a resource the package does not bring stays out.
            string namePkg=Path.Combine(root,"content","name_pack");
            Write(Path.Combine(namePkg,"soviet.mod.ini"),"[mod]\nid=example.names\nname=Name Pack\nversion=1.0\nenabled=1\n[content]\nresources=tesmio\\resources.ini\n");
            Write(Path.Combine(namePkg,"tesmio","resources.ini"),"[list]\nwire = steel, Wire\n\n[names:German]\nwire = Draht\nstranger = Fremd\n");
            var named=new ContentSession(Package.Load(namePkg),build);named.PendingOn=true;named.Commit(NoGame,sessions);
            var namedDoc=new LooseIni(SafeFiles.Text(resIni));
            C(24,namedDoc.Get("list","wire")=="steel, Wire"&&namedDoc.Get("names:German","wire")=="Draht"&&namedDoc.Get("names:German","stranger")==null);
            // A hand edit of the effective INI and then, as the very next step, switching the
            // package off: its entries go all the same and do not become base entries.
            namedDoc.Set("names:German","sand","Sand DE");File.WriteAllText(resIni,namedDoc.Render(),SafeFiles.Utf8);
            var namedOff=new ContentSession(Package.Load(namePkg),build);namedOff.PendingOn=false;namedOff.Commit(NoGame,sessions);
            namedDoc=new LooseIni(SafeFiles.Text(resIni));
            C(25,namedDoc.Get("list","wire")==null&&namedDoc.Get("names:German","wire")==null&&namedDoc.Get("names:German","sand")=="Sand DE");
            var afterNames=new LocalResourceSession(resourceSpec,build);
            C(26,!SafeFiles.Text(afterNames.UpstreamFile).Contains("wire")&&SafeFiles.Text(afterNames.UpstreamFile).Contains("Sand DE"));
            C(27,new ContentSession(Package.Load(namePkg),build).Conflicts(sessions).Count==0);
            // 0.6.8: a package's resource the player has hidden does not stand in the way when the
            // package is switched off, and nothing of it is left behind.
            var hidePkg=new ContentSession(Package.Load(namePkg),build);hidePkg.PendingOn=true;hidePkg.Commit(NoGame,sessions);
            var hider=new LocalResourceSession(resourceSpec,build);hider.Suppress("wire");hider.Commit(NoGame);
            C(28,!SafeFiles.Text(resIni).Contains("wire")&&new LocalResourceSession(resourceSpec,build).SuppressedIds().Contains("wire"));
            var hideOff=new ContentSession(Package.Load(namePkg),build);hideOff.PendingOn=false;hideOff.Commit(NoGame,sessions);
            var afterHide=new LocalResourceSession(resourceSpec,build);
            C(29,!afterHide.Items().Any(x=>x.Id=="wire")&&afterHide.SuppressedIds().Count==0&&!SafeFiles.Text(resIni).Contains("wire"));
        });

        // 0.6.8: an original resource can be left out of the effective list.
        Test("an original resource can be hidden and shown again, with its custom section, prices and names (0.6.8)",()=>
        {
            string rpSchema=Path.GetFullPath(Path.Combine(args[0],"..","..","settings_schemas","resources_plus.launcher.ini"));
            LocalEditorSpec rp=LocalEditorSpec.Load(rpSchema);
            string build=MakeBuild(root,"hide-build");
            File.WriteAllBytes(Path.Combine(build,"tesmioloader.dll"),simple.Dll);File.WriteAllBytes(Path.Combine(build,"tesmiolauncher.exe"),simple.Dll);
            File.WriteAllBytes(Path.Combine(build,"plugins","resources_plus.dll"),simple.Dll);
            Write(Path.Combine(build,"plugins","resources_plus.ini"),"[list]\nsand = bauxite, Sand\nglass = eletronics, Glass\nclay = bauxite, Clay\n\n[base_price]\nglass = 260, 300\nclay = 5, 4\n\n[price]\n\n[resources]\nhook = 2\n\n[custom:glass]\ntransport = covered\n\n[custom:clay]\ncargo = bulk\n\n[names:German]\nglass = Glas\nclay = Lehm\n");
            Write(Path.Combine(build,"tesmioloader.ini"),"[tesmioloader]\nplugins=1\n[plugins]\nresources_plus=1\n");
            var s=new LocalResourceSession(rp,build);
            Reject(()=>s.Suppress("nothere"));
            s.Suppress("glass");
            var doc=new LooseIni(s.Effective());
            Check(doc.Get("list","glass")==null&&!doc.HasSection("custom:glass")&&doc.Get("base_price","glass")==null&&doc.Get("names:German","glass")==null,"the hidden resource leaves the list with its custom section, its price and its name");
            Check(doc.Get("list","sand")=="bauxite, Sand"&&doc.Get("list","clay")=="bauxite, Clay"&&doc.HasSection("custom:clay")&&doc.Get("base_price","clay")!=null&&doc.Get("names:German","clay")=="Lehm"&&doc.Get("resources","hook")=="2","the other resources and the plugin's own settings stay");
            Check(!s.Items().Any(x=>x.Id=="glass")&&s.SuppressedIds().SequenceEqual(new[]{"glass"})&&s.Dirty,"it is gone from the list and named as hidden");
            s.Commit(NoGame);
            var again=new LocalResourceSession(rp,build);
            Check(!again.Dirty&&!again.Items().Any(x=>x.Id=="glass")&&again.SuppressedIds().Count==1&&SafeFiles.Text(again.UpstreamFile).Contains("glass = eletronics, Glass")&&!SafeFiles.Text(again.LocalIni).Contains("glass"),"after the save it stays hidden; the original base keeps it, the effective file does not");
            again.Unsuppress("glass");doc=new LooseIni(again.Effective());
            Check(doc.Get("list","glass")=="eletronics, Glass"&&doc.HasSection("custom:glass")&&doc.Get("base_price","glass")!=null&&doc.Get("names:German","glass")=="Glas"&&again.SuppressedIds().Count==0,"shown again it comes back with everything");
            var order=new LooseIni(again.Effective()).Entries("list").Select(x=>x.Key).ToList();
            Check(order.SequenceEqual(new[]{"sand","glass","clay"}),"and at its old place in the list: "+String.Join(",",order));
        });

        // 0.6.7: Deposits Plus takes the deposits the player added to the original's deposits.ini
        // over once, and a package-backed editor keeps entries of its effective INI that RMM did
        // not write.
        Test("a package-backed editor takes the original's own entries over once and rescues a foreign effective INI (0.6.7)",()=>
        {
            string dpRoot=Path.Combine(root,"pkg-dp");
            File.WriteAllBytes(Path.Combine(Directory.CreateDirectory(Path.Combine(dpRoot,"plugins")).FullName,"deposits_plus.dll"),simple.Dll);
            Write(Path.Combine(dpRoot,"plugins","deposits_plus.ini"),"[deposits_plus]\ncode_patch = 1\n\n[copper]\ntoken = $TYPE_MINE_COPPER\ntype = 10\n\n[sand]\ntoken = $TYPE_MINE_SAND\ntype = 11\n");
            Write(Path.Combine(dpRoot,"rmm.mod.ini"),"[mod]\nid = tesmio.deposits_plus\nname = Deposits Plus\nversion = 0.4.15\nreplaces = deposits\n");
            Write(Path.Combine(dpRoot,"config","deposits_plus.launcher.ini"),"[launcher]\nlayout_version=1\neditor_type=keyed_sections\nid=tesmio.deposits_plus\nname=Deposits Plus\ndefault_tab=deposits\n[editor]\nplugin=deposits_plus\nconfig=deposits_plus.ini\nlist_section=list\nreserved_sections=deposits_plus\nmaximum_items=20\n[tab:general]\nlabel=General\norder=10\n[tab:deposits]\nlabel=Deposits\norder=30\n[group:deposits]\ntab=deposits\nlabel=Deposits\n[global]\ntab=general\nlabel=Plugin\n[detail:code_patch]\nscope=global\nsection=deposits_plus\nkey=code_patch\ntype=boolean\n[detail:token]\nscope=item\nkey=token\ntype=text\nunique=1\ndialog=1\n[detail:type]\nscope=item\nkey=type\ntype=integer\nminimum=10\nmaximum=127\nunique=1\nauto_increment=1\ndialog=1\n[detail:map]\nscope=item\nkey=map\ntype=text\n");
            Package dp=Package.Load(dpRoot);LocalEditorSpec dpSpec=LocalEditorSpec.Load(dp.EditorSchema);
            string b=MakeBuild(root,"dp-build");Write(Path.Combine(b,"tesmioloader.dll"),"fake");Write(Path.Combine(b,"tesmiolauncher.exe"),"fake");
            // Nothing to take over: no original, then an original with the shipped deposits only.
            var plainDp=new LocalResourceSession(dpSpec,b,dp);Check(!plainDp.Dirty&&plainDp.Items().Count==2,"without an original nothing changes");
            string theirs=Path.Combine(b,"plugins","deposits.ini");
            Write(theirs,"[deposits]\ncode_patch = 1\n\n[copper]\ntoken = $TYPE_MINE_COPPER\ntype = 10\n\n[sand]\ntoken = $TYPE_MINE_SAND\ntype = 11\n");
            var same=new LocalResourceSession(dpSpec,b,dp);Check(!same.Dirty&&same.Overrides.Adopted==""&&same.Items().Count==2,"an original with only the shipped deposits leaves the session clean");
            // A deposit of the player, two with a number or token that is taken, and the settings section.
            Write(theirs,"[deposits]\ncode_patch = 1\n\n[copper]\ntoken = $TYPE_MINE_COPPER\ntype = 10\n\n[Nickel]\ntoken = $TYPE_MINE_NICKEL\ntype = 14\nmap = auto\n\n[lead]\ntoken = $TYPE_MINE_LEAD\ntype = 11\n\n[tin]\ntoken = $TYPE_MINE_SAND\ntype = 15\n");
            var take=new LocalResourceSession(dpSpec,b,dp);
            Check(take.Dirty&&take.Overrides.Adopted=="deposits"&&take.Items().Count==3&&take.Items().Single(x=>x.Id=="nickel").Owned&&!take.Items().Any(x=>x.Id=="deposits"||x.Id=="lead"||x.Id=="tin"),"the player's deposit becomes his own entry; the settings section and the two collisions do not: "+String.Join(",",take.Items().Select(x=>x.Id)));
            Check(take.Notes.Any(n=>Msg.KeyOf(n)=="res_note_adopted_original"&&Msg.Plain(n).Contains("nickel"))&&take.Notes.Any(n=>Msg.KeyOf(n)=="res_note_not_taken"&&Msg.Plain(n).Contains("lead")&&Msg.Plain(n).Contains("tin")),"one note names what came over, one what did not");
            take.Commit(NoGame);
            var eff=new LooseIni(SafeFiles.Text(take.LocalIni));Check(eff.Get("nickel","token")=="$TYPE_MINE_NICKEL"&&eff.Get("nickel","type")=="14"&&eff.Get("nickel","map")=="auto"&&eff.HasSection("copper")&&!eff.HasSection("deposits"),"number, token and the other keys are written as they were");
            // Once: a deposit the player deletes afterwards does not come back.
            var later=new LocalResourceSession(dpSpec,b,dp);Check(!later.Dirty&&later.Items().Count==3,"the saved state opens clean");later.Remove("nickel");later.Commit(NoGame);
            var gone=new LocalResourceSession(dpSpec,b,dp);Check(!gone.Dirty&&gone.Items().Count==2&&!gone.Items().Any(x=>x.Id=="nickel"),"the take-over runs once");
            // Rescue: the effective INI was written by someone else (the plugin itself, or by hand).
            string b2=MakeBuild(root,"dp-build-2");Write(Path.Combine(b2,"tesmioloader.dll"),"fake");Write(Path.Combine(b2,"tesmiolauncher.exe"),"fake");
            Write(Path.Combine(b2,"plugins","deposits_plus.ini"),"; created by deposits_plus\n[deposits_plus]\ncode_patch = 1\n\n[copper]\ntoken = $TYPE_MINE_COPPER\ntype = 10\n\n[sand]\ntoken = $TYPE_MINE_SAND\ntype = 11\n\n[zinc]\ntoken = $TYPE_MINE_ZINC\ntype = 20\n");
            var rescue=new LocalResourceSession(dpSpec,b2,dp);
            Check(rescue.Dirty&&rescue.Items().Single(x=>x.Id=="zinc").Owned&&rescue.Notes.Any(n=>Msg.KeyOf(n)=="res_note_rescued"&&Msg.Plain(n).Contains("zinc")),"an entry RMM did not write becomes an entry of the player");
            rescue.Commit(NoGame);Check(new LooseIni(SafeFiles.Text(rescue.LocalIni)).Get("zinc","type")=="20","and is still there after the save");
            var calm=new LocalResourceSession(dpSpec,b2,dp);Check(!calm.Dirty&&calm.Items().Count==3&&!calm.Notes.Any(n=>Msg.KeyOf(n)=="res_note_rescued"),"the file RMM wrote itself is not rescued again");
        });

        // 0.6.6: the names per game language of Resources Plus are keyed fields; their section
        // carries a colon ([names:German]).
        Test("Resources Plus: a name per game language is a keyed field in [names:<Language>] (0.6.6)",()=>
        {
            string rpSchema=Path.GetFullPath(Path.Combine(args[0],"..","..","settings_schemas","resources_plus.launcher.ini"));
            LocalEditorSpec rp=LocalEditorSpec.Load(rpSchema);
            LocalDetailField german=rp.Fields.Single(x=>x.Id=="name_german"),french=rp.Fields.Single(x=>x.Id=="name_french");
            Check(german.Scope=="keyed"&&german.Section=="names:German"&&german.Type=="text"&&rp.Fields.Count(x=>x.Section.StartsWith("names:"))==19,"19 name fields, one per language file of the game but English");
            string build=MakeBuild(root,"names-build");
            File.WriteAllBytes(Path.Combine(build,"tesmioloader.dll"),simple.Dll);File.WriteAllBytes(Path.Combine(build,"tesmiolauncher.exe"),simple.Dll);
            File.WriteAllBytes(Path.Combine(build,"plugins","resources_plus.dll"),simple.Dll);
            Write(Path.Combine(build,"plugins","resources_plus.ini"),"[list]\nsand = bauxite, Sand\nclay = bauxite, Clay\n\n[resources]\nhook = 2\n\n[names:German]\nsand = Sand DE\n");
            Write(Path.Combine(build,"tesmioloader.ini"),"[tesmioloader]\nplugins=1\n[plugins]\nresources_plus=1\n");
            var s=new LocalResourceSession(rp,build);
            Check(s.BaselineValue("sand",german)=="Sand DE"&&s.Value("sand",german)=="Sand DE"&&s.Value("clay",german)=="","the original name is the baseline, a resource without one reads empty");
            s.SetField("clay",german,"Lehm");s.SetField("sand",german,"Feiner Sand");s.SetField("sand",french,"Sable");
            Reject(()=>s.SetField("clay",german,"a=b"));Reject(()=>s.SetField("clay",german,new String('x',64)));
            s.Add("wire","steel","Wire","open");s.SetField("wire",german,"Draht");
            s.Commit(NoGame);
            var doc=new LooseIni(SafeFiles.Text(Path.Combine(build,"plugins","resources_plus.ini")));
            Check(doc.Get("names:German","clay")=="Lehm"&&doc.Get("names:German","sand")=="Feiner Sand"&&doc.Get("names:French","sand")=="Sable"&&doc.Get("names:German","wire")=="Draht"&&doc.Get("list","sand")=="bauxite, Sand","names land in their language sections, the list line is untouched");
            // Back to the original name, and a resource of the player takes its name along when it goes.
            var again=new LocalResourceSession(rp,build);
            again.SetField("sand",german,again.BaselineValue("sand",german));again.Remove("wire");again.Commit(NoGame);
            doc=new LooseIni(SafeFiles.Text(Path.Combine(build,"plugins","resources_plus.ini")));
            Check(doc.Get("names:German","sand")=="Sand DE"&&doc.Get("names:German","wire")==null&&doc.Get("list","wire")==null&&doc.Get("names:German","clay")=="Lehm","reset and removal");
        });

        // 0.4.80: the Buildings Plus comfort round - donor picker, donor lines, assigned id.
        Test("game_donor, donor_lines and assigned_from are parsed and checked (0.4.80)",()=>
        {
            string schema=Path.GetFullPath(Path.Combine(args[0],"..","..","settings_schemas","buildings_plus.launcher.ini"));
            var spec=LocalEditorSpec.Load(schema);
            LocalDetailField donor=spec.Fields.Single(x=>x.Id=="bp_donor"),line=spec.Fields.Single(x=>x.Id=="bp_line"),bid=spec.Fields.Single(x=>x.Id=="bp_id");
            Check(donor.Picker=="game_donor"&&donor.Reference=="game_donor"&&line.Picker=="donor_lines"&&bid.AssignedFrom=="build:plugins\\buildings_plus.ids.ini"&&bid.AssignedSection=="ids");
            // A fake game folder: two donors, one of them with lines to take over.
            string game=Path.Combine(root,"donor-game"),build=Path.Combine(game,"tesmioloader","build");
            Directory.CreateDirectory(Path.Combine(build,"plugins"));
            string types=Path.Combine(game,"media_soviet","buildings_types");
            Write(Path.Combine(types,"bauxite_mine.ini"),"$NAME 26301\n$TYPE_MINE_BAUXITE\n\n$WORKERS_NEEDED 45\n$PRODUCTION rawbauxite 0.50\n-----------\n$CONNECTION_ROAD\n1 0 0\n2 0 0\n$PRODUCTION rawbauxite 0.50\n");
            Write(Path.Combine(types,"shop_clothes.ini"),"$TYPE_SHOP\n");
            var lines=GameBuildings.DonorLines(build,"bauxite_mine");
            Check(lines.SequenceEqual(new[]{"$NAME 26301","$TYPE_MINE_BAUXITE","$WORKERS_NEEDED 45","$PRODUCTION rawbauxite 0.50","$CONNECTION_ROAD"}));
            // 0.7.3: the command editor needs the file itself - data lines and repeated lines included.
            var rawDonor=GameBuildings.DonorFileLines(build,"bauxite_mine");
            Check(rawDonor!=null&&rawDonor.Count(x=>x.Trim()=="$PRODUCTION rawbauxite 0.50")==2&&rawDonor.Contains("1 0 0")&&GameBuildings.DonorFileLines(build,"missing")==null,"the donor's file comes back unchanged: the repeated line twice, the data lines, and null for a donor that is not there");
            Check(ClonePlan.Check(rawDonor,null,null,new[]{"$PRODUCTION rawbauxite 0.50"},null).Any&&!ClonePlan.Check(lines,null,null,new[]{"$PRODUCTION rawbauxite 0.50"},null).Any,"a line that stands twice in the donor is refused when the file is judged - the pick list would have let it through");
            Check(GameBuildings.DonorFile(build,"shop_clothes")!=null&&GameBuildings.DonorFile(build,"missing")==null&&GameBuildings.DonorFile(build,"..\\evil")==null&&GameBuildings.DonorLines(build,"missing").Count==0);
            var sets=new ReferenceSets(build,null,"en");
            Check(ReferenceSets.Known("game_donor")&&sets.Contains("game_donor","bauxite_mine")&&!sets.Contains("game_donor","nope"));
            // An unknown donor blocks saving; the picked one passes.
            File.WriteAllBytes(Path.Combine(build,"plugins","buildings_plus.dll"),p.Dll);
            Write(Path.Combine(build,"plugins","buildings_plus.ini"),"[buildings_plus]\nenabled = 1\n");
            Write(Path.Combine(build,"tesmioloader.ini"),"[tesmioloader]\nplugins=1\n[plugins]\nbuildings_plus=1\n");
            var bp=new LocalResourceSession(spec,build);bp.References=sets;
            bp.AddSection("salt_mine",new Dictionary<string,string>{{"bp_donor","nope"}});
            string reason="";try{bp.ValidateAll();}catch(RuleException e){reason=e.TranslationKey;}
            Check(reason=="reference_donor");
            bp.SetField("salt_mine",donor,"bauxite_mine");bp.ValidateAll();bp.Commit(NoGame);
            // assigned_from: the catalog of the plugin, read by the entry's id.
            Write(Path.Combine(build,"plugins","buildings_plus.ids.ini"),"[ids]\nsalt_mine = 9300000001\n");
            Check(new LooseIni(SafeFiles.Text(spec.ResolvePath(bid.AssignedFrom,build))).Get(bid.AssignedSection,"salt_mine")=="9300000001");
            // 0.4.86: one building of a Workshop item as the donor, written <item>\<object>.
            // Searched in the Steam Workshop folder and in media_soviet\workshop_wip - the
            // fake game has no Steam library above it, so the second place is what answers.
            Write(Path.Combine(game,"media_soviet","workshop_wip","mod_item","house_a","building.ini"),"$NAME 6161\n$TYPE_LIVING\n");
            Write(Path.Combine(game,"media_soviet","workshop_wip","mod_item","house_b","building.ini"),"$NAME 6162\n$TYPE_LIVING\n");
            Check(GameBuildings.DonorFile(build,"mod_item\\house_a")!=null&&GameBuildings.DonorFile(build,"mod_item/house_a")!=null);
            Check(GameBuildings.DonorFile(build,"mod_item\\house_x")==null&&GameBuildings.DonorFile(build,"mod_item\\..\\..\\secret")==null&&GameBuildings.DonorFile(build,"mod_item\\a\\b")==null);
            var donorSets=new ReferenceSets(build,null,"en");
            Check(donorSets.Contains("game_donor","mod_item\\house_b")&&!donorSets.Contains("game_donor","mod_item\\house_c"));
            // Bad schema values are refused.
            string bad=Path.Combine(root,"donor-bad","bad.launcher.ini");string text=SafeFiles.Text(schema);
            Write(bad,text.Replace("picker = game_donor","picker = game_buildings"));Check(!SafeFiles.Text(bad).Contains("picker = game_donor"));Reject(()=>LocalEditorSpec.Load(bad));
            Write(bad,text.Replace("assigned_from = build:plugins\\buildings_plus.ids.ini|ids","assigned_from = build:plugins\\buildings_plus.ids.ini"));Reject(()=>LocalEditorSpec.Load(bad));
        });

        // 0.4.85: empty_hint / empty_hint_key - what an empty field means, shown in place of the origin line.
        Test("empty_hint says what an empty field means and is translated (0.4.85)",()=>
        {
            string folder=Path.Combine(root,"empty-hint"),schema=Path.Combine(folder,"sample.launcher.ini");
            Write(schema,"[launcher]\neditor_type=keyed_sections\nlayout_version=1\nid=x\nname=X\nlanguage_directory=languages\n[editor]\nplugin=x\nconfig=x.ini\nmaximum_items=8\n"+
                "[detail:vb_name]\nscope=item\nkey=name\ntype=text\nlabel=Name\nempty_hint=empty = the game's own id\nempty_hint_key=vb.item_name.empty\n"+
                "[detail:bare]\nscope=item\nkey=bare\ntype=text\nlabel=Plain\n");
            Write(Path.Combine(folder,"languages","de.ini"),"[strings]\nvb.item_name.empty = leer = Spieleigene ID\n");
            var spec=LocalEditorSpec.Load(schema);
            LocalDetailField named=spec.Fields.Single(x=>x.Id=="vb_name"),bare=spec.Fields.Single(x=>x.Id=="bare");
            Check(named.EmptyHint=="empty = the game's own id"&&named.EmptyHintKey=="vb.item_name.empty"&&bare.EmptyHint.Length==0);
            // The key wins where a translation exists; English falls back to the literal in the schema.
            // (A Language of its own needs the embedded UI texts, which the test EXE does not carry.)
            Check(spec.LanguageDirectory=="languages"&&SafeFiles.Text(Path.Combine(folder,"languages","de.ini")).Contains("vb.item_name.empty = leer = Spieleigene ID"));
        });

        // 0.5.21: choice_labels / choice_labels_key - the list shows a text per value, the INI keeps the value.
        Test("choice_labels show a text per value and map it back (0.5.21)",()=>
        {
            string folder=Path.Combine(root,"choice-labels"),schema=Path.Combine(folder,"sample.launcher.ini");
            Write(schema,"[launcher]\neditor_type=keyed_sections\nlayout_version=1\nid=x\nname=X\n[editor]\nplugin=x\nconfig=x.ini\nmaximum_items=8\n"+
                "[detail:word]\nscope=global\nsection=x\nkey=word\ntype=choice\nchoices=stop|halt\nchoice_labels=STOP (Latin)|HALT (old)\nchoice_labels_key=x.word\nlabel=Word\n"+
                "[detail:wrong]\nscope=global\nsection=x\nkey=wrong\ntype=choice\nchoices=a|b|c\nchoice_labels=A|B\nlabel=Wrong count\n"+
                "[detail:plain]\nscope=global\nsection=x\nkey=plain\ntype=choice\nchoices=a|b\nlabel=Plain\n");
            var spec=LocalEditorSpec.Load(schema);
            LocalDetailField word=spec.Fields.Single(x=>x.Id=="word"),wrong=spec.Fields.Single(x=>x.Id=="wrong"),bareChoice=spec.Fields.Single(x=>x.Id=="plain");
            Check(word.ChoiceLabels.Length==2&&word.ChoiceLabelsKey=="x.word"&&bareChoice.ChoiceLabels.Length==0&&bareChoice.ChoiceLabelsKey.Length==0);
            // Without a key the literal labels are used; a count that does not match falls back to the values.
            // (The key lookup needs a Language, which needs the embedded UI texts the test EXE does not carry.)
            word.ChoiceLabelsKey="";
            Check(spec.ChoiceText(null,word,"halt")=="HALT (old)"&&spec.ChoiceValue(null,word,"HALT (old)")=="halt");
            Check(spec.ChoiceText(null,word,"other")=="other"&&spec.ChoiceValue(null,word,"other")=="other"&&spec.ChoiceText(null,word,"")=="");
            Check(spec.ChoiceText(null,wrong,"b")=="b"&&spec.ChoiceText(null,bareChoice,"a")=="a"&&spec.ChoiceValue(null,bareChoice,"b")=="b");
        });

        // 0.4.88: what a saved game needs, and what the next start will do.
        Test("saved games are read and the start order is derived (0.4.88)",()=>
        {
            string game=Path.Combine(root,"start-game"),build=Path.Combine(game,"tesmioloader","build");
            Directory.CreateDirectory(Path.Combine(build,"plugins"));
            Write(Path.Combine(game,"media_soviet","save","1 - Siberia",SaveGames.Manifest),
                "[tesmioloader]\nversion=1\n[plugins]\nresources=1\nvanilla_buildings=1\n[resources]\ncable=1\nsteel, Cable=1\n[deposits]\nrocksalt=1\n");
            Write(Path.Combine(game,"media_soviet","save","2 - Empty",SaveGames.Manifest),"[tesmioloader]\nversion=1\n[plugins]\nresources=1\n");
            Write(Path.Combine(game,"media_soviet","saved_last",SaveGames.Manifest),"[tesmioloader]\nversion=1\n[plugins]\nneeds=1\n[resources]\nglass=1\n");
            var saves=SaveGames.Scan(build);
            Check(saves.Count==3,"three saves, found "+saves.Count);
            Check(saves.Count==3&&saves.Any(s=>s.Name=="1 - Siberia"&&s.Plugins.Contains("vanilla_buildings")&&s.Resources.Contains("cable")&&s.Deposits.Contains("rocksalt")));
            // The broken key of an older loader ("steel, Cable=1") still names the resource in front of the comma.
            Check(saves.Single(s=>s.Name=="1 - Siberia").Resources.Count(x=>x=="cable")==1);
            Check(SaveGames.Using(saves,"plugins",new[]{"vanilla_buildings"}).Count==1&&SaveGames.Using(saves,"resources",new[]{"glass"}).Count==1&&SaveGames.Using(saves,"resources",new[]{"nothing"}).Count==0);
            Check(SaveGames.Names(SaveGames.Using(saves,"plugins",new[]{"resources"}),1,"und {0} weitere").Contains("und 1 weitere"));
            // Load order: the loader sorts every plugin DLL it finds by key and loads what is not switched off.
            Write(Path.Combine(build,"tesmioloader.ini"),"[tesmioloader]\nplugins=1\n[plugins]\nlocalization=1\nold_thing=0\nswitched_off=0\n");
            string shop=Path.Combine(root,"start-shop");
            File.WriteAllBytes(Path.Combine(build,"plugins","localization.dll"),simple.Dll);
            foreach(string name in new[]{"vanilla_buildings","Zebra","switched_off"}){Directory.CreateDirectory(Path.Combine(shop,name,"plugins"));File.WriteAllBytes(Path.Combine(shop,name,"plugins",name+".dll"),simple.Dll);}
            Write(Path.Combine(shop,"vanilla_buildings","rmm.mod.ini"),"[mod]\nid = tesmio.vanilla_buildings\nname = Vanilla Buildings\nversion = 0.4.4\n[dependencies]\ntesmio.localization = >=0.4.0\n");
            LoaderFiles.Forget();
            var lineup=Catalog.ScanAll(build,shop,"",new List<string>());
            var order=Startup.Order(build,lineup);
            Check(order.Count==3&&order[0].Key=="localization"&&!order[0].Workshop&&order[1].Key=="vanilla_buildings"&&order[1].Workshop&&order[1].Name=="Vanilla Buildings"&&order[2].Key=="Zebra","by key, without case, wherever the DLL lies");
            Check(!order.Any(s=>s.Key=="old_thing"||s.Key=="switched_off"),"a key without a DLL and a switched-off plugin load nothing");
            // Vanilla Buildings needs Localization, which sorts first: nothing is late and nothing is unmet.
            Check(Startup.LateDependencies(order,lineup).Count==0,"nothing late");Check(Startup.Unmet(build,lineup).Count==0,"nothing unmet: "+String.Join(" | ",Startup.Unmet(build,lineup).Select(u=>u.Entry.Key+" "+u.Reason+" "+u.Dependency.Id)));
            // A plugin whose provider sorts after it gets its service too late.
            Write(Path.Combine(shop,"Zebra","rmm.mod.ini"),"[mod]\nid = example.zebra\nname = Zebra\nversion = 1.0\n");
            Write(Path.Combine(shop,"vanilla_buildings","rmm.mod.ini"),"[mod]\nid = tesmio.vanilla_buildings\nname = Vanilla Buildings\nversion = 0.4.4\n[dependencies]\nexample.zebra = *\n");
            LoaderFiles.Forget();lineup=Catalog.ScanAll(build,shop,"",new List<string>());
            var late=Startup.LateDependencies(Startup.Order(build,lineup),lineup);
            Check(late.Count==1&&late[0].Key=="Vanilla Buildings"&&late[0].Value=="Zebra","late: "+String.Join(" | ",late.Select(x=>x.Key+">"+x.Value)));
            // The provider switched off: the plugin that needs it is named, and switching that
            // one off as well settles the start.
            Write(Path.Combine(build,"tesmioloader.ini"),"[tesmioloader]\nplugins=1\n[plugins]\nZebra=0\nswitched_off=0\n");
            Catalog.ResolveAll(lineup,build);
            var unmet=Startup.Unmet(build,lineup);
            Check(unmet.Count==1&&unmet[0].Entry.Key=="vanilla_buildings"&&unmet[0].Reason=="off"&&unmet[0].Dependency.Target=="Zebra","unmet: "+String.Join(" | ",unmet.Select(u=>u.Entry.Key+" "+u.Reason+" "+u.Dependency.Target)));
            Startup.SwitchOff(build,unmet.Select(u=>u.Entry.Key),NoGame);
            Check(!LoaderFiles.Enabled(build,"vanilla_buildings")&&Startup.Unmet(build,lineup).Count==0&&Startup.Order(build,lineup).Count==1,"switched off for the start");
            // The version history of a README is what an update mark does not say.
            string pkg=Path.Combine(root,"changes");
            Write(Path.Combine(pkg,"README_EN.md"),"# Thing\n\n- **0.2.0:** donors may come from the Workshop\n- **0.1.0:** first release\n");
            Check(Startup.Changes(pkg,"0.2.0","en")=="donors may come from the Workshop"&&Startup.Changes(pkg,"9.9","en")==""&&Startup.Changes(null,"0.2.0","en")=="");
        });




        // 0.5.10: Resources Plus is a fork of resources.dll with a file of its own,
        // plugins\resources_plus.ini, and its own local schema - like Deposits Plus.
        Test("Resources Plus has its own schema, INI and loader key, and switches the original off with itself",()=>{
            Check(ContentTargets.PluginsFor("resources").SequenceEqual(new[]{"resources_plus","resources"})&&ContentTargets.PluginsFor("needs").SequenceEqual(new[]{"needs_plus","needs"}),"content packages prefer the forks, then the originals");
            string rpSchema=Path.GetFullPath(Path.Combine(args[0],"..","..","settings_schemas","resources_plus.launcher.ini"));
            var rpSpec=LocalEditorSpec.Load(rpSchema);
            Check(rpSpec.Plugin=="resources_plus"&&rpSpec.ConfigName=="resources_plus.ini","the schema names the fork and its INI");
            string rpBuild=MakeBuild(root,"resources-plus-build");
            File.WriteAllBytes(Path.Combine(rpBuild,"plugins","resources_plus.dll"),simple.Dll);
            Write(Path.Combine(rpBuild,"plugins","resources_plus.ini"),"[resources]\nhook = 2\n[list]\nunobtainium = aluminium, Unobtainium\n");
            Write(Path.Combine(rpBuild,"tesmioloader.ini"),"[tesmioloader]\nplugins=1\n[plugins]\nresources=0\nresources_plus=1\n");
            File.WriteAllBytes(Path.Combine(rpBuild,"tesmioloader.dll"),simple.Dll);File.WriteAllBytes(Path.Combine(rpBuild,"tesmiolauncher.exe"),simple.Dll);
            var rp=new LocalResourceSession(rpSpec,rpBuild);
            Check(rp.LoaderEnabled&&rp.LocalDll.EndsWith("resources_plus.dll")&&rp.Items().Any(x=>x.Id=="unobtainium"),"the editor opens on the fork's own files");
            rp.SetLoaderEnabled(false);rp.ValidateAll();rp.Commit(()=>LocalResourceGuard.Check(rp));
            Check(new Ini(SafeFiles.Text(Path.Combine(rpBuild,"tesmioloader.ini"))).Get("plugins","resources_plus","1")=="0","the switch is written under the fork's key");
            var rpEntry=new CatalogEntry{Root=rpSchema,Supported=true,Problem="",LocalEditor=true};
            Check(!RuntimeStatus.ConfiguredActive(rpEntry,rpBuild),"switched off, the fork does not count as active");
            // The fork and the plugin it replaces never run together: switching the fork on
            // switches the original off in the same save.
            File.WriteAllBytes(Path.Combine(rpBuild,"plugins","resources.dll"),simple.Dll);
            Write(Path.Combine(rpBuild,"tesmioloader.ini"),"[tesmioloader]\nplugins=1\n[plugins]\nresources=1\nresources_plus=0\n");
            LoaderFiles.Forget();
            var rp2=new LocalResourceSession(rpSpec,rpBuild);rp2.Replaced.Add("resources");
            Check(!rp2.LoaderEnabled&&!rp2.LoaderChanged&&rp2.SwitchesOff().Count==0,"off at load: nothing to settle");
            rp2.SetLoaderEnabled(true);
            Check(rp2.LoaderChanged&&rp2.SwitchesOff().SequenceEqual(new[]{"resources"}),"switched on, it takes the original's switch along");
            rp2.ValidateAll();rp2.Commit(()=>LocalResourceGuard.Check(rp2));
            var rpLoader=new Ini(SafeFiles.Text(Path.Combine(rpBuild,"tesmioloader.ini")));
            Check(rpLoader.Get("plugins","resources_plus","")=="1"&&rpLoader.Get("plugins","resources","")=="0","both switches are written in one save");
            Check(RuntimeStatus.ConfiguredActive(rpEntry,rpBuild),"and the fork counts as active");
        });
        // 0.5.15: Needs Plus is the same kind of fork for needs.dll - own schema, own INI
        // (plugins\needs_plus.ini seeded from the player's needs.ini), paused under SML.
        Test("Needs Plus has its own schema, INI and loader key and is seeded from needs.ini",()=>{
            string npSchema=Path.GetFullPath(Path.Combine(args[0],"..","..","settings_schemas","needs_plus.launcher.ini"));
            var npSpec=LocalEditorSpec.Load(npSchema);
            Check(npSpec.Plugin=="needs_plus"&&npSpec.ConfigName=="needs_plus.ini"&&npSpec.IsList,"the schema names the fork and its INI and keeps the list editor");
            string npBuild=MakeBuild(root,"needs-plus-build");
            File.WriteAllBytes(Path.Combine(npBuild,"plugins","needs_plus.dll"),simple.Dll);
            File.WriteAllBytes(Path.Combine(npBuild,"plugins","resources_plus.dll"),simple.Dll);
            Write(Path.Combine(npBuild,"plugins","resources_plus.ini"),"[resources]\nhook = 2\n[list]\nfurniture = eletronics, Furniture\n");
            Write(Path.Combine(npBuild,"plugins","needs.ini"),"[list]\nfurniture = eletronics, 1.0, advanced, 0.35, 0.010\n[needs]\nenabled = 1\n");
            Write(Path.Combine(npBuild,"tesmioloader.ini"),"[tesmioloader]\nplugins=1\n[plugins]\nneeds=0\nneeds_plus=1\nresources=0\nresources_plus=1\n");
            File.WriteAllBytes(Path.Combine(npBuild,"tesmioloader.dll"),simple.Dll);File.WriteAllBytes(Path.Combine(npBuild,"tesmiolauncher.exe"),simple.Dll);
            string from=ForkSeed.Ensure(npBuild,"needs_plus","needs_plus.ini");
            Check(from.EndsWith(Path.Combine("plugins","needs.ini")),"the original's needs.ini is the source of the fork's file");
            string made=SafeFiles.Text(Path.Combine(npBuild,"plugins","needs_plus.ini"));
            Check(made.StartsWith("; needs_plus.ini - created by Republic Mod Manager")&&made.Contains("furniture = eletronics, 1.0, advanced"),"the copy carries a header and the original's list");
            var np=new LocalResourceSession(npSpec,npBuild);
            Check(np.LoaderEnabled&&np.LocalDll.EndsWith("needs_plus.dll")&&np.Items().Any(x=>x.Id=="furniture"),"the editor opens on the fork's own files");
            np.SetLoaderEnabled(false);np.ValidateAll();np.Commit(()=>LocalResourceGuard.Check(np));
            Check(new Ini(SafeFiles.Text(Path.Combine(npBuild,"tesmioloader.ini"))).Get("plugins","needs_plus","1")=="0","the switch is written under the fork's key");
            Check(ContentTargets.Resolve(npBuild,"needs")=="needs_plus","a needs content package lands in the fork the loader finds");
        });
        // 0.5.11: the fork's INI is created once from the player's list, by the rule the plugin uses.
        Test("Resources Plus takes the player's list over when its INI is missing (0.5.11)",()=>{
            string rpSchema=Path.GetFullPath(Path.Combine(args[0],"..","..","settings_schemas","resources_plus.launcher.ini"));
            // The original lies in the Workshop folder, its INI beside the DLL: that list is the
            // source when plugins\ has no resources.ini.
            string seedBuild=MakeBuild(root,"fork-seed-shop"),seedShop=Path.Combine(root,"fork-seed-items");
            File.WriteAllBytes(Path.Combine(seedBuild,"plugins","resources_plus.dll"),simple.Dll);
            Directory.CreateDirectory(Path.Combine(seedShop,"3777","plugins"));File.WriteAllBytes(Path.Combine(seedShop,"3777","plugins","resources.dll"),simple.Dll);
            Write(Path.Combine(seedShop,"3777","plugins","resources.ini"),"[resources]\nhook = 2\n[list]\nsand = bauxite, Sand\n");
            Write(Path.Combine(seedBuild,"tesmioloader.ini"),"[tesmioloader]\nplugins=1\n[plugins]\nresources=0\nresources_plus=1\n");
            LoaderFiles.Remember(seedBuild,seedShop,LoaderFiles.Plugins(seedBuild,seedShop));
            string from=ForkSeed.Ensure(seedBuild,"resources_plus","resources_plus.ini");
            Check(from.EndsWith(Path.Combine("3777","plugins","resources.ini")),"the Workshop original's INI is the source when plugins\\ has none");
            string made=SafeFiles.Text(Path.Combine(seedBuild,"plugins","resources_plus.ini"));
            Check(made.StartsWith("; resources_plus.ini - created by Republic Mod Manager")&&made.Contains("sand = bauxite, Sand"),"the copy carries a header and the original's list");
            Check(ForkSeed.Ensure(seedBuild,"resources_plus","resources_plus.ini")=="","an existing file is never touched again");
            Check(SafeFiles.Text(Path.Combine(seedShop,"3777","plugins","resources.ini")).StartsWith("[resources]"),"the Workshop file is unchanged");
            // A plugin that is no fork gets the INI shipped beside its own Workshop DLL - what a
            // plugin that only looks into plugins\ needs to work at all.
            Directory.CreateDirectory(Path.Combine(seedShop,"3781","plugins"));File.WriteAllBytes(Path.Combine(seedShop,"3781","plugins","deposits.dll"),simple.Dll);
            Write(Path.Combine(seedShop,"3781","plugins","deposits.ini"),"[deposits]\ncode_patch = 1\n");
            LoaderFiles.Remember(seedBuild,seedShop,LoaderFiles.Plugins(seedBuild,seedShop));
            Check(ForkSeed.Ensure(seedBuild,"deposits","deposits.ini").EndsWith(Path.Combine("3781","plugins","deposits.ini"))&&SafeFiles.Text(Path.Combine(seedBuild,"plugins","deposits.ini")).Contains("code_patch = 1"),"the shipped INI is copied into plugins");
            Check(ForkSeed.Ensure(seedBuild,"deposits_plus","deposits_plus.ini")=="","a plugin that is not found seeds nothing");
            LoaderFiles.Forget();
            // The original's own file is the source when it is not generated output.
            string rpPlainBuild=MakeBuild(root,"fork-seed-plain");
            File.WriteAllBytes(Path.Combine(rpPlainBuild,"plugins","resources_plus.dll"),simple.Dll);
            Write(Path.Combine(rpPlainBuild,"plugins","resources.ini"),"[resources]\nhook = 2\n[list]\nunobtainium = aluminium, Unobtainium\n");
            Write(Path.Combine(rpPlainBuild,"tesmioloader.ini"),"[tesmioloader]\nplugins=1\n[plugins]\nresources=0\nresources_plus=1\n");
            var seeded=new LocalResourceSession(LocalEditorSpec.Load(rpSchema),rpPlainBuild);
            Check(seeded.SeededFrom.EndsWith("resources.ini")&&seeded.Items().Any(x=>x.Id=="unobtainium"),"opening the page creates the file from plugins\\resources.ini and lists its entries");
            Check(SafeFiles.Text(Path.Combine(rpPlainBuild,"plugins","resources.ini")).StartsWith("[resources]"),"the original's file is unchanged");
            // The source catalogue of the collection dialogs and the resources reference set read the fork.
            var reg=ResourceRegistry.Load(rpPlainBuild,"resources","list","resources","hook","2");
            Check(reg.Ready&&String.IsNullOrEmpty(reg.Problem)&&reg.Find("unobtainium")!=null&&reg.Path.EndsWith("resources_plus.ini"),"the resource catalogue comes from resources_plus.ini without a resources.dll");
            // Nothing to seed from: the page reports the missing file as before.
            string rpBareBuild=MakeBuild(root,"fork-seed-bare");
            File.WriteAllBytes(Path.Combine(rpBareBuild,"plugins","resources_plus.dll"),simple.Dll);
            Reject(()=>new LocalResourceSession(LocalEditorSpec.Load(rpSchema),rpBareBuild));
            // The shipped template is the last resort.
            Write(Path.Combine(rpBareBuild,"plugins","resources_plus.template.ini"),"[resources]\nhook = 2\n[list]\n");
            Check(ForkSeed.Ensure(rpBareBuild,"resources_plus","resources_plus.ini").EndsWith("resources_plus.template.ini"),"the template beside the DLL seeds an empty list");
        });
        // 0.5.12: a content package provided while resources.dll was the target follows the fork once
        // resources_plus.ini exists - the receipt's old target must not hide the fragment.
        Test("a provided content package moves to the fork that takes its fragments today (0.5.12)",()=>{
            string rpSchema=Path.GetFullPath(Path.Combine(args[0],"..","..","settings_schemas","resources_plus.launcher.ini"));
            string pkg=Path.Combine(root,"content","salty_fork");
            Write(Path.Combine(pkg,"soviet.mod.ini"),"[mod]\nid=example.salty_fork\nname=Salty Fork\nversion=1.0\nenabled=1\n[content]\nresources=tesmio\\resources.ini\n");
            Write(Path.Combine(pkg,"tesmio","resources.ini"),"[list]\nrocksalt = rawgravel, Rock Salt\n");
            Package cp=Package.Load(pkg);
            string build=MakeBuild(root,"content-fork-build");
            File.WriteAllBytes(Path.Combine(build,"tesmioloader.dll"),simple.Dll);File.WriteAllBytes(Path.Combine(build,"tesmiolauncher.exe"),simple.Dll);
            File.WriteAllBytes(Path.Combine(build,"plugins","resources_plus.dll"),simple.Dll);
            Write(Path.Combine(build,"plugins","resources_plus.ini"),"[list]\nsand = bauxite, Sand\n\n[resources]\nhook = 2\n");
            Write(Path.Combine(build,"tesmioloader.ini"),"[tesmioloader]\nplugins=1\n[plugins]\nresources=0\nresources_plus=1\n");
            // The receipt of a provisioning made when the original still took the fragment.
            string hash=new ContentSession(cp,build).CurrentHash;
            string state=ContentTargets.StateDir(build,cp.Id);
            Write(Path.Combine(state,"receipt.ini"),"[content]\nid = example.salty_fork\nname = Salty Fork\nprovided = 1\nversion = 1.0\nhash = "+hash+"\ntarget.resources = resources\n\n[assets]\n\n[added]\n\n[skipped]\n");
            Write(Path.Combine(state,"resources.ini"),"[list]\nrocksalt = rawgravel, Rock Salt\n");
            var moved=new ContentSession(cp,build);
            Check(moved.Provided&&!moved.UpdatePending&&moved.Targets["resources"]=="resources_plus"&&moved.RecordedTargets["resources"]=="resources","today's target is the fork, the receipt still names the original");
            Check(moved.TargetChanged&&moved.NeedsWrite&&moved.Notes.Any(n=>Msg.KeyOf(n)=="content_target_changed"),"the move is pending and named on the page");
            Check(ContentLayer.Fragments(build,"resources_plus").Any(f=>f.PackageId=="example.salty_fork"),"the fork's editor sees the fragment despite the old receipt");
            var editor=new LocalResourceSession(LocalEditorSpec.Load(rpSchema),build);
            Check(editor.Items().Any(x=>x.Id=="rocksalt"),"and lists the package entry as an original");
            moved.Commit(NoGame,plugin=>plugin=="resources_plus"?new LocalResourceSession(LocalEditorSpec.Load(rpSchema),build):null);
            Check(SafeFiles.Text(Path.Combine(build,"plugins","resources_plus.ini")).Contains("rocksalt"),"saving writes the entry into resources_plus.ini");
            var receipt=new LooseIni(SafeFiles.Text(Path.Combine(state,"receipt.ini")));
            Check(receipt.Get("content","target.resources")=="resources_plus"&&(receipt.Get("added","resources")??"").Contains("rocksalt"),"the receipt now names the fork and the added id");
            Check(!new ContentSession(cp,build).TargetChanged,"after the save nothing is pending any more");
            // The fragments are always provided in the same order, by the name of their state
            // folder: the order of the resources is part of every save.
            string cableState=ContentTargets.StateDir(build,"aaa.cable_first");
            Write(Path.Combine(cableState,"receipt.ini"),"[content]\nid = aaa.cable_first\nname = Cable First\nprovided = 1\nversion = 1.0\nhash = x\ntarget.resources = resources_plus\n\n[assets]\n\n[added]\n\n[skipped]\n");
            Write(Path.Combine(cableState,"resources.ini"),"[list]\ncable = steel, Cable\n");
            var byName=ContentLayer.Fragments(build,"resources_plus");
            Check(byName.Count==2&&byName[0].PackageId=="aaa.cable_first"&&byName[1].PackageId=="example.salty_fork","the folders come by name");
        });
        // 0.5.13: a building line that names a resource nothing publishes crashes the game at start.
        Test("building lines naming an unknown resource are found before the game starts (0.5.13)",()=>{
            Check(Startup.ResourceOf("$STORAGE_IMPORT_SPECIAL RESOURCE_TRANSPORT_GRAVEL 50 road_salt")=="road_salt"&&Startup.ResourceOf("$PRODUCTION unobtainium 3.0")=="unobtainium"&&Startup.ResourceOf("$CONSUMPTION_PER_SECOND copper 0.02")=="copper"&&Startup.ResourceOf("$COST_RESOURCE steel 12")=="steel","the resource is read off the line");
            Check(Startup.ResourceOf("$STORAGE_SPECIAL RESOURCE_TRANSPORT_OPEN 40 cable")=="cable"&&Startup.ResourceOf("$STORAGE_EXPORT_SPECIAL_TRASH_BIG RESOURCE_TRANSPORT_WASTE 78 waste_mixed")=="waste_mixed"&&Startup.ResourceOf("$STORAGE_SPECIAL_TRASH_SMALL RESOURCE_TRANSPORT_WASTE 10 waste_mixed")=="waste_mixed"&&Startup.ResourceOf("$STORAGE_SPECIAL RESOURCE_TRANSPORT_OPEN 40")=="","every storage line that names its resource is read (0.6.13)");
            Check(Startup.ResourceOf("$STORAGE_FUEL OIL 15")==""&&Startup.ResourceOf("$COST_RESOURCE_AUTO ground_asphalt 1.0")==""&&Startup.ResourceOf("$WORKERS_NEEDED 150")==""&&Startup.ResourceOf("position 1 2 3")=="","lines without a resource name are skipped");
            string mrBuild=MakeBuild(root,"missing-resources");
            File.WriteAllBytes(Path.Combine(mrBuild,"plugins","resources_plus.dll"),simple.Dll);
            Write(Path.Combine(mrBuild,"plugins","resources_plus.ini"),"[resources]\nhook = 2\n[list]\nsand = bauxite, Sand\n");
            File.WriteAllBytes(Path.Combine(mrBuild,"plugins","vanilla_buildings.dll"),simple.Dll);
            Write(Path.Combine(mrBuild,"plugins","vanilla_buildings.ini"),"[general]\nenabled = 1\n[technical_storage]\nenabled = 1\ntarget = buildings_types\\muddy_technical.ini\nadd = $STORAGE_IMPORT_SPECIAL RESOURCE_TRANSPORT_GRAVEL 50 road_salt\nreplace = $CONSUMPTION coal 0.35 | $CONSUMPTION sand 0.35\n[off]\nenabled = 0\nadd = $PRODUCTION nothere 1.0\n");
            File.WriteAllBytes(Path.Combine(mrBuild,"plugins","buildings_plus.dll"),simple.Dll);
            Write(Path.Combine(mrBuild,"plugins","buildings_plus.ini"),"[buildings_plus]\nprune = 0\n[glassworks]\ndonor = bauxite_mine\nline = $PRODUCTION unobtainium 3.0\nline = $STORAGE_EXPORT_SPECIAL RESOURCE_TRANSPORT_OPEN 80 steel\n");
            Write(Path.Combine(mrBuild,"tesmioloader.ini"),"[tesmioloader]\nplugins=1\n[plugins]\nresources_plus=1\nvanilla_buildings=1\nbuildings_plus=1\n");
            LoaderFiles.Forget();var mrEntries=Catalog.ScanAll(mrBuild,"","",new List<string>());
            var steps=Startup.Order(mrBuild,mrEntries);
            Check(steps.Count==3,"the three plugins load");
            var missing=Startup.MissingResources(mrBuild,steps);
            Check(missing.Count==2&&missing.Any(m=>m.Section=="technical_storage"&&m.Resource=="road_salt")&&missing.Any(m=>m.Section=="glassworks"&&m.Resource=="unobtainium"),"road_salt and unobtainium are missing; sand, steel, coal-replaced lines and the switched-off section are not");
            Write(Path.Combine(mrBuild,"plugins","resources_plus.ini"),"[resources]\nhook = 2\n[list]\nsand = bauxite, Sand\nroad_salt = gravel, Road Salt\nunobtainium = steel, Unobtainium\n");
            Check(Startup.MissingResources(mrBuild,steps).Count==0,"once the fork publishes them nothing is missing");
            Write(Path.Combine(mrBuild,"tesmioloader.ini"),"[tesmioloader]\nplugins=1\n[plugins]\nresources_plus=1\nvanilla_buildings=0\nbuildings_plus=0\n");
            Write(Path.Combine(mrBuild,"plugins","resources_plus.ini"),"[resources]\nhook = 2\n[list]\nsand = bauxite, Sand\n");
            Check(Startup.Order(mrBuild,mrEntries).Count==1&&Startup.MissingResources(mrBuild,Startup.Order(mrBuild,mrEntries)).Count==0,"a plugin that does not load cannot crash the game");
        });
        Test("a rule set that does not fit every target is counted before the game starts (0.7.1)",()=>{
            string rsGame=Path.Combine(root,"rule-spread","game"),rsBuild=Path.Combine(rsGame,"tesmioloader");Directory.CreateDirectory(Path.Combine(rsBuild,"plugins"));
            Write(Path.Combine(rsGame,"media_soviet","buildings_types","a.ini"),"$NAME 1\n$STORAGE_FUEL RESOURCE_TRANSPORT_OIL 40\n$WORKERS_NEEDED 20\nend\n");
            Write(Path.Combine(rsGame,"media_soviet","buildings_types","b.ini"),"$NAME 2\n$STORAGE_FUEL RESOURCE_TRANSPORT_OIL 20\n$WORKERS_NEEDED 20\nend\n");
            Func<string,List<string>> read=f=>GameBuildings.TargetLines(rsBuild,"",f);
            string[] none=new string[0],both={@"buildings_types\a.ini",@"buildings_types\b.ini",@"buildings_types\gone.ini"};
            RuleSpread narrow=TargetPlan.Spread(both,read,none,none,none,new[]{"1 | $STORAGE_FUEL RESOURCE_TRANSPORT_OIL 40 | $STORAGE_IMPORT_SPECIAL RESOURCE_TRANSPORT_GRAVEL 120 sand"},null,null,null);
            Check(narrow.Found==2&&narrow.Fit==1&&narrow.Trouble&&narrow.Bad.Count==1&&narrow.Bad[0].EndsWith("b.ini"),"an anchor that stands in one of two files gives 1 of 2; a target file that is not there is not counted [found "+narrow.Found+", fit "+narrow.Fit+"]");
            RuleSpread wide=TargetPlan.Spread(both,read,new[]{"$STORAGE_IMPORT_SPECIAL RESOURCE_TRANSPORT_GRAVEL 120 sand"},none,none,none,null,null,null);
            Check(wide.Found==2&&wide.Fit==2&&!wide.Trouble,"appending needs no anchor and fits both");
            File.WriteAllBytes(Path.Combine(rsBuild,"plugins","vanilla_buildings.dll"),simple.Dll);
            Write(Path.Combine(rsBuild,"plugins","vanilla_buildings.ini"),"[general]\nenabled = 1\n[service]\nenabled = 1\ntarget = buildings_types\\a.ini\ntarget = buildings_types\\b.ini\ninsert = 1 | $STORAGE_FUEL RESOURCE_TRANSPORT_OIL 40 | $STORAGE_IMPORT_SPECIAL RESOURCE_TRANSPORT_GRAVEL 120 sand\n[fine]\nenabled = 1\ntarget = buildings_types\\a.ini\ntarget = buildings_types\\b.ini\nreplace = $WORKERS_NEEDED 20 | $WORKERS_NEEDED 10\n[off]\nenabled = 0\ntarget = buildings_types\\b.ini\nremove = $NOT_THERE 1\n");
            Write(Path.Combine(rsBuild,"tesmioloader.ini"),"[tesmioloader]\nplugins=1\n[plugins]\nvanilla_buildings=1\n");
            LoaderFiles.Forget();var rsEntries=Catalog.ScanAll(rsBuild,"","",new List<string>());
            var spreads=Startup.RuleSpreads(rsBuild,Startup.Order(rsBuild,rsEntries),"");
            Check(spreads.Count==1&&spreads[0].Section=="service"&&spreads[0].Fit==1&&spreads[0].Found==2,"the start check names the one rule set that is switched on and misses a target ["+String.Join(",",spreads.Select(s=>s.Section+" "+s.Fit+"/"+s.Found))+"]");
            // 0.7.5: the target files are kept until they change - and a change is seen.
            Write(Path.Combine(rsGame,"media_soviet","buildings_types","b.ini"),"$NAME 2\n$STORAGE_FUEL RESOURCE_TRANSPORT_OIL 40\n$WORKERS_NEEDED 20\n$LIFESPAN 9\nend\n");
            Check(Startup.RuleSpreads(rsBuild,Startup.Order(rsBuild,rsEntries),"").Count==0,"a target file that changed on disk is read again: the rule set fits both targets now");
            // 0.7.5: the start check gets the Workshop folder RMM works with, like the window and the list.
            string rsShop=Path.Combine(root,"rule-spread","shop");Write(Path.Combine(rsShop,"4711","house","building.ini"),"$NAME 3\n$WORKERS_NEEDED 5\nend\n");
            Write(Path.Combine(rsBuild,"plugins","vanilla_buildings.ini"),"[general]\nenabled = 1\n[shop]\nenabled = 1\ntarget = 4711\\house\\building.ini\nremove = $NOT_THERE 1\n");
            Check(Startup.RuleSpreads(rsBuild,Startup.Order(rsBuild,rsEntries),"").Count==0&&Startup.RuleSpreads(rsBuild,Startup.Order(rsBuild,rsEntries),rsShop).Count==1,"a target that only the given Workshop folder has is judged with that folder and left out without it");
            Write(Path.Combine(rsBuild,"tesmioloader.ini"),"[tesmioloader]\nplugins=1\n[plugins]\nvanilla_buildings=0\n");
            LoaderFiles.Forget();Check(Startup.RuleSpreads(rsBuild,Startup.Order(rsBuild,rsEntries),"").Count==0,"a plugin that does not load is not checked");
        });

        // 0.6.11: a package with a resource and a building that produces it. Switched off, the
        // folder Buildings Plus generated has to leave workshop_wip - the game loads every folder
        // in there at start, and a building.ini that names a resource which is gone stops it.
        Test("switching a content package off takes its generated building folders out of workshop_wip (0.6.11)",()=>
        {
            Action<int,bool> C=(n,ok)=>{if(!ok)throw new Exception("park check "+n+" failed");};
            string game=Path.Combine(root,"parkgame"),build=Path.Combine(game,"tesmioloader"),wip=Path.Combine(game,"media_soviet","workshop_wip");
            Directory.CreateDirectory(Path.Combine(build,"plugins"));
            string pkg=Path.Combine(root,"content","cable_pack");
            Write(Path.Combine(pkg,"soviet.mod.ini"),"[mod]\nid=example.cable\nname=Cable Pack\nversion=1.0\nenabled=1\n[content]\nresources=tesmio\\resources.ini\nbuildings=tesmio\\buildings.ini\n");
            Write(Path.Combine(pkg,"tesmio","resources.ini"),"[list]\ncable = aluminium, Cable\n");
            Write(Path.Combine(pkg,"tesmio","buildings.ini"),"[cable_factory]\ndonor = bauxite_mine\nname = Cable Factory\nline = $PRODUCTION cable 0.025\n");
            File.WriteAllBytes(Path.Combine(build,"plugins","resources.dll"),p.Dll);Write(Path.Combine(build,"plugins","resources.ini"),"[list]\nsand = bauxite, Sand\n\n[base_price]\n\n[price]\n\n[resources]\nhook = 2\n");
            File.WriteAllBytes(Path.Combine(build,"plugins","buildings_plus.dll"),p.Dll);Write(Path.Combine(build,"plugins","buildings_plus.ini"),"[buildings_plus]\nenabled = 1\nprune = 0\n");
            Write(Path.Combine(build,"tesmioloader.ini"),"[tesmioloader]\nplugins=1\n[plugins]\nresources=1\nbuildings_plus=1\n");
            LocalEditorSpec bpSpec=LocalEditorSpec.Load(Path.GetFullPath(Path.Combine(args[0],"..","..","settings_schemas","buildings_plus.launcher.ini")));
            Func<string,LocalResourceSession> sessions=plugin=>plugin=="resources"?new LocalResourceSession(resourceSpec,build):plugin=="buildings_plus"?new LocalResourceSession(bpSpec,build):null;
            // Four folders: ours for the section, ours for another section, one of the original
            // generator that names the same section, and one without a stamp.
            Action generated=()=>{Write(Path.Combine(wip,"9300000001","tesmioloader.stamp"),"buildings_plus generated this folder.\r\nsection=cable_factory donor=bauxite_mine object=CableFactory\r\nhash=1\r\n");Write(Path.Combine(wip,"9300000001","CableFactory","building.ini"),"$PRODUCTION cable 0.025\n");};
            generated();
            Write(Path.Combine(wip,"9300000000","tesmioloader.stamp"),"buildings_plus generated this folder.\r\nsection=example_pharmacy donor=shop_clothes object=Pharmacy\r\nhash=2\r\n");
            Write(Path.Combine(wip,"9188000001","tesmioloader.stamp"),"tesmioloader plugins\\buildings.dll generated this folder.\r\nsection=cable_factory donor=bauxite_mine\r\nhash=3\r\n");
            Write(Path.Combine(wip,"3790000001","workshopconfig.ini"),"$ITEM_ID 3790000001\r\n\r\n$OBJECT_BUILDING MyHouse\r\n\r\n$END\r\n");
            var on=new ContentSession(Package.Load(pkg),build);on.PendingOn=true;on.Commit(NoGame,sessions);
            C(1,on.Provided&&on.Added.ContainsKey("buildings")&&on.Added["buildings"].SequenceEqual(new[]{"cable_factory"})&&on.Parked.Count==0&&Directory.Exists(Path.Combine(wip,"9300000001")));
            var off=new ContentSession(Package.Load(pkg),build);off.PendingOn=false;off.Commit(NoGame,sessions);
            C(2,!off.Provided&&off.Parked.SequenceEqual(new[]{"9300000001"})&&off.ParkFailed.Count==0&&off.Notes.Count==0);
            C(3,!Directory.Exists(Path.Combine(wip,"9300000001"))&&SafeFiles.Text(Path.Combine(off.ParkDir,"9300000001","CableFactory","building.ini")).Contains("$PRODUCTION cable"));
            C(4,Directory.Exists(Path.Combine(wip,"9300000000"))&&Directory.Exists(Path.Combine(wip,"9188000001"))&&Directory.Exists(Path.Combine(wip,"3790000001")));
            C(5,!new LooseIni(SafeFiles.Text(Path.Combine(build,"plugins","buildings_plus.ini"))).HasSection("cable_factory")&&!SafeFiles.Text(Path.Combine(build,"plugins","resources.ini")).Contains("cable"));
            // Provided again, written again by the plugin, switched off again: the newer folder replaces the parked copy.
            var back=new ContentSession(Package.Load(pkg),build);back.PendingOn=true;back.Commit(NoGame,sessions);C(6,back.Provided&&back.Parked.Count==0);
            generated();Write(Path.Combine(wip,"9300000001","CableFactory","building.ini"),"$PRODUCTION cable 0.050\n");
            var offAgain=new ContentSession(Package.Load(pkg),build);offAgain.PendingOn=false;offAgain.Commit(NoGame,sessions);
            C(7,offAgain.Parked.Count==1&&!Directory.Exists(Path.Combine(wip,"9300000001"))&&SafeFiles.Text(Path.Combine(offAgain.ParkDir,"9300000001","CableFactory","building.ini")).Contains("0.050"));
            // Nothing is moved for an empty list or a section nobody generated; a package that was never provided parks nothing.
            generated();
            C(8,WipBuildings.Park(build,new List<string>(),offAgain.ParkDir,null).Count==0&&WipBuildings.Park(build,new[]{"nothing"},offAgain.ParkDir,null).Count==0&&Directory.Exists(Path.Combine(wip,"9300000001")));
            var never=new ContentSession(Package.Load(pkg),build);never.PendingOn=false;C(9,!never.NeedsWrite);
            // 0.6.13: an update that stays switched on and renames the section parks the folder of the old one.
            var again=new ContentSession(Package.Load(pkg),build);again.PendingOn=true;again.Commit(NoGame,sessions);
            C(10,again.Provided&&again.Parked.Count==0&&Directory.Exists(Path.Combine(wip,"9300000001")));
            Write(Path.Combine(pkg,"tesmio","buildings.ini"),"[cable_works]\ndonor = bauxite_mine\nname = Cable Works\nline = $PRODUCTION cable 0.025\n");
            var update=new ContentSession(Package.Load(pkg),build);C(11,update.Provided&&update.UpdatePending&&update.PendingOn);update.Commit(NoGame,sessions);
            C(12,update.Provided&&update.Added["buildings"].SequenceEqual(new[]{"cable_works"})&&update.Parked.SequenceEqual(new[]{"9300000001"})&&!Directory.Exists(Path.Combine(wip,"9300000001")));
            C(13,new LooseIni(SafeFiles.Text(Path.Combine(build,"plugins","buildings_plus.ini"))).HasSection("cable_works")&&Directory.Exists(Path.Combine(wip,"9300000000")));
            // When the buildings editor is not there to write, an update parks nothing: the old declaration still stands.
            Write(Path.Combine(wip,"9300000003","tesmioloader.stamp"),"buildings_plus generated this folder.\r\nsection=cable_works donor=bauxite_mine object=CableWorks\r\nhash=4\r\n");
            Write(Path.Combine(wip,"9300000003","CableWorks","building.ini"),"$PRODUCTION cable 0.025\n");
            Write(Path.Combine(pkg,"tesmio","buildings.ini"),"[cable_plant]\ndonor = bauxite_mine\nname = Cable Plant\nline = $PRODUCTION cable 0.025\n");
            var blind=new ContentSession(Package.Load(pkg),build);blind.Commit(NoGame,plugin=>plugin=="resources"?sessions(plugin):null);
            C(14,blind.Parked.Count==0&&Directory.Exists(Path.Combine(wip,"9300000003"))&&new LooseIni(SafeFiles.Text(Path.Combine(build,"plugins","buildings_plus.ini"))).HasSection("cable_works"));
        });
        // 0.6.12: the folders in workshop_wip are read like the declarations, a resource that a
        // building names is reported before it is hidden, and a section removed on the Buildings
        // Plus page takes its generated folder along.
        Test("generated building folders are checked for resources, users of a resource are found, a removed section parks its folder (0.6.12)",()=>
        {
            Action<int,bool> C=(n,ok)=>{if(!ok)throw new Exception("needs check "+n+" failed");};
            string game=Path.Combine(root,"needgame"),build=Path.Combine(game,"tesmioloader"),wip=Path.Combine(game,"media_soviet","workshop_wip");
            string resIni=Path.Combine(build,"plugins","resources_plus.ini"),bpIni=Path.Combine(build,"plugins","buildings_plus.ini");
            Directory.CreateDirectory(Path.Combine(build,"plugins"));
            File.WriteAllBytes(Path.Combine(build,"plugins","resources_plus.dll"),simple.Dll);Write(resIni,"[resources]\nhook = 2\n[list]\nsand = bauxite, Sand\ncable = aluminium, Cable\n");
            File.WriteAllBytes(Path.Combine(build,"plugins","buildings_plus.dll"),simple.Dll);
            const string Declared="[buildings_plus]\nenabled = 1\nprune = 0\n[cable_factory]\ndonor = bauxite_mine\nname = Cable Factory\nline = $PRODUCTION cable 0.025\n";
            Write(bpIni,Declared+"[off_one]\nenabled = 0\ndonor = bauxite_mine\nname = Off\nline = $PRODUCTION cable 1.0\n");
            Write(Path.Combine(build,"tesmioloader.ini"),"[tesmioloader]\nplugins=1\n[plugins]\nresources_plus=1\nbuildings_plus=1\n");
            // The generated factory, a generated folder whose section is gone, and a hand-built one.
            Write(Path.Combine(wip,"9300000001","tesmioloader.stamp"),"buildings_plus generated this folder.\r\nsection=cable_factory donor=bauxite_mine object=CableFactory\r\nhash=1\r\n");
            Write(Path.Combine(wip,"9300000001","CableFactory","building.ini"),"$NAME_STR \"Cable Factory\"\r\n  $PRODUCTION cable 0.025\r\n$CONSUMPTION waste 1.0\r\n$CONSUMPTION_PER_SECOND eletric 1.25\r\n");
            Write(Path.Combine(wip,"9300000002","tesmioloader.stamp"),"buildings_plus generated this folder.\r\nsection=old_works donor=bauxite_mine object=Works\r\nhash=2\r\n");
            Write(Path.Combine(wip,"9300000002","Works","building.ini"),"$CONSUMPTION cable 0.5\r\n$STORAGE_IMPORT_SPECIAL RESOURCE_TRANSPORT_OPEN 20 cable\r\n");
            Write(Path.Combine(wip,"3790000009","House","building.ini"),"$CONSUMPTION sand 1.0\r\n");
            LoaderFiles.Forget();var found=Catalog.ScanAll(build,"","",new List<string>());var steps=Startup.Order(build,found);
            C(1,steps.Count==2);
            // Everything is published: nothing is missing, and "waste" is the game's own name.
            C(2,Startup.MissingResources(build,steps).Count==0);
            // Who names cable: the declaration (its own folder is not named twice, the switched-off section not at all) and the orphan folder.
            var users=Startup.BuildingsNaming(build,steps,"cable");
            C(3,users.Count==2&&users.Any(u=>!u.Folder&&u.Section=="cable_factory")&&users.Any(u=>u.Folder&&u.Own&&u.Section=="9300000002"));
            var sandUsers=Startup.BuildingsNaming(build,steps,"sand");
            C(4,sandUsers.Count==1&&sandUsers[0].Folder&&!sandUsers[0].Own&&sandUsers[0].Section=="3790000009"&&Startup.BuildingsNaming(build,steps,"glass").Count==0&&Startup.BuildingsNaming(build,steps,"").Count==0);
            // cable leaves the list: the declaration and the orphan folder are both reported.
            Write(resIni,"[resources]\nhook = 2\n[list]\nsand = bauxite, Sand\n");
            var missing=Startup.MissingResources(build,steps);
            C(5,missing.Count==2&&missing.Any(m=>!m.Folder&&m.Section=="cable_factory"&&m.Resource=="cable")&&missing.Any(m=>m.Folder&&m.Section=="9300000002"&&m.Resource=="cable"));
            // The declaration gone by hand, its folder still there: now the folder itself is named.
            Write(bpIni,"[buildings_plus]\nenabled = 1\nprune = 0\n");
            missing=Startup.MissingResources(build,steps);
            C(6,missing.Count==2&&missing.All(m=>m.Folder&&m.Resource=="cable")&&missing.Any(m=>m.Section=="9300000001")&&missing.Any(m=>m.Section=="9300000002"));
            // A section hidden on the Buildings Plus page: its generated folder leaves workshop_wip, the other folders stay.
            Write(bpIni,Declared);Write(resIni,"[resources]\nhook = 2\n[list]\nsand = bauxite, Sand\ncable = aluminium, Cable\n");
            LocalEditorSpec bpSpec=LocalEditorSpec.Load(Path.GetFullPath(Path.Combine(args[0],"..","..","settings_schemas","buildings_plus.launcher.ini")));
            var page=new LocalResourceSession(bpSpec,build);page.Suppress("cable_factory");page.Commit(NoGame);
            C(7,page.RemovedSections.SequenceEqual(new[]{"cable_factory"})&&page.Parked.SequenceEqual(new[]{"9300000001"})&&page.ParkFailed.Count==0);
            C(8,!Directory.Exists(Path.Combine(wip,"9300000001"))&&File.Exists(Path.Combine(WipBuildings.ParkRoot(build),"9300000001","CableFactory","building.ini"))&&Directory.Exists(Path.Combine(wip,"9300000002"))&&Directory.Exists(Path.Combine(wip,"3790000009")));
            // Now only the orphan folder still names cable; a save that removes nothing parks nothing.
            users=Startup.BuildingsNaming(build,steps,"cable");C(9,users.Count==1&&users[0].Folder&&users[0].Section=="9300000002");
            var idle=new LocalResourceSession(bpSpec,build);idle.Commit(NoGame);C(10,idle.RemovedSections.Count==0&&idle.Parked.Count==0&&Directory.Exists(Path.Combine(wip,"9300000002")));
        });
        // 0.5.5: the generated folders under media_soviet\workshop_wip. The fixture mimics a real
        // library, because the workshop root is derived from the game path.
        string wipGame=Path.Combine(root,"wip","steamapps","common","SovietRepublic");
        string wipBuild=Path.Combine(wipGame,"tesmioloader","build");
        string wipRoot=Path.Combine(wipGame,"media_soviet","workshop_wip");
        string wipShop=Path.Combine(root,"wip","steamapps","workshop","content","784150");
        const string SmlStamp="tesmioloader plugins\\buildings.dll generated this folder.\r\nsection=salt_mine donor=bauxite_mine\r\nhash=1\r\n";
        const string SmlConfig="$ITEM_ID 9188000001\r\n\r\n$OWNER_ID 0\r\n\r\n$ITEM_TYPE WORKSHOP_ITEMTYPE_BUILDING\r\n\r\n$VISIBILITY 0\r\n$OBJECT_BUILDING SaltMine\r\n\r\n$ITEM_NAME \"Salt Mine\"\r\n\r\n$END\r\n";
        Directory.CreateDirectory(Path.Combine(wipBuild,"plugins"));Directory.CreateDirectory(wipShop);
        Write(Path.Combine(wipRoot,"9188000001","tesmioloader.stamp"),SmlStamp);
        File.WriteAllBytes(Path.Combine(wipRoot,"9188000001","workshopconfig.ini"),Encoding.ASCII.GetBytes(SmlConfig));
        Write(Path.Combine(wipRoot,"9300000001","tesmioloader.stamp"),"buildings_plus generated this folder.\r\nsection=pharmacy donor=shop_clothes\r\nhash=2\r\n");
        Write(Path.Combine(wipRoot,"9300000001","workshopconfig.ini"),"$ITEM_ID 9300000001\r\n\r\n$OWNER_ID 76561198000012345\r\n\r\n$OBJECT_BUILDING Pharmacy\r\n\r\n$END\r\n");
        Write(Path.Combine(wipRoot,"3790000001","workshopconfig.ini"),"$ITEM_ID 3790000001\r\n\r\n$OWNER_ID 76561198000012345\r\n\r\n$OBJECT_BUILDING MyHouse\r\n\r\n$END\r\n");
        Test("the workshop_wip scan classifies by stamp and reads name, object and owner (0.5.5)",()=>
        {
            var sml=WipBuildings.Scan(wipBuild,"sml");
            Check(sml.Count==1&&sml[0].Id=="9188000001"&&sml[0].Origin=="sml");
            Check(sml[0].Name=="Salt Mine"&&sml[0].Object=="SaltMine"&&sml[0].Section=="salt_mine");
            Check(sml[0].OwnerMissing&&sml[0].SmlRange&&!sml[0].NoStamp);
            var own=WipBuildings.Scan(wipBuild,"own");
            Check(own.Count==1&&own[0].Id=="9300000001"&&own[0].Origin=="buildings_plus"&&!own[0].OwnerMissing);
            var all=WipBuildings.Scan(wipBuild,"all");
            Check(all.Count==3&&all.Any(x=>x.Origin=="editor"&&x.NoStamp&&!x.Generated&&x.Id=="3790000001"));
        });
        Test("filling in the owner changes that one number and no other byte (0.5.5)",()=>
        {
            string file=Path.Combine(wipRoot,"9188000001","workshopconfig.ini");
            byte[] before=File.ReadAllBytes(file);
            Check(WipBuildings.FixOwner(Path.Combine(wipRoot,"9188000001"),"76561198000012345"));
            byte[] after=File.ReadAllBytes(file);
            Check(after.Length==before.Length+16);
            Check(before.Count(b=>b==13)==after.Count(b=>b==13));   // every CRLF survived
            string text=Encoding.ASCII.GetString(after);
            Check(text==SmlConfig.Replace("$OWNER_ID 0","$OWNER_ID 76561198000012345"));
            Check(File.Exists(Path.Combine(wipRoot,"9188000001","tesmioloader.stamp")));
            // Nothing left to do, and a refusal is not an error.
            Check(!WipBuildings.FixOwner(Path.Combine(wipRoot,"9188000001"),"76561198000012345"));
            Check(!WipBuildings.FixOwner(Path.Combine(wipRoot,"9300000001"),"76561198000012345"));
            Check(!WipBuildings.FixOwner(Path.Combine(wipRoot,"nothing-here"),"76561198000012345"));
            Check(!WipBuildings.Scan(wipBuild,"sml")[0].OwnerMissing);
            File.WriteAllBytes(file,before);
        });
        Test("an id of ten digits starting with 9 counts as generated, a Steam number does not (0.5.5)",()=>
        {
            Check(WipBuildings.IsGeneratedId("9188026318")&&WipBuildings.IsSmlId("9188026318"));
            Check(WipBuildings.IsGeneratedId("9300000001")&&!WipBuildings.IsSmlId("9300000001"));
            Check(!WipBuildings.IsGeneratedId("3801766045")&&!WipBuildings.IsGeneratedId("91880263")&&!WipBuildings.IsGeneratedId("abc"));
        });
        // 0.5.5 step 3: personal changes to a generated building.ini, stored as operations.
        const string Generated="; generated by tesmioloader plugins\\buildings.dll - section [salt_mine]\r\n$NAME_STR \"Salt Mine\"\r\n$TYPE_FACTORY\r\n$WORKERS_NEEDED 10\r\n$PRODUCTION rocksalt 3.0\r\n$POLLUTION_SMALL\r\n";
        string wipObject=Path.Combine(wipRoot,"9188000001","SaltMine"),wipFile=Path.Combine(wipObject,"building.ini");
        Write(wipFile,Generated);
        Test("changes are applied to the generator's lines, and a bad anchor is reported (0.5.5)",()=>
        {
            var ops=new List<WipOperation>{
                new WipOperation{Kind="replace",Anchor="$PRODUCTION rocksalt 3.0",Value="$PRODUCTION rocksalt 4.5"},
                new WipOperation{Kind="remove",Anchor="$POLLUTION_SMALL"},
                new WipOperation{Kind="add",Value="$STORAGE_EXPORT_SPECIAL RESOURCE_TRANSPORT_GRAVEL 60 rocksalt"}};
            List<string> problems;
            var result=WipEdits.Apply(WipEdits.Split(Generated),ops,out problems);
            Check(problems.Count==0&&result.Contains("$PRODUCTION rocksalt 4.5")&&!result.Contains("$PRODUCTION rocksalt 3.0"));
            Check(!result.Contains("$POLLUTION_SMALL")&&result.Last()=="$STORAGE_EXPORT_SPECIAL RESOURCE_TRANSPORT_GRAVEL 60 rocksalt");
            // An anchor that is gone, and one that is there twice: both are reported, not applied.
            WipEdits.Apply(WipEdits.Split(Generated),new List<WipOperation>{new WipOperation{Kind="remove",Anchor="$NOT_HERE"}},out problems);
            Check(problems.Count==1&&problems[0]=="$NOT_HERE");
            WipEdits.Apply(WipEdits.Split(Generated+"$TYPE_FACTORY\r\n"),new List<WipOperation>{new WipOperation{Kind="replace",Anchor="$TYPE_FACTORY",Value="$TYPE_MINE"}},out problems);
            Check(problems.Count==1);
        });
        Test("writing puts the changes into the building.ini and records what was written (0.5.5)",()=>
        {
            WipBuilding entry=WipBuildings.Scan(wipBuild,"sml").Single();
            var edit=WipEdits.Load(wipBuild,entry.Id);
            Check(!edit.Exists&&edit.Operations.Count==0&&WipEdits.State(wipBuild,entry,edit)==WipEdits.Sync.None);
            edit.Baseline=SafeFiles.Text(wipFile);
            edit.Operations.Add(new WipOperation{Kind="replace",Anchor="$WORKERS_NEEDED 10",Value="$WORKERS_NEEDED 25"});
            Check(WipEdits.Write(wipBuild,entry,edit).Count==0);
            Check(SafeFiles.Text(wipFile).Contains("$WORKERS_NEEDED 25")&&SafeFiles.Text(wipFile).Contains("$PRODUCTION rocksalt 3.0"));
            var loaded=WipEdits.Load(wipBuild,entry.Id);
            Check(loaded.Exists&&loaded.Operations.Count==1&&loaded.Operations[0].Anchor=="$WORKERS_NEEDED 10"&&loaded.Operations[0].Value=="$WORKERS_NEEDED 25");
            Check(loaded.Baseline==Generated&&WipEdits.State(wipBuild,entry,loaded)==WipEdits.Sync.InSync);
        });
        Test("after a regeneration the changes go in again and the package's own improvement stays (0.5.5)",()=>
        {
            // The generator writes anew: a different stamp hash and, in this run, a better recipe.
            string improved=Generated.Replace("$PRODUCTION rocksalt 3.0","$PRODUCTION rocksalt 3.6\r\n$CONSUMPTION eletric 0.5");
            Write(wipFile,improved);
            Write(Path.Combine(wipRoot,"9188000001","tesmioloader.stamp"),SmlStamp.Replace("hash=1","hash=2"));
            WipBuilding entry=WipBuildings.Scan(wipBuild,"sml").Single();
            var edit=WipEdits.Load(wipBuild,entry.Id);
            Check(WipEdits.State(wipBuild,entry,edit)==WipEdits.Sync.Regenerated);
            Check(WipEdits.Reapply(wipBuild,entry,edit).Count==0);
            string now=SafeFiles.Text(wipFile);
            Check(now.Contains("$WORKERS_NEEDED 25"),"the personal change is back");
            Check(now.Contains("$PRODUCTION rocksalt 3.6")&&now.Contains("$CONSUMPTION eletric 0.5"),"and the update's improvement survived - a stored copy would have thrown it away");
            Check(WipEdits.State(wipBuild,entry,WipEdits.Load(wipBuild,entry.Id))==WipEdits.Sync.InSync);
        });
        Test("a hand edit outside RMM is seen and not overwritten, and reverting restores the generator's file (0.5.5)",()=>
        {
            WipBuilding entry=WipBuildings.Scan(wipBuild,"sml").Single();
            Write(wipFile,SafeFiles.Text(wipFile)+"$BY_HAND 1\r\n");
            var edit=WipEdits.Load(wipBuild,entry.Id);
            Check(WipEdits.State(wipBuild,entry,edit)==WipEdits.Sync.Foreign);
            // An anchor the next package update took away is reported instead of silently skipped.
            edit.Baseline=Generated.Replace("$WORKERS_NEEDED 10\r\n","");
            List<string> trouble=WipEdits.Write(wipBuild,entry,edit);
            Check(trouble.Count==1&&trouble[0]=="$WORKERS_NEEDED 10");
            WipEdits.Revert(wipBuild,entry,edit);
            Check(SafeFiles.Text(wipFile)==edit.Baseline&&!WipEdits.Load(wipBuild,entry.Id).Exists);
            Check(!Directory.EnumerateFiles(WipEdits.Folder(wipBuild),"9188000001.*").Any());
            Write(wipFile,Generated);Write(Path.Combine(wipRoot,"9188000001","tesmioloader.stamp"),SmlStamp);
        });
        Test("a full reset puts a changed building.ini back before it drops the receipts (0.5.5)",()=>
        {
            WipBuilding entry=WipBuildings.Scan(wipBuild,"sml").Single();
            var edit=WipEdits.Load(wipBuild,entry.Id);edit.Baseline=SafeFiles.Text(wipFile);
            edit.Operations.Add(new WipOperation{Kind="replace",Anchor="$TYPE_FACTORY",Value="$TYPE_MINE"});
            WipEdits.Write(wipBuild,entry,edit);
            Check(SafeFiles.Text(wipFile).Contains("$TYPE_MINE")&&WipEdits.Recorded(wipBuild).Contains("9188000001"));
            ResetPlan plan=ResetTool.Plan(wipBuild,new List<CatalogEntry>(),new List<SaveGame>(),true);
            Check(plan.Buildings.Contains("9188000001"));
            ResetTool.Apply(wipBuild,plan,NoGame);
            Check(SafeFiles.Text(wipFile)==Generated,"the generator's file is back");
            Check(WipEdits.Recorded(wipBuild).Count==0,"and no receipt is left");
        });
        Test("[wip_buildings] is parsed, and an unknown range is refused (0.5.5)",()=>
        {
            string folder=Path.Combine(root,"wip-schema");
            string schema="[launcher]\neditor_type=keyed_sections\nlayout_version=1\nid=bp\nname=BP\n[editor]\nplugin=buildings_plus\nconfig=buildings_plus.ini\n"
                         +"[tab:general]\nlabel=General\norder=10\n[tab:sml]\nlabel=SML\norder=20\n[group:buildings]\ntab=general\nlabel=B\n[list]\nlabel=B\n"
                         +"[wip_buildings]\ntab=sml\nrange=sml\nlabel=Generated\n";
            Write(Path.Combine(folder,"ok.launcher.ini"),schema);
            LocalEditorSpec spec=LocalEditorSpec.Load(Path.Combine(folder,"ok.launcher.ini"));
            Check(spec.Wip!=null&&spec.Wip.Tab=="sml"&&spec.Wip.Range=="sml"&&spec.Wip.Label=="Generated");
            Write(Path.Combine(folder,"bad.launcher.ini"),schema.Replace("range=sml","range=everything"));
            Reject(()=>LocalEditorSpec.Load(Path.Combine(folder,"bad.launcher.ini")));
            Write(Path.Combine(folder,"bad2.launcher.ini"),schema.Replace("tab=sml","tab=nowhere"));
            Reject(()=>LocalEditorSpec.Load(Path.Combine(folder,"bad2.launcher.ini")));
        });
        // 0.5.8: what Buildings Plus will write, worked out in RMM so the editor can show the
        // effect of a line before the game does. These cases mirror the plugin's own self tests
        // (buildings_plus.cpp: DonorLineIsReplaced, Replaces, WriteBuildingIni) - if the rule
        // there ever changes, this is the second place that has to follow.
        Test("a declared line removes the donor lines of its whole token family (0.5.8)",()=>
        {
            var donor=new List<string>{"$TYPE_MINE_BAUXITE","$NAME_STR \"Bauxite mine\"","$WORKERS_NEEDED 80","$PRODUCTION rawbauxite 1.0","$CONSUMPTION eletric 0.35","$STORAGE_EXPORT RESOURCE_TRANSPORT_GRAVEL 40","$STORAGE_FUEL OIL 15","$RESOURCE_VISUALIZATION 0","position -12.0 0.0 8.0","rotation 0.0","","$CONNECTION_ROAD 4.0 0.0 12.0"};
            var mine=new List<string>{"$TYPE_MINE_ROCKSALT","$PRODUCTION rocksalt 3.0","$STORAGE_EXPORT_SPECIAL GRAVEL 60 rocksalt"};
            DonorOutcome outcome=DonorPlan.Build(donor,mine,new List<string>(),"$NAME_STR \"Salzmine\"");
            Func<string,DonorLine> find=text=>outcome.Donor.First(x=>x.Text==text);
            Check(find("$TYPE_MINE_BAUXITE").Dropped&&find("$TYPE_MINE_BAUXITE").DroppedBy=="line:0","$TYPE_* replaces every $TYPE_*");
            Check(find("$NAME_STR \"Bauxite mine\"").Dropped&&find("$NAME_STR \"Bauxite mine\"").DroppedBy=="name","the name field removes the donor's name line");
            Check(find("$PRODUCTION rawbauxite 1.0").Dropped&&find("$CONSUMPTION eletric 0.35").Dropped,"production and consumption are one recipe");
            Check(find("$STORAGE_EXPORT RESOURCE_TRANSPORT_GRAVEL 40").Dropped&&find("$STORAGE_FUEL OIL 15").Dropped,"one $STORAGE line of yours removes every storage of the donor");
            Check(find("$RESOURCE_VISUALIZATION 0").Dropped&&find("position -12.0 0.0 8.0").Dropped&&find("rotation 0.0").Dropped,"the pile display goes with the storages, and its data lines with it");
            Check(!find("$WORKERS_NEEDED 80").Dropped&&!find("$CONNECTION_ROAD 4.0 0.0 12.0").Dropped,"everything else of the donor stays");
            Check(outcome.Result[0].Text=="$NAME_STR \"Salzmine\""&&outcome.Result[1].Text=="$TYPE_MINE_ROCKSALT","your block is written in front of the donor, not in place of the lines it replaces");
        });
        Test("strip removes a token, and an unknown one does nothing (0.5.8)",()=>
        {
            var donor=new List<string>{"$WORKERS_NEEDED 80","$CONNECTION_ROAD_DEAD -19.1 0.3 -27.0","$POLLUTION SMALL"};
            DonorOutcome outcome=DonorPlan.Build(donor,new List<string>(),new List<string>{"$WORKERS_NEEDED","$NOT_IN_THE_FILE"},"");
            Check(outcome.Donor[0].Dropped&&outcome.Donor[0].DroppedBy=="strip:0","a stripped token is taken out");
            Check(!outcome.Donor[1].Dropped&&!outcome.Donor[2].Dropped,"a token the donor does not have changes nothing");
            Check(outcome.Result.Count(x=>x.Text.Length>0)==2,"and the rest of the file is written as it was");
        });
        Test("storages are numbered and a visualisation behind water is reported (0.5.8)",()=>
        {
            var mine=new List<string>{"$STORAGE_IMPORT RESOURCE_TRANSPORT_WATER 30","$STORAGE_EXPORT_SPECIAL OPEN 75 cable","$RESOURCE_VISUALIZATION 1","position -88.0 0.0 -30.0"};
            DonorOutcome bad=DonorPlan.Build(new List<string>(),mine,new List<string>(),"");
            Check(bad.Result.First(x=>x.Text.Contains("WATER")).Storage==0&&bad.Result.First(x=>x.Text.Contains("cable")).Storage==1,"every $STORAGE line carries the number a visualisation would use");
            Check(bad.Warnings.Any(x=>x.Contains("donor_warn_water_first")),"a storage that is visualised must not sit behind water or sewage");
            var good=new List<string>{"$STORAGE_EXPORT_SPECIAL OPEN 75 cable","$STORAGE_IMPORT RESOURCE_TRANSPORT_WATER 30","$RESOURCE_VISUALIZATION 0"};
            Check(DonorPlan.Build(new List<string>(),good,new List<string>(),"").Warnings.Count==0,"with water last there is nothing to report");
            var high=new List<string>{"$STORAGE_EXPORT_SPECIAL OPEN 75 cable","$RESOURCE_VISUALIZATION 4"};
            Check(DonorPlan.Build(new List<string>(),high,new List<string>(),"").Warnings.Any(x=>x.Contains("donor_warn_no_storage")),"a visualisation pointing at a storage that does not exist is reported");
        });
        // 0.5.8: two of your own lines that set the same thing. The key is the token plus every
        // word that is not a number - the resource of a recipe stands at the END of the line.
        // Tokens whose payload IS the number repeat legitimately: the two cable yards are both
        // "$RESOURCE_VISUALIZATION 3", and h_dealer of dlc3 carries two $STORAGE_FUEL lines.
        // Measured over the 1021 building.ini of the game and its DLCs.
        Test("a setting that is set twice is reported, a repeated token is not (0.5.8)",()=>
        {
            Check(DonorPlan.SettingKey("$WORKERS_NEEDED 8")==DonorPlan.SettingKey("$WORKERS_NEEDED 15"),"the same setting with another number is one key");
            Check(DonorPlan.SettingKey("$PRODUCTION cable 0.06")!=DonorPlan.SettingKey("$PRODUCTION steel 0.2"),"two recipes are two keys");
            Check(DonorPlan.SettingKey("$COST_RESOURCE_AUTO ground_asphalt\t1.0")=="","a tab separates like a space and $COST repeats anyway");
            Check(DonorPlan.SettingKey("$RESOURCE_VISUALIZATION 3")==""&&DonorPlan.SettingKey("$STORAGE_FUEL RESOURCE_TRANSPORT_OIL 15")=="","a token whose payload is the index or the geometry never folds");
            var twice=new List<string>{"$WORKERS_NEEDED 8","$CITIZEN_ABLE_SERVE 6","$WORKERS_NEEDED 15"};
            Check(DonorPlan.Build(new List<string>(),twice,new List<string>(),"").Warnings.Any(x=>x.Contains("donor_warn_double")),"the same setting twice is reported");
            var yards=new List<string>{"$STORAGE_EXPORT_SPECIAL OPEN 75 cable","$RESOURCE_VISUALIZATION 0","$RESOURCE_VISUALIZATION 0"};
            Check(!DonorPlan.Build(new List<string>(),yards,new List<string>(),"").Warnings.Any(x=>x.Contains("donor_warn_double")),"two yards on the same storage are not a double");
        });
        // 0.5.8: what Vanilla Buildings will do to ONE target file. Second implementation of
        // vanilla_buildings.cpp ValidateOperations - the same cases stand in the plugin's self tests.
        Test("every command of a rule set is judged against the unchanged target file (0.5.8)",()=>
        {
            var file=new List<string>{"$NAME_STR \"Shop\"","$WORKERS_NEEDED 20","$COST_WORK SOVIET_CONSTRUCTION_GROUNDWORKS 0.0","$STORAGE RESOURCE_TRANSPORT_COVERED 40","$STORAGE RESOURCE_TRANSPORT_COVERED 40","end"};
            TargetOutcome ok=TargetPlan.Check(file,new[]{"$CITIZEN_ABLE_SERVE 6"},new[]{"$WORKERS_NEEDED 20 | $WORKERS_NEEDED 30"},new string[0],new[]{"1 | $WORKERS_NEEDED 20 | $LIFESPAN 4000"});
            Check(!ok.Any,"a fitting rule set reports nothing");
            Check(ok.Commands.First(x=>x.Kind=="replace").At==1,"the replace command names the line it hits");
            Check(ok.Lines[1].Command>=0,"the line of the target is marked");
            TargetOutcome bad=TargetPlan.Check(file,new[]{"$WORKERS_NEEDED 20"},new string[0],new[]{"$COST_WORK SOVIET_CONSTRUCTION_GROUNDWORKS 0.0"},new[]{"1 | $STORAGE RESOURCE_TRANSPORT_COVERED 40 | $LIFESPAN 4000"});
            Check(bad.Commands[0].Problem=="target_problem_exists","a line that is already there cannot be added");
            Check(bad.Commands[1].Problem=="target_problem_cost_work","a $COST_WORK phase cannot be removed on its own");
            Check(bad.Commands[2].Problem.Contains("target_problem_many"),"an anchor that stands twice is refused");
            TargetOutcome cost=TargetPlan.Check(file,new[]{"$COST_RESOURCE_AUTO gravel 1.0"},new string[0],new string[0],new string[0]);
            Check(cost.Commands[0].Problem=="target_problem_cost","a $COST_ line cannot be appended");
            TargetOutcome chain=TargetPlan.Check(file,new[]{"$TYPE_SHOP"},new string[0],new string[0],new[]{"1 | $TYPE_SHOP | $LIFESPAN 4000"});
            Check(!chain.Any&&chain.Commands[1].FromOwn,"an insert may anchor on a line an earlier command of the same rule set produces");
            TargetOutcome block=TargetPlan.Check(file,new[]{"$CONNECTION_ROAD 1 2 3"},new string[0],new string[0],new string[0]);
            Check(block.Commands[0].Problem=="target_problem_block","a connection block has its own commands and is refused here");
        });
        // 0.5.8: the three connection commands in the same editor, on blocks.
        Test("a connection command is judged on the block it names (0.5.8)",()=>
        {
            var file=new List<string>{"$NAME_STR \"Shop\"","$CONNECTION_ROAD","-19.12 0.36 -27.02","-19.12 0.36 -30.02","$CONNECTION_ROAD_DEAD 5.0 0.0 7.0","end"};
            var found=TargetPlan.Connections(file);
            Check(found.Count==2&&found[0].Points==2&&found[0].Start==1&&found[0].End==3&&found[1].Points==1,"a two-line block and an inline one-point entry are both found");
            TargetOutcome ok=TargetPlan.Check(file,new string[0],new string[0],new string[0],new string[0],
                new[]{"$CONNECTION_ROAD | 1 0 1 | 2 0 2"},new[]{"$CONNECTION_ROAD | -19.12 0.36 -27.02 | -19.12 0.36 -30.02 | $CONNECTION_PEDESTRIAN"},new[]{"$CONNECTION_ROAD_DEAD | 5.0 0.0 7.0"});
            Check(!ok.Any,"a fitting set of connection commands reports nothing");
            Check(ok.Lines[2].Command>=0,"every line of the matched block is marked");
            TargetOutcome bad=TargetPlan.Check(file,new string[0],new string[0],new string[0],new string[0],
                new[]{"$CONNECTION_ROAD | -19.12 0.36 -27.02 | -19.12 0.36 -30.02"},new[]{"$CONNECTION_ROAD | 9 9 9 | 8 8 8 | $CONNECTION_PEDESTRIAN"},new[]{"$CONNECTION_ROAD_DEAD | 1 1 1"});
            Check(bad.Commands[0].Problem=="target_problem_link_exists","a connection that is already there cannot be appended");
            Check(bad.Commands[1].Problem=="target_problem_link_missing"&&bad.Commands[2].Problem=="target_problem_link_missing","a block that is not in the file is reported");
            TargetOutcome pass=TargetPlan.Check(file,new string[0],new string[0],new string[0],new string[0],
                new[]{"$CONNECTION_ROAD_ALLOWPASS | 1 0 1 | 2 0 2"},null,null);
            Check(pass.Commands[0].Problem=="target_problem_link_allowpass","*_ALLOWPASS is refused here as it is in the plugin");
            // The point compare is numeric with the plugin's tolerance, not a string compare.
            TargetOutcome loose=TargetPlan.Check(file,new string[0],new string[0],new string[0],new string[0],null,null,new[]{"$CONNECTION_ROAD_DEAD | 5 0 7"});
            Check(!loose.Any,"5 and 5.0 are the same point");
        });
        // 0.5.8: the result view. A second implementation of ApplyOperations in vanilla_buildings.cpp,
        // and a rule set is all or nothing - one command that does not fit makes the plugin drop the
        // WHOLE set for that target (BuildOverlays -> Fail), so the answer is the unchanged file.
        Test("the result view shows the file the game will read (0.5.8)",()=>
        {
            var file=new List<string>{"$NAME_STR \"Shop\"","$WORKERS_NEEDED 20","$CONNECTION_ROAD","1 0 1","2 0 2","$COST_WORK SOVIET_CONSTRUCTION_GROUNDWORKS 0.0","end"};
            TargetOutcome ok=TargetPlan.Check(file,new[]{"$TYPE_SHOP"},new[]{"$WORKERS_NEEDED 20 | $WORKERS_NEEDED 30"},new string[0],
                new[]{"1 | $WORKERS_NEEDED 30 | $LIFESPAN 4000"},new string[0],new string[0],new[]{"$CONNECTION_ROAD | 1 0 1 | 2 0 2"});
            Check(!ok.Any,"the rule set fits this target");
            List<TargetResultLine> made=TargetPlan.Result(ok,file);
            var text=made.Select(x=>x.Text).ToList();
            Check(text.Contains("$WORKERS_NEEDED 30")&&!text.Contains("$WORKERS_NEEDED 20"),"a replace stands in place of the old line");
            Check(!text.Contains("$CONNECTION_ROAD")&&!text.Contains("1 0 1"),"a removed connection takes its point lines with it");
            Check(text.IndexOf("$LIFESPAN 4000")==text.IndexOf("$WORKERS_NEEDED 30")+1,"an insert anchored on a produced line lands next to it");
            Check(text.IndexOf("$TYPE_SHOP")==text.IndexOf("end")-1,"a new line goes in front of the closing end");
            Check(made[text.IndexOf("$TYPE_SHOP")].Command>=0&&made[0].Command<0,"the result says which command produced a line");
            TargetOutcome bad=TargetPlan.Check(file,new[]{"$WORKERS_NEEDED 20"},new string[0],new string[0],new string[0]);
            Check(TargetPlan.Result(bad,file).Select(x=>x.Text).SequenceEqual(file),"one command that misses drops the whole rule set, so nothing is written");
        });
        // 0.7.0: the commands of a Buildings Plus section. Same donor and same cases as the
        // plugin's own self-test (TestCommands in buildings_plus.cpp) - the two must agree.
        Test("ClonePlan gives the answers of the plugin for every command case",()=>{
            var donor=("$NAME 2020\n$TYPE_SHOP\n$WORKERS_NEEDED 20\n$STORAGE RESOURCE_TRANSPORT_COVERED 40\n$STORAGE_FUEL RESOURCE_TRANSPORT_OIL 40\n"
                +"$RESOURCE_VISUALIZATION 2\nposition 1 0 1\nrotation 0.0\n\n$RESOURCE_VISUALIZATION 2\nposition 2 0 2\nrotation 0.0\n\n"
                +"$CONNECTION_ROAD\n19 0 38\n19 0 26\n$PARTICLE smoke 1 2 3 1 1\nend").Split('\n').ToList();
            int n=donor.Count;
            Func<string[],string[],string[],string[],TargetOutcome> plan=(a,r,m,i)=>ClonePlan.Check(donor,a,r,m,i);
            Func<TargetOutcome,string> text=o=>String.Join("\n",ClonePlan.Result(o,donor).Select(x=>x.Text))+"\n";
            Func<TargetOutcome,string> problem=o=>o.Commands.Where(x=>x.Problem.Length>0).Select(x=>Msg.IsKey(x.Problem)?Msg.KeyOf(x.Problem):x.Problem).FirstOrDefault()??"";
            string[] none=new string[0];
            TargetOutcome o1=plan(none,none,none,new[]{"1 | $STORAGE_FUEL RESOURCE_TRANSPORT_OIL 40 | $STORAGE_IMPORT_SPECIAL RESOURCE_TRANSPORT_GRAVEL 120 sand"});
            Check(!o1.Any&&ClonePlan.Result(o1,donor).Count==n+1&&text(o1).Contains("$STORAGE_FUEL RESOURCE_TRANSPORT_OIL 40\n$STORAGE_IMPORT_SPECIAL RESOURCE_TRANSPORT_GRAVEL 120 sand\n$RESOURCE_VISUALIZATION 2\n")&&text(o1).Contains("$STORAGE RESOURCE_TRANSPORT_COVERED 40\n"),"insert 1 puts one new storage behind its anchor and leaves every other line alone");
            TargetOutcome o2=plan(none,none,none,new[]{"0 | $STORAGE_FUEL RESOURCE_TRANSPORT_OIL 40 | $STORAGE_IMPORT_SPECIAL RESOURCE_TRANSPORT_GRAVEL 120 sand"});
            Check(!o2.Any&&text(o2).Contains("$STORAGE RESOURCE_TRANSPORT_COVERED 40\n$STORAGE_IMPORT_SPECIAL RESOURCE_TRANSPORT_GRAVEL 120 sand\n$STORAGE_FUEL RESOURCE_TRANSPORT_OIL 40\n"),"insert 0 puts it in front of the anchor");
            TargetOutcome o3=plan(none,new[]{"$WORKERS_NEEDED 20 | $WORKERS_NEEDED 30"},none,none);
            Check(!o3.Any&&ClonePlan.Result(o3,donor).Count==n&&text(o3).Contains("$TYPE_SHOP\n$WORKERS_NEEDED 30\n$STORAGE ")&&!text(o3).Contains("$WORKERS_NEEDED 20"),"replace swaps one line in place");
            TargetOutcome o4=plan(none,none,new[]{"$RESOURCE_VISUALIZATION 2"},none);
            Check(o4.Any&&problem(o4)=="clone_problem_many"&&ClonePlan.Result(o4,donor).Count==n,"a line that stands twice is no anchor, and nothing is written");
            TargetOutcome o5=plan(none,none,new[]{"$RESOURCE_VISUALIZATION 2 | position 2 0 2"},none);
            Check(!o5.Any&&ClonePlan.Result(o5,donor).Count==n-3&&text(o5).Contains("position 1 0 1\nrotation 0.0\n\n\n$CONNECTION_ROAD")&&!text(o5).Contains("position 2 0 2"),"a data line as second field tells two equal token lines apart, and remove takes the whole block");
            TargetOutcome o6=plan(new[]{"$RESOURCE_VISUALIZATION 0 | position 5 0 5 | rotation 0.0"},none,none,none);
            Check(!o6.Any&&ClonePlan.Result(o6,donor).Count==n+3&&text(o6).Contains("$PARTICLE smoke 1 2 3 1 1\n$RESOURCE_VISUALIZATION 0\nposition 5 0 5\nrotation 0.0\nend\n"),"add writes a block of several lines in front of the closing end");
            TargetOutcome o7=plan(none,none,none,new[]{"0 | end | $POLLUTION_SMALL"});
            Check(!o7.Any&&text(o7).Contains("$PARTICLE smoke 1 2 3 1 1\n$POLLUTION_SMALL\nend\n"),"the anchor end is the closing line");
            TargetOutcome o8=plan(none,none,none,new[]{"1 | end | $POLLUTION_SMALL"});
            Check(o8.Any&&problem(o8)=="target_problem_end_after","nothing can be inserted behind end");
            TargetOutcome o9=plan(none,new[]{"$CONNECTION_ROAD | $CONNECTION_ROAD_DEAD | 1 0 1"},none,none);
            Check(!o9.Any&&ClonePlan.Result(o9,donor).Count==n-1&&text(o9).Contains("\n$CONNECTION_ROAD_DEAD\n1 0 1\n$PARTICLE")&&!text(o9).Contains("19 0 38"),"replacing a connection replaces its points with it");
            TargetOutcome o10=plan(new[]{"$AAA 1"},none,none,new[]{"1 | $AAA 1 | $BBB 2"});
            Check(!o10.Any&&text(o10).Contains("$AAA 1\n$BBB 2\nend\n"),"an insert can hang on a line an earlier command adds");
            TargetOutcome o11=plan(none,none,none,new[]{"1 | $RESOURCE_VISUALIZATION 2 | position 1 0 1 | $XXX 1"});
            Check(!o11.Any&&text(o11).Contains("position 1 0 1\nrotation 0.0\n$XXX 1\n\n$RESOURCE_VISUALIZATION 2\nposition 2 0 2"),"behind a block means behind its last data line");
            Check(problem(plan(new[]{"$TYPE_SHOP"},none,none,none))=="target_problem_exists","add refuses a line the donor already has");
            Check(problem(plan(none,new[]{"$WORKERS_NEEDED 20 | $WORKERS_NEEDED 20"},none,none))=="target_problem_same","replace by the same line is refused");
            Check(problem(plan(none,new[]{"$WORKERS_NEEDED 20"},none,none))=="clone_problem_replace_form","replace without a new line is refused");
            Check(problem(plan(none,none,none,new[]{"1 | $TYPE_SHOP | position 1"}))=="clone_problem_new_missing","a new line has to begin with a token");
            Check(problem(plan(none,none,new[]{"$NOT_THERE 1"},none))=="target_problem_missing","an anchor the donor does not have is refused");
            TargetOutcome o17=plan(none,none,new[]{"$PARTICLE smoke 1 2 3 1 1"},none);
            Check(!o17.Any&&ClonePlan.Result(o17,donor).Count==n-1&&text(o17).Contains("19 0 26\nend\n"),"a block never swallows the closing end");
            TargetOutcome o18=plan(new[]{"$AAA 1"},none,new[]{"$NOT_THERE 1"},none);
            Check(o18.Any&&!text(o18).Contains("$AAA 1"),"one command that does not fit stops all of them");
            TargetOutcome o19=plan(new[]{"$AAA 1","$BBB 2"},new[]{"$WORKERS_NEEDED 20 | $WORKERS_NEEDED 30"},new[]{"$TYPE_SHOP"},new[]{"0 | $STORAGE_FUEL RESOURCE_TRANSPORT_OIL 40 | $CCC 3","1 | $CCC 3 | $DDD 4"});
            Check(!o19.Any&&!text(o19).Contains("$TYPE_SHOP")&&text(o19).Contains("$NAME 2020\n$WORKERS_NEEDED 30\n$STORAGE RESOURCE_TRANSPORT_COVERED 40\n$CCC 3\n$DDD 4\n$STORAGE_FUEL")&&text(o19).Contains("$AAA 1\n$BBB 2\nend\n"),"all four kinds together, adds in their order");
            TargetOutcome o20=plan(none,new[]{"$WORKERS_NEEDED 20 | $WORKERS_NEEDED 30"},new[]{"$WORKERS_NEEDED 20"},none);
            Check(o20.Any&&problem(o20)=="clone_problem_taken"&&ClonePlan.Result(o20,donor).Count==n,"a second command on the same block is refused, not passed over ["+problem(o20)+"]");
            TargetOutcome o21=plan(new[]{"$RESOURCE_VISUALIZATION 3 | position 7 0 7","$RESOURCE_VISUALIZATION 3 | position 8 0 8"},none,none,new[]{"1 | $RESOURCE_VISUALIZATION 3 | position 8 0 8 | $XXX 1"});
            Check(!o21.Any&&text(o21).Contains("position 7 0 7\n$RESOURCE_VISUALIZATION 3\nposition 8 0 8\n$XXX 1\nend\n"),"a data line tells two blocks apart that earlier commands write, too ["+problem(o21)+"]");
            TargetOutcome o22=plan(new[]{"$RESOURCE_VISUALIZATION 3 | position 7 0 7","$RESOURCE_VISUALIZATION 3 | position 8 0 8"},none,none,new[]{"1 | $RESOURCE_VISUALIZATION 3 | $XXX 1"});
            Check(o22.Any&&problem(o22)=="clone_problem_made_many","an anchor two earlier commands write is refused until a data line says which ["+problem(o22)+"]");
            // The name field and the storage numbers, which only the editor shows.
            var named=ClonePlan.Named(ClonePlan.Result(o3,donor),"$NAME_STR \"Pharmacy\"");
            Check(named[0].Text=="$NAME_STR \"Pharmacy\""&&!named.Any(x=>x.Text=="$NAME 2020")&&named.Count==n,"the name field takes the donor's own name line out and stands on top");
            List<int> numbers;var warn=ClonePlan.Warnings(ClonePlan.Result(plan(new[]{"$STORAGE_IMPORT RESOURCE_TRANSPORT_WATER 30","$RESOURCE_VISUALIZATION 2 | position 9 0 9"},none,none,none),donor),out numbers);
            Check(numbers.Count(x=>x>=0)==3&&warn.Any(x=>Msg.KeyOf(x)=="donor_warn_no_storage")==false&&warn.Count==0,"three storages are numbered 0..2 and a pile on storage 2 has one to show");
            var warn2=ClonePlan.Warnings(ClonePlan.Result(plan(none,none,none,new[]{"0 | $STORAGE_FUEL RESOURCE_TRANSPORT_OIL 40 | $STORAGE_IMPORT RESOURCE_TRANSPORT_WATER 30","1 | $STORAGE_FUEL RESOURCE_TRANSPORT_OIL 40 | $STORAGE_SPECIAL RESOURCE_TRANSPORT_COVERED 8 medicine"}),donor),out numbers);
            Check(warn2.Any(x=>Msg.KeyOf(x)=="donor_warn_water_first"),"a shown storage behind water is warned about");
        });
        // 0.5.8: a block as the anchor. `rotation 0.0` occurs three times in a generated
        // building.ini and could never be changed - the block it belongs to occurs once.
        Test("a block anchor reaches a line that a text anchor cannot (0.5.8)",()=>
        {
            var file=new List<string>{"$RESOURCE_VISUALIZATION 0","position -12.0 0.0 8.0","rotation 0.0","$RESOURCE_VISUALIZATION 3","position -88.0 0.0 -30.0","rotation 0.0"};
            List<string> problems;
            // The old way: the line alone is ambiguous, so the change is skipped and reported.
            var single=new List<WipOperation>{new WipOperation{Kind="replace",Anchor="rotation 0.0",Value="rotation 90.0"}};
            List<string> after=WipEdits.Apply(file,single,out problems);
            Check(problems.Count==1&&after.SequenceEqual(file),"an ambiguous line is still refused instead of hitting the wrong one");
            // The block of the second visualisation is unique.
            string block=WipEdits.BlockAt(file,5);
            Check(block=="$RESOURCE_VISUALIZATION 3\r\nposition -88.0 0.0 -30.0\r\nrotation 0.0","a line of a block selects the whole block");
            var asBlock=new List<WipOperation>{new WipOperation{Kind="replace",Anchor=block,Value="$RESOURCE_VISUALIZATION 3\nposition -88.0 0.0 -30.0\nrotation 90.0"}};
            after=WipEdits.Apply(file,asBlock,out problems);
            Check(problems.Count==0&&after[5]=="rotation 90.0"&&after[2]=="rotation 0.0","the block anchor changes the right one and leaves the other alone");
            Check(after.Count==file.Count,"and the file keeps its length");
        });
        Test("receipts of both forms are read, and old ones stay in the short form (0.5.8)",()=>
        {
            string build=Path.Combine(root,"wip-receipt");Directory.CreateDirectory(build);
            string folder=WipEdits.Folder(build);Directory.CreateDirectory(folder);
            // What the user's machine has: six single-line replaces of 0.5.5.
            Write(Path.Combine(folder,"9174590863.receipt.ini"),"[wip]\nid = 9174590863\nobject = CableFactory\nstamp_hash = 1E90\nresult_hash = AB\n\n[operations]\nreplace = $WORKERS_NEEDED 200 | $WORKERS_NEEDED 150\nremove = $POLLUTION SMALL\nadd = $CONNECTION_ROAD_DEAD 1 2 3\n");
            WipEdit old=WipEdits.Load(build,"9174590863");
            Check(old.Operations.Count==3&&old.Operations[0].Anchor=="$WORKERS_NEEDED 200"&&old.Operations[0].Value=="$WORKERS_NEEDED 150","a receipt written before 0.5.8 is read unchanged");
            old.Baseline="x";WipEdits.Save(build,old);
            Check(SafeFiles.Text(Path.Combine(folder,"9174590863.receipt.ini")).Contains("[operations]"),"and stays in the short form as long as every change is one line");
            // One block turns the whole receipt into sections, so the order survives.
            old.Operations.Add(new WipOperation{Kind="replace",Anchor="$RESOURCE_VISUALIZATION 3\nrotation 0.0",Value="$RESOURCE_VISUALIZATION 3\nrotation 90.0"});
            WipEdits.Save(build,old);
            string written=SafeFiles.Text(Path.Combine(folder,"9174590863.receipt.ini"));
            Check(written.Contains("[op:1]")&&written.Contains("[op:4]")&&!written.Contains("[operations]"),"a block makes every change a section of its own");
            WipEdit back=WipEdits.Load(build,"9174590863");
            Check(back.Operations.Count==4&&back.Operations[0].Kind=="replace"&&back.Operations[1].Kind=="remove"&&back.Operations[2].Kind=="add","the order and the kinds come back");
            Check(back.Operations[3].AnchorLines.Count==2&&back.Operations[3].ValueLines[1]=="rotation 90.0","and so does the block");
        });

        // ---- the loader's own plugin search: what the list is built from ----
        Test("the plugin search mirrors the loader: exports, order by key, local before Workshop",()=>{
            string sb=MakeBuild(root,"scan-build"),shop=Path.Combine(root,"scan-shop");
            File.WriteAllBytes(Path.Combine(sb,"plugins","beta.dll"),p.Dll);
            Write(Path.Combine(sb,"plugins","notaplugin.dll"),"not a plugin");
            File.WriteAllBytes(Path.Combine(sb,"plugins","nodll.txt"),p.Dll);
            Directory.CreateDirectory(Path.Combine(sb,"plugins","sub"));File.WriteAllBytes(Path.Combine(sb,"plugins","sub","hidden.dll"),p.Dll);
            foreach(string rel in new[]{@"200\plugins\Alpha.dll",@"100\deep\er\beta.dll",@"300\gamma.dll",@"150\x\gamma.dll",@"400\rail_x.dll",@"400\railx.dll"}){string f=Path.Combine(shop,rel);Directory.CreateDirectory(Path.GetDirectoryName(f));File.WriteAllBytes(f,p.Dll);}
            var found=PluginScan.Discover(Path.Combine(sb,"plugins"),shop);
            Check(String.Join(",",found.Select(f=>f.Key))=="Alpha,beta,gamma,rail_x,railx","keys in the loader's order: "+String.Join(",",found.Select(f=>f.Key)));
            var beta=found[1];Check(beta.Local&&beta.Shadowed.Count==1&&beta.Shadowed[0].Path.EndsWith(@"100\deep\er\beta.dll"),"a DLL in plugins wins its key and names the one it shadows");
            var gamma=found[2];Check(!gamma.Local&&gamma.Path.EndsWith(@"150\x\gamma.dll")&&gamma.Shadowed.Single().Path.EndsWith(@"300\gamma.dll")&&gamma.Item.EndsWith("150"),"among Workshop DLLs the smallest path wins");
            Check(found[0].Ini.EndsWith(@"200\plugins\Alpha.ini")&&!found.Any(f=>f.Key=="notaplugin"||f.Key=="hidden"||f.Key=="nodll"),"the INI lies beside the DLL; a DLL without the exports and a DLL below plugins are not plugins");
            Check(PluginScan.CompareKeys("rail_physics_fix","railphysics")<0&&PluginScan.CompareKeys("TESMIOAGRI","traffic")<0&&PluginScan.SameKey("Needs","needs"),"keys compare like the loader does");
            Check(PluginScan.WorkshopDefault(@"D:\Lib\steamapps\common\SovietRepublic\tesmioloader")==@"D:\Lib\steamapps\workshop\content\784150"&&PluginScan.WorkshopDefault(@"D:\Games\Soviet")=="","the Workshop folder is found from steamapps");
            Check(!PluginScan.IsPluginImage(new byte[64])&&!PluginScan.IsPluginImage(null)&&PluginScan.IsPluginImage(p.Dll),"the export check");
        });
        Test("rmm.mod.ini is the manifest, soviet.mod.ini the fallback, and a DLL without either is listed by its name",()=>{
            string shop=Path.Combine(root,"manifest-shop");
            string a=Path.Combine(shop,"1001");Directory.CreateDirectory(Path.Combine(a,"plugins"));File.WriteAllBytes(Path.Combine(a,"plugins","new_layout.dll"),p.Dll);Write(Path.Combine(a,"plugins","new_layout.ini"),"; what it does\n[new_layout]\n; 0 or 1\nenabled = 1\n");
            Write(Path.Combine(a,"rmm.mod.ini"),"[mod]\nid = example.new_layout\nname = New Layout\nversion = 1.2.3\nreplaces = old_thing\n[dependencies]\nresources = *\nexample.other = >=1.0\n");
            Write(Path.Combine(a,"soviet.mod.ini"),"[mod]\nid=example.legacy\nname=Legacy Name\nversion=0.1\n[hooks]\ndll=hooks\\gone.dll\n");
            Package n=Package.Load(a);
            Check(n.Id=="example.new_layout"&&n.Name=="New Layout"&&n.Version=="1.2.3"&&n.Replaces=="old_thing"&&n.ManifestPath.EndsWith("rmm.mod.ini"),"rmm.mod.ini wins");
            Check(n.Target=="new_layout"&&n.ConfigName=="new_layout.ini"&&n.DllPath.EndsWith(@"plugins\new_layout.dll")&&n.DefaultsPath.EndsWith(@"plugins\new_layout.ini")&&n.HasConfig&&n.UserOverlay&&n.Generic,"DLL and INI are found below the package folder");
            Check(n.Dependencies.Count==2&&n.Dependencies.Any(d=>d.Id=="resources")&&n.RequiresLocal.Contains("resources"),"dependencies name mod ids or plugin keys");
            Check(Package.Load(Path.Combine(a,"plugins","new_layout.dll")).Id=="example.new_layout"&&Package.RootOf(Path.Combine(a,"plugins","new_layout.dll")).Equals(a,StringComparison.OrdinalIgnoreCase),"a DLL path leads to the same package");
            string b=Path.Combine(shop,"1002");Directory.CreateDirectory(Path.Combine(b,"plugins"));File.WriteAllBytes(Path.Combine(b,"plugins","bare.dll"),p.Dll);
            Package bare=Package.Load(Path.Combine(b,"plugins"));
            Check(bare.Id=="local.bare"&&bare.Name=="bare"&&bare.Version=="?"&&bare.ManifestPath==""&&!bare.HasConfig&&bare.Replaces=="","a DLL without a manifest is listed by its name");
            string c=Path.Combine(shop,"1003");Directory.CreateDirectory(Path.Combine(c,"plugins"));File.WriteAllBytes(Path.Combine(c,"plugins","one.dll"),p.Dll);File.WriteAllBytes(Path.Combine(c,"plugins","two.dll"),p.Dll);
            Write(Path.Combine(c,"rmm.mod.ini"),"[mod]\nid = example.pair\nname = Pair\nversion = 1.0\n");
            Reject(()=>Package.Load(c));
            Write(Path.Combine(c,"rmm.mod.ini"),"[mod]\nid = example.pair\nname = Pair\nversion = 1.0\nplugin = three\n");
            Reject(()=>Package.Load(c));
            Write(Path.Combine(c,"rmm.mod.ini"),"[mod]\nid = example.pair\nname = Pair\nversion = 1.0\nplugin = two\n");
            Check(Package.Load(c).Target=="two","[mod] plugin names the DLL the manifest belongs to");
            Reject(()=>Package.Load(Path.Combine(shop,"nothing-here")));
            var list=ScanWorkshop(shop,new List<string>());
            Check(list.Count==4&&list.Single(e=>e.Key=="new_layout").Root.Equals(a,StringComparison.OrdinalIgnoreCase)&&list.Single(e=>e.Key=="new_layout").Replaces=="old_thing","one entry per plugin DLL: "+String.Join(",",list.Select(e=>e.Key)));
            var two=list.Single(e=>e.Key=="two");var one=list.Single(e=>e.Key=="one");
            Check(two.Root.EndsWith("two.dll")&&two.Supported&&two.Problem.Length==0&&two.Id=="example.pair"&&one.Id=="local.one"&&one.Problem.Length==0,"a folder with two DLLs names its entries by DLL, and the manifest belongs to one of them");
        });
        Test("two different plugins with the same id are both flagged",()=>{
            string shop=Path.Combine(root,"twin-shop");
            foreach(string name in new[]{"twin_a","twin_b"}){Directory.CreateDirectory(Path.Combine(shop,name,"plugins"));File.WriteAllBytes(Path.Combine(shop,name,"plugins",name+".dll"),p.Dll);Write(Path.Combine(shop,name,"rmm.mod.ini"),"[mod]\nid = example.twin\nname = Twin "+name+"\nversion = 1.0\n");}
            var twins=ScanWorkshop(shop,new List<string>());Check(twins.Count==2&&twins.All(e=>Msg.KeyOf(e.Problem)=="catalog_duplicate_id"));
        });
        Test("a content package is listed beside the plugins, and a keyed editor only when its plugin is found",()=>{
            string cb=MakeBuild(root,"listing-build"),shop=Path.Combine(root,"listing-shop"),real=Path.GetDirectoryName(localSchema);
            Write(Path.Combine(shop,"salt","rmm.mod.ini"),"[mod]\nid = example.salt\nname = Salt\nversion = 1.0\n[content]\nresources = tesmio\\resources.ini\n");Write(Path.Combine(shop,"salt","tesmio","resources.ini"),"[list]\nrocksalt = rawgravel, Rock Salt\n");
            LoaderFiles.Forget();var none=Catalog.ScanAll(cb,shop,real,new List<string>());
            Check(none.Count==1&&none[0].Kind=="content"&&none[0].Id=="example.salt"&&none[0].Key==""&&none[0].Status=="Inhalt","only the content package: "+none.Count);
            File.WriteAllBytes(Path.Combine(cb,"plugins","needs.dll"),p.Dll);Write(Path.Combine(cb,"plugins","needs.ini"),"[list]\n[needs]\nenabled = 1\n");
            LoaderFiles.Forget();var some=Catalog.ScanAll(cb,shop,real,new List<string>());
            Check(some.Count==2&&some.Any(e=>e.LocalEditor&&e.Key=="needs"&&e.Dll.EndsWith("needs.dll")&&e.Origin=="loader"),"the Needs editor appears with its plugin: "+some.Count);
        });
        Test("a replacement and the plugin it replaces never both stay on",()=>{
            string rb=MakeBuild(root,"replace-build"),shop=Path.Combine(root,"replace-shop"),sr=Path.Combine(root,"replace-schemas");
            Write(Path.Combine(sr,"replacements.ini"),"[replaces]\nforked = original\n");
            File.WriteAllBytes(Path.Combine(rb,"plugins","forked.dll"),p.Dll);
            Directory.CreateDirectory(Path.Combine(shop,"7","plugins"));File.WriteAllBytes(Path.Combine(shop,"7","plugins","original.dll"),p.Dll);
            Directory.CreateDirectory(Path.Combine(shop,"8","plugins"));File.WriteAllBytes(Path.Combine(shop,"8","plugins","newer.dll"),p.Dll);Write(Path.Combine(shop,"8","rmm.mod.ini"),"[mod]\nid = example.newer\nname = Newer\nversion = 1.0\nreplaces = older\n");
            Directory.CreateDirectory(Path.Combine(shop,"9","plugins"));File.WriteAllBytes(Path.Combine(shop,"9","plugins","older.dll"),p.Dll);
            Write(Path.Combine(rb,"tesmioloader.ini"),"; the launcher's file\n[tesmioloader]\nplugins=1\n[plugins]\nolder=0\n");
            LoaderFiles.Forget();var list=Catalog.ScanAll(rb,shop,sr,new List<string>());
            Check(list.Single(e=>e.Key=="forked").Replaces=="original"&&list.Single(e=>e.Key=="newer").Replaces=="older","the table and the manifest both say what replaces what");
            Check(list.Single(e=>e.Key=="forked").Hints.Any(h=>Msg.KeyOf(h)=="hint_replaces"),"the replacement's page says so");
            var open=Catalog.OpenReplacements(list,rb);
            Check(open.Count==1&&open[0].Key.Key=="forked"&&open[0].Value.Key=="original","only a pair with both switched on is open");
            Check(Catalog.SettleReplacements(list,rb,NoGame).Count==1&&!LoaderFiles.Enabled(rb,"original")&&LoaderFiles.Enabled(rb,"forked")&&Catalog.OpenReplacements(list,rb).Count==0,"settling switches the replaced plugin off");
            Check(SafeFiles.Text(Path.Combine(rb,"tesmioloader.ini")).Contains("; the launcher's file")&&Catalog.SettleReplacements(list,rb,NoGame).Count==0,"the file keeps its comments, and a second run has nothing to do");
            var original=new Session(Package.Load(list.Single(e=>e.Key=="original").Root),rb);original.Replacing.Add("forked");
            Check(!original.LoaderEnabled&&original.SwitchesOff().Count==0,"switched off, the replaced plugin asks for nothing");original.SetLoaderEnabled(true);
            Check(original.SwitchesOff().SequenceEqual(new[]{"forked"}),"switched on by the player, it takes the replacement's switch along");original.Commit(false,NoGame);
            Check(LoaderFiles.Enabled(rb,"original")&&!LoaderFiles.Enabled(rb,"forked"),"one save writes both switches");
            var forked=new Session(InstalledPlugins.Load(rb,"forked",sr),rb);forked.Replaced.Add("original");
            forked.SetLoaderEnabled(true);Check(forked.LoaderChanged&&forked.SwitchesOff().SequenceEqual(new[]{"original"}));forked.Commit(false,NoGame);
            Check(!LoaderFiles.Enabled(rb,"original")&&LoaderFiles.Enabled(rb,"forked"),"and the other way round");
        });
        Test("the loader folder, the game exe and the Workshop folder are found in every layout",()=>{
            string lib=Path.Combine(root,"layouts"),game=Path.Combine(lib,"steamapps","common","SovietRepublic");
            Write(Path.Combine(game,"SOVIET64.exe"),"MZ");
            string sub=Path.Combine(game,"tesmioloader"),old=Path.Combine(game,"tesmioloader","build"),nowhere=Path.Combine(root,"nowhere");
            var libs=new[]{lib};
            Check(Discovery.LoaderFolder(nowhere,libs)==null,"no loader anywhere");
            Write(Path.Combine(game,"tesmioloader.dll"),"x");Check(Discovery.LoaderFolder(nowhere,libs).Equals(game,StringComparison.OrdinalIgnoreCase),"in the game folder");
            Write(Path.Combine(old,"tesmioloader.dll"),"x");Check(Discovery.LoaderFolder(nowhere,libs).Equals(old,StringComparison.OrdinalIgnoreCase),"tesmioloader\\build comes before the game folder");
            Write(Path.Combine(sub,"tesmioloader.dll"),"x");Check(Discovery.LoaderFolder(nowhere,libs).Equals(sub,StringComparison.OrdinalIgnoreCase),"tesmioloader comes first");
            Check(Discovery.LoaderFolder(old,libs).Equals(old,StringComparison.OrdinalIgnoreCase),"a loader beside the program wins");
            foreach(string folder in new[]{game,sub,old})Check(GameVersion.ExePath(folder).Equals(Path.Combine(game,"SOVIET64.exe"),StringComparison.OrdinalIgnoreCase)&&LoaderFiles.WorkshopRoot(folder).Equals(Path.Combine(lib,"steamapps","workshop","content","784150"),StringComparison.OrdinalIgnoreCase),"exe and Workshop folder from "+folder);
            Write(Path.Combine(sub,"tesmioloader.ini"),"[tesmioloader]\nworkshop_path = D:\\Other\\Shop\n[plugins]\na=0\n");
            Check(LoaderFiles.WorkshopRoot(sub)==@"D:\Other\Shop"&&!LoaderFiles.Enabled(sub,"a")&&LoaderFiles.Enabled(sub,"b")&&LoaderFiles.PluginsAllowed(sub),"workshop_path overrides, a missing key is on");
            Write(Path.Combine(sub,"tesmioloader.ini"),"[tesmioloader]\nplugins = 0\n");Check(!LoaderFiles.PluginsAllowed(sub),"plugins = 0 switches every plugin off");
            Check(!LoaderFiles.LaysOverlay(sub),"a loader without the overlay");
            File.WriteAllBytes(Path.Combine(sub,"tesmioloader.dll"),Encoding.Unicode.GetBytes("xx user_config yy"));Check(LoaderFiles.LaysOverlay(sub),"the loader that lays user_config over a plugin's INI");
        });
        Test("a banner at the top of an INI is not the plugin's description",()=>{
            string bb=MakeBuild(root,"banner-build");
            File.WriteAllBytes(Path.Combine(bb,"plugins","bannered.dll"),p.Dll);
            Write(Path.Combine(bb,"plugins","bannered.ini"),"; ============================================================================\n;  Bannered Plugin 0.3.4 - Configuration\n; ============================================================================\n;  Controls road-snow accumulation and natural melting.\n;  Works without a snowplow.\n;\n;  File format:\n;    - UTF-8 without BOM\n\n[snow]\n; ----------------------------------------------------------------\n; 1 = apply the settings below\n; 0 = leave them alone\nenabled = 1\n");
            Package banner=InstalledPlugins.Load(bb,"bannered",schemaRoot);
            string description=banner.Schema.Get("launcher","description","");
            Check(description=="Controls road-snow accumulation and natural melting. Works without a snowplow.","the first paragraph after the banner: ["+description+"]");
            Check(!banner.Fields.Single(f=>f.Key=="enabled").Description.Contains("---")&&banner.Fields.Single(f=>f.Key=="enabled").Description.StartsWith("1 = apply"),"a rule above a key is not part of its description");
            Write(Path.Combine(bb,"plugins","plainhead.ini"),"; plainhead - how far people walk.\n; -----\n; second line\n[plainhead]\nenabled = 1\n");File.WriteAllBytes(Path.Combine(bb,"plugins","plainhead.dll"),p.Dll);
            Check(InstalledPlugins.Load(bb,"plainhead",schemaRoot).Schema.Get("launcher","description","")=="plainhead - how far people walk. second line","a single rule frames nothing; its text stays");
        });
        Console.WriteLine("RESULT "+passed+" passed, "+failed+" failed; fixtures: "+root);return failed==0?0:1;
    }
}
