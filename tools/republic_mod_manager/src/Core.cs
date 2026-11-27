// Republic Mod Manager 0.8.16: generic manifest/schema driven plugin deployment.
// Never loads a DLL during discovery and never edits Workshop defaults or loader code.
// Since 0.9.0 a package needs only [mod] and [hooks] dll; everything Autoload used
// to declare is derived by convention, and a plugin without a launcher schema gets
// its user interface from its own INI (comments become descriptions).
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using Microsoft.Win32;

namespace TesmioAutoload
{
    // Messages the core hands to the window as a language key plus arguments, so
    // the window renders them in the chosen language (Language.Localize). Logs and
    // tests read them through Plain. Layout: \u0001key\u0002arg\u0002arg...
    public static class Msg
    {
        public static string Key(string key, params object[] args) { return "\u0001" + key + String.Concat(args.Select(a => "\u0002" + Convert.ToString(a, System.Globalization.CultureInfo.InvariantCulture))); }
        public static bool IsKey(string text) { return text != null && text.StartsWith("\u0001", StringComparison.Ordinal); }
        public static string KeyOf(string text) { if (!IsKey(text)) return ""; int at = text.IndexOf('\u0002'); return at < 0 ? text.Substring(1) : text.Substring(1, at - 1); }
        public static string[] ArgsOf(string text) { if (!IsKey(text)) return new string[0]; return text.Split('\u0002').Skip(1).ToArray(); }
        public static string Plain(string text) { if (!IsKey(text)) return text; string[] args = ArgsOf(text); return KeyOf(text) + (args.Length > 0 ? " " + String.Join(" | ", args) : ""); }
    }

    public static class SafeFiles
    {
        public static readonly UTF8Encoding Utf8 = new UTF8Encoding(false, true);
        public static string Hash(byte[] bytes)
        {
            using (var sha = SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", "");
        }
        public static string HashFile(string path) { return File.Exists(path) ? Hash(File.ReadAllBytes(path)) : "absent"; }
        public static void NoLinks(string path)
        {
            for (string p = Path.GetFullPath(path); p != null; p = Path.GetDirectoryName(p))
                if ((Directory.Exists(p) || File.Exists(p)) && (File.GetAttributes(p) & FileAttributes.ReparsePoint) != 0)
                    throw new IOException(Msg.Key("err_verknuepfung_reparse_point_nicht", p));
        }
        public static string Child(string root, string relative)
        {
            if (String.IsNullOrWhiteSpace(relative) || Path.IsPathRooted(relative) || relative.Contains(':') ||
                relative.Split('\\', '/').Any(x => x == ".." || x == "." || x.Length == 0 || x.EndsWith(".") || x.EndsWith(" ")))
                throw new IOException(Msg.Key("err_ungueltiger_relativer_pfad", relative));
            string prefix = Path.GetFullPath(root).TrimEnd('\\', '/') + Path.DirectorySeparatorChar;
            string result = Path.GetFullPath(Path.Combine(prefix, relative));
            if (!result.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) throw new IOException(Msg.Key("err_pfad_verlaesst_paket", relative));
            NoLinks(result);
            return result;
        }
        public static byte[] Read(string path, int limit)
        {
            NoLinks(path);
            using (var file = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                if (file.Length > limit) throw new IOException(Msg.Key("err_datei_zu_gross", path));
                var data = new byte[(int)file.Length];
                int done = 0;
                while (done < data.Length)
                {
                    int n = file.Read(data, done, data.Length - done);
                    if (n == 0) throw new EndOfStreamException(path);
                    done += n;
                }
                return data;
            }
        }
        public static string Text(string path) { return Decode(Read(path, 1024 * 1024)); }
        public static string Decode(byte[] bytes)
        {
            string text = Utf8.GetString(bytes);
            if (text.IndexOf('\0') >= 0 || text.StartsWith("\uFEFF", StringComparison.Ordinal)) throw new FormatException(Msg.Key("err_ini_muss_utf_8"));
            return text;
        }
        public static void RequireX64Dll(byte[] b)
        {
            if (b.Length < 256 || b[0] != 'M' || b[1] != 'Z') throw new FormatException(Msg.Key("err_keine_pe_dll"));
            int p = BitConverter.ToInt32(b, 60);
            if (p < 64 || p > b.Length - 26 || BitConverter.ToUInt32(b, p) != 0x4550 ||
                BitConverter.ToUInt16(b, p + 4) != 0x8664 || (BitConverter.ToUInt16(b, p + 22) & 0x2000) == 0 ||
                BitConverter.ToUInt16(b, p + 24) != 0x20b)
                throw new FormatException(Msg.Key("err_eine_x64_plugin_dll"));
        }
    }

    // Original lines are retained. Repeated sections/keys are rejected explicitly,
    // not collapsed: plugins with repeated directives need a future dedicated adapter.
    public sealed class Ini
    {
        public readonly List<string> Lines;
        public readonly Dictionary<string, string> Values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        public readonly HashSet<string> Sections = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, int> indices = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        public readonly string Newline;
        public Ini(string text)
        {
            if (text.Length > 1024 * 1024 || text.IndexOf('\0') >= 0 || text.StartsWith("\uFEFF", StringComparison.Ordinal)) throw new FormatException(Msg.Key("err_ini_zu_gross_oder"));
            Newline = text.Contains("\r\n") ? "\r\n" : "\n";
            Lines = text.Replace("\r\n", "\n").Split('\n').ToList();
            string section = null;
            for (int i = 0; i < Lines.Count; ++i)
            {
                string line = Lines[i].Trim();
                if (line.Length == 0 || line.StartsWith(";") || line.StartsWith("#") || line.All(c => c == '-')) continue;
                if (line.StartsWith("["))
                {
                    if (!line.EndsWith("]") || line.Length < 3) throw new FormatException(Msg.Key("err_zeile_abschnitt_ungueltig", i + 1));
                    section = line.Substring(1, line.Length - 2).Trim();
                    if (!Sections.Add(section)) throw new FormatException(Msg.Key("err_zeile_abschnitt_doppelt", i + 1, section));
                    continue;
                }
                int eq = line.IndexOf('=');
                if (section == null || eq <= 0) throw new FormatException(Msg.Key("err_zeile_zuweisung_ohne_abschnitt", i + 1));
                string key = line.Substring(0, eq).Trim(), value = line.Substring(eq + 1).Trim();
                // An empty value is legal INI (the loader's own plugins ship "speed_concrete =" for
                // "use the default"); only an empty key is a broken line.
                if (key.Length == 0) throw new FormatException(Msg.Key("err_zeile_leerer_schluessel_wert", i + 1));
                string id = Id(section, key);
                if (Values.ContainsKey(id)) throw new FormatException(Msg.Key("err_zeile_mehrdeutiger_doppelter_schluessel", i + 1, id));
                Values.Add(id, value); indices.Add(id, i);
            }
        }
        public static string Id(string section, string key) { return section.ToLowerInvariant() + "/" + key.ToLowerInvariant(); }
        public string Get(string section, string key, string fallback = null)
        {
            string value; return Values.TryGetValue(Id(section, key), out value) ? value : fallback;
        }
        public string Required(string section, string key)
        {
            string value = Get(section, key); if (value == null) throw new FormatException(Msg.Key("err_fehlt", section, key)); return value;
        }
        public string Render(Dictionary<string, string> changes)
        {
            var lines = new List<string>(Lines);
            var extra = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
            foreach (var pair in changes)
            {
                if (pair.Value.Contains('\n') || pair.Value.Contains('\r') || pair.Value.Contains('\0') || pair.Value.Length == 0)
                    throw new FormatException(Msg.Key("err_ungueltiger_ini_wert", pair.Key));
                int at;
                if (indices.TryGetValue(pair.Key, out at))
                {
                    if (Values[pair.Key] != pair.Value) lines[at] = lines[at].Substring(0, lines[at].IndexOf('=') + 1) + " " + pair.Value;
                }
                else
                {
                    string[] id = pair.Key.Split('/');
                    if (id.Length != 2) throw new FormatException(Msg.Key("err_ungueltiger_ini_schluessel", pair.Key));
                    if (!extra.ContainsKey(id[0])) extra[id[0]] = new List<string>();
                    extra[id[0]].Add(id[1] + " = " + pair.Value);
                }
            }
            // Insert missing keys into their existing section, never create duplicates.
            var result = new List<string>(); string current = null;
            Action flush = () => { if (current != null && extra.ContainsKey(current)) { result.AddRange(extra[current]); extra.Remove(current); } };
            foreach (string line in lines)
            {
                string t = line.Trim();
                if (t.StartsWith("[") && t.EndsWith("]")) { flush(); current = t.Substring(1, t.Length - 2).Trim().ToLowerInvariant(); }
                result.Add(line);
            }
            flush();
            foreach (var section in extra) { result.Add("[" + section.Key + "]"); result.AddRange(section.Value); }
            return String.Join(Newline, result);
        }
        public static string Sparse(Dictionary<string, string> values)
        {
            var b = new StringBuilder("; Personal overrides. Package defaults remain untouched.\r\n");
            foreach (var group in values.OrderBy(p => p.Key).GroupBy(p => p.Key.Split('/')[0]))
            {
                b.Append("\r\n[" + group.Key + "]\r\n");
                foreach (var pair in group) b.Append(pair.Key.Substring(pair.Key.IndexOf('/') + 1) + " = " + pair.Value + "\r\n");
            }
            return b.ToString();
        }
        public string RewriteValues(Dictionary<string, string> values)
        {
            var removed = new HashSet<int>(indices.Where(p => !values.ContainsKey(p.Key)).Select(p => p.Value));
            string text = String.Join(Newline, Lines.Where((line, i) => !removed.Contains(i)));
            return new Ini(text).Render(values);
        }
    }

    public sealed class Field
    {
        public string Section, Key, Label, Description, Type;
        public string DefaultValue;
        public bool HasPackageDefault;
        public string CollectionId = "", CollectionItem = "";
        public bool RemovableCollectionItem;
        public decimal Minimum, Maximum;
        public string[] Choices;
        // Generic (INI-derived) schema: a value may carry an inline comment such as
        // "130 ; (stock 121)". The comment is ignored for comparison and validation
        // and is dropped only when the user actually changes that value.
        public bool Lenient;
        public string Id { get { return Ini.Id(Section, Key); } }
        public string Normalize(string value)
        {
            if (Type == "readonly") return value;
            if (Lenient) value = GenericSchema.StripComment(value);
            if (Type == "text")
            {
                // Lenient (INI-derived or local) schemas accept an empty text value: the plugin then uses its default.
                if ((value.Length == 0 && !Lenient) || value.Length > 4096 || value.IndexOfAny(new[] { '\n', '\r', '\0' }) >= 0) throw new FormatException(Msg.Key("err_ein_einzeiliger_wert_ist", Label));
                return value;
            }
            if (Type == "boolean") { if (value != "0" && value != "1") throw new FormatException(Msg.Key("err_nur_0_oder_1", Label)); return value; }
            if (Type == "choice") { if (!Choices.Contains(value)) throw new FormatException(Msg.Key("err_ungueltige_auswahl", Label)); return value; }
            decimal n;
            if (!Decimal.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out n) || n < Minimum || n > Maximum ||
                (Type == "integer" && Decimal.Truncate(n) != n)) throw new FormatException(Msg.Key("err_zahl_von_bis_erforderlich", Label, Minimum, Maximum));
            return n.ToString("0.############################", CultureInfo.InvariantCulture);
        }
        public bool Equivalent(string left,string right)
        {
            try{return Normalize(left)==Normalize(right);}catch(FormatException){return false;}
        }
    }

    public sealed class CollectionSpec
    {
        public string Id, Source, SourcePlugin, SourceSection, SourceReadySection, SourceReadyKey, SourceReadyValue;
        public string ResourceGroup, MatrixGroup, CountSection, CountKey, ItemPrefix;
        public string[] TargetSections, TargetLabels, TargetLabelKeys, TargetIcons;
        public string Type, DefaultValue, Unit, UnitKey, ItemLabel, ItemLabelKey, ItemDescriptionKey, CoefficientDescriptionKey;
        public string EmptyNotice, EmptyNoticeKey, EmptyNoticeStyle;
        public decimal Minimum, Maximum, Step;
        public int MaximumItems;
        public bool UserOwned, AllowRemoveDefaults, RequirePositiveWhenEnabled;
        public string CountId { get { return Ini.Id(CountSection, CountKey); } }
    }

    // Tolerant reader for a package manifest: rmm.mod.ini, or an older soviet.mod.ini. Old
    // manifests repeat directives and carry sections this program no longer uses; the strict Ini
    // class rejects both on purpose, so the manifest gets its own reader. Nothing here is
    // executed or resolved - it is text.
    public sealed class Manifest
    {
        readonly Dictionary<string, List<KeyValuePair<string, string>>> sections = new Dictionary<string, List<KeyValuePair<string, string>>>(StringComparer.OrdinalIgnoreCase);
        public static Manifest Parse(string text)
        {
            var m = new Manifest(); string section = null; int number = 0;
            foreach (string raw in text.Replace("\r\n", "\n").Split('\n'))
            {
                number++; string line = raw.Trim();
                if (line.Length == 0 || line.StartsWith(";") || line.StartsWith("#")) continue;
                if (line.StartsWith("["))
                {
                    if (!line.EndsWith("]") || line.Length < 3) throw new FormatException(Msg.Key("err_manifest_zeile_abschnitt_ungueltig", number));
                    section = line.Substring(1, line.Length - 2).Trim();
                    if (!sections_(m).ContainsKey(section)) sections_(m)[section] = new List<KeyValuePair<string, string>>();
                    continue;
                }
                int eq = line.IndexOf('=');
                if (section == null || eq <= 0) throw new FormatException(Msg.Key("err_manifest_zeile_zuweisung_ohne", number));
                string key = line.Substring(0, eq).Trim(), value = line.Substring(eq + 1).Trim();
                if (key.Length == 0) throw new FormatException(Msg.Key("err_manifest_zeile_leerer_schluessel", number));
                sections_(m)[section].Add(new KeyValuePair<string, string>(key, value));
            }
            return m;
        }
        static Dictionary<string, List<KeyValuePair<string, string>>> sections_(Manifest m) { return m.sections; }
        public bool Has(string section) { return sections.ContainsKey(section); }
        // Keys of a section that actually carry a value (a bare `key =` counts as absent).
        public List<string> Keys(string section)
        {
            List<KeyValuePair<string, string>> list;
            return sections.TryGetValue(section, out list) ? list.Where(x => x.Value.Length > 0).Select(x => x.Key).ToList() : new List<string>();
        }
        public List<string> All(string section, string key)
        {
            List<KeyValuePair<string, string>> list;
            if (!sections.TryGetValue(section, out list)) return new List<string>();
            return list.Where(x => x.Key.Equals(key, StringComparison.OrdinalIgnoreCase) && x.Value.Length > 0).Select(x => x.Value).ToList();
        }
        public string Get(string section, string key, string fallback = null)
        {
            var values = All(section, key);
            if (values.Count > 1) throw new FormatException(Msg.Key("err_mehrdeutiger_doppelter_schluessel_im", section, key));
            return values.Count == 1 ? values[0] : fallback;
        }
        public string Required(string section, string key)
        {
            string value = Get(section, key);
            if (value == null) throw new FormatException(Msg.Key("err_fehlt_im_manifest", section, key));
            return value;
        }
    }

    // Builds a launcher schema for a plugin INI that ships without one. Every key
    // becomes a field, the comment block above it (plus an inline comment) becomes
    // the description, and the value decides the type. Fields are grouped by INI
    // section on the application's own settings tab. The result is an ordinary
    // schema Ini, so the rest of the application does not know the difference -
    // except ConfigRules, which cannot reject unknown keys it never declared.
    public static class GenericSchema
    {
        static readonly Regex BooleanKey = new Regex("^(enabled?|debug|probe|verbose|active|front|log|allow_.*|use_.*|show_.*|hide_.*|force_.*|.*_enabled|.*_only|.*_check|.*_fix)$", RegexOptions.IgnoreCase);
        static readonly Regex BooleanHint = new Regex(@"\b0\s*(or|oder|/|and|und)\s*1\b|\b1\s*(or|oder|/)\s*0\b|\b0\s+(leaves|disables|turns off|unloads|removes|schaltet|deaktiviert|laesst|lÃ¤sst)\b|\b(on/off|ein/aus|aktiviert|deaktiviert|disable|enable)\b", RegexOptions.IgnoreCase);
        static readonly Regex IntegerValue = new Regex(@"^-?\d{1,18}$"), DecimalValue = new Regex(@"^-?(\d{1,18}\.\d{1,18}|\.\d{1,18}|\d{1,18}\.)$");
        // A 0/1 value is a switch unless something says it is a quantity: a key
        // that names a count, or a comment that mentions another whole number or a
        // range ("2-12", "1..13", "up to 400"). Version-like tokens (v1.6, 1.1.1.9)
        // and decimals are not whole numbers for this purpose.
        static readonly Regex QuantityKey = new Regex("(^|_)(count|max|maximum|min|minimum|limit|number|num|days|frequency|size|level|index|slot|priority|seed|port|id|scale|steps?|amount|radius|distance|speed|percent|factor|ratio|delay|timeout|interval|width|height|length|depth|mode|rva|address|offset|origin|component|channel|x|y|z|cm|mm|m|km|kg|ms|sec|seconds|minutes|hours|value|weight|strength|threshold|version)(_|$)", RegexOptions.IgnoreCase);
        static readonly Regex OtherNumber = new Regex(@"(?<![\w.\-])(\d+)(?![\w.])");
        static readonly Regex RangeHint = new Regex(@"\b\d+\s*(-|\.\.|to|bis)\s*\d+\b|\b(up to|at most|at least|maximal|mindestens|hoechstens|hÃ¶chstens)\b", RegexOptions.IgnoreCase);
        static bool LooksLikeSwitch(string key, string value, string text)
        {
            if (value != "0" && value != "1") return false;
            if (BooleanKey.IsMatch(key) || BooleanHint.IsMatch(text)) return true;
            if (QuantityKey.IsMatch(key) || RangeHint.IsMatch(text)) return false;
            foreach (Match m in OtherNumber.Matches(text)) if (m.Groups[1].Value != "0" && m.Groups[1].Value != "1") return false;
            return true;
        }
        public static string StripComment(string value)
        {
            if (value == null) return "";
            for (int i = 0; i < value.Length; i++)
            {
                char c = value[i];
                if ((c == ';' || c == '#') && (i == 0 || Char.IsWhiteSpace(value[i - 1]))) return value.Substring(0, i).Trim();
            }
            return value.Trim();
        }
        static string Clean(string comment)
        {
            string s = comment.Trim();
            if (s.StartsWith(";") || s.StartsWith("#")) s = s.Substring(1);
            if (s.StartsWith(" ")) s = s.Substring(1);
            return s.TrimEnd();
        }
        static string OneLine(string text) { return text.Replace("\r", " ").Replace("\n", " ").Replace("\0", "").Trim(); }
        // A comment line that only draws a rule ("=====", "-----", "* * *"): decoration, not text.
        static bool Ruler(string comment)
        {
            string s = comment.Trim();
            return s.Length >= 3 && s.All(c => "=-*#~_+ ".IndexOf(c) >= 0) && s.Any(c => c != ' ');
        }
        public static Ini Build(Package p)
        {
            var b = new StringBuilder(); var lines = p.Defaults.Lines;
            // The file's opening comment. Many INIs start with a banner - a rule, a title line
            // such as "Name 1.2 - Configuration", a rule - and only then say what the plugin
            // does. Rules are dropped, and what stands between the first two rules is the banner.
            var header = new List<string>(); var framed = new List<string>(); int rules = 0;
            for (int i = 0; i < lines.Count; i++)
            {
                string t = lines[i].Trim();
                if (t.StartsWith("[")) break;
                if (!t.StartsWith(";") && !t.StartsWith("#")) continue;
                string text = Clean(t);
                if (Ruler(text)) { rules++; continue; }
                if (rules == 1) framed.Add(text); else header.Add(text);
            }
            // A single rule with nothing closing it framed no banner: its lines are the text.
            if (rules == 1) header.AddRange(framed);
            // The header goes into the one-line heading area: first paragraph only,
            // cut at a sentence end once it gets long.
            var firstParagraph = new List<string>();
            foreach (string h in header) { if (h.Length == 0 && firstParagraph.Count > 0) break; if (h.Length > 0) firstParagraph.Add(h); }
            string description = Regex.Replace(OneLine(String.Join(" ", firstParagraph)), @"\s{2,}", " ");
            if (description.Length > 200)
            {
                int cut = description.LastIndexOf(". ", 200, StringComparison.Ordinal);
                description = cut > 40 ? description.Substring(0, cut + 1) : description.Substring(0, 200).TrimEnd() + " ...";
            }
            b.Append("[launcher]\nlayout_version = 1\nvisible = 1\nid = " + p.Id + "\nconfig = " + p.ConfigName + "\nmaximum_value_length = 4096\ndefault_tab = settings\nicon = builtin:gear\n");
            if (description.Length > 0) b.Append("description = " + description + "\n");
            string enabledField = ""; var fields = new StringBuilder(); var groupText = new StringBuilder(); var groups = new List<string>(); string section = null; int order = 0;
            for (int i = 0; i < lines.Count; i++)
            {
                string t = lines[i].Trim();
                if (t.Length == 0 || t.StartsWith(";") || t.StartsWith("#") || t.All(c => c == '-')) continue;
                if (t.StartsWith("["))
                {
                    section = t.Substring(1, t.Length - 2).Trim();
                    if (!groups.Contains(section)) { groups.Add(section); groupText.Append("[group:" + section + "]\ntab = settings\nlabel = " + section + "\norder = " + (groups.Count * 10) + "\n"); }
                    continue;
                }
                int eq = t.IndexOf('='); if (section == null || eq <= 0) continue;
                string key = t.Substring(0, eq).Trim(), raw = t.Substring(eq + 1).Trim(), value = StripComment(raw);
                if (key.Length == 0 || raw.Length == 0) continue;
                var block = new List<string>();
                for (int j = i - 1; j >= 0; j--) { string c = lines[j].Trim(); if (c.StartsWith(";") || c.StartsWith("#")) block.Insert(0, Clean(c)); else break; }
                if (raw.Length > value.Length) block.Add(Clean(raw.Substring(value.Length).Trim()));
                // Line structure survives as the two-character sequence \n, which the
                // presentation turns back into line breaks; an INI value itself cannot
                // hold a newline. Runs of blank comment lines collapse to one.
                var kept = new List<string>();
                foreach (string line in block) { if (Ruler(line)) continue; if (line.Length == 0 && (kept.Count == 0 || kept[kept.Count - 1].Length == 0)) continue; kept.Add(line); }
                while (kept.Count > 0 && kept[kept.Count - 1].Length == 0) kept.RemoveAt(kept.Count - 1);
                string text = OneLine(String.Join("\\n", kept));
                string type, range = "";
                if (LooksLikeSwitch(key, value, text)) type = "boolean";
                else if (IntegerValue.IsMatch(value)) { type = "integer"; range = "minimum = -1000000000\nmaximum = 1000000000\nstep = 1\n"; }
                else if (DecimalValue.IsMatch(value)) { type = "decimal"; range = "minimum = -1000000000\nmaximum = 1000000000\nstep = 0.01\n"; }
                else type = "text";
                if (enabledField.Length == 0 && type == "boolean" && key.Equals("enabled", StringComparison.OrdinalIgnoreCase)) enabledField = Ini.Id(section, key);
                order++;
                fields.Append("[field:g" + order + "]\nsection = " + section + "\nkey = " + key + "\nlabel = " + key + "\ntype = " + type + "\ngroup = " + section + "\norder = " + order + "\n" + range);
                if (text.Length > 0) fields.Append("description = " + text + "\n");
            }
            if (enabledField.Length > 0) b.Append("enabled_field = " + enabledField + "\n");
            b.Append(groupText);
            if (groups.Count == 0) b.Append("[group:general]\ntab = settings\nlabel = general\norder = 10\nnotice = Dieses Paket hat keine Einstellungen.\n");
            b.Append(fields);
            return new Ini(b.ToString());
        }
    }

    public sealed class Package
    {
        public string Root, Id, Name, Version, Target, ConfigName, DllPath, DefaultsPath, SchemaPath, EnabledField;
        public string AssetsDir, AssetsFolder = ""; public readonly List<string> AssetFiles = new List<string>();
        // The manifest this package was read from ("" for a DLL that has none), and the key of the
        // plugin this one takes the place of ("" = none): [mod] replaces.
        public string ManifestPath = "", Replaces = "";
        // A schema from settings_schemas beside rmm.exe that describes this plugin ("" = none);
        // its language files lie beside it, not in the package.
        public string LocalSchemaPath = "";
        public const string ManifestName = "rmm.mod.ini", LegacyManifestName = "soviet.mod.ini";
        // 0.4.80: a content package's INI fragments by kind (resources, deposits, needs, buildings).
        public readonly Dictionary<string, string> ContentFragments = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        // 0.8.0: extra materials for vehicle production (Vehicle Materials): the normalised
        // [materials] text of the package and the resource names it carries.
        public string VehicleFragment = ""; public readonly List<string> VehicleResources = new List<string>();
        public readonly Dictionary<string, string> VehicleCaptions = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        // 0.8.3: rule sets for Vanilla Buildings that belong to the vehicle part - the storage a
        // vehicle factory needs for the material. Provided only while the vehicle part is on.
        public string VehicleRules = "";
        // 0.8.9: building sections this package shares with other packages ([content]
        // shared_buildings). Whichever provided package comes first in the fixed order supplies
        // such a section; the others step in when it is switched off. Both have to name it.
        public readonly List<string> SharedBuildings = new List<string>();
        // A keyed editor schema (editor_type = keyed_*) shipped in the package: the
        // master-detail editor owns the INI, Session only deploys DLL, assets and switches.
        public string EditorSchema; public bool EditorManaged;
        public byte[] Dll, DefaultsBytes = new byte[0];
        public Ini Defaults, Schema;
        // "plugin": a plugin DLL the loader finds; this application switches and configures it.
        // "content": resources/deposits/needs/buildings fragments without a DLL of their own.
        public string Kind = "plugin";
        // Generic: no launcher schema shipped, user interface derived from the INI.
        // UserOverlay: the INI beside the DLL is the author's original and is never written;
        // personal values live in user_config\<name>.ini only. True for every Workshop DLL.
        // HasConfig: false for a DLL that ships without any INI.
        public bool Generic, UserOverlay, HasConfig = true;
        // EffectiveIni ([configuration] effective_ini = 1 in the manifest): the plugin opens its
        // INI by path - <loader>\plugins\<key>.ini when that file exists, else the one beside its
        // DLL - and never looks into user_config. Personal values are still stored there, and a
        // save also writes the finished INI (original plus personal values) to
        // <loader>\plugins\<key>.ini, where the plugin reads it.
        public bool EffectiveIni;
        // Installed: a DLL found in <loader>\plugins, which no Workshop update replaces. Its
        // configuration is managed as a protected base (see InstalledPlugins):
        // UpstreamPath keeps the original INI, RefreshUpstream says that the INI
        // currently in plugins\ is the (new) original and must be copied there.
        public bool Installed, RefreshUpstream;
        // A local schema (settings_schemas\<name>.launcher.ini) describes a foreign
        // INI it does not own: values may carry inline comments and the INI may
        // have keys the schema does not know. Only a package's own schema is strict.
        public bool LenientValues;
        public string UpstreamPath = "", DefaultsHash = "absent";
        public readonly List<string> Hints = new List<string>();
        // [dependencies] of the SML manifest, resolved against the catalog by Catalog.ResolveDependencies.
        public readonly List<Dependency> Dependencies = new List<Dependency>();
        public readonly List<Field> Fields = new List<Field>();
        public readonly List<CollectionSpec> Collections = new List<CollectionSpec>();
        public readonly List<string> RequiresLocal = new List<string>(), ConflictsLocal = new List<string>();
        public readonly Dictionary<string, string> InputHashes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        public bool Visible;
        public int MaximumValueLength = 4096;
        public bool IsDefault(string id,string value)
        {
            string standard;if(!Defaults.Values.TryGetValue(id,out standard))return false;
            Field field=Fields.FirstOrDefault(x=>x.Id==id);
            return field==null?standard==value:field.Equivalent(standard,value);
        }
        public override string ToString() { return Name + " â€” " + Version + " â€” " + Root; }
        // 0.4.48: a file or folder name under [assets] dir - letters, digits, space and plain punctuation,
        // starting with a letter or digit, not ending in a dot or space (Windows would drop it).
        static bool SafeAssetName(string part)
        { return !String.IsNullOrEmpty(part) && part.Length <= 96 && Regex.IsMatch(part, "^[A-Za-z0-9][A-Za-z0-9 ._+()&,'-]*$") && !part.EndsWith(".") && !part.EndsWith(" ") && !part.Contains(".."); }
        static bool SafeToken(string value, int maximum)
        { return !String.IsNullOrWhiteSpace(value) && value.Length <= maximum && Regex.IsMatch(value, "^[A-Za-z0-9][A-Za-z0-9._-]*$"); }
        static string[] Tokens(string raw, string label)
        {
            if (String.IsNullOrWhiteSpace(raw)) return new string[0];
            string[] values = raw.Split('|').Select(x => x.Trim()).ToArray();
            if (values.Any(x => !SafeToken(x, 64)) || values.Distinct(StringComparer.OrdinalIgnoreCase).Count() != values.Length)
                throw new FormatException(Msg.Key("err_ungueltige_oder_doppelte_kennung", label));
            return values;
        }
        static string[] List(Ini ini, string section, string key, int length, string fallback)
        {
            string raw = ini.Get(section, key, "");
            string[] values = raw.Length == 0 ? Enumerable.Repeat(fallback, length).ToArray() : raw.Split('|').Select(x => x.Trim()).ToArray();
            if (values.Length != length) throw new FormatException(Msg.Key("err_anzahl_passt_nicht_zu", section, key));
            return values;
        }
        // [assets] dir of a plugin package (travels with the DLL) or [content] assets of a content
        // package (copied into the loader's vfs): a folder of plain files, no executables.
        static void LoadAssets(Package p, string declaredAssets)
        {
            if (declaredAssets.Length == 0) return;
            p.AssetsDir = SafeFiles.Child(p.Root, declaredAssets);
            if (!Directory.Exists(p.AssetsDir)) throw new FormatException(Msg.Key("err_deklarierter_assets_ordner_fehlt", declaredAssets));
            p.AssetsFolder = Path.GetFileName(p.AssetsDir.TrimEnd('\\', '/'));
            if (!SafeToken(p.AssetsFolder, 64) || p.AssetsFolder.Equals(p.Target + ".dll", StringComparison.OrdinalIgnoreCase)) throw new FormatException(Msg.Key("err_ungueltiger_assets_ordnername", p.AssetsFolder));
            SafeFiles.NoLinks(p.AssetsDir);
            foreach (string file in Directory.GetFiles(p.AssetsDir, "*", SearchOption.AllDirectories).OrderBy(x => x, StringComparer.OrdinalIgnoreCase))
            {
                string relative = file.Substring(p.AssetsDir.Length).TrimStart('\\', '/');
                if (relative.Split('\\', '/').Any(part => !SafeAssetName(part))) throw new FormatException(Msg.Key("err_ungueltiger_asset_dateiname", relative));
                if (relative.EndsWith(".dll", StringComparison.OrdinalIgnoreCase) || relative.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)) throw new FormatException(Msg.Key("err_ausfuehrbare_dateien_gehoeren_nicht", relative));
                if (new FileInfo(file).Length > 64L * 1024 * 1024) throw new FormatException(Msg.Key("err_asset_zu_gross", relative));
                p.AssetFiles.Add(relative);
            }
            if (p.AssetFiles.Count > 512) throw new FormatException(Msg.Key("err_zu_viele_asset_dateien"));
        }
        // rmm.mod.ini, else an older soviet.mod.ini, else null.
        public static string FindManifest(string folder)
        {
            foreach (string name in new[] { ManifestName, LegacyManifestName }) { string path = Path.Combine(folder, name); if (File.Exists(path)) return path; }
            return null;
        }
        // The package folder of a plugin DLL: the nearest folder upwards with a manifest, else the
        // Workshop item folder (the one directly below "784150"), else the DLL's own folder.
        public static string RootOf(string dllPath)
        {
            string home = Path.GetDirectoryName(Path.GetFullPath(dllPath)), dir = home, item = null;
            for (int depth = 0; depth < 5 && !String.IsNullOrEmpty(dir); depth++)
            {
                if (FindManifest(dir) != null) return dir;
                string parent = Path.GetDirectoryName(dir);
                if (parent != null && Path.GetFileName(parent).Equals("784150", StringComparison.OrdinalIgnoreCase)) { item = dir; break; }
                dir = parent;
            }
            return item ?? home;
        }
        static List<FoundPlugin> PluginsBelow(string root) { return PluginScan.Discover("", root); }
        // root: a package folder, or the full path of one plugin DLL (the only way to name a DLL
        // that shares its folder with another one).
        public static Package Load(string root)
        {
            var p = new Package(); string full = Path.GetFullPath(root), chosenDll = null;
            if (full.EndsWith(".dll", StringComparison.OrdinalIgnoreCase) && File.Exists(full)) { chosenDll = full; full = RootOf(full); }
            p.Root = full; SafeFiles.NoLinks(p.Root);
            string manifestPath = FindManifest(p.Root);
            Manifest manifest;
            if (manifestPath != null)
            {
                byte[] manifestBytes = SafeFiles.Read(manifestPath, 1024 * 1024);
                manifest = Manifest.Parse(SafeFiles.Decode(manifestBytes));
                p.InputHashes.Add(manifestPath, SafeFiles.Hash(manifestBytes)); p.ManifestPath = manifestPath;
            }
            else manifest = Manifest.Parse("");
            // The plugin DLL: the one named by the caller, the one an old manifest names, [mod]
            // plugin, or the only one below the package folder.
            var declaredDlls = manifest.All("hooks", "dll");
            if (declaredDlls.Count > 1) throw new FormatException(Msg.Key("err_mehrere_native_hooks_in", declaredDlls.Count));
            // A DLL named by the caller in a folder that holds several: the manifest belongs to
            // one of them only - the one [mod] plugin names, the one an old [hooks] dll names, or
            // the one called like the last part of the id. The others have no manifest.
            if (chosenDll != null && manifestPath != null)
            {
                var siblings = PluginsBelow(p.Root).SelectMany(x => new[] { x }.Concat(x.Shadowed)).ToList();
                if (siblings.Count > 1)
                {
                    string key = Path.GetFileNameWithoutExtension(chosenDll), named = manifest.Get("mod", "plugin", ""), last = manifest.Get("mod", "id", ""); last = last.Substring(last.LastIndexOf('.') + 1);
                    bool mine = named.Length > 0 ? PluginScan.SameKey(named, key)
                        : declaredDlls.Count == 1 ? String.Equals(SafeFiles.Child(p.Root, declaredDlls[0]), chosenDll, StringComparison.OrdinalIgnoreCase)
                        : PluginScan.SameKey(last, key);
                    if (!mine) { p.InputHashes.Remove(manifestPath); manifestPath = null; p.ManifestPath = ""; manifest = Manifest.Parse(""); declaredDlls = manifest.All("hooks", "dll"); }
                }
            }
            if (chosenDll == null && declaredDlls.Count == 1) { string declared = SafeFiles.Child(p.Root, declaredDlls[0]); if (File.Exists(declared)) chosenDll = declared; }
            if (chosenDll == null)
            {
                var below = PluginsBelow(p.Root).SelectMany(x => new[] { x }.Concat(x.Shadowed)).ToList();
                string wanted = manifest.Get("mod", "plugin", "");
                if (wanted.Length > 0) below = below.Where(x => PluginScan.SameKey(x.Key, wanted)).ToList();
                if (below.Count > 1 && manifestPath != null)
                {
                    string last = manifest.Get("mod", "id", ""); last = last.Substring(last.LastIndexOf('.') + 1);
                    var named = below.Where(x => PluginScan.SameKey(x.Key, last)).ToList(); if (named.Count == 1) below = named;
                }
                if (below.Count > 1) throw new FormatException(Msg.Key("err_mehrere_plugin_dlls", below.Count));
                if (below.Count == 1) chosenDll = below[0].Path;
                else if (wanted.Length > 0) throw new FormatException(Msg.Key("err_manifest_plugin_fehlt", wanted));
            }
            if (manifestPath == null && chosenDll == null) throw new FormatException(Msg.Key("err_ordner_ohne_plugin"));
            string derivedKey = chosenDll != null ? Path.GetFileNameWithoutExtension(chosenDll) : "";
            p.Id = manifestPath != null ? manifest.Required("mod", "id") : "local." + derivedKey;
            p.Name = manifestPath != null ? manifest.Required("mod", "name") : derivedKey;
            p.Version = manifest.Get("mod", "version", "?");
            p.Replaces = manifest.Get("mod", "replaces", "").Trim();
            if (p.Replaces.Length > 0 && !SafeToken(p.Replaces, 64)) throw new FormatException(Msg.Key("err_ungueltige_oder_doppelte_kennung", "replaces"));
            if (manifest.Get("mod", "enabled", "1") != "1") throw new FormatException(Msg.Key("err_paket_ist_im_manifest"));
            if (!SafeToken(p.Id, 160)) throw new FormatException(Msg.Key("err_ungueltige_paket_id", p.Id));
            if (p.Name.Length > 200 || p.Name.Any(Char.IsControl) || p.Version.Length > 80 || p.Version.Any(Char.IsControl)) throw new FormatException(Msg.Key("err_ungueltiger_paketname_oder_version"));
            // The API range is optional; when given it must include the loader's API 4.
            string apiMinRaw = manifest.Get("mod", "tesmio_api_min", ""), apiMaxRaw = manifest.Get("mod", "tesmio_api_max", "");
            if (apiMinRaw.Length > 0 || apiMaxRaw.Length > 0)
            {
                int apiMin = 1, apiMax = 4;
                if ((apiMinRaw.Length > 0 && !Int32.TryParse(apiMinRaw, out apiMin)) || (apiMaxRaw.Length > 0 && !Int32.TryParse(apiMaxRaw, out apiMax)) || apiMin > 4 || apiMax < 4)
                    throw new FormatException(Msg.Key("err_tesmioloader_api_4_liegt"));
            }
            var content = manifest.Keys("content");
            var dependencies = manifest.Keys("dependencies");
            if (chosenDll == null)
            {
                if (content.Count == 0) throw new FormatException(Msg.Key("err_manifest_ohne_hooks_dll"));
                // A content package: no DLL of its own. Its fragments reach the game through the
                // editors of the target plugins, its files through the loader's vfs.
                p.Kind = "content"; p.HasConfig = false; p.Target = ""; p.ConfigName = ""; p.Visible = false;
                foreach (string key in ContentTargets.Keys)
                {
                    string relative = manifest.Get("content", key, ""); if (relative.Length == 0) continue;
                    string path = SafeFiles.Child(p.Root, relative);
                    if (!File.Exists(path)) throw new FormatException(Msg.Key("err_inhaltsdatei_fehlt", key, relative));
                    byte[] bytes = SafeFiles.Read(path, 1024 * 1024); string text = SafeFiles.Decode(bytes);
                    try { new LooseIni(text); } catch (Exception) { throw new FormatException(Msg.Key("err_inhaltsdatei_ungueltig", relative)); }
                    p.ContentFragments[key] = text; p.InputHashes.Add(path, SafeFiles.Hash(bytes));
                }
                {
                    // 0.8.0: no editor takes this part - it becomes a file of its own that Vehicle
                    // Materials reads, and only when the player says so on the package page.
                    string relative = manifest.Get("content", "vehicles", "");
                    if (relative.Length > 0)
                    {
                        string path = SafeFiles.Child(p.Root, relative);
                        if (!File.Exists(path)) throw new FormatException(Msg.Key("err_inhaltsdatei_fehlt", "vehicles", relative));
                        byte[] bytes = SafeFiles.Read(path, 1024 * 1024);
                        try { p.VehicleFragment = VehicleParts.Normalize(SafeFiles.Decode(bytes), p.VehicleResources); } catch (Exception) { throw new FormatException(Msg.Key("err_inhaltsdatei_ungueltig", relative)); }
                        VehicleParts.Captions(SafeFiles.Decode(bytes), p.VehicleCaptions);
                        p.InputHashes.Add(path, SafeFiles.Hash(bytes));
                    }
                }
                {
                    // 0.8.3: rule sets that go with the vehicle part. They reach Vanilla Buildings
                    // like the plain `rules`, but only while the player has the vehicle part on.
                    string relative = manifest.Get("content", "vehicle_rules", "");
                    if (relative.Length > 0)
                    {
                        string path = SafeFiles.Child(p.Root, relative);
                        if (!File.Exists(path)) throw new FormatException(Msg.Key("err_inhaltsdatei_fehlt", "vehicle_rules", relative));
                        byte[] bytes = SafeFiles.Read(path, 1024 * 1024); string text = SafeFiles.Decode(bytes);
                        bool usable = p.VehicleFragment.Length > 0;
                        try { new LooseIni(text); } catch (Exception) { usable = false; }
                        if (!usable || ContentTargets.IdsOf("rules", text).Count == 0) throw new FormatException(Msg.Key("err_inhaltsdatei_ungueltig", relative));
                        p.VehicleRules = text; p.InputHashes.Add(path, SafeFiles.Hash(bytes));
                    }
                }
                {
                    // 0.8.9: every name has to be a section of the package's own buildings file.
                    string shared = manifest.Get("content", "shared_buildings", "");
                    if (shared.Trim().Length > 0)
                    {
                        string buildings; p.ContentFragments.TryGetValue("buildings", out buildings);
                        var sections = new HashSet<string>(buildings == null ? new List<string>() : ContentTargets.IdsOf("buildings", buildings), StringComparer.OrdinalIgnoreCase);
                        foreach (string name in shared.Split(new[] { ',', '|' }).Select(x => x.Trim()).Where(x => x.Length > 0))
                        {
                            if (!sections.Contains(name)) throw new FormatException(Msg.Key("err_shared_building_missing", name));
                            if (!p.SharedBuildings.Contains(name, StringComparer.OrdinalIgnoreCase)) p.SharedBuildings.Add(name);
                        }
                    }
                }
                foreach (string key in content)
                    // allow_settings belongs to older manifests; the [custom:] sections of a
                    // fragment are read anyway, so the key is not ours to complain about.
                    if (!ContentTargets.Keys.Contains(key, StringComparer.OrdinalIgnoreCase) && !key.Equals("assets", StringComparison.OrdinalIgnoreCase) && !key.Equals("vehicles", StringComparison.OrdinalIgnoreCase) && !key.Equals("vehicle_rules", StringComparison.OrdinalIgnoreCase) && !key.Equals("shared_buildings", StringComparison.OrdinalIgnoreCase) && !key.Equals("allow_settings", StringComparison.OrdinalIgnoreCase)) p.Hints.Add(Msg.Key("hint_content_unknown_kind", key));
                LoadAssets(p, manifest.Get("content", "assets", ""));
                if (p.ContentFragments.Count == 0 && p.AssetFiles.Count == 0 && p.VehicleFragment.Length == 0) throw new FormatException(Msg.Key("err_inhaltspaket_ohne_inhalt"));
                foreach (string id in dependencies)
                {
                    if (!SafeToken(id, 160)) throw new FormatException(Msg.Key("err_ungueltige_abhaengigkeits_id", id));
                    p.Dependencies.Add(new Dependency { Id = id, Constraint = manifest.Get("dependencies", id, "").Trim() });
                }
                p.Hints.Add(Msg.Key("hint_content_package", String.Join(", ", p.ContentFragments.Keys.Concat(p.VehicleFragment.Length > 0 ? new[] { "vehicles" } : new string[0]).Concat(p.AssetFiles.Count > 0 ? new[] { "assets" } : new string[0]))));
                p.Defaults = new Ini(""); p.Schema = new Ini("[launcher]\nlayout_version = 1\nvisible = 0\nid = " + p.Id + "\nconfig = none.ini\n");
                return p;
            }
            p.DllPath = chosenDll;
            if (!(p.DllPath.TrimEnd('\\') + "\\").StartsWith(p.Root.TrimEnd('\\') + "\\", StringComparison.OrdinalIgnoreCase)) throw new FormatException(Msg.Key("err_ordner_ohne_plugin"));
            p.Target = Path.GetFileNameWithoutExtension(p.DllPath);
            if (!SafeToken(p.Target, 64)) throw new FormatException(Msg.Key("err_ungueltiger_dll_name_als", p.Target));
            if (content.Count > 0) p.Hints.Add(Msg.Key("hint_content_extra", String.Join(", ", content)));
            foreach (string id in dependencies)
            {
                if (!SafeToken(id, 160)) throw new FormatException(Msg.Key("err_ungueltige_abhaengigkeits_id", id));
                p.Dependencies.Add(new Dependency { Id = id, Constraint = manifest.Get("dependencies", id, "").Trim() });
            }
            // Older manifests name plugins they need or cannot share the game with by DLL name.
            // A needed one is a dependency like any other; RequiresLocal also tells a collection
            // where its list may come from.
            p.RequiresLocal.AddRange(Tokens(manifest.Get("autoload", "requires_local", ""), "requires_local"));
            foreach (string needed in p.RequiresLocal) if (!p.Dependencies.Any(d => d.Id.Equals(needed, StringComparison.OrdinalIgnoreCase))) p.Dependencies.Add(new Dependency { Id = needed });
            foreach (Dependency d in p.Dependencies) if (!p.RequiresLocal.Contains(d.Id, StringComparer.OrdinalIgnoreCase)) p.RequiresLocal.Add(d.Id);
            p.ConflictsLocal.AddRange(Tokens(manifest.Get("autoload", "conflicts_local", ""), "conflicts_local"));
            // The loader hands a plugin the INI beside its DLL, named like the DLL.
            p.ConfigName = p.Target + ".ini";
            p.UserOverlay = true;
            string effectiveIni = manifest.Get("configuration", "effective_ini", "0").Trim();
            if (effectiveIni != "0" && effectiveIni != "1") throw new FormatException(Msg.Key("err_effective_ini_wert"));
            p.EffectiveIni = effectiveIni == "1";
            FormatException defaultsProblem = null;
            p.DefaultsPath = Path.Combine(Path.GetDirectoryName(p.DllPath), p.ConfigName);
            p.Dll = SafeFiles.Read(p.DllPath, 64 * 1024 * 1024); SafeFiles.RequireX64Dll(p.Dll);
            p.InputHashes.Add(p.DllPath, SafeFiles.Hash(p.Dll));
            if (File.Exists(p.DefaultsPath))
            {
                p.DefaultsBytes = SafeFiles.Read(p.DefaultsPath, 1024 * 1024);
                p.DefaultsHash = SafeFiles.Hash(p.DefaultsBytes);
                p.InputHashes.Add(p.DefaultsPath, p.DefaultsHash);
                // An editor-managed package may repeat keys in its INI (vanilla_buildings:
                // target = ..., add = ...); its editor reads the file loosely, so the strict
                // parse only decides for packages without an editor schema.
                try { p.Defaults = new Ini(SafeFiles.Decode(p.DefaultsBytes)); }
                catch (FormatException problem) { p.Defaults = new Ini(""); defaultsProblem = problem; }
            }
            else
            {
                p.HasConfig = false; p.Defaults = new Ini(""); p.InputHashes.Add(p.DefaultsPath, "absent");
                p.Hints.Add(Msg.Key("hint_no_ini"));
            }
            string declaredSchema = manifest.Get("configuration", "launcher_schema", "");
            string conventionalSchema = Path.Combine(p.Root, "config", p.Target + ".launcher.ini");
            if (declaredSchema.Length > 0) p.SchemaPath = SafeFiles.Child(p.Root, declaredSchema);
            else if (File.Exists(conventionalSchema)) p.SchemaPath = conventionalSchema;
            // Second tier: a schema beside rmm.exe for a plugin whose package ships none. It
            // describes an INI it does not own, so values are read leniently.
            Ini localSchema = null;
            if (p.SchemaPath == null && !String.IsNullOrWhiteSpace(Catalog.SchemaRoot))
            {
                string candidatePath = Path.Combine(Path.GetFullPath(Catalog.SchemaRoot), p.Target + ".launcher.ini");
                if (File.Exists(candidatePath))
                {
                    SafeFiles.NoLinks(candidatePath);
                    byte[] bytes = SafeFiles.Read(candidatePath, 1024 * 1024); var candidate = new Ini(SafeFiles.Decode(bytes));
                    // Keyed editors are list entries of their own (LocalEditorSpec).
                    if (candidate.Get("launcher", "editor_type", "").Length == 0)
                    {
                        if (candidate.Get("launcher", "config", "") != p.ConfigName) throw new FormatException(Msg.Key("err_lokales_schema_passt_nicht", candidatePath));
                        localSchema = candidate; p.LenientValues = true; p.InputHashes.Add(candidatePath, SafeFiles.Hash(bytes));
                        p.LocalSchemaPath = candidatePath;
                        if (manifestPath == null) { p.Id = candidate.Get("launcher", "id", p.Id); p.Name = candidate.Get("launcher", "name", p.Name); }
                    }
                }
            }
            if (p.SchemaPath != null)
            {
                try { if (new Ini(SafeFiles.Decode(SafeFiles.Read(p.SchemaPath, 1024 * 1024))).Get("launcher", "editor_type", "").Length > 0) { p.EditorSchema = p.SchemaPath; p.EditorManaged = true; p.SchemaPath = null; } }
                catch (FormatException) { }
                if (p.EditorManaged && !p.HasConfig) throw new FormatException(Msg.Key("err_ein_editor_schema_braucht"));
            }
            if (defaultsProblem != null && !p.EditorManaged) throw defaultsProblem;
            Ini schema;
            if (p.SchemaPath != null)
            {
                byte[] schemaBytes = SafeFiles.Read(p.SchemaPath, 1024 * 1024);
                p.InputHashes.Add(p.SchemaPath, SafeFiles.Hash(schemaBytes));
                schema = new Ini(SafeFiles.Decode(schemaBytes));
                if (schema.Get("launcher", "id") != p.Id || schema.Get("launcher", "config") != p.ConfigName) throw new FormatException(Msg.Key("err_schema_passt_nicht_zu"));
            }
            else if (localSchema != null) schema = localSchema;
            else
            {
                p.Generic = true; schema = GenericSchema.Build(p);
                if (p.HasConfig && !p.EditorManaged) p.Hints.Add(Msg.Key("hint_no_schema_comments"));
            }
            if (p.HasConfig && !p.EditorManaged) p.Hints.Add(Msg.Key(p.EffectiveIni ? "hint_effective" : "hint_overlay", p.ConfigName));
            p.ApplySchema(schema);
            if (p.LenientValues)
            {
                // Keys the INI has and the local schema does not: not editable here, but named.
                var known = new HashSet<string>(p.Fields.Select(f => f.Id), StringComparer.OrdinalIgnoreCase);
                var missing = p.Defaults.Values.Keys.Where(k => !known.Contains(k)).ToList();
                if (missing.Count > 0) p.Hints.Add(Msg.Key("hint_not_in_schema", String.Join(", ", missing)));
            }
            return p;
        }
        // Shared by Workshop packages and installed plugins: turns the schema into
        // fields and collections and validates the defaults against them.
        public void ApplySchema(Ini schema)
        {
            var p = this;
            p.Schema = schema; p.Visible = schema.Get("launcher", "visible", "0") == "1";
            if (!Int32.TryParse(schema.Get("launcher", "maximum_value_length", "4096"), out p.MaximumValueLength) || p.MaximumValueLength < 1 || p.MaximumValueLength > 65535)
                throw new FormatException(Msg.Key("err_ungueltige_maximum_value_length"));
            var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (string section in schema.Sections.Where(s => s.StartsWith("field:", StringComparison.OrdinalIgnoreCase)))
            {
                var f = new Field { Section = schema.Required(section, "section"), Key = schema.Required(section, "key"),
                    Label = schema.Required(section, "label"), Description = schema.Get(section, "description", ""), Type = schema.Required(section, "type") };
                if (!new[] { "boolean", "integer", "decimal", "choice", "readonly", "text" }.Contains(f.Type)) throw new FormatException(Msg.Key("err_nicht_unterstuetzter_feldtyp", f.Type));
                if (!used.Add(f.Id) || !p.Defaults.Values.ContainsKey(f.Id)) throw new FormatException(Msg.Key("err_schemafeld_doppelt_oder_ohne", f.Id));
                f.Lenient = p.Generic || p.LenientValues;
                if (f.Type == "decimal" || f.Type == "integer")
                {
                    f.Minimum = Decimal.Parse(schema.Required(section, "minimum"), CultureInfo.InvariantCulture);
                    f.Maximum = Decimal.Parse(schema.Required(section, "maximum"), CultureInfo.InvariantCulture);
                    if (f.Minimum > f.Maximum) throw new FormatException(Msg.Key("err_ungueltiger_zahlenbereich", f.Id));
                }
                f.Choices = schema.Get(section, "choices", "").Split('|');
                f.DefaultValue = p.Defaults.Values[f.Id]; f.HasPackageDefault = true;
                f.Normalize(f.DefaultValue); p.Fields.Add(f);
            }
            p.EnabledField = schema.Get("launcher", "enabled_field", "");
            if (p.EnabledField.Length > 0 && !p.Fields.Any(f => f.Id == p.EnabledField && f.Type == "boolean"))
                throw new FormatException(Msg.Key("err_enabled_field_muss_auf"));
            foreach (string s in schema.Sections.Where(x => x.StartsWith("collection:", StringComparison.OrdinalIgnoreCase)))
            {
                decimal minimum, maximum, step; int maximumItems;
                if (!Decimal.TryParse(schema.Get(s,"minimum","0"),NumberStyles.Float,CultureInfo.InvariantCulture,out minimum) ||
                    !Decimal.TryParse(schema.Get(s,"maximum","1000000"),NumberStyles.Float,CultureInfo.InvariantCulture,out maximum) || minimum>maximum ||
                    !Decimal.TryParse(schema.Get(s,"step","0.001"),NumberStyles.Float,CultureInfo.InvariantCulture,out step) || step<=0 ||
                    !Int32.TryParse(schema.Get(s,"maximum_items","32"),NumberStyles.None,CultureInfo.InvariantCulture,out maximumItems) || maximumItems<1 || maximumItems>256)
                    throw new FormatException(Msg.Key("err_ungueltige_sammlungsgrenzen", s));
                string source=schema.Required(s,"source"); Match sourceMatch=Regex.Match(source,"^local-plugin:([A-Za-z0-9][A-Za-z0-9._-]{0,63})/([A-Za-z0-9][A-Za-z0-9._-]{0,63})$");
                if(!sourceMatch.Success)throw new FormatException(Msg.Key("err_nicht_unterstuetzte_sammlungsquelle", source));
                string[] targets=Tokens(schema.Required(s,"target_sections"),s+" target_sections");
                if(targets.Length==0)throw new FormatException(Msg.Key("err_sammlung_ohne_target_sections", s));
                var collection=new CollectionSpec {
                    Id=s.Substring(11),Source=source,SourcePlugin=sourceMatch.Groups[1].Value,SourceSection=sourceMatch.Groups[2].Value,
                    SourceReadySection=schema.Get(s,"source_ready_section",""),SourceReadyKey=schema.Get(s,"source_ready_key",""),SourceReadyValue=schema.Get(s,"source_ready_value",""),
                    ResourceGroup=schema.Required(s,"resource_group"),MatrixGroup=schema.Required(s,"matrix_group"),CountSection=schema.Get(s,"count_section","resources"),CountKey=schema.Get(s,"count_key","count"),ItemPrefix=schema.Get(s,"item_prefix","resource"),
                    TargetSections=targets,TargetLabels=List(schema,s,"target_labels",targets.Length,""),TargetLabelKeys=List(schema,s,"target_label_keys",targets.Length,""),TargetIcons=List(schema,s,"target_icons",targets.Length,""),
                    Type=schema.Get(s,"type","decimal"),DefaultValue=schema.Get(s,"default","0"),Unit=schema.Get(s,"unit",""),UnitKey=schema.Get(s,"unit_key",""),
                    ItemLabel=schema.Get(s,"item_label","Item"),ItemLabelKey=schema.Get(s,"item_label_key",""),ItemDescriptionKey=schema.Get(s,"item_description_key",""),CoefficientDescriptionKey=schema.Get(s,"coefficient_description_key",""),
                    EmptyNotice=schema.Get(s,"empty_notice",""),EmptyNoticeKey=schema.Get(s,"empty_notice_key",""),EmptyNoticeStyle=schema.Get(s,"empty_notice_style","warning"),
                    Minimum=minimum,Maximum=maximum,Step=step,MaximumItems=maximumItems,UserOwned=schema.Get(s,"ownership","package")=="user",AllowRemoveDefaults=schema.Get(s,"allow_remove_defaults","0")=="1",RequirePositiveWhenEnabled=schema.Get(s,"require_positive_when_enabled","0")=="1" };
                if(!SafeToken(collection.Id,64) || collection.Type!="decimal" && collection.Type!="integer" || !p.Fields.Any(f=>f.Id==collection.CountId && f.Type=="readonly"))
                    throw new FormatException(Msg.Key("err_unvollstaendige_oder_ungueltige_sammlung", s));
                if(collection.EmptyNoticeStyle!="info"&&collection.EmptyNoticeStyle!="warning")throw new FormatException(Msg.Key("err_empty_notice_style_muss", s));
                bool noReady=collection.SourceReadySection.Length==0&&collection.SourceReadyKey.Length==0&&collection.SourceReadyValue.Length==0;
                bool fullReady=SafeToken(collection.SourceReadySection,64)&&SafeToken(collection.SourceReadyKey,64)&&collection.SourceReadyValue.Length>0;
                if(!noReady&&!fullReady)throw new FormatException(Msg.Key("err_source_ready_section_source", s));
                if(!p.RequiresLocal.Contains(collection.SourcePlugin,StringComparer.OrdinalIgnoreCase))throw new FormatException(Msg.Key("err_sammlungsquelle_muss_auch_in", collection.SourcePlugin));
                new Field {Type=collection.Type,Minimum=minimum,Maximum=maximum,Label=collection.Id}.Normalize(collection.DefaultValue);
                p.Collections.Add(collection);
            }
            ConfigRules.Validate(p,p.Defaults);
        }
        public void AssertUnchanged()
        {
            foreach (var pair in InputHashes) { SafeFiles.NoLinks(pair.Key); if (SafeFiles.HashFile(pair.Key) != pair.Value) throw new IOException(Msg.Key("err_paket_wurde_inzwischen_aktualisiert", pair.Key)); }
        }
    }

    public static class CollectionRules
    {
        public static List<string> Names(CollectionSpec collection, Ini ini)
        {
            int count;if(!Int32.TryParse(ini.Required(collection.CountSection,collection.CountKey),NumberStyles.None,CultureInfo.InvariantCulture,out count)||count<0||count>collection.MaximumItems)
                throw new FormatException(Msg.Key("err_ganzzahl_0_erforderlich", collection.CountSection, collection.CountKey, collection.MaximumItems));
            var result=new List<string>();for(int i=0;i<count;i++)result.Add(ini.Required(collection.CountSection,collection.ItemPrefix+i));return result;
        }
        public static bool SafeItem(string name)
        {return !String.IsNullOrWhiteSpace(name)&&name.Length<64&&!name.Any(c=>c<33||c>126||"=;#[]:/\\\"'".Contains(c));}
        // An item id of an extra section group (0.4.29): "<prefix>:<name>" with both parts safe.
        public static bool SafeItemOrPrefixed(string name)
        {if(SafeItem(name))return true;if(name==null)return false;int colon=name.IndexOf(':');return colon>0&&colon==name.LastIndexOf(':')&&SafeItem(name.Substring(0,colon))&&SafeItem(name.Substring(colon+1));}
        // A typed name as a section or item id (0.4.25): trimmed, lower case, spaces and
        // runs of other separators become one underscore, umlauts are transliterated,
        // everything else outside letters, digits, '_', '-' and '.' is dropped. What comes
        // out either passes SafeItem or is empty.
        public static string IdFromName(string raw)
        {
            if(raw==null)return "";
            string text=raw.Trim().ToLowerInvariant().Replace("ä","ae").Replace("ö","oe").Replace("ü","ue").Replace("ß","ss");
            var result=new System.Text.StringBuilder();bool separator=false;
            foreach(char c in text)
            {
                bool keep=(c>='a'&&c<='z')||(c>='0'&&c<='9')||c=='_'||c=='-'||c=='.';
                if(keep){if(separator&&result.Length>0)result.Append('_');separator=false;result.Append(c);}
                else if(c==' '||c=='\t'||c=='/'||c=='\\'||c==':')separator=true;
            }
            return result.ToString();
        }
        public static Ini Add(Package package,CollectionSpec collection,Ini current,string name,IDictionary<string,string> coefficients)
        {
            ConfigRules.Validate(package,current);if(!SafeItem(name))throw new FormatException(Msg.Key("err_ungueltige_eintrags_id", name));
            var names=Names(collection,current);if(names.Any(x=>x.Equals(name,StringComparison.OrdinalIgnoreCase)))throw new FormatException(Msg.Key("err_eintrag_wird_bereits_verwendet", name));
            if(names.Count>=collection.MaximumItems)throw new FormatException(Msg.Key("err_sammlung_erlaubt_hoechstens_eintraege", collection.MaximumItems));
            var changes=new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase){{collection.CountId,(names.Count+1).ToString(CultureInfo.InvariantCulture)},{Ini.Id(collection.CountSection,collection.ItemPrefix+names.Count),name}};
            var validator=new Field {Type=collection.Type,Minimum=collection.Minimum,Maximum=collection.Maximum,Label=name};
            foreach(string target in collection.TargetSections){string value;if(!coefficients.TryGetValue(target,out value))throw new FormatException(Msg.Key("err_standardwert_fehlt", target, name));changes[Ini.Id(target,name)]=validator.Normalize(value);}
            var added=new Ini(current.Render(changes));ConfigRules.Validate(package,added);return added;
        }
        public static bool CanRemove(Package package,CollectionSpec collection,string name)
        {
            return collection.AllowRemoveDefaults||!Names(collection,package.Defaults).Any(x=>x.Equals(name,StringComparison.OrdinalIgnoreCase));
        }
        public static Ini Remove(Package package,CollectionSpec collection,Ini current,string name)
        {
            ConfigRules.Validate(package,current);if(!CanRemove(package,collection,name))throw new FormatException(Msg.Key("err_paketstandard_kann_nicht_entfernt", name));
            var names=Names(collection,current);int at=names.FindIndex(x=>x.Equals(name,StringComparison.OrdinalIgnoreCase));if(at<0)throw new FormatException(Msg.Key("err_eintrag_wird_nicht_verwendet", name));names.RemoveAt(at);
            var values=new Dictionary<string,string>(current.Values,StringComparer.OrdinalIgnoreCase);
            for(int i=0;i<=names.Count;i++)values.Remove(Ini.Id(collection.CountSection,collection.ItemPrefix+i));
            values[collection.CountId]=names.Count.ToString(CultureInfo.InvariantCulture);for(int i=0;i<names.Count;i++)values[Ini.Id(collection.CountSection,collection.ItemPrefix+i)]=names[i];
            foreach(string target in collection.TargetSections)values.Remove(Ini.Id(target,name));
            if(collection.RequirePositiveWhenEnabled&&package.EnabledField.Length>0)
            {
                bool active=names.Any(item=>collection.TargetSections.Any(target=>{decimal number;return Decimal.TryParse(values.ContainsKey(Ini.Id(target,item))?values[Ini.Id(target,item)]:collection.DefaultValue,NumberStyles.Float,CultureInfo.InvariantCulture,out number)&&number>0;}));
                if(!active)values[package.EnabledField]="0";
            }
            var removed=new Ini(current.RewriteValues(values));ConfigRules.Validate(package,removed);return removed;
        }
        public static Dictionary<string,string> State(CollectionSpec collection,Ini ini)
        {
            var result=new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase);var names=Names(collection,ini);result[collection.CountId]=names.Count.ToString(CultureInfo.InvariantCulture);
            for(int i=0;i<names.Count;i++){string name=names[i];result[Ini.Id(collection.CountSection,collection.ItemPrefix+i)]=name;foreach(string target in collection.TargetSections){string value=ini.Get(target,name);if(value!=null)result[Ini.Id(target,name)]=value;}}
            return result;
        }
    }

    public sealed class RuleException : FormatException
    {
        public readonly string TranslationKey;
        public readonly object[] TranslationArguments;
        public RuleException(string translationKey, params object[] translationArguments)
            : base(translationKey)
        {
            TranslationKey=translationKey;
            TranslationArguments=translationArguments??new object[0];
        }
    }

    // A plugin that exists only as build\plugins\<name>.dll (+ .ini), the way a
    // plain TesmioLoader user installs it, without any Workshop package. There
    // is no shipped original to fall back to, so the INI found in plugins\ is
    // declared the original, copied once to user_config\.autoload\<name>.upstream.ini
    // and never edited in place without that copy existing ("protected base").
    // An INI changed outside this application (a new plugin version, a manual
    // edit) is recognised by the receipt and becomes the new original; personal
    // values from user_config are laid over it again.
    // The loader's log files as data: which ones exist, what a line is about,
    // and whether it reports a problem. Used by the log window and the notices.
    public static class GameLogs
    {
        public sealed class Source { public string Name, Path; }
        public enum Kind { Plain, Warning, Problem }
        static readonly Regex Problem = new Regex(@"\b(error|fatal|fail(ed|ure)?|refused?|mismatch|crash(ed)?|not initialised|exception)\b", RegexOptions.IgnoreCase);
        static readonly Regex Warning = new Regex(@"\b(warn(ing)?|declined|skipped|missing|unknown|unsupported|idle)\b", RegexOptions.IgnoreCase);
        // Plugin summaries such as "0 warning(s), 0 error(s), 0 fatal error(s)" are good news.
        static readonly Regex Clean = new Regex(@"\b0 warning\(s\), 0 error\(s\), 0 fatal", RegexOptions.IgnoreCase);
        // 0.4.78: the level a plugin prints after its name ("weather_roads  INFO  Init ...",
        // "research_expansion  WARN: ...") decides first; an INFO line is raised only by a
        // counter above zero ("12 warning(s)", "errors=2"), never by the word "error" in "errors=0".
        // The subject before the level is optional: a plugin's own detail log starts its lines
        // with the stamp and the level ("[2026-09-12 04:53:48.614] INFO  Init [ready] ...").
        static readonly Regex Level = new Regex(@"^\[[^\]]*\]\s*(?:\S+\s+)?(INFO|WARN(?:ING)?|ERROR|FATAL|DEBUG|TRACE)\b:?", RegexOptions.IgnoreCase);
        static readonly Regex ErrorCount = new Regex(@"\b[1-9]\d*\s+(?:fatal )?error\(s\)|\b(?:errors|fatal)=[1-9]\d*", RegexOptions.IgnoreCase);
        static readonly Regex WarningCount = new Regex(@"\b[1-9]\d*\s+warning\(s\)|\bwarnings=[1-9]\d*", RegexOptions.IgnoreCase);
        // 0.4.83: loader lines carry no level ("buildings_plus  2 generated, ... 0 failed ... 0 error(s)").
        // A word that a number introduces is a counter, not a complaint: a counter above zero decides,
        // a counter at zero is struck out before the keywords are looked at.
        static readonly Regex ZeroCount = new Regex(@"\b0\s+(?:fatal\s+)?[a-z_]+(?:\(s\))?|\b[a-z_]+=0\b", RegexOptions.IgnoreCase);
        static readonly Regex Stamp = new Regex(@"^\[[^\]]*\]\s*");
        public static List<Source> Sources(string build)
        {
            var result = new List<Source>();
            try
            {
                string root = Path.GetFullPath(build);
                string main = Path.Combine(root, "tesmioloader.log");
                if (File.Exists(main)) result.Add(new Source { Name = "tesmioloader.log", Path = main });
                foreach (string file in Directory.GetFiles(root, "tesmioloader.*.log").OrderBy(f => f, StringComparer.OrdinalIgnoreCase))
                    result.Add(new Source { Name = Path.GetFileName(file), Path = file });
                // Plugin detail logs live in logs\ next to the loader's log since the logs folder round; older
                // plugins still write next to tesmioloader.log, so both places are listed.
                string logs = Path.Combine(root, "logs");
                if (Directory.Exists(logs))
                    foreach (string file in Directory.GetFiles(logs, "tesmioloader.*.log").OrderBy(f => f, StringComparer.OrdinalIgnoreCase))
                        result.Add(new Source { Name = "logs\\" + Path.GetFileName(file), Path = file });
            }
            catch (Exception) { }
            return result;
        }
        // Shared read: the game keeps its logs open while it runs.
        public static string Read(string path)
        {
            try
            {
                using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
                using (var reader = new StreamReader(stream, new UTF8Encoding(false, false))) return reader.ReadToEnd();
            }
            catch (Exception e) { return "(" + e.Message + ")"; }
        }
        public static Kind Classify(string line)
        {
            if (line == null) return Kind.Plain;
            if (Clean.IsMatch(line)) return Kind.Plain;
            if (line.IndexOf("hook ok", StringComparison.OrdinalIgnoreCase) >= 0) return Kind.Plain;
            Match level = Level.Match(line);
            if (level.Success)
            {
                string kind = level.Groups[1].Value.ToUpperInvariant();
                if (kind == "ERROR" || kind == "FATAL") return Kind.Problem;
                if (kind.StartsWith("WARN")) return Kind.Warning;
                if (ErrorCount.IsMatch(line)) return Kind.Problem;
                if (WarningCount.IsMatch(line)) return Kind.Warning;
                return Kind.Plain;
            }
            // The game's own lines carry the level in the subject: "game.WARN 6", "game.ERROR setfocus".
            string subject = SubjectOf(line);
            if (subject.EndsWith(".ERROR", StringComparison.OrdinalIgnoreCase) || subject.EndsWith(".FATAL", StringComparison.OrdinalIgnoreCase)) return Kind.Problem;
            if (subject.EndsWith(".WARN", StringComparison.OrdinalIgnoreCase) || subject.EndsWith(".WARNING", StringComparison.OrdinalIgnoreCase)) return Kind.Warning;
            if (ErrorCount.IsMatch(line)) return Kind.Problem;
            if (WarningCount.IsMatch(line)) return Kind.Warning;
            string residue = ZeroCount.Replace(line, " ");
            if (Problem.IsMatch(residue)) return Kind.Problem;
            if (Warning.IsMatch(residue)) return Kind.Warning;
            return Kind.Plain;
        }
        // The first word after the time stamp: "plugin", "bridge", "hook", a plugin's own name, "game.ERROR".
        public static string SubjectOf(string line)
        {
            if (line == null) return "";
            string rest = Stamp.Replace(line, "");
            int end = rest.IndexOfAny(new[] { ' ', '\t' });
            return end < 0 ? rest : rest.Substring(0, end);
        }
        public static List<string> Subjects(IEnumerable<string> lines)
        {
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase); var result = new List<string>();
            foreach (string line in lines) { string s = SubjectOf(line); if (s.Length > 0 && s.Length <= 40 && s != "---" && seen.Add(s)) result.Add(s); }
            result.Sort(StringComparer.OrdinalIgnoreCase); return result;
        }
        // What the loader said at the end of the last run, or its plugin count.
        public static string LastRun(IEnumerable<string> lines)
        {
            string loaded = "", shutdown = "";
            foreach (string line in lines)
            {
                if (line.IndexOf("--- shutdown", StringComparison.Ordinal) >= 0) shutdown = Stamp.Replace(line, "").Trim('-', ' ');
                else if (Regex.IsMatch(line, @"plugin\s+\d+ loaded")) { loaded = Stamp.Replace(line, ""); shutdown = ""; }
            }
            return shutdown.Length > 0 ? shutdown : loaded;
        }
    }

    public static class InstalledPlugins
    {
        // "plugin   <name> <version> from <full path>\<file>.dll": the path may carry spaces
        // ("C:\Program Files (x86)\..."), so the key is the last path part before ".dll".
        static readonly Regex LogLine = new Regex(@"^\[[^\]]*\]\s+plugin\s+(?!service\s)(\S[^\r\n]*?)\s+(\S+)\s+from\s+[^\r\n]*?([^\\/\r\n]+)\.dll[ \t]*\r?$", RegexOptions.Multiline);
        public static string UpstreamPath(string build, string name) { return SafeFiles.Child(Path.GetFullPath(build), "user_config\\.autoload\\" + name + ".upstream.ini"); }
        // Versions as the loader reported them on its last start, by DLL key.
        public static Dictionary<string, string> LoaderVersions(string build)
        {
            var versions = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            try
            {
                string log = Path.Combine(Path.GetFullPath(build), "tesmioloader.log");
                if (!File.Exists(log)) return versions;
                string text = GameLogs.Read(log);
                foreach (Match m in LogLine.Matches(text)) versions[m.Groups[3].Value] = m.Groups[2].Value;
            }
            catch (Exception) { }
            return versions;
        }
        // Plugins that stepped aside on the last start ("<name>  idle - ..."): a replacement does
        // that while the plugin it replaces is loaded. The loader prints no "plugin <name>
        // <version>" line for them, so without this they would read as "not loaded".
        static readonly Regex IdleLine = new Regex(@"^\[[^\]]*\]\s+(\S+)\s+idle - ", RegexOptions.Multiline);
        public static HashSet<string> IdleAtLastStart(string build)
        {
            var idle = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            try
            {
                string log = Path.Combine(Path.GetFullPath(build), "tesmioloader.log");
                if (!File.Exists(log)) return idle;
                foreach (Match m in IdleLine.Matches(GameLogs.Read(log))) idle.Add(m.Groups[1].Value);
            }
            catch (Exception) { }
            return idle;
        }
        // When the loader last wrote its log, or null.
        public static DateTime? LogTime(string build)
        {
            try { string log = Path.Combine(Path.GetFullPath(build), "tesmioloader.log"); return File.Exists(log) ? File.GetLastWriteTime(log) : (DateTime?)null; }
            catch (Exception) { return null; }
        }
        public static Package Load(string build, string name, string schemaRoot)
        {
            build = Path.GetFullPath(build);
            if (!Regex.IsMatch(name ?? "", "^[A-Za-z0-9][A-Za-z0-9._-]{0,63}$")) throw new FormatException(Msg.Key("err_ungueltiger_plugin_name", name));
            var p = new Package { Installed = true, Kind = "plugin", Target = name, ConfigName = name + ".ini" };
            p.DllPath = SafeFiles.Child(build, "plugins\\" + name + ".dll");
            if (!File.Exists(p.DllPath)) throw new FileNotFoundException(Msg.Key("err_installierte_plugin_dll_fehlt", p.DllPath));
            p.Dll = SafeFiles.Read(p.DllPath, 64 * 1024 * 1024); SafeFiles.RequireX64Dll(p.Dll);
            p.InputHashes.Add(p.DllPath, SafeFiles.Hash(p.Dll));
            string local = SafeFiles.Child(build, "plugins\\" + p.ConfigName), upstream = UpstreamPath(build, name);
            string receipt = SafeFiles.Child(build, "user_config\\.autoload\\" + name + ".receipt.ini");
            p.UpstreamPath = upstream; p.Root = Path.GetDirectoryName(p.DllPath);
            bool managed = false;
            if (File.Exists(receipt))
            {
                try { var r = new Ini(SafeFiles.Text(receipt)); managed = r.Get("state", "mode", "") == "installed" && r.Get("state", "ini_hash", "") == SafeFiles.HashFile(local); }
                catch (FormatException) { managed = false; }
            }
            if (managed && File.Exists(upstream)) p.DefaultsPath = upstream;
            else if (File.Exists(local)) { p.DefaultsPath = local; p.RefreshUpstream = true; }
            else { p.DefaultsPath = local; p.HasConfig = false; }
            if (p.HasConfig)
            {
                p.DefaultsBytes = SafeFiles.Read(p.DefaultsPath, 1024 * 1024);
                p.Defaults = new Ini(SafeFiles.Decode(p.DefaultsBytes)); p.DefaultsHash = SafeFiles.Hash(p.DefaultsBytes);
            }
            else { p.Defaults = new Ini(""); p.Hints.Add(Msg.Key("hint_no_ini_installed")); }
            // Schema tiers: settings_schemas\<name>.launcher.ini, otherwise the INI itself.
            Ini schema = null;
            string schemaPath = String.IsNullOrWhiteSpace(schemaRoot) ? "" : Path.Combine(Path.GetFullPath(schemaRoot), name + ".launcher.ini");
            if (schemaPath.Length > 0 && File.Exists(schemaPath))
            {
                SafeFiles.NoLinks(schemaPath);
                byte[] bytes = SafeFiles.Read(schemaPath, 1024 * 1024); var candidate = new Ini(SafeFiles.Decode(bytes));
                if (candidate.Get("launcher", "editor_type", "").Length == 0)     // keyed editors belong to LocalEditorSpec
                {
                    if (candidate.Get("launcher", "config", "") != p.ConfigName) throw new FormatException(Msg.Key("err_lokales_schema_passt_nicht", schemaPath));
                    p.SchemaPath = schemaPath; p.InputHashes.Add(schemaPath, SafeFiles.Hash(bytes)); schema = candidate;
                    p.Root = Path.GetDirectoryName(schemaPath);                  // language_directory resolves beside the schema
                    p.LenientValues = true;
                }
            }
            p.Id = schema != null ? schema.Get("launcher", "id", "local." + name) : "local." + name;
            p.Name = schema != null ? schema.Get("launcher", "name", name) : name;
            string version; p.Version = LoaderVersions(build).TryGetValue(name, out version) ? version : "installiert";
            if (schema == null) { p.Generic = true; schema = GenericSchema.Build(p); }
            if (p.HasConfig)
            {
                if (p.RefreshUpstream && File.Exists(upstream)) p.Hints.Add(Msg.Key("hint_installed_ini_changed"));
                else if (p.RefreshUpstream) p.Hints.Add(Msg.Key("hint_installed_first_save", name));   // 0.4.54: the routine "original kept" line is gone, the footer button says it
                if (p.Generic) p.Hints.Add(Msg.Key("hint_no_local_schema"));
            }
            p.ApplySchema(schema);
            if (p.LenientValues)
            {
                // Keys the INI has and the local schema does not: not editable here, but named.
                var known = new HashSet<string>(p.Fields.Select(f => f.Id), StringComparer.OrdinalIgnoreCase);
                var missing = p.Defaults.Values.Keys.Where(k => !known.Contains(k)).ToList();
                if (missing.Count > 0) p.Hints.Add(Msg.Key("hint_not_in_schema", String.Join(", ", missing)));
            }
            return p;
        }
    }

    public static class ConfigRules
    {
        public static void Validate(Package package,Ini ini)
        {
            var allowed=new HashSet<string>(StringComparer.OrdinalIgnoreCase);var sections=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach(Field field in package.Fields)
            {
                // A generic schema was derived from the package INI; a local INI may
                // legitimately lack a key the package added later, or carry keys the
                // package never declared. Only a shipped schema is authoritative.
                string value=ini.Get(field.Section,field.Key);
                if(value==null){if(package.Generic)continue;throw new FormatException(Msg.Key("err_fehlt", field.Section, field.Key));}
                field.Normalize(value);allowed.Add(field.Id);sections.Add(field.Section);
            }
            foreach(CollectionSpec collection in package.Collections)
            {
                bool anyPositive=false;
                sections.Add(collection.CountSection);foreach(string target in collection.TargetSections)sections.Add(target);
                var names=CollectionRules.Names(collection,ini);var unique=new HashSet<string>(StringComparer.OrdinalIgnoreCase);allowed.Add(collection.CountId);
                var validator=new Field {Type=collection.Type,Minimum=collection.Minimum,Maximum=collection.Maximum,Label=collection.Id};
                for(int i=0;i<names.Count;i++)
                {
                    string name=names[i];if(!CollectionRules.SafeItem(name)||!unique.Add(name))throw new FormatException(Msg.Key("err_ungueltiger_oder_doppelter_sammlungseintrag", name));
                    allowed.Add(Ini.Id(collection.CountSection,collection.ItemPrefix+i));
                    foreach(string target in collection.TargetSections){string id=Ini.Id(target,name);allowed.Add(id);string raw=ini.Get(target,name,collection.DefaultValue);decimal n=Decimal.Parse(validator.Normalize(raw),CultureInfo.InvariantCulture);if(n>0)anyPositive=true;}
                }
                if(collection.RequirePositiveWhenEnabled&&package.EnabledField.Length>0&&ini.Values[package.EnabledField]=="1"&&!anyPositive)
                    throw new RuleException("error_positive_collection_required",collection.Id);
            }
            if(package.Generic||package.LenientValues)
            {
                foreach(var pair in ini.Values)if(pair.Value.Length>package.MaximumValueLength)throw new FormatException(Msg.Key("err_zu_langer_wert", pair.Key));
                return;
            }
            foreach(string section in ini.Sections)if(!sections.Contains(section))throw new FormatException(Msg.Key("err_unbekannter_abschnitt", section));
            foreach(var pair in ini.Values)if(!allowed.Contains(pair.Key)||pair.Value.Length>package.MaximumValueLength)throw new FormatException(Msg.Key("err_unbekannter_schluessel_oder_zu", pair.Key));
        }
    }

    // The loader's own files. TesmioLoader finds every plugin DLL itself - <loader>\plugins and, in
    // any depth, the Steam Workshop folder - and tesmioloader.ini says which of them it loads:
    // [plugins] <key> = 0|1, where the key is the DLL's file name without ".dll" and a missing key
    // means on. This program lists what the loader finds and writes those switches; it never
    // copies a DLL.
    public static class LoaderFiles
    {
        public static string IniPath(string build) { return SafeFiles.Child(Path.GetFullPath(build), "tesmioloader.ini"); }
        static Ini Read(string build)
        {
            string path = IniPath(build);
            try { return File.Exists(path) ? new Ini(SafeFiles.Text(path)) : new Ini(""); }
            catch (FormatException) { return new Ini(""); }
        }
        // [tesmioloader] plugins = 0 keeps the loader from loading any plugin at all.
        public static bool PluginsAllowed(string build) { return Read(build).Get("tesmioloader", "plugins", "1") != "0"; }
        public static bool Enabled(string build, string key) { return Read(build).Get("plugins", key, "1") != "0"; }
        // The switches of several keys from one read of the file.
        public static Dictionary<string, bool> Switches(string build, IEnumerable<string> keys)
        {
            Ini ini = Read(build); var result = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
            foreach (string key in keys) result[key] = ini.Get("plugins", key, "1") != "0";
            return result;
        }
        // The file with the given switches set, edited in place so that its comments and every
        // other line survive, never written with a BOM.
        public static byte[] Render(string build, IDictionary<string, bool> switches)
        {
            string path = IniPath(build);
            var change = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var pair in switches) change[Ini.Id("plugins", pair.Key)] = pair.Value ? "1" : "0";
            if (File.Exists(path)) return SafeFiles.Utf8.GetBytes(new Ini(SafeFiles.Text(path)).Render(change));
            var text = new StringBuilder("[plugins]\r\n");
            foreach (var pair in switches) text.Append(pair.Key + "=" + (pair.Value ? "1" : "0") + "\r\n");
            return SafeFiles.Utf8.GetBytes(text.ToString());
        }
        // Where the loader looks for Workshop plugins: [tesmioloader] workshop_path, else from its
        // own folder upwards to "steamapps" and down to workshop\content\784150. "" when neither
        // gives a folder.
        public static string WorkshopRoot(string build)
        {
            string declared = (Read(build).Get("tesmioloader", "workshop_path", "") ?? "").Trim().Trim('"');
            return declared.Length > 0 ? declared : PluginScan.WorkshopDefault(Path.GetFullPath(build));
        }
        // Every plugin DLL the loader will consider on its next start, in its load order.
        public static List<FoundPlugin> Plugins(string build, string workshop)
        { return PluginScan.Discover(Path.Combine(Path.GetFullPath(build), "plugins"), workshop ?? ""); }
        // The last search, kept so that "is this plugin there" does not walk the Workshop folder
        // on every question. The catalog renews it with every scan; Forget drops it.
        static string knownBuild = "", knownWorkshop = ""; static List<FoundPlugin> known;
        public static void Remember(string build, string workshop, List<FoundPlugin> found)
        { knownBuild = Path.GetFullPath(build); knownWorkshop = workshop ?? ""; known = found; }
        public static void Forget() { known = null; }
        public static List<FoundPlugin> Known(string build)
        {
            string full = Path.GetFullPath(build);
            if (known == null || !knownBuild.Equals(full, StringComparison.OrdinalIgnoreCase))
            {
                // A search for another loader folder than the remembered one keeps the Workshop
                // folder the window was told to use only when the loader itself names none.
                string workshop = WorkshopRoot(full);
                Remember(full, workshop, Plugins(full, workshop));
            }
            return known;
        }
        // The DLL the loader takes for a key, or null when it finds none. A DLL put into plugins\
        // since the last search is seen at once: a local DLL wins its key anyway.
        public static FoundPlugin Find(string build, string key)
        {
            if (String.IsNullOrWhiteSpace(key)) return null;
            string full = Path.GetFullPath(build), local = Path.Combine(full, "plugins", key + ".dll");
            FoundPlugin hit = Known(full).FirstOrDefault(f => PluginScan.SameKey(f.Key, key));
            if (hit != null && hit.Local && File.Exists(hit.Path)) return hit;
            if (File.Exists(local) && PluginScan.HasPluginExports(local))
                return new FoundPlugin { Path = local, File = key + ".dll", Key = key, Ini = local.Substring(0, local.Length - 4) + ".ini", Local = true };
            return hit != null && File.Exists(hit.Path) ? hit : null;
        }
        public static bool Present(string build, string key) { return Find(build, key) != null; }
        // Whether this tesmioloader.dll lays user_config\<name>.ini over a plugin's INI for plugins
        // that read their settings through the host table. The loader shipped with this program
        // does; without it only plugins that merge the file themselves see personal values.
        public static bool LaysOverlay(string build)
        {
            try
            {
                string dll = SafeFiles.Child(Path.GetFullPath(build), "tesmioloader.dll");
                if (!File.Exists(dll)) return false;
                byte[] data = SafeFiles.Read(dll, 64 * 1024 * 1024), mark = Encoding.Unicode.GetBytes("user_config");
                for (int i = 0; i + mark.Length <= data.Length; i++)
                {
                    int k = 0; while (k < mark.Length && data[i + k] == mark[k]) k++;
                    if (k == mark.Length) return true;
                }
                return false;
            }
            catch (Exception) { return false; }
        }
    }

    // Update detection: the receipt written at the last save remembers the package
    // version and the hashes of its DLL and INI. A package whose DLL or INI no longer
    // matches has been updated (Steam, or the author) since then; saving acknowledges it.
    public sealed class UpdateState
    {
        public bool Deployed, Pending;
        public string PreviousVersion = "", CurrentVersion = "";
        public bool DllChanged, DefaultsChanged;
    }
    public static class UpdateCheck
    {
        public static UpdateState Of(Package package, string build)
        {
            var state = new UpdateState { CurrentVersion = package.Version };
            if (package == null || package.Installed || package.Kind != "plugin") return state;
            try
            {
                string receipt = SafeFiles.Child(Path.GetFullPath(build), "user_config\\.autoload\\" + package.Target + ".receipt.ini");
                if (!File.Exists(receipt)) return state;
                var r = new Ini(SafeFiles.Text(receipt));
                if (r.Get("state", "id", "") != package.Id) return state;
                state.Deployed = true; state.PreviousVersion = r.Get("state", "version", "");
                // The game always loads the package's own DLL, so a changed DLL needs nothing
                // from the player; the mark only says that Steam brought a new one.
                string dllHash = r.Get("state", "dll_hash", "");
                state.DllChanged = dllHash.Length > 0 && dllHash != "absent" && package.Dll != null && dllHash != SafeFiles.Hash(package.Dll);
                state.DefaultsChanged = package.HasConfig && r.Get("state", "defaults_hash", "") != package.DefaultsHash;
                state.Pending = state.DllChanged || state.DefaultsChanged;
            }
            catch (Exception) { }
            return state;
        }
    }

    // One line of a manifest's [dependencies]: `mod.id = >=1.2.0`, or a plugin key
    // (`resources = *`). A plugin that replaces the named one satisfies it as well.
    public sealed class Dependency
    {
        public string Id, Constraint = "";
        public bool Found, VersionOk = true;
        public string Target = "", Kind = "", Version = "", Note = "", Root = "";
        public string Name = "";   // 0.4.27: display name of the matched package, for the notices
    }

    // Version constraints as SML writes them: ">=1.2.0", ">1", "=1.0", "<=2", a
    // bare "1.0" (meaning at least), "*" or nothing (anything). Dotted numbers are
    // compared part by part; a suffix such as "-beta" is ignored.
    public static class VersionRule
    {
        static int[] Parts(string version)
        {
            var m = Regex.Match(version ?? "", @"^\s*v?(\d+(?:\.\d+)*)");
            if (!m.Success) return null;
            return m.Groups[1].Value.Split('.').Select(x => { int n; return Int32.TryParse(x, out n) ? n : 0; }).ToArray();
        }
        static int Compare(int[] a, int[] b)
        {
            for (int i = 0; i < Math.Max(a.Length, b.Length); i++)
            {
                int x = i < a.Length ? a[i] : 0, y = i < b.Length ? b[i] : 0;
                if (x != y) return x < y ? -1 : 1;
            }
            return 0;
        }
        public static bool Satisfied(string constraint, string version)
        {
            string c = (constraint ?? "").Trim();
            if (c.Length == 0 || c == "*" || c == "any") return true;
            var m = Regex.Match(c, @"^(>=|<=|==|=|>|<)?\s*(.+)$");
            string op = m.Groups[1].Value, wanted = m.Groups[2].Value;
            int[] have = Parts(version), need = Parts(wanted);
            if (have == null || need == null) return false;
            int cmp = Compare(have, need);
            switch (op)
            {
                case ">": return cmp > 0;
                case "<": return cmp < 0;
                case "<=": return cmp <= 0;
                case "=": case "==": return cmp == 0;
                default: return cmp >= 0;   // ">=" and a bare version
            }
        }
    }

    // Which game build is installed. Every plugin in this ecosystem carries
    // addresses for exactly one build of SOVIET64.exe, identified by the PE
    // header's TimeDateStamp (docs/07-pitfalls.md). A Steam update changes the
    // stamp, and from then on those plugins must not be injected.
    public static class GameVersion
    {
        public static bool CheckEnabled = true;
        static readonly Dictionary<uint, string> supported = new Dictionary<uint, string> { { 0x6B05A574u, "1.2.0.9" } };
        static readonly Dictionary<uint, string> retired = new Dictionary<uint, string> { { 0x6B0097B2u, "1.2.0.8" }, { 0x6A8D4087u, "1.1.1.13" }, { 0x6A3EB6ADu, "1.1.1.9" }, { 0x69C4098Cu, "1.1.1.7" } };
        // settings_schemas\game_versions.ini: [supported] 6A3EB6AD = 1.1.1.9
        public static void LoadTable(string schemaRoot)
        {
            try
            {
                string path = Path.Combine(schemaRoot ?? "", "game_versions.ini");
                if (!File.Exists(path)) return;
                var ini = new Ini(SafeFiles.Text(path));
                foreach (var pair in ini.Values)
                {
                    if (!pair.Key.StartsWith("supported/")) continue;
                    uint stamp;
                    if (UInt32.TryParse(pair.Key.Substring(10), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out stamp)) supported[stamp] = pair.Value;
                }
            }
            catch (Exception) { }
        }
        public static string ExePath(string build)
        {
            string root = Path.GetFullPath(build);
            try
            {
                string cfg = Path.Combine(root, "tesmioloader.ini");
                if (File.Exists(cfg)) { string exe = new Ini(SafeFiles.Text(cfg)).Get("tesmioloader", "game_exe", ""); if (exe.Length > 0 && File.Exists(exe)) return Path.GetFullPath(exe); }
            }
            catch (Exception) { }
            // The loader lies in the game folder itself, in <game>\tesmioloader or in
            // <game>\tesmioloader\build: the first folder upwards with the game's exe.
            string dir = root;
            for (int depth = 0; depth < 4 && !String.IsNullOrEmpty(dir); depth++)
            {
                string exe = Path.Combine(dir, "SOVIET64.exe"); if (File.Exists(exe)) return exe;
                dir = Path.GetDirectoryName(dir);
            }
            return Path.GetFullPath(Path.Combine(root, "..\\SOVIET64.exe"));
        }
        // 0 when the file is not a PE image.
        public static uint Stamp(string exe)
        {
            try
            {
                using (var file = new FileStream(exe, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
                {
                    var head = new byte[64];
                    if (file.Read(head, 0, 64) != 64 || head[0] != 'M' || head[1] != 'Z') return 0;
                    int pe = BitConverter.ToInt32(head, 60);
                    if (pe < 64 || pe > file.Length - 12) return 0;
                    file.Seek(pe, SeekOrigin.Begin);
                    var sig = new byte[12];
                    if (file.Read(sig, 0, 12) != 12 || BitConverter.ToUInt32(sig, 0) != 0x4550) return 0;
                    return BitConverter.ToUInt32(sig, 8);
                }
            }
            catch (Exception) { return 0; }
        }
        // True only when the game executable is there and its build is not one the
        // plugins were made for. A missing executable is the launcher's problem.
        public static bool Warn(string build, out string text)
        {
            bool ok; text = Describe(build, out ok);
            return !ok && File.Exists(ExePath(build));
        }
        public static string Describe(string build, out bool ok)
        {
            ok = false;
            string exe = ExePath(build);
            if (!File.Exists(exe)) return Msg.Key("game_exe_missing", exe);
            uint stamp = Stamp(exe);
            if (stamp == 0) return Msg.Key("game_exe_unreadable");
            string hex = "0x" + stamp.ToString("X8");
            string name;
            if (supported.TryGetValue(stamp, out name)) { ok = true; return Msg.Key("game_version_supported", name, hex); }
            if (retired.TryGetValue(stamp, out name)) return Msg.Key("game_version_retired", name, hex);
            return Msg.Key("game_version_unknown_stamp", hex);
        }
    }

    public static class RuntimeStatus
    {
        // The package behind a list entry: a DLL in <loader>\plugins, or a package below the
        // Workshop folder.
        public static Package Load(CatalogEntry entry, string build)
        { return entry.Installed ? InstalledPlugins.Load(build, entry.Target, Catalog.SchemaRoot) : Package.Load(entry.Root); }
        // Whether the entry runs on the next game start as far as files can tell: the loader
        // finds its DLL and has it switched on, and the plugin's own switch is not off.
        public static bool ConfiguredActive(CatalogEntry entry, string build)
        {
            if (entry == null || !entry.Supported || entry.Problem.Length > 0) return false;
            if(entry.LocalEditor)return LocalResourceRuntime.Active(entry,build);
            try
            {
                Package package=Load(entry,build);
                if (package.Kind != "plugin") return package.Kind == "content" && ContentSession.IsProvided(build, package.Id);
                string root = Path.GetFullPath(build);
                if (!File.Exists(package.DllPath)) return false;
                if (package.HasConfig && !package.EditorManaged)
                {
                    // An installed plugin reads plugins\<key>.ini, a Workshop plugin the INI beside
                    // its DLL; the personal value of the switch in user_config wins over both.
                    Ini plugin = package.Defaults;
                    if (package.Installed)
                    {
                        string ini = SafeFiles.Child(root, "plugins\\"+package.ConfigName);
                        if (!File.Exists(ini)) return false;
                        plugin = new Ini(SafeFiles.Text(ini)); ConfigRules.Validate(package,plugin);
                    }
                    if (package.EnabledField.Length>0)
                    {
                        string overlayPath = SafeFiles.Child(root, "user_config\\"+package.ConfigName);
                        var overlay = File.Exists(overlayPath) ? new Ini(SafeFiles.Text(overlayPath)) : null;
                        string state; if (overlay == null || !overlay.Values.TryGetValue(package.EnabledField, out state)) plugin.Values.TryGetValue(package.EnabledField, out state);
                        if (GenericSchema.StripComment(state) != "1") return false;
                    }
                }
                return LoaderFiles.PluginsAllowed(root) && LoaderFiles.Enabled(root, package.Target);
            }
            catch (Exception) { return false; }
        }
    }

    // How the game is started. tesmiolauncher --nogui launches straight away with
    // what tesmioloader.ini says; tesmiolauncher_window = 1 in rmm.ini brings the
    // launcher's own window back.
    public static class LauncherOptions
    {
        public static bool ShowWindow;
        public static string Arguments { get { return ShowWindow ? "" : "--nogui"; } }
        // 0.4.92: how many seconds RMM keeps looking after the game start before it closes
        // itself. 0 turns the watch off and closes right away, the way it worked before.
        public static double WatchSeconds = 15;
        // 0.5.9: leave the window standing after the game was started - for everyone who wants
        // to carry on setting things up while the game loads and close RMM by hand.
        public static bool KeepOpen;
    }

    // 0.4.92: the launcher hands the game over and ends, so nobody notices when the game
    // quits again one second later - the signature of a Steam client that refuses a game
    // started outside Steam. Three outcomes: the game shows up and stays (nothing to say),
    // it never shows up (a slow disk looks the same, so also nothing to say), or it shows
    // up and is gone again inside the window - that one is worth a word.
    public sealed class LaunchWatch
    {
        public readonly double Seconds;
        // 0.5.6: a game that quits again is only worth a warning when it quits IMMEDIATELY.
        // Measured twice on the real machine: the Steam failure ran 1.3 s (hooks installed
        // 08:45:53.5, shutdown 08:45:54.8), a player closing it on purpose as fast as he could
        // still took 10.1 s (20:17:52.998 to 20:18:03.082) - the game has to load first. Below
        // the grace it is Steam, above it the player, and the player gets no lecture.
        public const double DefaultGrace = 5.0;
        public readonly double Grace;
        public LaunchWatch(double seconds) : this(seconds, DefaultGrace) { }
        public LaunchWatch(double seconds, double grace) { Seconds = seconds; Grace = grace; }
        double seenAt = -1;
        public bool Finished { get; private set; }
        public bool Died { get; private set; }
        public double Lived { get; private set; }
        public void Tick(bool gameRunning, double elapsedSeconds)
        {
            if (Finished) return;
            if (gameRunning)
            {
                if (seenAt < 0) seenAt = elapsedSeconds;
                if (elapsedSeconds >= Seconds) Finished = true;
                return;
            }
            if (seenAt >= 0)
            {
                Lived = elapsedSeconds - seenAt;
                Died = Lived < Grace;
                Finished = true;
                return;
            }
            if (elapsedSeconds >= Seconds) Finished = true;
        }
    }


    // 0.4.93: the handful of settings that belong to RMM itself and not to a plugin. rmm.ini
    // ships the defaults (that is the file the Workshop package installs); what the options
    // window sets goes into the saved view and wins, the same rule that already applies to
    // folders and language. The window shows the rmm.ini value underneath when both differ.
    public static class AppOptions
    {
        public static string IniLauncherWindow = "0", IniWatchSeconds = "15", IniVersionCheck = "1", IniKeepOpen = "0";
        public static double DefaultWatchSeconds { get { double d; return Double.TryParse(IniWatchSeconds, NumberStyles.Float, CultureInfo.InvariantCulture, out d) && d >= 0 ? d : 15; } }
        public static void Apply(UiState state)
        {
            if (state == null) return;
            if (state.LauncherWindow == "0" || state.LauncherWindow == "1") LauncherOptions.ShowWindow = state.LauncherWindow == "1";
            if (state.VersionCheck == "0" || state.VersionCheck == "1") GameVersion.CheckEnabled = state.VersionCheck == "1";
            if (state.KeepOpen == "0" || state.KeepOpen == "1") LauncherOptions.KeepOpen = state.KeepOpen == "1";
            double seconds;
            if (Double.TryParse(state.WatchSeconds, NumberStyles.Float, CultureInfo.InvariantCulture, out seconds) && seconds >= 0) LauncherOptions.WatchSeconds = seconds;
        }
    }
    // 0.5.20: the two log settings of the loader itself, in [tesmioloader] of
    // tesmioloader.ini. That file belongs to the loader, not to a plugin, so no
    // schema describes it and no entry in the list owns it - it is edited from the
    // options window, in place, through Ini.Render, so the launcher's comments and
    // every other key survive. Both values are read by the loader at game start.
    public static class LoaderLog
    {
        public const int DefaultLevel = 2, DefaultRepeat = 3, MaxRepeat = 50;
        static string Path(string build) { return SafeFiles.Child(build, "tesmioloader.ini"); }
        static int Read(string build, string key, int fallback, int max)
        {
            try
            {
                string file = Path(build);
                if (!File.Exists(file)) return fallback;
                string raw = new Ini(SafeFiles.Text(file)).Get("tesmioloader", key, "");
                int v;
                if (raw == null || !Int32.TryParse(raw.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out v)) return fallback;
                return Math.Max(0, Math.Min(max, v));
            }
            catch (IOException) { return fallback; }
            catch (FormatException) { return fallback; }
        }
        // 0 off, 1 errors, 2 errors and warnings, 3 everything.
        public static int Level(string build) { return Read(build, "log_game", DefaultLevel, 3); }
        // How often a repeating message of the game is written before it is only counted; 0 = always.
        public static int Repeat(string build) { return Read(build, "game_repeat_limit", DefaultRepeat, MaxRepeat); }
        public static bool Available(string build) { return File.Exists(Path(build)); }
        // The file as it would look with these two values. Returns null when nothing changes.
        public static byte[] Render(string build, int level, int repeat)
        {
            string file = Path(build);
            if (!File.Exists(file)) return null;
            if (Level(build) == level && Repeat(build) == repeat) return null;
            var change = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { Ini.Id("tesmioloader", "log_game"), level.ToString(CultureInfo.InvariantCulture) },
                { Ini.Id("tesmioloader", "game_repeat_limit"), repeat.ToString(CultureInfo.InvariantCulture) }
            };
            return SafeFiles.Utf8.GetBytes(new Ini(SafeFiles.Text(file)).Render(change));
        }
        public static void Save(string build, int level, int repeat)
        {
            byte[] text = Render(build, level, repeat);
            if (text == null) return;
            string file = Path(build);
            var writes = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase) { { file, text } };
            var before = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { { file, SafeFiles.HashFile(file) } };
            Transaction.Apply(build, "loader-log", writes, before, () => RuntimeGuard.NoGameRunning(build));
        }
    }
    public static class RuntimeGuard
    {
        // Profiles and restore points need only this part: nothing may be written
        // while the game or the launcher runs.
        public static void NoGameRunning(string build)
        {
            SafeFiles.NoLinks(build);
            foreach (var process in Process.GetProcesses())
                using (process)
                {
                    string name;
                    try { name = process.ProcessName; } catch (InvalidOperationException) { continue; }
                    if (name.StartsWith("SOVIET", StringComparison.OrdinalIgnoreCase) || name.Equals("tesmiolauncher", StringComparison.OrdinalIgnoreCase))
                        throw new IOException(Msg.Key("err_zuerst_spiel_und_tesmiolauncher", name));
                }
        }
        // 0.4.92: the same name rule as the guard above, as a question instead of a refusal -
        // the launch watch asks it every half second.
        public static bool GameRunning()
        {
            foreach (var process in Process.GetProcesses())
                using (process)
                {
                    string name;
                    try { name = process.ProcessName; } catch (InvalidOperationException) { continue; }
                    if (name.StartsWith("SOVIET", StringComparison.OrdinalIgnoreCase)) return true;
                }
            return false;
        }

        // Whether a found dependency really loads on the next game start: a provided content
        // package, or a plugin DLL the loader finds and has switched on. False for unresolved ones.
        public static bool Provisioned(string build, Dependency d)
        {
            if(!d.Found||!d.VersionOk) return false;
            if(d.Kind=="content") return ContentSession.IsProvided(build,d.Id);
            if(d.Target.Length==0) return true;
            return LoaderFiles.Enabled(build,d.Target);
        }
        public static bool Provisioned(Session session, Dependency d) { return Provisioned(session.Build,d); }
        // What must hold before anything is written for a package: no game, a loader, and a
        // loader that loads plugins at all. What a plugin needs from other plugins is a matter
        // of the game start (Startup.Unmet), not of saving its settings.
        public static void Check(Session session)
        {
            string build=session.Build;
            NoGameRunning(build);
            if (!File.Exists(SafeFiles.Child(build, "tesmioloader.dll"))) throw new IOException(Msg.Key("err_benoetigte_lokale_datei_fehlt", "tesmioloader.dll"));
            if (!LoaderFiles.PluginsAllowed(build)) throw new IOException(Msg.Key("err_plugins_sind_im_vorhandenen"));
        }
    }

    public sealed class Session
    {
        public readonly Package Package;
        // LocalDll is the DLL the loader takes: the package's own file below the Workshop folder,
        // or <loader>\plugins\<key>.dll for an installed plugin. It is never written.
        public readonly string Build, LocalDll, LocalIni, UserIni, Receipt, Upstream, LoaderIni;
        public readonly Dictionary<string, string> Overrides;
        public readonly Dictionary<string, string> Before = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        public readonly List<string> Notes = new List<string>();
        // What the last receipt says about this package versus the files now.
        public readonly UpdateState Update;
        // The loader's own switch, [plugins] <key> in tesmioloader.ini - whether TesmioLoader
        // loads the DLL at all. A content package has no DLL and no such switch.
        public readonly bool LoaderSwitchApplies;
        public bool LoaderEnabled { get; private set; }
        // What the single "plugin active" switch controls here: the loader entry, with the INI's
        // enabled field set alongside, or nothing at all.
        public string SwitchMode { get { return LoaderSwitchApplies ? "loader" : "none"; } }
        // Plugins that must not run beside this one, by key: the ones it replaces, and the ones
        // that replace it. The window fills both from the catalog before the first save.
        public readonly List<string> Replaced = new List<string>(), Replacing = new List<string>();
        readonly bool enabledAtLoad;
        // A plugins\<key>.ini left behind for a Workshop plugin by an earlier version of this
        // program (its receipt proves it). The plugin would read it instead of the package's own
        // INI, so the next save removes it; any other local INI is the player's and stays.
        readonly bool ownStaleLocalIni;
        bool restoring;

        // A local INI written from an earlier package revision may lack keys the
        // package added since, or carry keys it dropped. It is read on top of the
        // current defaults, so a package update never blocks the entry; the values
        // it still knows are kept and validated like any other.
        Ini StaleLocal(string path)
        {
            var known = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var pair in new Ini(SafeFiles.Text(path)).Values)
            {
                if (pair.Value.Length == 0) continue;
                // Collection sections are user-grown; every item of theirs travels along.
                string section = pair.Key.Split('/')[0];
                bool dynamic = Package.Collections.Any(c => string.Equals(section, c.CountSection, StringComparison.OrdinalIgnoreCase) || c.TargetSections.Any(t => string.Equals(t, section, StringComparison.OrdinalIgnoreCase)));
                if (dynamic || Package.Defaults.Values.ContainsKey(pair.Key)) known[pair.Key] = pair.Value;
            }
            var merged = new Ini(Package.Defaults.Render(known)); ConfigRules.Validate(Package, merged); return merged;
        }
        public Session(Package package, string build)
        {
            Package = package; Build = Path.GetFullPath(build);
            LoaderIni = LoaderFiles.IniPath(Build);
            LocalDll = Package.Installed ? SafeFiles.Child(Build, "plugins\\"+Package.Target+".dll") : Package.DllPath;
            LocalIni = SafeFiles.Child(Build, "plugins\\"+Package.ConfigName);
            UserIni = SafeFiles.Child(Build, "user_config\\"+Package.ConfigName); Receipt = SafeFiles.Child(Build, "user_config\\.autoload\\"+Package.Target+".receipt.ini");
            Upstream = Package.Installed ? InstalledPlugins.UpstreamPath(Build, Package.Target) : null;
            Update = UpdateCheck.Of(Package, Build);
            LoaderSwitchApplies = Package.Kind == "plugin";
            LoaderEnabled = !LoaderSwitchApplies || ReadLoaderSwitch(); enabledAtLoad = LoaderEnabled;
            if (!Package.Installed)
            {
                string packagePrefix = Package.Root.TrimEnd('\\', '/') + "\\", buildPrefix = Build.TrimEnd('\\', '/') + "\\";
                if (packagePrefix.StartsWith(buildPrefix, StringComparison.OrdinalIgnoreCase) || buildPrefix.StartsWith(packagePrefix, StringComparison.OrdinalIgnoreCase))
                    throw new IOException(Msg.Key("err_paket_und_loader_ordner"));
            }
            foreach (string path in new[] { LocalIni, UserIni, Receipt, LoaderIni }) Before[path] = SafeFiles.HashFile(path);
            if (Upstream != null) Before[Upstream] = SafeFiles.HashFile(Upstream);
            Overrides = File.Exists(UserIni) ? new Ini(SafeFiles.Text(UserIni)).Values : new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach(string id in Overrides.Keys.Where(id=>Package.IsDefault(id,Overrides[id])).ToList())Overrides.Remove(id);
            if (Package.Installed)
            {
                // The protected base decides what "original" means; InstalledPlugins
                // already chose it and told the package whether the copy must be refreshed.
                foreach (string hint in Package.Hints) if (Msg.KeyOf(hint).StartsWith("hint_installed_")) Notes.Add(hint);
            }
            else if (!Package.EditorManaged && File.Exists(LocalIni))
            {
                // A Workshop plugin with an INI of the same name in <loader>\plugins. Whoever
                // wrote it, the values it changed are worth keeping as personal values.
                Ini receipt = null;
                try { if (File.Exists(Receipt)) receipt = new Ini(SafeFiles.Text(Receipt)); } catch (FormatException) { receipt = null; }
                bool ours = receipt != null && receipt.Get("state", "id", "") == Package.Id && receipt.Get("state", "ini_hash", "") == Before[LocalIni];
                // For a package with an effective INI that file is no leftover: it is what the
                // last save wrote for the plugin to read.
                ownStaleLocalIni = ours && !Package.EffectiveIni;
                if (!(Package.EffectiveIni && ours) && (ownStaleLocalIni || !File.Exists(UserIni)))
                {
                    try
                    {
                        var local = StaleLocal(LocalIni);
                        foreach (var pair in local.Values)
                            if (!Overrides.ContainsKey(pair.Key) && !Package.IsDefault(pair.Key,pair.Value)) Overrides[pair.Key] = pair.Value;
                        if (!File.Exists(UserIni)) Notes.Add(Msg.Key("note_first_import"));
                    }
                    catch (FormatException) { }
                }
                if (!Package.EffectiveIni) Notes.Add(Msg.Key(ownStaleLocalIni ? "note_local_ini_removed" : "note_local_ini_shadows", "plugins\\" + Package.ConfigName));
            }
            Notes.Add(Msg.Key("note_originals_safe"));
            if (Update.Pending) Notes.Add(Msg.Key("note_update_pending", Update.PreviousVersion.Length > 0 ? Update.PreviousVersion : "?", Update.CurrentVersion, (Update.DllChanged ? "DLL" : "") + (Update.DllChanged && Update.DefaultsChanged ? ", " : "") + (Update.DefaultsChanged ? "INI" : "")));
            if (!Package.Installed && Package.HasConfig && !Package.EditorManaged) Notes.Add(Msg.Key(Package.EffectiveIni ? "note_effective" : "note_overlay", Package.ConfigName));
            if (!Package.HasConfig) Notes.Add(Msg.Key("note_no_ini"));
            if (Package.Generic && Package.HasConfig) Notes.Add(Msg.Key("note_no_schema"));
            Effective();
        }
        public string Effective()
        {
            string text = Package.Defaults.Render(Overrides); ConfigRules.Validate(Package,new Ini(text)); return text;
        }
        bool ReadLoaderSwitch() { return LoaderFiles.Enabled(Build, Package.Target); }
        public void SetLoaderEnabled(bool enabled) { if (LoaderSwitchApplies) LoaderEnabled = enabled; }
        // The plugins this save switches off because this one is on: always the ones it replaces,
        // and the ones replacing it when the player has just switched it on. Two plugins doing the
        // same job never load together; without this the brake inside the replacement would idle it.
        public List<string> SwitchesOff()
        {
            var off = new List<string>();
            if (!LoaderSwitchApplies || !LoaderEnabled) return off;
            var candidates = Replaced.Concat(enabledAtLoad ? Enumerable.Empty<string>() : Replacing).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            if (candidates.Count == 0) return off;
            var switches = LoaderFiles.Switches(Build, candidates);
            foreach (string key in candidates) if (switches[key] && !key.Equals(Package.Target, StringComparison.OrdinalIgnoreCase)) off.Add(key);
            return off;
        }
        public bool LoaderChanged { get { return LoaderSwitchApplies && (LoaderEnabled != ReadLoaderSwitch() || SwitchesOff().Count > 0); } }
        // tesmioloader.ini is the launcher's file: edited in place so its comments
        // and the other plugins' switches survive, never written with a BOM.
        byte[] RenderLoaderIni()
        {
            var switches = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase) { { Package.Target, LoaderEnabled } };
            foreach (string key in SwitchesOff()) switches[key] = false;
            return LoaderFiles.Render(Build, switches);
        }
        public void Set(Field field, string value)
        {
            if (field.Type == "readonly") return;
            Overrides[field.Id] = field.Normalize(value);
        }
        public void ResetVisible()
        {
            foreach (Field field in Package.Fields.Where(f => f.Type != "readonly")) Overrides.Remove(field.Id);
        }
        public void AssertUnchanged()
        {
            Package.AssertUnchanged();
            foreach (var pair in Before) { SafeFiles.NoLinks(pair.Key); if (SafeFiles.HashFile(pair.Key) != pair.Value) throw new IOException(Msg.Key("err_datei_inzwischen_geaendert_neu", pair.Key)); }
        }
        // The loader's plugin switches are a shared file: this program writes it when another
        // entry is saved. Its hash is taken afresh, so the next commit of this session does not
        // mistake that write for a foreign change; every other file stays guarded.
        public void RehashShared() { if (Before.ContainsKey(LoaderIni)) Before[LoaderIni] = SafeFiles.HashFile(LoaderIni); }
        // deploy = false writes the personal values and the switch only; true also brings the
        // files in the loader folder into line (the effective INI of an installed plugin, the
        // receipt).
        public string Commit(bool deploy, Action guard)
        {
            string mutexName = "Local\\TesmioAutoload_" + SafeFiles.Hash(SafeFiles.Utf8.GetBytes(Build.ToUpperInvariant()));
            using (var mutex = new Mutex(false, mutexName))
            {
                bool held = false;
                try
                {
                    try { held = mutex.WaitOne(0); } catch (AbandonedMutexException) { held = true; }
                    if (!held) throw new IOException(Msg.Key("err_ein_anderes_autoload_fenster"));
                    guard(); AssertUnchanged();
                    byte[] effective = SafeFiles.Utf8.GetBytes(Effective());
                    var writes = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase);
                    // No personal value and no file yet: nothing to write. An empty overlay would
                    // only make the loader report personal values that do not exist.
                    if (Package.HasConfig && !Package.EditorManaged && (Overrides.Count > 0 || File.Exists(UserIni)))
                        writes[UserIni] = SafeFiles.Utf8.GetBytes(File.Exists(UserIni)
                            ? new Ini(SafeFiles.Text(UserIni)).RewriteValues(Overrides) : Ini.Sparse(Overrides));
                    if (LoaderChanged) writes[LoaderIni] = RenderLoaderIni();
                    if (deploy)
                    {
                        string iniHash = "absent";
                        if (Package.Installed)
                        {
                            // An installed plugin reads plugins\<key>.ini: it gets the effective INI
                            // (original plus personal values), and the original is kept once, and
                            // again whenever it changed outside. Restoring writes it back verbatim.
                            // The DLL is the player's own installation and is never touched.
                            iniHash = Before[LocalIni];
                            if (Package.HasConfig && !Package.EditorManaged)
                            {
                                byte[] local = restoring ? Package.DefaultsBytes : effective;
                                writes[LocalIni] = local; iniHash = SafeFiles.Hash(local);
                            }
                            if (Package.HasConfig && (Package.RefreshUpstream || !File.Exists(Upstream))) writes[Upstream] = Package.DefaultsBytes;
                        }
                        else if (ownStaleLocalIni) writes[LocalIni] = null;
                        else if (Package.EffectiveIni && Package.HasConfig && !Package.EditorManaged)
                        {
                            // The plugin reads <loader>\plugins\<key>.ini before the INI beside its
                            // DLL and knows nothing of user_config: with personal values it gets the
                            // finished INI there; without any, the file goes away again, so the
                            // plugin reads the author's INI and every update of it arrives.
                            if (Overrides.Count > 0) { writes[LocalIni] = effective; iniHash = SafeFiles.Hash(effective); }
                            else if (File.Exists(LocalIni)) writes[LocalIni] = null;
                        }
                        writes[Receipt] = SafeFiles.Utf8.GetBytes("[state]\r\nid = " + Package.Id + "\r\nversion = " + Package.Version + "\r\nmode = " + (Package.Installed ? "installed" : "workshop") + "\r\ndll_hash = " + SafeFiles.Hash(Package.Dll) + "\r\nini_hash = " + iniHash + "\r\ndefaults_hash = "+Package.DefaultsHash+"\r\n");
                    }
                    string backup = Transaction.Apply(Build, Package.Id, writes, Before, guard);
                    foreach (string path in Before.Keys.ToList()) Before[path] = SafeFiles.HashFile(path);
                    if (deploy && Package.Installed) Package.RefreshUpstream = false;
                    return backup;
                }
                finally { if (held) mutex.ReleaseMutex(); }
            }
        }
        // Installed plugins only: the protected original goes back into plugins\
        // byte for byte and every personal value is dropped. Transactional like Commit.
        public string RestoreOriginal(Action guard)
        {
            if (!Package.Installed || !Package.HasConfig) throw new InvalidOperationException(Msg.Key("err_nur_ein_installiertes_plugin"));
            Overrides.Clear(); restoring = true;
            try { return Commit(true, guard); }
            finally { restoring = false; }
        }
    }

    public static class Transaction
    {
        // Keeps one complete rolling restore point per plugin. A new snapshot is
        // promoted only after all writes have passed verification; the former one
        // remains available throughout the transaction.
        public static string Apply(string build, string pluginId, Dictionary<string, byte[]> writes, Dictionary<string, string> before, Action guard)
        {
            string state = SafeFiles.Child(build, "user_config\\.autoload");
            Directory.CreateDirectory(state);
            string pending = SafeFiles.Child(state, "pending.txt");
            if (File.Exists(pending)) throw new IOException(Msg.Key("err_unvollstaendige_bereitstellung_erkannt_erst", SafeFiles.Text(pending)));
            // A null value deletes the file (only ever a copy this program made itself).
            Func<byte[], string> hashOf = bytes => bytes == null ? "absent" : SafeFiles.Hash(bytes);
            var changed = writes.Where(p => hashOf(p.Value) != before[p.Key]).ToList();
            if (changed.Count == 0) return "Keine Datei musste geaendert werden.";
            string backupRoot = SafeFiles.Child(state,"backups"); Directory.CreateDirectory(backupRoot);
            string group = SafeFiles.Child(backupRoot,PluginFolder(pluginId)); Directory.CreateDirectory(group);
            string backup = SafeFiles.Child(group,"new_"+Guid.NewGuid().ToString("N").Substring(0,8));
            Directory.CreateDirectory(backup);
            string staging=SafeFiles.Child(backup,"staged"); Directory.CreateDirectory(staging);
            var installed = new List<string>(); var staged = new Dictionary<string, string>(); var old = new Dictionary<string, string>();
            var map = new StringBuilder(); bool markerCreated = false;
            try
            {
                int snapshotIndex=0;
                foreach(var pair in before.OrderBy(p=>p.Key,StringComparer.OrdinalIgnoreCase))
                {
                    SafeFiles.NoLinks(pair.Key);
                    if (SafeFiles.HashFile(pair.Key) != before[pair.Key]) throw new IOException(Msg.Key("err_datei_wurde_waehrend_der", pair.Key));
                    string previous=Path.Combine(backup,snapshotIndex+".old");
                    if(File.Exists(pair.Key)) {File.Copy(pair.Key,previous);if(SafeFiles.HashFile(previous)!=before[pair.Key])throw new IOException(Msg.Key("err_sicherung_stimmt_nicht_ueberein"));old.Add(pair.Key,previous);}
                    map.AppendLine(snapshotIndex+": "+pair.Key+" (vorher "+before[pair.Key]+")"); snapshotIndex++;
                }
                foreach (var pair in changed)
                {
                    if (pair.Value == null) continue;
                    string temp=Path.Combine(staging,staged.Count+".new"); File.WriteAllBytes(temp,pair.Value); staged.Add(pair.Key,temp);
                }
                File.WriteAllText(Path.Combine(backup, "files.txt"), map.ToString(), SafeFiles.Utf8);
                File.WriteAllText(Path.Combine(backup,"plugin.id.txt"),pluginId,SafeFiles.Utf8);
                using (var file = new FileStream(pending, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                { byte[] b = SafeFiles.Utf8.GetBytes(backup); file.Write(b, 0, b.Length); file.Flush(true); }
                markerCreated = true;
                foreach (var pair in changed)
                {
                    guard(); SafeFiles.NoLinks(pair.Key);
                    if (SafeFiles.HashFile(pair.Key) != before[pair.Key]) throw new IOException(Msg.Key("err_zieldatei_wurde_zwischenzeitlich_geaendert", pair.Key));
                    if (pair.Value == null) { File.Delete(pair.Key); installed.Add(pair.Key); }
                    else { Directory.CreateDirectory(Path.GetDirectoryName(pair.Key)); Replace(staged[pair.Key], pair.Key); installed.Add(pair.Key); }
                    if (SafeFiles.HashFile(pair.Key) != hashOf(pair.Value)) throw new IOException(Msg.Key("err_schreibpruefung_fehlgeschlagen", pair.Key));
                }
                File.Delete(pending); markerCreated = false;
                try { DeleteTree(group,staging); } catch { /* Staged copies are harmless inside the verified backup. */ }
                string promoted;
                try { promoted=Promote(group,backup); }
                catch { return backup+" (rollierendes Aufraeumen beim naechsten Speichern wiederholen)"; }
                try { PruneGroup(group,promoted); PruneLegacy(backupRoot,group,new HashSet<string>(before.Keys,StringComparer.OrdinalIgnoreCase)); }
                catch { return promoted+" (alte Sicherungen konnten noch nicht vollstaendig entfernt werden)"; }
                return promoted;
            }
            catch (Exception failure)
            {
                bool restored = true;
                foreach (string path in installed.AsEnumerable().Reverse())
                {
                    try
                    {
                        SafeFiles.NoLinks(path);
                        if (SafeFiles.HashFile(path) != hashOf(writes[path])) { restored = false; continue; }
                        if (old.ContainsKey(path)) { string restore = Path.Combine(backup, Guid.NewGuid().ToString("N") + ".restore"); File.Copy(old[path], restore); Replace(restore, path); }
                        else File.Delete(path); // only a newly created file belonging to this transaction
                    }
                    catch { restored = false; }
                }
                if (restored && markerCreated) File.Delete(pending);
                if(restored) try {DeleteTree(group,backup);} catch { }
                if (!restored) throw new IOException(Msg.Key("err_bereitstellung_nicht_vollstaendig_zurueckgesetzt", backup), failure);
                throw;
            }
        }
        static string PluginFolder(string id)
        {
            if(Regex.IsMatch(id??"","^[A-Za-z0-9._-]{1,80}$")) return id;
            return "plugin_"+SafeFiles.Hash(SafeFiles.Utf8.GetBytes(id??"")).Substring(0,16);
        }
        static string Promote(string group,string candidate)
        {
            string previous=SafeFiles.Child(group,"previous"),retired=SafeFiles.Child(group,"old_"+Guid.NewGuid().ToString("N").Substring(0,8)); bool moved=false;
            if(Directory.Exists(previous)){SafeFiles.NoLinks(previous);Directory.Move(previous,retired);moved=true;}
            try {Directory.Move(candidate,previous);}
            catch {if(moved&&!Directory.Exists(previous)&&Directory.Exists(retired))Directory.Move(retired,previous);throw;}
            if(moved&&Directory.Exists(retired)) try {DeleteTree(group,retired);} catch { }
            return previous;
        }
        static void PruneLegacy(string backupRoot,string group,HashSet<string> managed)
        {
            foreach(string directory in Directory.GetDirectories(backupRoot))
            {
                if(String.Equals(Path.GetFullPath(directory),Path.GetFullPath(group),StringComparison.OrdinalIgnoreCase))continue;
                string map=Path.Combine(directory,"files.txt");if(!File.Exists(map))continue;
                var paths=new List<string>();bool valid=true;
                foreach(string line in File.ReadAllLines(map,SafeFiles.Utf8))
                {
                    var match=Regex.Match(line,"^\\d+: (.*) \\(vorher (?:[A-Fa-f0-9]{64}|absent)\\)$");
                    if(!match.Success){valid=false;break;}paths.Add(match.Groups[1].Value);
                }
                if(valid&&paths.Count>0&&paths.All(managed.Contains))DeleteTree(backupRoot,directory);
            }
        }
        static void PruneGroup(string group,string previous)
        {
            foreach(string directory in Directory.GetDirectories(group))
                if(!String.Equals(Path.GetFullPath(directory),Path.GetFullPath(previous),StringComparison.OrdinalIgnoreCase))DeleteTree(group,directory);
        }
        static void DeleteTree(string allowedRoot,string target)
        {
            string root=Path.GetFullPath(allowedRoot).TrimEnd('\\','/')+Path.DirectorySeparatorChar,full=Path.GetFullPath(target);
            if(!full.StartsWith(root,StringComparison.OrdinalIgnoreCase))throw new IOException(Msg.Key("err_sicherungsordner_liegt_ausserhalb_des", full));
            if(!Directory.Exists(full))return;
            SafeFiles.NoLinks(full);
            foreach(string directory in Directory.GetDirectories(full,"*",SearchOption.AllDirectories))SafeFiles.NoLinks(directory);
            foreach(string file in Directory.GetFiles(full,"*",SearchOption.AllDirectories))SafeFiles.NoLinks(file);
            Directory.Delete(full,true);
        }
        private static void Replace(string source, string destination)
        {
            if (File.Exists(destination)) File.Replace(source, destination, null); else File.Move(source, destination);
        }
    }

    // Steam's own note of who is logged in on this machine. A game started
    // outside Steam - which is what the TesmioLauncher does - reads exactly this
    // to attach to the client, and aborts with its own Steam error when no user
    // is in it. The usual way into that state is uploading a Workshop item:
    // SteamCMD logs in with the same account, Steam replaces the client's
    // session and deliberately does not reconnect it.
    public static class SteamSession
    {
        public enum State { Unknown, LoggedOut, Ready }
        const string Key = @"HKEY_CURRENT_USER\Software\Valve\Steam\ActiveProcess";
        public static State Check() { string reason; return Check(out reason); }
        // 0.4.91: the raw record behind Check(), for the --steam-check line. Diagnosis only,
        // never a decision - it is what a human would read out of the registry.
        public static string Evidence()
        {
            try
            {
                object user = Registry.GetValue(Key, "ActiveUser", null), pid = Registry.GetValue(Key, "pid", null);
                string clients;
                try { Process[] running = Process.GetProcessesByName("steam"); clients = running.Length + " steam process(es) running"; foreach (Process p in running) p.Dispose(); }
                catch (Exception) { clients = "process list unavailable"; }
                return "ActiveUser=" + (user ?? "<none>") + ", pid=" + (pid ?? "<none>") + ", " + clients;
            }
            catch (Exception e) { return e.GetType().Name; }
        }
        // 0.4.91: the check says what it found. "reason" is a language key for the warning and the
        // journal; it stays empty for Ready and Unknown. A lookup that simply fails is Unknown, never
        // LoggedOut - the old code turned every exception of the process list into "Steam is logged
        // out" and warned the user while his client was running and logged on.
        public static State Check(out string reason)
        {
            reason = "";
            try
            {
                object user = Registry.GetValue(Key, "ActiveUser", null);
                object pid = Registry.GetValue(Key, "pid", null);
                // No values at all: another Steam build, another platform, or no
                // Steam. Never block on a guess.
                if (!(user is int) || !(pid is int)) return State.Unknown;
                if ((int)user == 0) { reason = "steam_reason_user"; return State.LoggedOut; }
                if ((int)pid == 0) { reason = "steam_reason_pid"; return State.LoggedOut; }
                // Steam records a logged-in user and a client process. Only a client that is
                // really gone counts as logged out; a recorded id that no longer matches while
                // some client runs is Steam's own bookkeeping, not a login state.
                Process[] clients;
                try { clients = Process.GetProcessesByName("steam"); }
                catch (Exception) { return State.Unknown; }
                try { if (clients.Length == 0) { reason = "steam_reason_gone"; return State.LoggedOut; } }
                finally { foreach (Process client in clients) client.Dispose(); }
                return State.Ready;
            }
            catch (Exception) { return State.Unknown; }
        }
    }

    public static class Discovery
    {
        public static List<string> SteamLibraries()
        {
            var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            string steam = Registry.GetValue(@"HKEY_CURRENT_USER\Software\Valve\Steam", "SteamPath", null) as string;
            if (!String.IsNullOrEmpty(steam) && Directory.Exists(steam))
            {
                result.Add(Path.GetFullPath(steam)); string vdf = Path.Combine(steam, @"steamapps\libraryfolders.vdf");
                if (File.Exists(vdf)) foreach (Match match in Regex.Matches(SafeFiles.Text(vdf), "\"path\"\\s*\"([^\"]+)\""))
                { string p = match.Groups[1].Value.Replace(@"\\", @"\"); if (Directory.Exists(p)) result.Add(Path.GetFullPath(p)); }
            }
            return result.ToList();
        }
        // The folder tesmioloader.dll lies in: beside this program, else in a Steam library - the
        // game folder's "tesmioloader", its "build" below that (older installations), or the game
        // folder itself. null when no loader is found.
        public static readonly string[] LoaderPlaces = { @"steamapps\common\SovietRepublic\tesmioloader", @"steamapps\common\SovietRepublic\tesmioloader\build", @"steamapps\common\SovietRepublic" };
        public static bool HasLoader(string folder) { return !String.IsNullOrEmpty(folder) && File.Exists(Path.Combine(folder, "tesmioloader.dll")); }
        public static string LoaderFolder(string exeDir, IEnumerable<string> libraries)
        {
            if (HasLoader(exeDir)) return Path.GetFullPath(exeDir);
            foreach (string library in libraries)
                foreach (string place in LoaderPlaces)
                { string candidate = Path.Combine(library, place); if (HasLoader(candidate)) return Path.GetFullPath(candidate); }
            return null;
        }
    }
}
