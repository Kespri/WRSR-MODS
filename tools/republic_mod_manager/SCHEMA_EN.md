# Republic Mod Manager – Package and schema reference

**English** | [Deutsch](SCHEMA_DE.md)

This is the reference for plugin authors: what a Workshop package needs so that Republic Mod Manager (RMM) lists it, and how a settings page is described. Neither the manifest nor a schema contains executable commands; everything is plain INI text.

---

## Contents

1. [The manifest (`rmm.mod.ini`)](#1-the-manifest-rmmmodini)
2. [Settings page without a schema](#2-settings-page-without-a-schema)
3. [Presentation schema (`<name>.launcher.ini`)](#3-presentation-schema)
4. [Collections](#4-collections)
5. [Local schemas (`settings_schemas`)](#5-local-schemas-settings_schemas)
6. [Editor schemas: lists and sections](#6-editor-schemas)
7. [Reference checks](#7-reference-checks)
8. [Languages](#8-languages)
9. [Icons and path safety](#9-icons-and-path-safety)
10. [Receipts and restore points](#10-receipts-and-restore-points)
11. [Content packages (`[content]`)](#11-content-packages-content)

---

## 1. The manifest (`rmm.mod.ini`)

A package is a folder in the game's Steam Workshop folder. The TesmioLoader finds every plugin DLL in it by itself, at any depth; it needs no manifest. The manifest is for RMM: it gives the plugin its name and version, names its dependencies and says which plugin it replaces.

Recommended layout:

```
<package folder>\
├── rmm.mod.ini                         the manifest
├── plugins\
│   ├── my_plugin.dll                   the plugin DLL
│   ├── my_plugin.ini                   its default INI: same name, same folder
│   └── my_plugin\                      the DLL's assets (textures, texts), if needed
├── config\
│   ├── my_plugin.launcher.ini          the settings page (chapters 3 and 6), optional
│   └── languages\de.ini, en.ini        its texts
└── README_DE.md, README_EN.md
```

The smallest valid manifest:

```ini
[mod]
id = example.my_plugin
name = My Plugin
version = 1.0.0
```

From it RMM derives: the target `my_plugin` (the file name of the plugin DLL in the package), the default INI `my_plugin.ini` next to the DLL (if present), the personal values `user_config\my_plugin.ini` in the loader folder, and the schema `config\my_plugin.launcher.ini` (if present, otherwise one next to `rmm.exe`, otherwise generated from the INI).

All keys:

```ini
[mod]
id = example.my_plugin                  ; required, unique among all packages
name = My Plugin                        ; required
version = 1.0.0
plugin = my_plugin                      ; DLL name without .dll; needed when the package holds several plugin DLLs
replaces = old_plugin                   ; DLL name of the plugin this one replaces
tesmio_api_min = 4
tesmio_api_max = 4

[configuration]
launcher_schema = config\my_plugin.launcher.ini   ; only needed when the schema has another name or place
effective_ini = 1                       ; 1: the plugin opens its INI itself and does not see user_config (see below)

[dependencies]
other.plugin = >=1.2.0
```

- **What counts as a plugin:** a 64-bit DLL with the exports `TsmPluginApiVersion` and `TsmPluginInit`, exactly as for the loader. Its file name without `.dll` is the plugin's name: under it the plugin stands in `tesmioloader.ini`, and its INI and its personal values are named like it.
- **Whose manifest it is:** from the DLL upwards RMM looks for the nearest folder with a manifest, at most up to the package folder. When a package holds several plugin DLLs, each gets an entry of its own; the manifest belongs to the DLL that `plugin` names (without `plugin`, to the one called like the last part of the `id`). The others are listed without a manifest.
- **Without a manifest** a DLL is listed all the same: with the id `local.<name>`, its file name as the name and no version.
- `replaces = old_plugin`: your plugin replaces another one. RMM never lets the two run together: when the player switches one on, RMM switches the other one off on saving; when both are on, the replacement wins at a start through RMM. The key does not replace care inside the plugin – without RMM the loader loads both, so your plugin should detect the original itself and then stay idle.
- `tesmio_api_min` / `tesmio_api_max`: if given, API 4 must be in range.
- `[dependencies]`: `<id> = <condition>` with `>=`, `>`, `=`, `<=`, `<`, a bare version (minimum) or `*`. The id is the `id` of a package or the DLL name of a plugin. It is also met by a DLL called like the last part of the id (version unchecked) and by a plugin that replaces the one asked for. An unmet dependency does not block saving; before the game starts RMM offers to switch the plugin concerned off.
- `[mod] enabled = 0` rejects the package; duplicate `id`s reject both packages.
- `[content]` without a plugin DLL is a pure content package (chapter 11).
- **Older manifests:** when there is no `rmm.mod.ini`, RMM reads a `soviet.mod.ini`. From it `[hooks] dll = <path>` (exactly one line; it names the DLL) and `[autoload] requires_local = a|b` (counts as a dependency) apply as well. Every other key of older manifests is passed over.

**The player's values.** RMM never writes into the package folder. Personal values are in `user_config\<name>.ini` in the loader folder and hold only what differs from the original. How they reach your plugin depends on how it reads its INI:

- **Through the loader's host table** (`configInt`, `configString` with `plugins\<name>.ini`): the `tesmioloader.dll` from the RMM package lays `user_config\<name>.ini` over it by itself. Nothing for you to do.
- **The plugin opens the file itself:** set `effective_ini = 1`. On saving RMM then also writes the finished INI (original plus personal values) to `<loader>\plugins\<name>.ini`; when the player has no value of their own, that file does not exist. For this your plugin has to read `<loader>\plugins\<name>.ini` first and the INI next to its DLL only when there is none. The loader folder is `baseDir` of the host table; `pluginDir` is the folder of the DLL, which for a Workshop package is the package itself.
- **List editors** (chapter 6) always write the effective file to `<loader>\plugins\<name>.ini`; the same read rule applies to the plugin.

The `tesmioloader.dll` from the RMM package follows the same order: a plugin asking the host table for `plugins\<name>.ini` gets the file in the loader folder when it exists, otherwise the one next to its DLL.

---

## 2. Settings page without a schema

Without a schema RMM builds one from the default INI:

- every INI section becomes a card on the "General" tab;
- every key becomes a field, its name is the label;
- the comment block directly above the key and a comment behind the value become the description; line breaks are kept;
- the type follows the value: `0`/`1` becomes `boolean`, unless the key name names an amount (`count`, `days`, `frequency`, `size`, `limit`, `level`, `scale`, `mode` and similar) or the comment names other whole numbers or a range (`2-12`, `1..6`, "up to 400"); version strings such as `v1.6` do not count. Names such as `enabled`, `debug`, `use_*`, `*_enabled` or a comment naming both states force `boolean`. Other whole numbers become `integer`, decimals `decimal`, everything else `text`;
- a key `enabled` of type `boolean` becomes the activation field;
- the comment block at the start of the file becomes the package description. Rules made of `=`, `-`, `*` and similar characters are passed over, and so is a title box between two such rules; the description is the first paragraph after it.

In this mode a local INI may contain keys the package INI does not know; the strict key check applies only with a schema. A comment behind the value, such as `130 ; (stock 121)`, stays in the file until the value changes.

---

## 3. Presentation schema

`config\<name>.launcher.ini` describes tabs, cards, fields and texts. Every effective key of the INI must be described as a field or a collection element.

### 3.1 `[launcher]`

```ini
[launcher]
layout_version = 1
visible = 1
id = example.my_plugin
name = My Plugin
config = my_plugin.ini
description = Fallback text
description_key = plugin.description
language_directory = config\languages
icon = builtin:gear
enabled_field = general/enabled        ; optional; boolean field for the activation switch
default_tab = general
maximum_value_length = 63              ; default 4096
notice = ...                           ; package-wide box in the Notes card of the first tab
notice_key = plugin.notice
notice_style = warning                 ; warning (yellow) or info (blue)
info = ...                             ; second, always blue box below the notice
info_key = plugin.info
```

`\n` in any description, notice or help text is a line break. A path such as `plugins\needs.ini` in such a text is therefore torn apart; write `/` there.

### 3.2 Tabs and groups

```ini
[tab:general]
label = General
label_key = tab.general
order = 10

[group:general]
tab = general
label = General settings
label_key = group.general
description = ...
description_key = group.general.description
layout = fields                        ; or matrix
order = 10
```

There is no fixed number of tabs. Fields without a group land in the shared area `settings`, shown as "General". Every explicit tab and group reference must exist. A group without visible fields and without texts is not drawn.

A group with `layout = matrix` arranges its fields by `row`, `column`, `row_label`, `column_label`, `unit` and `icon`; the `_key` variants translate. Duplicate cells are invalid.

### 3.3 Fields

```ini
[field:limit]
section = general
key = limit
label = Limit
label_key = field.limit
description = What it changes in the game.
description_key = field.limit.description
type = integer
minimum = 0
maximum = 10000
step = 1
group = general
order = 20
```

Types:

| Type | Value |
|---|---|
| `boolean` | `0` or `1`, shown as a switch |
| `integer` | whole number within `minimum`/`maximum`; `step` sets the plus/minus step (default 1) |
| `decimal` | finite decimal within the limits; `step` default 0.1 |
| `choice` | exactly one of `choices = a|b|c` |
| `text` | free text |
| `readonly` | shown, not edited |

Every `[field:]` binds exactly one existing value of the default INI. The activation field is not shown in a card; it belongs to the "Plugin active" switch in the header.

### 3.4 Buttons that open package files

```ini
[links]
tab = general
label_key = plugin.links
root = ..                              ; relative to the schema folder

[link:readme_de]
file = README_DE.md                    ; .md, .txt, .html or .pdf, no ..
label_key = plugin.link.readme
language = de                          ; optional: only in this UI language
order = 10
```

### 3.5 Action row

A section `[action:<id>]` describes a row with a button that runs a command of RMM (`command = …`). At the moment RMM knows no such command, so a schema with an `[action:…]` section is refused.

### 3.6 Folder list

```ini
[folder_list:packs]
group = packs
paths = package:plugins\localization | build:plugins\localization   ; every subfolder becomes a row
description_prefix = loc.pack          ; short text per folder from the language file: <prefix>.<folder>
note = ...
note_key = loc.packs.note
order = 10
```

Each row names the folder, its short text and where it lies: "in the package" (`package:` path), "local" (`build:` / `vfs:` path) or "local, overlays the package" (both). Path prefixes: `package:` = `[links] root`, `build:` = loader folder (where `tesmioloader.dll` lies), `vfs:` = the loader's VFS root (a folder `vfs` next to the loader folder if there is one, otherwise `vfs` inside the loader folder).

---

## 4. Collections

A collection builds a user-managed list and a value matrix from a local plugin catalogue (Vehicle Materials uses it):

```ini
[collection:materials]
source = local-plugin:resources/list   ; reads only this section of build\plugins\resources.ini
source_ready_section = resources       ; optional readiness condition
source_ready_key = hook
source_ready_value = 2

resource_group = resources             ; group with the list
matrix_group = vehicles                ; group with the matrix
count_section = resources              ; a readonly field must exist for this key
count_key = count
item_prefix = resource                 ; resource0, resource1, ...
item_label = Material
item_label_key = material_number
item_description_key = field.resource.description

target_sections = road|rail|ship|airplane
target_labels = Road vehicles|Rail vehicles|Ships|Airplanes
target_label_keys = row.road|row.rail|row.ship|row.airplane
target_icons = builtin:truck|builtin:rail|builtin:ship|builtin:plane
unit = Coefficient
unit_key = unit.coefficient
coefficient_description_key = coefficient.description

empty_notice = No entries yet. Add one first.
empty_notice_key = collection.empty_notice
empty_notice_style = warning           ; warning or info; shown in matrix_group while empty

type = decimal
minimum = 0
maximum = 1000000
step = 0.001
default = 0
maximum_items = 32
ownership = user                       ; an existing list survives a change of the package defaults
allow_remove_defaults = 1              ; entries shipped by the package may be removed
require_positive_when_enabled = 1      ; at least one positive value while the plugin is on
```

The four `target_*` lists must have the same length. Adding creates gap-free `item_prefix0`, `item_prefix1`, … plus one value per target section; removing deletes all values and compacts the list. `require_positive_when_enabled` switches the plugin off when the last positive entry is removed; the error message names the visible tab and group.

---

## 5. Local schemas (`settings_schemas`)

A plugin whose package brings no schema – whether it lies as `plugins\<name>.dll` in the loader folder or comes from the Workshop – gets its schema from `settings_schemas\<name>.launcher.ini` next to `rmm.exe`, if that file exists, otherwise from its INI (section 2). Such a local schema is an ordinary presentation schema:

```ini
[launcher]
layout_version = 1
visible = 1
id = local.walking
name = Walking Distance
config = walking.ini                   ; must be <name>.ini
enabled_field = walking/enabled
language_directory = languages         ; relative to settings_schemas
```

RMM ships such schemas for accumulator, cities, daynight, depletion, easystart and walking; the German texts are in `settings_schemas\languages\de.ini` under the prefix `<name>.`, the English fallbacks in the schema itself. A local schema describes, it does not forbid: values may carry a comment (kept until the value changes), and keys the schema does not know are named in the notes and written back unchanged. Free values such as `auto` or years with `off`/`always` use `type = text`.

A plugin in the `plugins\` folder is managed as a protected base: original under `user_config\.autoload\<name>.upstream.ini`, effective INI in `plugins\`, personal values in `user_config\<name>.ini`. A `plugins\<name>.ini` changed outside RMM becomes the new original. For a plugin from the Workshop the INI next to the DLL stays the original, and RMM writes only `user_config\<name>.ini`.

The same folder holds `replacements.ini`: under `[replaces]` one line `<plugin> = <plugin it replaces>` each, for replacement plugins without a manifest. A `replaces` in a package's manifest takes priority.

---

## 6. Editor schemas

For INIs that hold lists instead of single values there are three editor types. The schema lies in `settings_schemas\` or in the package under `config\<name>.launcher.ini`; in a package `[editor] plugin` and `config` must name the DLL name and INI of the package. The package's INI is the original base, personal entries live in `user_config\<name>.editor.ini`, the effective file is `plugins\<name>.ini` in the loader folder. The switch in `tesmioloader.ini` works as for every plugin.

| `editor_type` | Structure of the INI | Example |
|---|---|---|
| `keyed_resources` | list section plus one section per entry (`[custom:<id>]`) and keyed values | Resources |
| `keyed_list` | one line per entry: `<id> = <column 1>, <column 2>, ...` | Needs, Technical Service Storage, UI Layout Fixes |
| `keyed_sections` | one section per entry | Deposits, Deposits Plus, Research Expansion, Vanilla Buildings |

### 6.1 `[editor]`, `[activity]`, `[source]`

```ini
[launcher]
layout_version = 1
editor_type = keyed_list
id = tesmio.needs.editor
name = Needs
language_directory = languages
default_tab = needs
notice = ...                           ; as in presentation schemas
info = ...

[editor]
plugin = needs
config = needs.ini
list_section = list                    ; keyed_list / keyed_resources
item_section_prefix = custom:          ; keyed_resources
reserved_sections = deposits           ; keyed_sections: sections that are NOT entries
section_prefix = modify:               ; keyed_sections: only sections with this prefix are entries
maximum_items = 8

[activity]                             ; the plugin's on/off field; not shown in a card,
section = needs                        ; it belongs to the "Plugin active" switch
key = enabled
values = 1

[source]                               ; identifiers for the + dialog
plugin = resources                     ; keys of the section from plugins\resources.ini
section = list
ready_section = resources              ; optional: source counts only with [resources] hook = 2
ready_key = hook
ready_value = 2
```

Without `[source]` the + dialog asks for the identifier as an editable field and suggests the base-game resources plus the list of the Resources plugin. `[list] id_suggestions = 0` drops these suggestions (for text ids and the like). Identifiers follow the usual rules: letters, digits, `_`, `-`, `.`.

### 6.2 Tabs, list texts, dialog texts

```ini
[tab:general]
label_key = tab.general
order = 10

[list]
label_key = needs.list                 ; list title
add_label_key = needs.add              ; + button
select_help_key = needs.select_help    ; help text while nothing is selected
id_label_key = needs.resource          ; label of the identifier
id_help_key = needs.resource.help      ; help under the identifier in the detail area
add_id_help_key = needs.add_help       ; help under the identifier in the + dialog (otherwise id_help)
save_warning_key = needs.save_warning  ; yellow save-game warning on the list card
note_key = needs.note                  ; small text left under the list, next to +
summary = type|map                     ; keys shown under the name in the list
id_picker = game_texts                 ; game_texts: "Choose text..." button; game_research: "Choose research..."
id_suggestions = 0
remove_label_key = re.remove           ; "entry only" button in the delete dialog of entries with attachments

[new]                                  ; + dialog of keyed_sections
name_label_key = deposits.new_name
token_key = token                      ; field the dialog suggests from the name
token_template = $TYPE_MINE_{NAME}
hint_key = deposits.new_hint           ; text at the end of the dialog

[group:materials]                      ; the list card
tab = materials
label_key = tss.group.materials
description_key = tss.group.materials.description
notice_key = tss.group.notice          ; blue box between description and save warning
id_reference = resources               ; every own identifier must exist (section 7)

[global]                               ; card for plugin-wide values (scope = global)
tab = general
label_key = group.needs.plugin
description_key = ...
notice_key = ...
order = 10                             ; sorts among the [card:] cards of the tab; 0 = first

[card:log]                             ; further cards for plugin-wide fields
tab = general
label_key = tss.card.log
notice_key = ...
notice_style = info                    ; blue instead of yellow
order = 20
```

**A card list instead of a list beside a detail panel.** With `style = cards` in `[list]` every entry becomes a row of its own, and the fields move into a dialog behind "Change...":

```ini
[list]
style = cards
title = bp_name                 ; the field used as the heading of the row
subtitle = bp_id|bp_object|bp_donor   ; the grey line below it
toggle = bp_enabled             ; switch on the left, has to be type = boolean
state = buildings_plus          ; built-in state provider, otherwise no state column
actions = edit|open|delete      ; Change... / Open / bin
```

`title`, `subtitle` and `toggle` name field ids; an unknown one makes the schema fail to load. A search box appears from eight entries on, and the rows are sorted by their heading. An original (from the effective INI or from a content package) carries a padlock and is hidden rather than deleted.

In the dialog every field with `card = global` sits on the first tab; each further `card` value becomes a tab of its own, labelled from `[card:<id>]`. Such a card can carry the line editor instead of fields:

```ini
[card:lines]
tab = buildings
label_key = bp.card.lines
editor = donor_lines            ; the donor's file on the left, your lines on the right
lines = bp_line                 ; the field with the building.ini lines
strip = bp_strip                ; the field with the tokens to drop
```

The editor shows the donor's `building.ini` on the left (switchable to the result), strikes through what your declaration removes, and names below each of your lines which donor lines it kills - with a button that copies them back as lines of your own. In the result view every `$STORAGE` line carries the number `$RESOURCE_VISUALIZATION` would use, and water or sewage in front of a shown storage is reported. The result view is read only. Two of your lines that set the same thing are reported, and adding a second one asks first.

The second line editor works on real target files instead of a donor:

```ini
[card:commands]
tab = buildings
label_key = vb.card.commands
editor = target_lines           ; the target file on the left, the commands on the right
targets = target                ; the field with the target files
add = add                       ; the four line commands
replace = replace
remove = remove
insert = insert
add_connection = add_connection ; the three connection commands, same card
replace_connection = replace_connection
remove_connection = remove_connection
```

At the top you pick the target file - a rule set can have many, and every command is checked against each one separately. On the left are the lines of the unchanged file; clicking one makes it the anchor, and the buttons **Append, Replace, Before, After, Remove** turn it into the finished command. The commands are on the right; one that does not fit this file is red and says why below (not found, several matches, a `$COST_` line, already there …). A target file that is not on this machine switches the buttons off.

Connections are blocks, not lines. Click any point line on the left and the editor takes the whole connection (the token line plus one or two point lines, or the single-line form `$TOKEN x y z`), and the buttons write the matching connection command by themselves instead of `add`/`replace`/`remove`. Points are compared as numbers, exactly as the plugin does, so `5` and `5.0` are the same point; a `*_ALLOWPASS` token is refused by the plugin and the editor says so right away. Before and After do not exist for connections - they always go behind the last one.

Every command is always run against **all** target files of the rule set, not only the chosen one. That matters more than it sounds: one command that misses in a single target file invalidates the **whole rule set** for that file - the plugin then writes "No rule for this target was applied" to the log, and none of your changes arrive. So the editor says it in three places: a red line above the lists tells for how many target files the rule set works; in the choice of target files the ones concerned stand red with an `x` in front; and below a picked command it says in how many files it fits and which one it misses first. The same number stands beside the name in the list of rule sets and on the page "Before the game starts". A command that only fits some of the files belongs in a rule set of its own - or use **Append**, which needs no anchor.

The editor has two views. **Original** is the unchanged target file, where you click your anchors. **Result** shows, read only, the file the game will read once the commands have run, and marks every line a command adds (`+`) or replaces (`>`); clicking one names the command that wrote it. While a command is red the result is the unchanged original - which is exactly what happens in the game as well.

Several lists in one `keyed_sections` schema: a `[group:<id>]` with its own `section_prefix` is an additional list; its texts come from `[list:<id>]` and `[new:<id>]`, its fields carry `group = <id>`. The default list is the first `[group:]` without a prefix. Identifiers of additional lists are full section names (`research:quartz`), shown without the prefix.

### 6.3 Columns (`keyed_list`)

```ini
[column:donor]
type = choice                          ; text, integer, decimal, choice
choices = food|meat|clothes|eletronics|alcohol
allow_other = 1                        ; choice with free input
required = 1
default = auto                         ; fills missing columns
minimum = 0
maximum = 1
step = 0.1
label_key = needs.donor
description_key = needs.donor.description
heading_key = ...                      ; separator line with a title above the field
reference = resources                  ; section 7
order = 10
```

Lines are normalised on writing (`1.0` becomes `1`, choice values in the schema's spelling).

### 6.4 Detail fields (`keyed_sections`, `keyed_resources`, global values)

```ini
[detail:type]
scope = item                           ; item: key in the entry's section
                                       ; global: [section] key of the plugin INI, shown on [global] or a [card:]
                                       ; custom: key in [custom:<id>] (keyed_resources)
                                       ; keyed: line <id> = <value> in the section `section` (keyed_resources),
                                       ;        for example [base_price] or [names:German]
group = research                       ; additional list (keyed_sections)
card = log                             ; card for scope = global
section = deposits                     ; scope = global or keyed; may carry one colon (names:German)
key = type
type = integer                         ; text, integer, decimal, choice, boolean (global), lines, pair, triple
minimum = 10
maximum = 127
step = 1
choices = technical|soviet|medical
choices_source = registry              ; choice: identifiers from [source] plus base game
choice_labels = STOP|СТОП|停           ; choice: what the list shows per value, same order; the INI
choice_labels_key = x.field.choice     ; keeps the value. Language key <prefix>.<value> wins;
                                       ; not with allow_other or choices_source
allow_other = 1
unique = 1                             ; no value twice
auto_increment = 1                     ; dialog suggests maximum + 1
dialog = 1                             ; part of the + dialog
default = {name}                       ; dialog default; {name} = name of the new entry
maximum_length = 7
length_rule = map=terrain:4            ; shorter while another key has this value
empty_hint = empty = the game's own id ; shown in bold under the empty field as "Default: <text>";
empty_hint_key = x.field.empty         ; it disappears as soon as the field carries a value
suffix = .name                         ; locked box with the suffix behind the field, identifier as placeholder
position = above_id                    ; field above the identifier line
heading_key = ...
label_key = ...
description_key = ...
reference = game_research              ; section 7
reference_format = requires
reference_own = research
picker = game_buildings                ; lines: building picker; game_research: research picker;
                                       ; research_lines: lines of the entry's Vanilla block;
                                       ; donor_lines: lines of the donor's building.ini (see game_donor);
                                       ; files: file list; text: game_donor (a base-game building, one name)
assigned_from = build:plugins\x.ids.ini|ids   ; text: empty = the plugin assigns the value itself and keeps
                                       ; it in this INI under the entry's id; RMM shows it as a grey
                                       ; placeholder in the field
picker_format = line_edit              ; research_lines: line, line_edit, line_anchor, anchor_edit, edit
picker_folders = package:plugins\x\assets | build:plugins\x\assets   ; files: first existing folder
picker_pattern = *.dds
count_label_key = ...                  ; lines: counter label (default "Number of lines")
maximum_lines = 64                     ; lines: cap, counter turns red above it
order = 20
```

`type = lines` (scope = item) describes a key the INI repeats in the section (`target = …`, `add = …`): one line per occurrence, empty lines dropped, written as one block at the place of the first occurrence, stored as `field.<id>.0`, `field.<id>.1`, … in the override. Lines fields have a fixed height with a scrollbar, a grip to resize, and plus/minus to fold. For `[list] summary` a lines field shows its first line with a counter `(+n)`.

Original entries are locked but can be hidden (`suppressed = 1` in `user_config\<plugin>.editor.ini`): they vanish from the effective file, stay in the original base and can be shown again. Personal entries are stored as `[item:<id>] owned = 1` with `field.<id>` values (or `list = <tuple>` for keyed_list) and written as a new section in field order; plugin-wide values as `[global] field.<id> = <value>`. A hidden original loses its whole section in the effective file; an overwritten original keeps section and comments.

### 6.5 Rows on cards: folder, file, picture, Vanilla block

```ini
[folder:icon_store]                    ; folder row on a card
card = icons                           ; card or global
path = vfs:media_soviet\research
missing_key = re.icon_store.missing    ; yellow box while the folder is missing
create = 1                             ; "Create folder" button while missing
label_key = re.icon_store              ; row with path field, "Open" and "Refresh" once it exists
order = 10

[file:noimage]                         ; file row: first existing candidate is shown
card = icons
paths = workshop=package:plugins\x\noimage.png | local=build:plugins\x\noimage.png
label_key = re.noimage
order = 20

[picture:icon]                         ; picture preview per entry with "Insert picture..."
group = research
folder = vfs:media_soviet\research
file = {id}.png
size = 128                             ; required size in pixels, 0 = any
order = 15

[research_block:block]                 ; read-only box with the entry's block from media_soviet\research\research.ini
label_key = re.block
description_key = re.block.description
group = modify
order = 12
```

Entries with attachments (texts, picture) get a delete dialog with three buttons: "Delete all" removes the entry, its texts from all language files and the picture; the `remove_label` button removes only the entry.

### 6.6 Text pack tab

```ini
[tab:localization]
label = Localization

[textpack]
tab = localization
folder = build:plugins\localization\research_expansion   ; local pack, RMM writes here
seed = dependency:tesmio.localization|plugins\localization\research_expansion   ; initial content from the dependency's package
namespace = research_expansion         ; for a new pack without seed
keys_from = research                   ; list group whose identifiers give the lines <id>.name / <id>.desc
label_key = ...
description_key = ...
missing_key = re.textpack.missing      ; yellow box with "Create locally" while the folder is missing
required = 1                           ; red box on every tab, red tab and no saving (with own entries) while the folder is missing
required_notice_key = re.textpack.required   ; text of the red box (required_notice = fallback); without it a default text
```

The tab shows the fallback language, the language files (with "+" from the game's languages), per own entry name and description, and further keys with a trash button that removes a key from all language files.

### 6.7 A tab for generated Workshop folders

```ini
[tab:generated]
label = Generated buildings
label_key = bp.tab.generated
order = 30

[wip_buildings]
tab = generated
range = generated            ; generated = written by Buildings Plus or by the original buildings plugin
                             ; own = the folders carrying this plugin's own stamp
                             ; all = every folder under media_soviet\workshop_wip
label_key = bp.wip
description_key = bp.wip.description
empty_key = bp.wip.empty     ; text when no folder matches
```

The tab reads `media_soviet\workshop_wip` and shows name (from `$ITEM_NAME`, otherwise from `$NAME_STR` of the building.ini), number, object folder and origin per folder. The origin comes from `tesmioloader.stamp`: Buildings Plus, the original buildings plugin, or no stamp at all - then the folder came from the game's own editor.

Reported without asking: a folder with `$OWNER_ID 0` (the button fills the Steam id in, and **only** that number) and a folder no package declares any more. The stamp is never touched.

"Change..." lets the player edit the generated `building.ini`. What is stored are **changes**, not the file: per building `user_config\.autoload\wip\` holds a receipt `<id>.receipt.ini` with `replace = old line | new line`, `remove = line` and `add = line`, plus `<id>.baseline.ini`, the generator's output verbatim.

An anchor may span **several lines**. A token and the lines below it without a token of their own are one block, and only that makes a line addressable: `rotation 0.0` often occurs three times in a generated file, the block it belongs to exactly once. Clicking any line of a block selects the whole block. As soon as one change is a block, RMM writes the receipt as sections so their order survives (both forms are read):

```ini
[op:4]
kind = replace
old = $RESOURCE_VISUALIZATION 3
old = rotation 0.0
new = $RESOURCE_VISUALIZATION 3
new = rotation 90.0
```

When the generator writes anew (the hash in `tesmioloader.stamp` changes), its fresh output becomes the new baseline and the changes are applied again - whatever the package improved is kept. An anchor that matches no line, or more than one, is reported and left out. A schema needs nothing beyond `[wip_buildings]` for this.

---

## 7. Reference checks

Values that must name something the game knows are checked before saving. A hit turns the footer red ("Configuration invalid") with the entry and field; Save and "Save + Start" stop with the same message. Only own entries and personally set values are checked. Without a game folder next to the loader the check is skipped.

```ini
[group:modify]
id_reference = game_research           ; the identifier of every own entry must be an active Vanilla research

[detail:r_requires]
type = lines
reference = game_research              ; game_research, game_buildings, game_donor, game_texts, resources, files
reference_format = requires            ; id (default): whole value / every line
                                       ; requires: "<predecessor> | before/after | <anchor>", both checked
                                       ; directive:$UNLOCK_RESEARCH: only line parts with this directive
                                       ; file: file relative to media_soviet or the Workshop folder must exist
                                       ; exists | dds_dxt1 | dds_dxt5: for reference = files
reference_own = research               ; own, switched-on entries of this group count as known
```

Sets: `game_research` = active blocks of `research.ini`; `game_buildings` = building files of the game, the DLCs and subscribed Workshop items; `game_texts` = ids of the `.btf` of the game language; `resources` = the 57 base-game names plus `plugins\resources.ini [list]` when the Resources plugin is ready; `files` = the picker folders. The DDS formats additionally check compression, square power-of-two size 256..4096 and the full mipmap chain.

---

## 8. Languages

```ini
[language]
name = Deutsch

[strings]
plugin.description = Beschreibung
field.enabled = Plugin aktivieren
```

`language_directory` is relative to the package root (presentation schema) or to `settings_schemas` (local schema); files are `<code>.ini`. Order: chosen language, English, the plain text in the schema. Section, key, group, tab and entry identifiers are never translated. Language files are read literally: a semicolon inside a text is not a comment.

---

## 9. Icons and path safety

Built-in icons include `builtin:gear`, `builtin:truck`, `builtin:rail`, `builtin:ship`, `builtin:plane` and `builtin:rocket`. Alternatively a safe relative PNG/ICO path inside the package. Absolute paths, `..`, URLs, reparse points, oversized files and invalid image sizes are refused or logged as optional and replaced by a default icon. Asset names may contain spaces and `. _ + ( ) & , ' -`, but no leading dot, no trailing dot or space and no `..`.

---

## 10. Receipts and restore points

RMM never copies a DLL, and whether a plugin loads is decided by the entry `[plugins] <name>` in `tesmioloader.ini` alone (no entry means ON). Every save leaves a receipt under `user_config\.autoload\` with the package version and the hashes of the DLL and the default INI. Its `mode` says where the DLL lies:

| `mode` | Meaning |
|---|---|
| `workshop` | DLL in the Workshop folder; `user_config\<name>.ini` is written, with `effective_ini = 1` also `plugins\<name>.ini` |
| `installed` | DLL in `plugins\`, protected base |

A changed package (new version, new DLL or default INI) shows the yellow "Update" marker. The game loads the new version anyway; saving brings the receipt up to date and keeps personal values. Before every save RMM keeps one rolling restore point per plugin under `user_config\.autoload\backups\<package>\previous`; a pending marker protects against a half-written state after a crash.

---

## 11. Content packages (`[content]`)

A package without a plugin DLL but with `[content]` in its manifest is a content package: it ships no DLL, only sections for plugins that already exist.

```ini
[mod]
id = example.salt
name = Salt Resources
version = 1.0.0
enabled = 1

[content]
resources = tesmio\resources.ini       ; [list], [custom:<id>] and [names:<Language>] as in resources_plus.ini
deposits  = tesmio\deposits.ini        ; one section per deposit, as in deposits_plus.ini
needs     = tesmio\needs.ini           ; [list] as in needs.ini
buildings = tesmio\buildings.ini       ; one section per building, as in buildings_plus.ini
rules     = tesmio\vanilla_buildings.ini ; one section per rule set, as in vanilla_buildings.ini
vehicles  = tesmio\vehicle_materials.ini ; [materials] with <resource> = <road>, <rail>, <ship>, <airplane>[, <rocket>]
vehicle_rules = tesmio\vehicle_buildings.ini ; rule sets that count only while "Vehicle production" is ON
shared_buildings = gas_powerplant, gas_heating_plant ; buildings the package shares with other packages
assets    = assets                     ; folder whose layout starts at media_soviet\...
```

All nine lines are optional on their own; at least one content file or the asset folder must be there. RMM reads the files loosely (repeated keys are allowed); a broken file rejects the whole package.

**What RMM makes of it.** The package page has no settings, only the switch "Provide in the game". On save:

- Every content file goes to the first plugin of its row that the loader finds and that is switched on (otherwise to the first one it finds at all): `resources` → resources_plus, else resources; `deposits` → deposits_plus, else deposits; `needs` → needs_plus, else needs; `buildings` → buildings_plus, else buildings. If the plugin is missing, the page says so and that part is left out.
- The entries appear in that plugin's editor as originals: overridable, not deletable. If an id already exists there (an entry of your own, an original entry or one from another package), RMM does not provide the package at all: the package page names the id, nothing is written and no file is copied until the existing entry is gone.
- For resources, every list entry takes its lines from the other sections of the content file along: prices (`[base_price]`, `[price]`) and names per game language (`[names:German]` with `<id> = <name>`; `German` as in the game's file name `sovietGerman.btf`). A line for an id the package does not bring itself stays out. Resources Plus 0.1.4 or newer shows the names; older versions and the original Resources skip the section.
- `assets` is copied to `tesmioloader\vfs`, path for path.
- `rules` goes to vanilla_buildings: every section but `[general]` is a rule set and appears there as an original, with all its repeated lines (`target`, `add`, ...).
- `vehicles` is taken by no editor. The file has one section `[materials]`; every line names a resource and four or five numbers from 0 up (road, rail, ship, airplane and, if given, rocket parts), at least one above 0 - the coefficients of Vehicle Materials. It gives the package page the card "Vehicle production" with ON and OFF. With ON (and the package provided) RMM enters every resource of the file as an ordinary entry in the personal values of Vehicle Materials (`user_config\vehicle_materials.ini`: `[resources]` and one line each in `[road]`, `[rail]`, `[ship]`, `[airplane]`) and sets `enabled = 1` there. A resource the player already lists keeps his values; the package's numbers are starting values only. With OFF, and when the package is switched off, RMM takes the entries out again. The card shows what is in the list: with every resource of the package there it is ON, whoever entered them. The card shows the resource by the name from the package's resource list. If the package does not bring the resource itself, the file gives the name in two more sections: `[names]` for every language and `[names:German]` for the German RMM, one line `<resource> = <name>` each; without them the card shows the id. That the player has decided is kept in the receipt (`vehicles = on|off`); as long as that is missing and the resource is not in the list, RMM says so on the card, in "Before the game starts" and before every start.
- `vehicle_rules` only exists together with `vehicles`. The file is built like the one of `rules` (one section per rule set for vanilla_buildings) and carries what vehicle production needs on top - as a rule the storage for the material: `add = $STORAGE_IMPORT_SPECIAL <class> <amount> <resource>` with one `target` line per vehicle factory. The rule sets reach vanilla_buildings only with ON and leave again with OFF. A `target` line whose file the player does not have (DLC not owned) is left out by RMM; a rule set without a target left is dropped. Several packages name the same factory, so these rule sets may only use `add` (Vanilla Buildings 0.4.8 or newer lets such rule sets share a target file). If vanilla_buildings is missing or off, RMM reports it like a missing choice.
- `shared_buildings` names sections of the package's own `buildings` file that another package brings just the same. When both packages name the section here, it is no conflict: the package that comes first in the fixed order (by the name of the receipt folder, that is the mod id) supplies it, the other steps in when the first is switched off. The section stays in the INI of Buildings Plus, its generated folder stays, and since the section name is the same, so is the building's number - saved games keep loading. Only when no package supplies it any more does it go. If the packages bring different versions, the other package's page says so; the supplying one counts. A package that does not name the section here is a conflict as before. Every name has to be a section of the package's own buildings file.
- For deposits RMM assigns `type` itself (10 to 127, free against everything the plugin already knows) and sets `map = auto`; `component` is dropped. A number once assigned stays with its section.
- A receipt under `user_config\.autoload\content\<mod id>\` records what is provided. If the package changes in the Workshop, the "Update" mark appears; saving takes over the new state.

Switching off and saving takes entries and files out again; personal values and your own entries stay.
