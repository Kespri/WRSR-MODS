# Vanilla Buildings – build notes

Target: WRSR 1.2.0.9 (the plugin uses no game address), TesmioLoader API 4. Build:
the standard line (`cl /O2 /MT /W3 /EHsc /LD ... /link kernel32.lib`); exports
TsmPluginApiVersion/TsmPluginInit/TsmPluginStart. History newest first.

## 0.4.18 (2026-11-27)

- Code review: the storage follow-up (RemoveGone, Resize, AfterLoad) ran for every building.
  A production line keeps storages of its own behind those of its definition (a car factory:
  8 for 4), and a plain single storage among them could look like one whose line is gone.
  `storage_retrofit::g_skipProductionLines` (new in the shared header) is now set unless
  `retrofit_vehicle_factories = 1`: then buildings of type 40 are not touched at all.

## 0.4.17 (2026-11-27)

- Stage C in the shared header: `Direction` (the parser sets +0x84 for the IMPORT tokens and
  +0x85 for the EXPORT tokens, 1.2.0.9 0x12F650..0x12F728), `GoodsClass` / `ClassNumber`
  (covered, open, gravel, oil, cement, cooler, livestock, concrete, nuclear1/2, cryo,
  hypergolic). `PlainSingle`, `MakePlan`, `StorageGone`, `DefinitionKeeps` and `Resize` take
  import and export storages of these classes; a storage that goes the other way is another
  storage in every comparison.
- No room, no change: a definition storage whose type slot holds nothing (the line names a
  class the resource has no factor for - seen in the game as "0.00 of 0.00" on a newly built
  salt refinery) is never given to a building, and it does not make the working storage of
  another class leave.
- `ParseStorageLine` reads `$STORAGE_EXPORT_SPECIAL` and every goods class.
- Offline test `my_plugins\tests\run_storage_retrofit_test.bat`: 31 checks.
- Export storages not seen in the game at the time of writing.

## 0.4.16 (2026-11-27)

- Storages follow their line: `storage_retrofit::Resize` (new in the shared header), called
  for every loaded building between `RemoveGone` and `AfterLoad`. A plain single-resource
  import storage takes the capacity its definition names now; only the capacity value of
  the storage is written (Storage +0x8C, the value the game's save routine at 0x2068E0
  writes per storage), nothing is moved, cloned or freed. Bigger at once; smaller when the
  stock plus what is on its way fit into what the type's slot names.
- Another class: `StorageGone` now asks `DefinitionKeeps` - a plain storage of the
  definition counts only when it has the same class - so the old storage leaves when it is
  empty and `AfterLoad` puts the new one in during the same load.
- Offline test of these rules: `my_plugins\tests\run_storage_retrofit_test.bat` (20 checks,
  the game's structures laid out in plain buffers).
- Not seen in the game at the time of writing.

## 0.4.15 (2026-11-27)

- Storages leave again: `storage_retrofit::RemoveGone` (new in the shared header), called
  for every loaded building before `AfterLoad`. A plain single-resource import storage goes
  when its resource is on record (`<loader>\plugins\vanilla_buildings.storages.txt`, every
  resource an enabled rule set ever gave a storage for), the building's definition has no
  storage for it any more, and it is empty and not waited for. At the end of the list the
  vector's end moves back; in front of another storage the ones that stay are moved
  together into a block the game made. Nothing is cloned, destroyed or freed.
- All building types. A building with a storage of vehicles only with
  `retrofit_vehicle_factories = 1`.
- With no rule set enabled the plugin stays loaded while the record is not empty and
  `retrofit_storages` is 1 (`g_cleanupOnly`): no file redirects, only the subscription.
- The same routine exists as a copy inside Vehicle Materials (`FactoryRemoveGone`), where it
  was seen in the game; the header's helper is named `TryBuildKept` so that the two do not
  collide. To be merged.
- Not seen in the game for this plugin at the time of writing.

## 0.4.14 (2026-11-27)

- Storage retrofit: `storage_retrofit::g_keepExisting` and `g_appendOnly` are set. The
  storages a building has are moved bit by bit into a block the game made, the new ones go
  behind them, the old block is left alone. Before, every storage was replaced by a clone
  and the new one was put where the definition has it. Seen in the game with 0.4.13 on a
  depot (rule set line for limestone, 2026-11-27): the storage came, the depot said "no
  power" - the same fault Vehicle Materials had on factories, where it also crashed the
  game. The game keeps addresses into the storages of a building.
- A depot's grit storage is a rule set line too, so this retrofit runs before the one of
  Technical Service Storage (its `after` callback comes first); both now work the same way.
- `retrofit_vehicle_factories` still decides whether a building with a storage of vehicles
  is served.
- Not seen in the game at the time of writing.

## 0.4.13 (2026-11-26)

- Diagnostics only. With `debug = 1` the `after` callback calls `storage_retrofit::Describe` for
  every loaded building whose definition carries one of the rule sets' storage lines: one log
  line per storage of the definition and of the building (index, class, capacity, the parser's
  flags +0x84/+0x85/+0x88, +0x80, whether it is a storage of vehicles, the resources it holds),
  plus the type's own storage numbers (+0x3C0, +0x3C4, +0xC58) - before a retrofit and again
  after one. Read-only.
- Reason: in the user's test neither an old nor a newly built car factory accepted glass panes
  or cable from trucks although a blueprint was selected; the logs so far do not show how such a
  building keeps its storages.

## 0.4.12 (2026-11-26)

- Where a retrofitted storage goes. Read from the game's building loader (1.2.0.8 image,
  0x20B890; the block that tests transport class 0xF, and the same again for 0x10): when a
  building lacks the water or sewage storage its definition has, the game itself builds one
  from the definition and INSERTS it at the definition's index, clamped to the number of
  storages the building has (`vector::insert`, 0x21C150). So the game keeps a building's
  storages in the order of its definition, and the retrofit appended at the end - wrong for
  every building that has storages behind the definition's (a production line; any building
  with the automatic water and sewage tanks).
- `storage_retrofit.h`, `BuildReplacement`: the staged vector is now built in that order - the
  existing storages up to the definition index, the new one, the rest. `Candidate::position`
  is logged as `storage_index=<n> of=<count>`.
- New key `[general] retrofit_vehicle_factories` (default 0): sets
  `storage_retrofit::g_serveVehicleStorages`. Experimental - it is not known whether the right
  position is all a production line needs, or whether it also knows its storages by address
  (the crash of 0.4.10). Shown on the settings page as a switch marked experimental.
- Self-test: the new key and its default. Nothing of this confirmed in the game at the time of
  writing.

## 0.4.11 (2026-11-26)

- 0.4.10 crashed the game. The user's run of 19:19: the saved game loaded, the log said
  `Storage retrofit [added] building=... definition_index=3 storage_index=8 resource=cable` for a
  finished car factory, and two seconds later `ACCESS_VIOLATION at SOVIET64.exe+0x6CD691`,
  reading 0x00000000CC2109F4: `movss xmm0,[rcx+0x604]` with `rcx` taken from a vector of
  pointers at +0x680 of another object - a building pointer whose upper half is zero. No dump;
  the loader's crash report is all there is. The saved game was not written after the retrofit.
- What the log shows beyond the crash: the factory had 8 storages where its definition explains
  4 (two CARPLANT, vehicles, glass_panes). A production line carries storages of its own behind
  the definition's, so `storage_index=8` put the new one behind those - not where a newly built
  factory has it - and the vector was replaced under a building that evidently knows its
  storages by position or address. Which of the two broke it is not established.
- `storage_retrofit.h`: `SERVE_VEHICLE_STORAGES = false` - a building with a storage of vehicles
  (+0xC0 set) is refused as a whole again, as in 0.4.9 (`building-with-a-storage-of-vehicles-is-
  not-served`). The checks for the nested object stay in the file, unused.
- Proven so far for this routine: nothing in the game for this plugin. The same staged swap is
  proven for depots in Technical Service Storage. Other building kinds are untested.

## 0.4.10 (2026-11-26)

- The retrofit refused every vehicle factory: the game's log of the user's run said
  `Storage retrofit [rejected] ... resource=cable reason=existing-storage-index=2:
  unsupported-specialized-storage-owner-0xC0`. Storage 2 of `production_vehicle` is its
  `$STORAGE_EXPORT RESOURCE_TRANSPORT_VEHICLES`; a storage of vehicles owns one more object at
  +0xC0 (0x90 bytes: one byte and four 4-byte values, then five vectors with records of 0x28,
  0x28, 0x28, 8 and 0x24 bytes).
- `storage_retrofit.h` now accepts such a storage among the EXISTING ones (`NestedValid`) and
  checks its clone field by field (`NestedMatches`, `RecordsMatch` - the game's record copies
  leave the padding behind the leading bytes alone, so a plain `memcmp` would not do). Nothing
  new is done to it: the game's Storage copy constructor clones the object itself (`operator
  new(0x90)` and its own copy constructor), and the game's vector growth is copy + destroy as
  well - read from the 1.2.0.8 image (0x124750, 0x124970, 0xE48E0) and compared with the
  disassembly of 1.2.0.9 (0x1245E0, 0x124800: same size, same five members). A storage that is
  ADDED must still be a plain one.
- No change in this plugin's own code. Not confirmed in the game at the time of writing.

## 0.4.9 (2026-11-26)

- Storage retrofit: buildings that already stand get the storages enabled rule sets add, when a
  saved game is loaded. `[general] retrofit_storages` (default 1). `CollectOwnedStorages` takes
  every `$STORAGE_IMPORT_SPECIAL <class> <capacity> <resource>` line that an `add`, `replace` or
  `insert` of a rule set with a built overlay puts into a definition (`ParseStorageLine`; classes
  covered, open, gravel). In `TsmPluginStart` the plugin subscribes an `after` callback to the
  loader's building-load event (`TSM_SERVICE_BUILDING_LOAD`, loader b0.4.5-rmm.3) and calls
  `storage_retrofit::AfterLoad` (shared header `my_plugins\storage_retrofit.h`) with
  `RetrofitWanted`: a definition storage counts when class, capacity and resource are those of
  one of the collected lines. Only this plugin's own lines are served; another plugin's storages
  are that plugin's business.
- `storage_retrofit.h` is the add-only reconciliation of Technical Service Storage, made
  independent of it: the same four native helpers (push 0xE3D60, deep copy 0x1245E0, vector
  destructor 0xC9360, controls 0x2067F0, each compared with its first bytes), the same staged
  swap of the building's storage vector (+0x970), the same refusal of a building whose existing
  storages include a specialised one (+0xC0). It is silent for a building that lacks nothing.
  **This is the first game address in this plugin** - only the retrofit depends on the game
  build; the file redirects do not, and work on any build as before.
- Without the loader service (MaxLegend's loader) or with changed game code the retrofit is off
  and says so; nothing else changes.
- Self-test: storage lines read and refused, the lines a rule set puts into a definition are
  collected (not the ones it removes), the switch and its default. Not run in the game at the
  time of writing.

## 0.4.8 (2026-11-26)

- Several enabled rule sets may share a target. `ValidateTargets` rejected every declaration of
  a target that stood in two enabled sections. Appending lines does not depend on the order of
  the rule sets, so a declaration that is add-only (`AddOnly`: every command a plain `add`, no
  `name`) now hands its operations to the one declaration that builds the overlay for that
  target (`Decl::mergedInto`, `Decl::joined`). When the add-only one comes first and an editing
  one follows, the editing one takes over and the added lines follow its own commands. Two
  declarations that both edit a target are rejected as before (`duplicate target`).
- `BuildOverlays` builds no overlay for a joined declaration; a second pass counts it as active
  when its carrier got an overlay (`applied together with [section]`) and reports it as rejected
  with the carrier's reason otherwise. The combined commands are validated as one list: one added
  line that does not fit rejects the target for every rule set involved.
- Reason: content packages bring a storage for vehicle factories each (Republic Mod Manager
  0.8.3, `vehicle_rules`); three packages on the same factory rejected each other.
- Self-test: add-only + editing + add-only on one target join into the editing one and the
  output carries all three changes; two add-only join; two editing ones and two rule sets with a
  name are rejected. `vb_selftest.exe <media_soviet> <ini>` with the three package rule sets: 39
  targets validated against the game files of 1.2.0.9.

## 0.4.7 (2026-11-25)

- Texts only: README, INI head and package name WRSR 1.2.0.9 (the hotfix that followed 1.2.0.8 on
  the same day). The plugin uses no game address; the code is unchanged and was rebuilt only for
  the new version number.

## 0.4.6 (2026-11-25)


- Rebuilt for WRSR 1.2.0.8 and TesmioLoader b0.4.5 with `/DTSM_RELEASE_VERSION=0.4.5`
  (`TsmPluginTargetVersion` export). No change to the code. The update changed 22 of the game's
  489 building type files; a rule that names a line of one of them may have to be adjusted.

## 0.4.5 (2026-11-18)

- The INI is looked up as `<baseDir>\plugins\vanilla_buildings.ini` first, then beside the DLL.
  Until now the first candidate was `<pluginDir>\vanilla_buildings.ini`. TesmioLoader b0.3.7
  hands every plugin the folder of its own DLL as `pluginDir`, so for a DLL loaded from a
  Workshop item both candidates were the package INI and the finished INI Republic Mod Manager
  writes into the loader folder was never read. For a DLL in `<loader>\plugins` nothing changes.
- No other change. Loaded on 1.1.1.13 in a smoke test with the build before this one (found its
  INI, nothing enabled, unloaded cleanly); a rule set has not been run on 1.1.1.13, and this
  build has not been in the game.

## 0.4.4 (2026-09-13)

- New per-section key `name =`. It replaces whatever `$NAME` or `$NAME_STR` line every target of
  the section carries, so the old line never has to be looked up; a target without exactly one
  name line is rejected and named. A value that looks like a localisation key (at least one dot,
  only letters, digits, dot, underscore, hyphen) is resolved through the Localization service and
  written as `$NAME <id>`, everything else becomes `$NAME_STR "..."`. An unresolved key falls back
  to the part after the last dot and warns once.
- The key is expanded into an ordinary `replace` at overlay-build time, once the target's own
  lines are known. Every existing guarantee therefore applies unchanged: the unique-match rule,
  overlap detection against the section's own rules, and the per-target rejection.
- The Localization service is consumed in the init phase. The Workshop Bridge loads Localization
  before this plugin, so it is on the noticeboard in time. In a local-copy install that loads this
  plugin first, a key falls back with the warning above instead of failing.
- Four self-tests: a literal name, a resolved key, the fallback without the service, and a source
  without a name line, which must be rejected.

## 0.4.3 (2026-09-12)

- One-point connections. The game writes dead ends such as `$CONNECTION_ROAD_DEAD` either inline
  (`$TOKEN x y z`) or as a token line plus one point line; neither the single-line commands (refused
  for every `$CONNECTION` token) nor the two-point connection commands could touch them. `FindConnections`
  now records both forms as blocks with `points = 1` (`pointText` keeps the point as written);
  `IsConnectionTokenName` accepts any `$CONNECTION_*` name for them. Commands: `remove_connection =
  token | point`, `add_connection = token | point` (emitted inline, only an exact duplicate is refused -
  dead ends share their point with a two-point connection by design), `replace_connection = old token |
  point | new token [| new point]` (result written inline; a four-field command is one-point when field
  3 starts with `$`). `Operation.points` carries the form to `ApplyOperations`. Self-tests: inline dead
  end removed, two-line dead end replaced and moved, dead end added before the two-point add, unknown
  point rejected, two-point remove of a dead end rejected.

## 0.4.2 (2026-09-12)

- `replace_connection` accepts six fields: `old token | point 1 | point 2 | new token | new point 1 |
  new point 2`. With the two extra fields the block's point lines are replaced as well (the user
  needed another connection height without remove + add). The token may stay the same then (a plain
  move); with four fields the old rule stands (token must change). Validation: both new points parse,
  differ, and neither touches a point of another existing block or of a planned `add_connection`
  (the moved connection's own old points are ignored); the new points are added to the planned list.
  `AddOperation` takes 4 or 6 fields for this key. Self-tests: moved connection output, same-token
  move accepted, occupied new point rejected.

## 0.4.1 (2026-09-09)

- 2026-09-11, version unchanged: the detail log `tesmioloader.vanilla_buildings.log` is written to `<loader>\logs\`
  (shared `TsmOpenLog` in `src/tesmio_plugin.h`, the folder is created on first use; if that fails the
  file lands next to `tesmioloader.log` as before).
- `insert` accepts as anchor a line that an earlier `add`, `insert` or `replace` of the same
  section produces (`Operation.anchorOp`). Validation: when the anchor has no match in the
  original, the earlier operations are searched (add field 0, insert field 2, replace field 1);
  none found is still "0 matches"; the same line twice at one produced anchor is rejected.
  Produced anchors skip the neighbour and collision checks (they are not original lines).
- `ApplyOperations` now builds a vector of (producer, line) pairs and splices produced-anchor
  inserts afterwards in INI order, so chains work (add sand -> insert 1 after sand: gravel ->
  insert 0 before gravel: road_salt gives sand, road_salt, gravel). The duplicate-position check
  ignores operations without a slot.
- Self-test extended with exactly that chain. Reason: the user's rule set anchored a storage
  line on another storage line his own `add` produced; every target was rejected with
  "anchor has 0 matches" and the plugin stayed inactive.
- In-game test 2026-09-09: confirmed by the user (storages present, insert before and after the anchor work).

## 0.4.0 (2026-09-09)

First published version.

- Temporary changes to building INIs of the game, the DLCs and the Workshop without touching the
  original files: the native building reader is reached through its actual `fopen` call
  (`fopen_s`, `_wfopen`, `_wfopen_s` covered as well, the engine buffer reader stays supported);
  modified copies are written to a temporary folder at every start and only the game's read
  accesses are redirected. Write and update accesses are not redirected.
- Targets below `buildings_types`, `dlcN/buildings` and Workshop ids (looked up in the game's Steam
  library under `steamapps/workshop/content/784150`); several `target = ...` lines per section
  share all patch rules; every target file is checked independently against its unchanged source
  file; at most 256 target files in total. The optional keys `target1..3` additionally check the
  target kind Vanilla, DLC or Workshop.
- Commands: `add`, `replace`, `remove`, `insert = <position> | <anchor> | <line>` (0 before, 1 after
  the anchor; `$COST_RESOURCE_AUTO` only with a `$COST_WORK` anchor, position 1 with the anchor `end`
  rejected), `add_connection`, `replace_connection`, `remove_connection`. Every rejected active
  target gets a precise warning, even with `debug = 0`: section, target, cause, and for rule errors
  the command and the INI line number. A failed target receives no partial change. The first
  successful access to a replacement file is logged as `[overlay-opened]`.
- Configuration: `plugins\vanilla_buildings.ini` when present, otherwise the INI beside the DLL
  (Workshop package under Soviet Mod Loader or the Workshop Bridge); the chosen file is logged
  (`Configuration file:`).
- Self-test: `cl /DVANILLA_BUILDINGS_TEST` builds `vb_selftest.exe <media_soviet> <ini>`; the
  transformed-output check covers the exact `$COST_RESOURCE_AUTO` sequences.
- Package: editor schema for Republic Mod Manager (keyed_sections with `lines` fields, building
  picker, launcher notice, [activity], Troubleshooting card, group notice, multi-line field texts);
  the shipped INI carries the plastics factory example only, switched off.

Verification of the file layer: 98/98 validation and file tests succeeded, including six really
installed Vanilla/DLC/Workshop buildings and unchanged source files; four DLL lifecycle tests
(disabled, active with all four CRT readers, partially failed hook installation, missing mandatory
`fopen` import); precise warnings for an already existing line and a missing file.

Scope until 0.4.8: existing saved buildings were not extended by missing storages (Technical
Service Storage did that for depots only). Since 0.4.9 the plugin retrofits its own storage lines.
