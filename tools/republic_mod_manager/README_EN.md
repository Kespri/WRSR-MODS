# Republic Mod Manager – Guide

**English** | [Deutsch](README_DE.md)

Republic Mod Manager (RMM for short) is one window for all your TesmioLoader plugins. You see which plugins you have, switch them on or off, set their values with a description and range check, and start the game. The TesmioLoader itself stays untouched; RMM only writes settings files.

---

## 📋 Contents

1. [Requirements](#-requirements)
2. [The window](#-the-window)
3. [The "Plugin active" switch](#-the-plugin-active-switch)
4. [Save and start](#-save-and-start)
5. [The "Notes" card](#-the-notes-card)
6. [How a plugin gets into the game](#-how-a-plugin-gets-into-the-game)
7. [Replacement plugins and dependencies](#-replacement-plugins-and-dependencies)
8. [Content packages](#-content-packages-resources-deposits-buildings)
9. [Before the game starts](#-before-the-game-starts)
10. [Saved games](#-saved-games)
11. [The log window](#-the-log-window)
12. [The settings window](#-the-settings-window)
13. [Profiles and restore points](#-profiles-and-restore-points)
14. [List editors: Resources, Needs, Deposits and package editors](#-list-editors)
15. [Generated buildings](#️-generated-buildings)
16. [Game version and rmm.ini](#-game-version-and-rmmini)
17. [Where your files are](#-where-your-files-are)
18. [When something does not work](#-when-something-does-not-work)
19. [For plugin authors](#-for-plugin-authors)

---

## 🚀 Requirements

- *Workers & Resources: Soviet Republic* 1.2.0.9
- TesmioLoader by MaxLegend, b0.4.5 or newer, in the game folder (folder `tesmioloader` with `tesmioloader.dll`, `tesmiolauncher.exe` and `tesmioloader.ini`). RMM brings a corrected build of `tesmioloader.dll` (b0.4.5-rmm.1, same API). With it your personal values also apply to plugins by other authors; with another build RMM warns you
- Windows 10 or 11

You do not need other loaders such as Soviet Mod Loader or the Workshop Bridge; RMM works with the TesmioLoader alone.

RMM lives as `rmm.exe` in the folder `tesmioloader`, next to `tesmioloader.dll`. Start it from there or through the desktop shortcut. It finds the loader, the game and the Steam Workshop folder on its own. Only one RMM window is open at a time; a second start brings the existing one to the front.

---

## 🖥️ The window

**The list on the left.** Every row is a plugin the TesmioLoader finds at game start: every plugin DLL in `tesmioloader\plugins` and every one in a subscribed Workshop package. Content packages without a DLL are listed too. The icon shows where it comes from: the Steam icon for Workshop packages, the TesmioLauncher icon for plugins in the plugins folder, a gear for anything else. A green dot means this plugin runs on the next game start. An orange dot means its replacement plugin is switched on as well, and this one will be switched off at the next start (see [Replacement plugins and dependencies](#-replacement-plugins-and-dependencies)). A yellow "Update" means the Workshop package has changed since you saved last. An amber dot means you changed something here and have not saved yet.

**Four filters under the search box:** "All", "Active", "Problems" and "Updates". They show exactly the entries that match — a plugin that is not under "Problems" does not have one. Only an entry with unsaved changes stays under every filter; the amber dot tells you why. The open page stays open even while its entry is filtered out; "All" brings it back.

The refresh button sits right next to the search box: it reads the package folders again after you subscribed to something or copied a file in between.

**The header at the top.** Name, version and description of the plugin, on the right the "Plugin active" switch and the language of the window. RMM speaks German and English; "Automatic" picks German when Windows runs in German, otherwise English. The pages of the plugins whose settings come with RMM (such as MaxLegend's plugins, Buildings Plus, Resources Plus and Needs Plus) also exist in Bulgarian, Czech, Spanish, French, Hungarian, Italian, Japanese, Korean, Polish, Portuguese (Brazil), Romanian, Russian, Slovak, Serbian, Turkish, Ukrainian and Chinese (Simplified and Traditional). You see them when Windows runs in one of these languages and "Automatic" is set; the rest of the window stays English then.

**The tabs below.** The tabs come from the plugin. A plugin without its own settings page gets a "General" tab that RMM builds from the comments of its INI file.

**The status line** under the tabs says whether the TesmioLoader loads the plugin (load path), where its DLL is (Workshop or plugins\) and when the loader loaded it last. Hover a dot and a tip explains it. When a plugin's replacement is switched on as well, an orange "Being replaced" dot is lit instead of the load path.

**The cards** carry the settings. Every row has a title, a short text on what the setting changes in the game, and the field on the right:

- Switches for ON and OFF.
- Number fields with plus and minus. Hover the field to see the allowed range. You may type a decimal comma; it is saved with a point.
- Choice lists, text fields and multi-line fields with a counter.
- A reset button appears only when your value differs from the plugin's default.

Right-click on any text copies it to the clipboard, handy for bug reports.

**The footer** shows the state: green "Saved", amber "Unsaved changes", red "Configuration invalid" with the reason. Next to it the buttons Reset, Save and Save + Start. Save is only clickable when there is something valid to save. If you changed several plugins, the footer names them all.

The window remembers size, language, selected plugin and tab. It needs at least 1560 by 760 points; in narrow windows the labels move above the fields.

---

## 🔛 The "Plugin active" switch

The switch in the header decides whether the plugin runs on the next game start. It is the TesmioLauncher's checkbox, the entry `[plugins] <name>` in `tesmioloader.ini`; `<name>` is the DLL's file name without `.dll`.

- **No entry means ON.** The loader loads everything it finds that is not switched off. A freshly subscribed plugin therefore runs on the very next start.
- **When switching on,** RMM also sets the `enabled` field in the plugin's values to 1, if there is one, so the DLL does not refuse right away.
- **Same DLL in several places:** the loader loads only one of them, and the switch applies to that one.
- **Replacement plugins:** when you switch on a plugin that replaces another one (or the other way round), RMM switches the other one off on saving.

The switch shows "on" only when everything fits, just like the green dot in the list. Content packages without a DLL (resources, deposits, buildings) carry the switch "Provide in the game" instead.

---

## 💾 Save and start

**Save** writes your changes:

- For a plugin from the Workshop your personal values go to `tesmioloader\user_config\<name>.ini`. The INI in the Workshop folder stays as the author ships it; RMM never writes there. So a package update never overwrites your values, and the game loads the new version by itself. Some plugins do not read `user_config` themselves; when their package asks for it, RMM also writes the finished INI to `tesmioloader\plugins\<name>.ini`, where the plugin reads it.
- For plugins that lie as a DLL in the plugins folder, RMM backs up the INI it found as the original under `user_config\.autoload\<name>.upstream.ini` on the first save and then writes the finished INI with your values to `plugins\`. "Restore original" in the footer brings the backed-up file back byte for byte.
- The "Plugin active" switch goes into `tesmioloader.ini`. RMM changes only the plugin's line there, everything else stays.
- Before every save RMM creates a restore point (see [Profiles and restore points](#-profiles-and-restore-points)).
- RMM never copies or changes a DLL.

Saving works only while the game and the TesmioLauncher are closed and all values are within their ranges. Opening, searching and switching between plugins or tabs changes none of your settings. There is one exception: when the list file of a list plugin (Resources Plus, for example) is missing in `tesmioloader\plugins`, RMM creates it so that there is something to edit; a blue notice names the source.

**Several plugins at once:** You do not have to save after every plugin. Just switch to the next entry; your changes stay in memory, and the entry gets an amber dot in the list. The footer lists every unsaved plugin. Save or Save + Start then writes them all at once, one after the other. If a value in one plugin is wrong, Save stays locked and the footer tells you which plugin it is. Reset only ever applies to the plugin that is open. When you close RMM it asks once more: save all, discard all or cancel.

**Save + Start** saves the same way and then starts the game through `tesmiolauncher.exe` without its window. RMM closes, unless you told it to stay open (see below). If you want to see the launcher window, set `tesmiolauncher_window = 1` under `[settings]` in `rmm.ini`.

After the start RMM watches the game for about 15 seconds and only then closes. If the game quits **immediately** - within the first five seconds, before it has even loaded - RMM tells you and names the most common reason: a Steam client that is running but right now does not accept a game that starts outside Steam; the only cure is to quit Steam completely and start it again. If you close the game yourself, even right after the loading screen, RMM stays quiet and simply closes. For the old behaviour (RMM closes at once) set `launch_watch_seconds = 0` under `[settings]` in `rmm.ini`.

**If you want RMM to stay put,** switch "Keep the window open" on in the settings window (or set `keep_open = 1` in `rmm.ini`). The game starts as always and RMM stays on screen until you close it yourself — handy when you want to look something up while the game loads. The footer then tells you the game is running. The warning about an immediate exit still appears.

Before the start RMM also checks three things:

- **Resources:** does every resource that switched-on plugins refer to exist in Resources Plus or Resources? If one is missing, the message names the plugin and the start waits.
- **Replacement pairs:** when a plugin and its replacement are both switched on, RMM switches the original off and tells you.
- **Dependencies:** when a switched-on plugin lacks something it needs, a window asks. "Switch off and start" switches the plugin concerned off (you can switch it on again later), "Start anyway" leaves everything as it is.

---

## 💡 The "Notes" card

The first tab of every plugin has a "Notes" card at the top. Everything you should know before saving appears there:

- **Blue:** information, such as a note from the plugin author, a package update with old and new version, that a dependency is active, or where your personal values are kept.
- **Yellow:** warnings, such as a dependency that exists but is switched off, a replacement plugin that is switched on as well, or a DLL that exists more than once (with the one the loader loads).
- **Red:** errors, such as a missing dependency or a Workshop folder that differs between RMM and the loader.

You can save with a yellow or red box as well. When a plugin lacks something it needs, RMM asks before "Save + Start".

---

## 🛤️ How a plugin gets into the game

The TesmioLoader looks for its plugins itself at every game start, in two places:

1. **In the folder `tesmioloader\plugins`:** every plugin DLL that lies directly there.
2. **In the game's Steam Workshop folder:** every plugin DLL in a subscribed package, in whatever subfolder.

RMM shows exactly what the loader finds and never copies a DLL. So subscribing is enough: Steam keeps the files up to date, and the plugin is there on the next game start. Unsubscribing takes it out again.

**Same DLL in several places.** The loader loads only one DLL per file name. A DLL in `plugins\` wins against one of the same name from the Workshop; among several copies in the Workshop the one with the smallest path wins. RMM shows a single entry for them and names in the "Notes" card which file is loaded and which ones are passed over.

**Your values stay separate from the package.** The INI next to a Workshop DLL is the author's original and is never changed. Your personal values are in `tesmioloader\user_config\<name>.ini`, and at game start they take priority over the original. That needs the `tesmioloader.dll` RMM brings; with another build RMM warns that your values do not reach plugins by other authors.

**Lists such as resources, needs and deposits** always live in `tesmioloader\plugins\<name>.ini`, even when the DLL comes from the Workshop – that is where these plugins read their list. If the file does not exist yet, RMM creates it: for a replacement plugin from the list of its original, otherwise from the package's INI. The source stays unchanged.

**Order.** Plugins load by the name of their DLL from A to Z, wherever it lies. The finished list is on the page [Before the game starts](#-before-the-game-starts).

**Another Workshop folder.** Normally the loader and RMM both look in your game's Steam Workshop folder. If you set another folder in `rmm.ini`, the loader has to know the same one (`workshop_path` under `[tesmioloader]` in `tesmioloader.ini`). Otherwise RMM shows plugins the game never loads; a red box tells you.

---

## 🔁 Replacement plugins and dependencies

**Replacement plugins.** Some plugins replace another one: they do the same job, fix bugs or can do more. The two must not run together. RMM knows these pairs:

| Replacement | replaces |
|---|---|
| Resources Plus | Resources |
| Needs Plus | Needs |
| Deposits Plus | Deposits |

- When you switch one of the two on, RMM switches the other one off on saving. Both entries stay in the list, you can switch back at any time.
- When both are switched on, for example right after subscribing, the original glows orange ("Being replaced"). At the next start through RMM the replacement wins and the original is switched off. If you prefer the original, switch the replacement off first.
- When you start the game without RMM, these replacement plugins take care themselves: as long as their original is loaded, they stay idle.

A package names its original with `replaces` in its manifest. For plugins without that entry RMM keeps the list `settings_schemas\replacements.ini`.

**Dependencies.** A package can say that it needs another one (Research Expansion needs Localization, for example). The "Notes" card shows whether what it needs is there and switched on. A replacement plugin also counts for its original: whoever needs Resources is happy with Resources Plus. Saving always works; when something is missing at the start, RMM offers to switch the plugin concerned off.

---

## 📦 Content packages (resources, deposits, buildings)

Some Workshop packages ship no DLL, only content: new resources, deposits, needs or buildings as INI sections, plus files such as icons and models. You recognise them in the list because their page has no settings, just a switch called "Provide in the game".

Switching on and saving does three things:

- The entries go into the INIs of the plugins in charge: resources to **Resources Plus**, deposits to **Deposits Plus**, needs to **Needs Plus**, buildings to **Buildings Plus**. If you run the original (Resources, Deposits, Needs) instead of a Plus plugin, RMM uses its INI.
- The files that came with the package land under `tesmioloader\vfs`, where the game reads them instead of its own.
- For deposits RMM assigns the type number itself, so it can never clash with another deposit. A number once assigned stays.

In the editors the entries appear as originals with a lock: you can override their values for yourself, but you cannot delete the entry, only hide it.

**Switching off.** When you switch a package off again, RMM takes its entries and files out with the next save. If the package brings resources, RMM asks first: a save made while these resources existed can no longer be loaded afterwards – even if it never used them. Switch the package on again and the save loads again. Buildings that Buildings Plus generated from the package are moved out of `media_soviet\workshop_wip` with the same save and kept in the state folder of the package. Otherwise the game would go on loading them and crash at start when their resource is missing. After switching on again, Buildings Plus writes them anew at the next game start, under the same number.

**Names in your language.** A package can bring names per game language for its resources; RMM writes them along. A German game then shows "Kabel" instead of "Cable", for example. Resources Plus 0.1.4 or newer can show them; with an older version or the original Resources the English name stays.

**You win a collision.** If a package brings an id your game already has — your own resource, an entry from another package, or one from the plugin's shipped INI — the package is **not provided at all**: no entries, no files in the vfs. The package page shows a red message naming the id that is in the way. Take it out first — one of your own entries in RMM, an original entry straight in `plugins\<plugin>.ini` — and switch the package on again. Nothing of yours is ever replaced quietly, and a file that was in the vfs before the package is neither overwritten nor removed later anyway.

Switching off and saving takes entries and files out again. Your own entries and your overrides stay. If the package changes in the Workshop, the yellow "Update" mark appears in the list; saving once takes over the new state.

If the loader finds no matching plugin for a kind of content, the package page says so (for example "No matching plugin is set up for buildings") and skips that part.

**Rule sets for game buildings.** A package can also bring rule sets for **Vanilla Buildings**, for example an extra storage for its resource in a building of the game. They arrive like the buildings do: as originals on the page of Vanilla Buildings, where you can switch each one on and off.

**Vehicle production.** Some packages can make their resource a material for vehicle production. The package page then has a card "Vehicle production" with two choices:

- **ON:** every vehicle factory needs this resource as well, or it builds nothing. This takes the plugin **Vehicle Materials**, installed and switched on.
- **OFF:** vehicles are built as before.

RMM never decides this for you. As long as you have neither chosen ON nor OFF and saved, nothing happens, and RMM asks before every game start so that you cannot miss it. The same goes when ON is chosen but Vehicle Materials is missing or switched off: the card and "Before the game starts" then say what is in the way.

With ON, RMM enters the material with the package's values in the list on the page of **Vehicle Materials** and switches the plugin on if it was off. There it is an entry like any other: you can change the values for road, rail, ship, aircraft and rocket parts, and your values stay after an update of the package. If an update brings a value for a class your entry does not have yet (the rocket parts, for example), RMM adds only that one. OFF takes the entry out again, just like the trash button on the page of Vehicle Materials. If you delete the entry there yourself, the card "Vehicle production" of the package shows OFF afterwards. If you had entered the material yourself before, the card shows ON from the start and your values stay. Your own entry is yours: neither OFF nor switching the package off takes it out, and the card says so in a note. Switching the whole package off takes out only the entry the package made itself.

**The storage in the vehicle factories.** A vehicle factory can only accept an extra material when it has a storage for it. A package can bring that storage along: with ON a rule set of the package then appears on the page of **Vanilla Buildings** and gives the game's vehicle factories the storage; with OFF it is gone again. The card tells you whether the package does that. Three things to know:

- Only factories you build **afterwards** have the storage. Factories that already stand do not get it and build nothing any more with ON.
- If Vanilla Buildings is missing or switched off, RMM says so on the card, in "Before the game starts" and before every start.
- Vehicle factories from the Workshop are not covered. RMM leaves out the target files of a DLC you do not own.

---

## ✅ Before the game starts

The clipboard icon in the sidebar opens a page that sums up what the next game start will do:

- **Overview:** how many plugins will load, how many are switched off, how many packages carry an unsaved update, when the game last ran and how many saved games were found.
- **What stands in the way:** missing or switched-off dependencies, refused packages, waiting updates, a plugin and its replacement that are both switched on, DLLs that exist more than once — and dependencies that load **too late**. A click on a row closes the window and shows the entry it belongs to.
- **Building lines without a resource:** a `$STORAGE`, `$PRODUCTION` or `$CONSUMPTION` line in Vanilla Buildings or Buildings Plus that names a resource no plugin publishes. The building folders in `media_soviet\workshop_wip` are checked as well, because the game loads every one of them. The game crashes while reading the buildings with it; RMM names plugin, section and resource here and asks once more on "Save + Start".
- **Rule sets that do not work everywhere:** a switched-on rule set of Vanilla Buildings with a command that does not fit every target file. It changes nothing for those buildings; the game starts all the same. RMM names the rule set, the number of buildings and the first file it misses.
- **Load order:** the list in the order the loader works through it: by the name of the DLL, whether it lies in `plugins\` or in the Workshop folder. Every entry says where it comes from. That matters when one plugin needs a service of another: the service only exists once its provider has loaded.

---

## 💾 Saved games

RMM reads `tesmioloader.save.ini`, the file the loader puts beside every saved game. It lists the plugins that were loaded and the resources and deposits the world knows. Nothing is ever written there.

Two things come out of it:

- The page of a plugin or content package carries a line **"Used by … saved games"** with their names.
- **Switching something off** asks first and names exactly those saves. "No" leaves the switch where it was.

---

## 📜 The log window

The log icon in the sidebar opens a window with three kinds of sources: the journal of this RMM session, `tesmioloader.log` from the loader folder, and every plugin log `tesmioloader.<plugin>.log`, whether it lies in the `logs\` subfolder or directly in the loader folder. The files can be read while the game runs; "Refresh" reads again, "Open folder" shows the loader folder in Explorer.

"Search" filters by text, "Sender" by the first word of a line (`plugin`, `hook`, a plugin name), "Problems and warnings only" hides everything unremarkable. Error lines are red, warnings yellow. The footer names lines, hits and the loader's last closing line.

---


## ⚙️ The settings window

The sliders icon in the sidebar opens the settings of RMM itself — not those of the selected plugin:

- **Show the TesmioLauncher window:** OFF starts the game right away, ON shows the launcher first.
- **Watch after the start:** how many seconds RMM checks whether the game stays up. 0 closes at once.
- **Keep the window open:** ON leaves RMM on screen after the start; you close it yourself.
- **Language** of the interface, the same as the `DE` button next to it.
- **Warn about an unknown game version:** ON tells you when your game is not the version the plugins were built for.
- **Messages from the game:** a switch with four positions, how much the game may write into `tesmioloader.log`. Left to right: off, errors only, errors and warnings, everything. The line under the switch says what the chosen position means.
- **Show repeats:** how often the same message is written out before it is only counted. The tally is at the end of the log. 0 writes out every repeat.
- **Details for a bug report:** puts versions, folders, the plugins that were found and the state of Steam into the clipboard.

The two values of the **Log** card belong to the loader, not to RMM: they go into `[tesmioloader]` of `tesmioloader.ini` and apply from the next game start. RMM changes only those two lines there; comments and every other key stay. The card appears only when that file exists.

The other values take effect at once and are saved when the window closes — in the place where RMM remembers everything, not in `rmm.ini`. Where your value differs from what `rmm.ini` says, a small line under the field tells you the shipped default.

**Start over.** At the bottom of the settings window sit two clearly separated buttons:

- **Delete RMM data** (amber) clears only what RMM remembers about itself: profiles, restore points and the remembered window state. Your plugins, their settings and your saved games are not touched.
- **Take everything back** (red) additionally takes back everything RMM ever wrote into the loader folder: your personal values, files in the `vfs` and the switches in `tesmioloader.ini`; the protected original INIs are written back.

Before the red button RMM shows what you lose — **including the names of the saved games** that build on the packages involved. After that you have to type the word `DELETE` before the final button becomes clickable at all. The focus sits on Cancel everywhere, so the Enter key cannot break anything.

Beforehand RMM copies every INI file to `tesmioloader\rmm_reset_backup\<timestamp>`. Not touched: the game folder with your saved games, your Workshop subscriptions, the TesmioLoader itself and everything RMM never wrote. To get rid of the program as well, run `Uninstall-RMM.bat` from the Workshop package.

---
## 🗂️ Profiles and restore points

The archive icon in the sidebar opens the window "Profiles and restore" with two tabs.

**Profiles** save the complete settings state under a name: `tesmioloader.ini`, all INIs in `plugins\` and `user_config\`, and the backed-up originals. DLLs are never part of it. "Save current state…" asks for a name and a note. On the right you see per file whether it matches the saved state. "Apply profile" writes everything back; INIs of plugins the profile does not know stay. Profiles live under `user_config\.autoload\profiles\`.

**Restore points** are created on every save: the state of all touched files from before the save, exactly one per plugin. "Reset to this point" writes these files back; the current state becomes the new point, so you can go back again. Points whose files are missing are only listed.

Both actions need a closed game and a closed TesmioLauncher.

---

## 📝 List editors

Some plugins manage lists instead of single values. RMM shows them as a list on the left and fields on the right. This applies to Resources, Needs and Deposits by MaxLegend, to their replacement plugins, to Buildings Plus and to packages that bring such a page (Research Expansion, Technical Service Storage, Vanilla Buildings, UI Layout Fixes, for example).

**Common to all:**

- Entries from the plugin or package carry a lock. You can change them but not delete them; the red button hides them, and "Hidden original entries" shows them again.
- You create your own entries with "+". The dialog asks only for what is needed and suggests what it can; everything else you set on the right afterwards. Own entries can be deleted.
- Identifiers that must name something in the game (a resource, a research entry, a building file, a text id) are checked by RMM before saving. If one is wrong, the footer turns red and names entry and field.
- A yellow warning reminds you when a change affects the save game, such as new resources or deposits that a running game then needs.

**Resources:** the list of all resources of the plugin. **Resources Plus** has the same page for `plugins\resources_plus.ini`, the file of the fixed fork. The two are a replacement pair: only one of them ever runs, both entries stay in the list. Template resource, transport class and material family are choice lists with the names from the game. The INI has no off switch; you switch the plugin off with "Plugin active". With Resources Plus the properties of a resource end with the fields "Names per game language": what you enter there for German is what a German game shows; an empty field leaves the display name in charge. That works for originals and for resources from content packages too. An original resource you do not want at all is hidden with the red bin next to its name: it is then missing from the list the game reads, but stays in the original file and can be shown again below the list under "Hidden original entries". Warning: a saved game remembers every resource by name. A save made while this resource existed can no longer be loaded afterwards – the game crashes while loading, even if the save never used the resource. Show it again and the save loads again. The same goes for removing a resource of your own or switching off a content package with resources. Settings of other plugins that name this resource are cleaned by RMM with the same save, and it tells you beforehand which ones. If a building still names the resource, RMM does not hide or remove it and shows you which one: a section of Buildings Plus or Vanilla Buildings, or a folder in `media_soviet\workshop_wip`. Without the resource the game would crash at start as soon as it loads that building. Take the building out first, then the resource. **Needs Plus** is the same kind of fork for needs: the same page as Needs, for `plugins\needs_plus.ini`.

**Needs:** the list of needs with donor, factor, shop category, chance and dissatisfaction. Eight entries at most, 16 with Needs Plus. The "General" tab carries the plugin's switches.

**Deposits:** the "General" tab with the plugin switches and the "Deposits" tab with one entry per deposit: identifier, type number, map, slot on the map, icon and more. The dialog suggests the next free type number; a duplicate is refused. **Deposits Plus** takes over once the deposits you added to the original's `deposits.ini` yourself: they then stand in the list as your own deposits, with the same type number and the same token, and a notice names them. If a number or a token is already in use, the deposit stays out and is named.

**Entries that did not come from RMM:** for plugins whose original is the INI from the package (Deposits Plus, Research Expansion, Technical Service Storage, UI Layout Fixes, Vanilla Buildings), RMM rewrites the file in the loader folder with every save. If it holds an entry that neither the package nor RMM knows, added by hand or created by the plugin itself, RMM takes it over as your own entry instead of overwriting it. This covers whole entries; a value of an original entry that was changed by hand is not taken over.

**Buildings Plus:** a "General" tab with the plugin switches, a "Buildings" tab with your own declarations - and the "Generated buildings" tab, see below.

The "Buildings" tab is a list with one row per building: switch, name, number, object folder and donor, on the right the state and the buttons **Change...**, **Open** and the bin. The state says what happens at the next game start - whether the building is in the game, is still to be written, will be rebuilt, or whether something is in the way: a donor that is not on your machine, or a number that already belongs to somebody else's folder. Either of those otherwise stops the plugin at game start and is only mentioned in the log.

Behind "Change..." are the details (number, donor, object name, name in the game, description, life) and a second tab for the lines. There the donor's `building.ini` is on the left and the list of your **commands** on the right. A command touches exactly one place: you click a line on the left, type the new line below and choose **Append** (to the end of the file), **Replace**, **Before** or **After**; **Remove line** beside the selected line takes it out. The list then shows the command as, for example, `Insert 1 | $STORAGE_FUEL ... | $STORAGE_IMPORT_SPECIAL ...`. Everything you do not touch stays as the donor has it - a new storage does not throw the others out. Lines that belong together (a connection with its points, a goods display with its placement) are picked and typed as one block. The view switches to **Result** - the file that will really be written at the next start, with the number of every storage for `$RESOURCE_VISUALIZATION` and a warning when water or sewage sits in front of a shown storage. While a command is red the plugin does not write the building. A building that is still written the older way with `line` rows opens the earlier window: there your lines stand beside the donor's, struck through where they remove something from the donor.

Below the two lists you see what a command is made of. **Selected line** shows the line you clicked above - read only; it is the anchor. **New line / lines** is where you type what is to stand there. You can write several lines below each other (separate them with Enter): each becomes a command of its own. **Append** adds them to the end in order; **Before**, **After** and **Replace** put the first one at the picked place and the others right below it. In Buildings Plus, lines that do not start with `$` (points, position) belong to the line above. A double-click copies the clicked line down when you only want to change a value. To change a command that already exists, click it in the list **Commands** on the right - its line then stands below -, change it and click **Change**. The command stays where it is, and a command that hangs on its line follows. Every resource belongs to a storage class (gravel, cement, open, covered ...). When you write a storage line with a class the resource does not have - industrial salt in `RESOURCE_TRANSPORT_GRAVEL`, for example - the window warns you in orange and names the class that fits: such a storage would hold 0 tons in the game. The buttons sit below. A button is pale while something it needs is missing, and the line underneath tells you what that is. The lines window of a Vanilla Buildings rule set is built the same way and has the same buttons; there you also have several target files, and connections have commands of their own. If a command does not fit every target file, a red line above the lists says so, and the files concerned are marked red with an `x` in the choice at the top – the rule set changes nothing in the game for them. The list of rule sets then says beside the name, for example, "Only works for 1 of 20 buildings".

**Package editors** bring their own tabs, lists and help texts. Some have buttons that open the package's guides, picker windows for buildings, research entries or game texts, picture previews or a tab for translations.

---

## 🏗️ Generated buildings

A building from a content package is written out as a complete Workshop item at game start, into `media_soviet\workshop_wip\<number>\` - by Buildings Plus. It looks like a subscription, but it is your own file on your own disk. When you remove or hide a building on the page of Buildings Plus, RMM moves its generated folder away with the same save (to `tesmioloader\user_config\.autoload\parked_buildings`), so the game stops loading it. When the section comes back, Buildings Plus writes the folder anew at the next game start, under the same number.

The "Generated buildings" tab of Buildings Plus shows name, number, object folder and origin per folder. Two things it reports on its own:

- **Owner missing.** With `$OWNER_ID 0` in the `workshopconfig.ini` the game reports missing Workshop items **every single time** a saved game is loaded. The "Fill in the owner" button puts your Steam id in - exactly that one number changes, every other byte of the file stays as it was.
- **Stale.** When no package declares a folder any more, RMM names the folders. Delete them once no saved game uses the buildings.

With Buildings Plus switched on it fills the missing number in at game start anyway - the button is for when you have it off. Nothing else is changed, not even the `tesmioloader.stamp` in the folder.

### Changing a generated building

"Change..." opens the building's `building.ini`. On the left the lines the generator writes, on the right **your changes**: replace a line, remove a line, add a line. A `*` on the left marks every line something of yours is attached to.

Clicking a line puts the **whole block** into the text box - a token together with the lines that belong to it (`$RESOURCE_VISUALIZATION` with its `position`, `rotation`, `scale`). That is not just convenient: a line like `rotation 0.0` often occurs three times in such a file, and RMM never changes the wrong one on a guess. As a block it is unique, and only that makes it editable at all.

What matters is **what** gets stored: not the changed file, but your changes. That sounds like hair-splitting until the day it counts - when the package gets an update the generator rewrites the file, and RMM applies your changes to the **new** version. Whatever the author improved in the meantime is kept; a stored copy would have thrown it away. If a line no longer matches after the update (gone, or there twice now), RMM says so and leaves that change out instead of quietly doing nothing.

That happens the moment you open the tab - so before you start the game. A blue line tells you it did.

If you changed the file **by hand**, outside RMM, it notices (the file is not the one it wrote) and leaves it alone. Your stored changes then sit idle until you hit "Apply" in the window.

"Reset everything" in the window drops your changes and restores the generator's version. "Undo everything" in the settings window does the same for every building at once.

---

## 🎮 Game version and rmm.ini

On start RMM reads the build stamp of `SOVIET64.exe`. If it belongs to no game version the plugins are made for, a warning appears and "Save + Start" asks before starting. Known is 1.2.0.9. A new stamp goes into `settings_schemas\game_versions.ini` under `[supported]`.

`rmm.ini` next to `rmm.exe` has a few switches, all explained in the file:

| Key | Meaning |
|---|---|
| `[paths] workshop_root` | Folder with the subscribed packages. Without the line (or with a `;` in front) RMM uses your game's Steam Workshop folder. An empty value is not allowed. The loader has to look in the same folder. |
| `[settings] language` | `auto` (German on a German Windows, otherwise English), `de` or `en`. |
| `[settings] version_check` | 0 turns the game version warning off. |
| `[settings] tesmiolauncher_window` | 1 shows the TesmioLauncher window on "Save + Start". |
| `[settings] launch_watch_seconds` | How long RMM watches after "Save + Start" whether the game stays up. Default 15, 0 turns it off. |
| `[settings] keep_open` | 1 leaves the RMM window open after "Save + Start"; you close it yourself. Default 0. |

What you set in the window takes priority over the file.
**Saving without the window.** For maintenance from the command line:

```bash
rmm.exe --save --build "<game>\tesmioloader" --package "<folder of the plugin or package>"
```

This is the same path as the Save button, with the same checks: the game and the TesmioLauncher must be closed and every value valid. Any question the window would ask is declined instead of answered. The answer is one line beginning with PASS or FAIL.

`--activate` moves the header switch before the save:

```bash
rmm.exe --activate on --save --build "<game>\tesmioloader" --package "<folder>"
```

`on` switches it on, `off` off. It is the same click as in the window, only without a mouse: on a content package that switch reads "Provide in the game", on a plugin "Plugin active". Nothing is written until `--save` runs; without `--save` the option is refused.

The choice on the card "Vehicle production" of a content package is made with `--vehicles`, again only together with `--save`:

```bash
rmm.exe --vehicles on --save --build "<game>\tesmioloader" --package "<folder>"
```

**Check the Steam login.** The game is started directly here and not through Steam, so it needs a logged-in Steam client. Whether one is there:

```bash
rmm.exe --steam-check
```

The answer is one line with PASS or FAIL plus what RMM found in Steam's registry entry. The same check runs before "Save + Start"; it only warns when Steam has no signed-in player on record or no client is running at all. If RMM cannot look, it says nothing and starts.

---

## 📦 Where your files are

```
SovietRepublic\tesmioloader\
├── tesmioloader.dll, tesmiolauncher.exe   the loader and its launcher
├── tesmioloader.ini                    switches of the plugins ([plugins])
├── rmm.exe, rmm.ini                    RMM and its base settings
├── settings_schemas\                   settings pages for plugins without a page of their own
├── plugins\                            local plugin DLLs, their INIs and the lists of the list editors
├── vfs\                                files from content packages
├── user_config\
│   ├── <name>.ini                      your personal values per plugin
│   └── .autoload\
│       ├── <name>.upstream.ini         backed-up originals
│       ├── backups\<package>\previous\ restore points
│       └── profiles\                   your profiles
└── logs\                               logs of the plugins

steamapps\workshop\content\784150\      your subscribed packages; RMM never writes here
```

Window size, language and selection are remembered under `%LOCALAPPDATA%\RepublicModManager`.

---

## 🛠️ When something does not work

| What you see | What is behind it | What helps |
|---|---|---|
| Red box "Missing: …" | The loader does not find the plugin this one needs | Subscribe to the missing package. Before the start RMM offers to switch the plugin concerned off |
| Yellow box "This DLL exists more than once" | The same DLL lies in several places, the loader loads only one | Remove the copy you do not want, usually the one in `plugins\` |
| Red box about the Workshop folder | RMM and the loader look in different folders | Remove the line `workshop_root` in `rmm.ini` or enter the same folder as the loader uses |
| Orange dot "Being replaced" | A plugin and its replacement are both switched on | Switch one of the two off; otherwise RMM switches the original off at the start |
| "Not loaded on the last game start" | The plugin was off on the last start, or the game did not run since you switched it on | Save and start the game once |
| Save does not work | The game or the TesmioLauncher is running, or a value is out of range | Close the programs, read the red note in the footer |
| A value is different again after saving | The plugin read another INI than expected | Check the status line: where are the files? Open the log window |
| A value of a Workshop plugin has no effect in the game | The `tesmioloader.dll` is not the one from the RMM package, or the plugin reads its INI itself and does not see your personal values | Use the `tesmioloader.dll` from the RMM package. If that does not help: copy the plugin's DLL and INI to `tesmioloader\plugins`, then RMM writes the finished INI next to the DLL (Steam updates no longer reach that copy by themselves) |

For a bug report: right-click the message, "Copy text", plus the log window with "Problems and warnings only".

---

## 🧩 For plugin authors

How a Workshop package has to be built so that RMM shows it, and how a settings page with tabs, cards, fields, lists and translations is described, is in `SCHEMA_EN.md` (German: `SCHEMA_DE.md`).

---

**Attention, comrade:** this program was written with the help of an artificial intelligence. The five-year plans behind it were still drawn up, tested and sworn at by a human every time the game crashed. If you do not want AI in your code, just stick to the base game. No hard feelings, no re-education.

**GNU GPL v3.** Source: https://github.com/Kespri/WRSR-MODS/tree/main/tools/republic_mod_manager
