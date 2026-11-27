# Republic Mod Manager - installer of the Steam Workshop package.
#
# Copies rmm.exe, its settings schemas and the guides from
# "Manual Installation\tesmioloader" into the loader folder of the game (the folder
# with tesmioloader.dll, normally <game>\tesmioloader) and can create a desktop
# shortcut. It also replaces tesmioloader.dll with the corrected build that ships in
# this package (b0.4.6-rmm.1, source in the GitHub repository); the previous file goes
# into the backup folder. The launcher (tesmiolauncher.exe) and tesmioloader.ini must
# already be there - the loader itself comes from MaxLegend's Workshop item.
#
# Resources Plus, Needs Plus and Buildings Plus are NOT copied: they lie in the
# folder "plugins" of this Workshop item, and the loader loads them from there.
# Copies of them that an earlier version of this installer put into the loader's
# plugins folder would win over the Workshop files, so they are moved into the backup.
#
# -Uninstall removes what this script installed and leaves the loader DLL, rmm.ini
# and your personal settings (user_config, the lists under plugins) alone. It also
# works after the loader itself was removed.
#
# Start it through Install-RMM.bat (double-click). Options for the console:
#   -GamePath "C:\...\SovietRepublic"   game folder when it is not found automatically
#   -Shortcut / -NoShortcut             desktop shortcut without asking
#   -Uninstall                          remove the installed files
param(
    [string]$GamePath = '',
    [switch]$Uninstall,
    [switch]$Shortcut,
    [switch]$NoShortcut
)
$ErrorActionPreference = 'Stop'
try { [Console]::OutputEncoding = [Text.Encoding]::UTF8 } catch { }

$german = $false
try { $german = (Get-UICulture).TwoLetterISOLanguageName -eq 'de' } catch { }
function T([string]$de, [string]$en) { if ($german) { return $de } else { return $en } }
function Step([string]$text) { Write-Host ('  ' + $text) }
function Ok([string]$text) { Write-Host ('  [OK] ' + $text) -ForegroundColor Green }
function Note([string]$text) { Write-Host ('  [--] ' + $text) -ForegroundColor DarkGray }
function Fail([string]$what, [string]$why) {
    Write-Host ''
    Write-Host ((T 'FEHLER: ' 'ERROR: ') + $what) -ForegroundColor Red
    Write-Host ((T 'Grund:  ' 'Reason: ') + $why) -ForegroundColor Yellow
    exit 1
}
function Hash([string]$path) { return (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash }
function Interactive() {
    try { return ([Environment]::UserInteractive -and -not [Console]::IsInputRedirected) } catch { return $false }
}

# ---------------------------------------------------------------- package
$source = Join-Path $PSScriptRoot 'Manual Installation\tesmioloader'
if (-not (Test-Path -LiteralPath (Join-Path $source 'rmm.exe') -PathType Leaf)) {
    Fail (T 'Das Paket ist unvollständig.' 'The package is incomplete.') `
         (T ('rmm.exe fehlt unter ' + $source + '. Bitte das Workshop-Objekt in Steam einmal abbestellen und neu abonnieren.') `
            ('rmm.exe is missing under ' + $source + '. Please unsubscribe and resubscribe the Workshop item in Steam.'))
}
$files = Get-ChildItem -LiteralPath $source -Recurse -File | ForEach-Object { $_.FullName.Substring($source.Length + 1) }
# Files with the player's own values: written once, never overwritten by an update.
$keep = @('rmm.ini')
# What earlier versions of this installer put into the loader folder and this one no
# longer ships there: moved into the backup, never deleted. The INIs of the three Plus
# plugins stay where they are - they hold the player's lists.
$retired = @('plugins\workshop_bridge.dll', 'plugins\workshop_bridge.ini', 'settings_schemas\workshop_bridge.launcher.ini',
             'plugins\buildings_plus.dll', 'plugins\resources_plus.dll', 'plugins\needs_plus.dll',
             'plugins\resources_plus.template.ini', 'plugins\needs_plus.template.ini')

# ---------------------------------------------------------------- game folder
function SteamLibraries() {
    $result = @()
    try {
        $steam = (Get-ItemProperty -Path 'HKCU:\Software\Valve\Steam' -ErrorAction Stop).SteamPath
        if ($steam) {
            $steam = $steam.Replace('/', '\')
            $result += $steam
            $vdf = Join-Path $steam 'steamapps\libraryfolders.vdf'
            if (Test-Path -LiteralPath $vdf) {
                foreach ($line in Get-Content -LiteralPath $vdf) {
                    if ($line -match '^\s*"path"\s+"(.+)"\s*$') { $result += $Matches[1].Replace('\\', '\') }
                }
            }
        }
    } catch { }
    return $result
}
$game = ''
if ($GamePath -ne '') {
    $game = $GamePath.TrimEnd('\')
} else {
    # The Workshop item lives in <library>\steamapps\workshop\content\784150\<item>;
    # the game in <library>\steamapps\common\SovietRepublic.
    $steamapps = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..\..\..'))
    $candidate = Join-Path $steamapps 'common\SovietRepublic'
    if (Test-Path -LiteralPath (Join-Path $candidate 'SOVIET64.exe') -PathType Leaf) { $game = $candidate }
    if ($game -eq '') {
        foreach ($library in SteamLibraries) {
            $candidate = Join-Path $library 'steamapps\common\SovietRepublic'
            if (Test-Path -LiteralPath (Join-Path $candidate 'SOVIET64.exe') -PathType Leaf) { $game = $candidate; break }
        }
    }
    if ($game -eq '' -and (Interactive)) {
        Write-Host (T 'Der Spielordner wurde nicht gefunden.' 'The game folder was not found.') -ForegroundColor Yellow
        $game = (Read-Host (T 'Bitte den Ordner mit SOVIET64.exe eingeben (z. B. C:\Program Files (x86)\Steam\steamapps\common\SovietRepublic)' `
                              'Please enter the folder with SOVIET64.exe (e.g. C:\Program Files (x86)\Steam\steamapps\common\SovietRepublic)')).Trim().Trim('"').TrimEnd('\')
    }
}
if ($game -eq '' -or -not (Test-Path -LiteralPath (Join-Path $game 'SOVIET64.exe') -PathType Leaf)) {
    Fail (T 'Der Spielordner wurde nicht gefunden.' 'The game folder was not found.') `
         (T ('Unter "' + $game + '" liegt keine SOVIET64.exe. Starte Install-RMM.bat aus dem abonnierten Workshop-Ordner oder gib den Ordner an: Install-RMM.bat -GamePath "C:\...\SovietRepublic".') `
            ('There is no SOVIET64.exe under "' + $game + '". Run Install-RMM.bat from the subscribed Workshop folder or pass the folder: Install-RMM.bat -GamePath "C:\...\SovietRepublic".'))
}
Ok ((T 'Spielordner: ' 'Game folder: ') + $game)

# ---------------------------------------------------------------- loader
# The loader folder is where tesmioloader.dll lies: <game>\tesmioloader since loader
# b0.3.7, <game>\tesmioloader\build in older installations, or the game folder itself.
$build = ''
foreach ($candidate in @((Join-Path $game 'tesmioloader'), (Join-Path $game 'tesmioloader\build'), $game)) {
    if ((Test-Path -LiteralPath (Join-Path $candidate 'tesmioloader.dll') -PathType Leaf) -and
        (Test-Path -LiteralPath (Join-Path $candidate 'tesmiolauncher.exe') -PathType Leaf) -and
        (Test-Path -LiteralPath (Join-Path $candidate 'tesmioloader.ini') -PathType Leaf)) { $build = $candidate; break }
}
function RemoveShortcut() {
    $lnk = Join-Path ([Environment]::GetFolderPath('Desktop')) 'Republic Mod Manager.lnk'
    if (Test-Path -LiteralPath $lnk) { Remove-Item -LiteralPath $lnk -Force; Ok (T 'Desktop-Verknüpfung entfernt.' 'Desktop shortcut removed.') }
}
$loaderFound = ($build -ne '')
if (-not $loaderFound -and $Uninstall) {
    # Uninstalling must also work after the loader was removed: then the folder is
    # wherever rmm.exe was installed.
    foreach ($candidate in @((Join-Path $game 'tesmioloader'), (Join-Path $game 'tesmioloader\build'), $game)) {
        if (Test-Path -LiteralPath (Join-Path $candidate 'rmm.exe') -PathType Leaf) { $build = $candidate; break }
    }
    if ($build -eq '') {
        RemoveShortcut
        Write-Host ''
        Write-Host (T 'Republic Mod Manager ist in diesem Spielordner nicht installiert - es gibt nichts zu entfernen.' 'Republic Mod Manager is not installed in this game folder - there is nothing to remove.') -ForegroundColor Green
        exit 0
    }
    Ok ((T 'Republic Mod Manager gefunden: ' 'Republic Mod Manager found: ') + $build)
}
if ($build -eq '') {
    Fail (T 'Der TesmioLoader ist nicht installiert.' 'The TesmioLoader is not installed.') `
         (T ('Unter ' + $game + '\tesmioloader fehlen tesmioloader.dll, tesmiolauncher.exe oder tesmioloader.ini. Republic Mod Manager braucht den TesmioLoader von MaxLegend ab b0.4.6: im Steam-Workshop abonnieren und nach seiner Anleitung installieren (Quelle: https://github.com/MaxLegend/TesmioLoader). Danach Install-RMM.bat erneut starten.') `
            ('tesmioloader.dll, tesmiolauncher.exe or tesmioloader.ini are missing under ' + $game + '\tesmioloader. Republic Mod Manager needs the TesmioLoader by MaxLegend, b0.4.6 or newer: subscribe to it in the Steam Workshop and install it as its guide says (source: https://github.com/MaxLegend/TesmioLoader). Then run Install-RMM.bat again.'))
}
if ($loaderFound) { Ok ((T 'TesmioLoader gefunden: ' 'TesmioLoader found: ') + $build) }
$loaderIni = Join-Path $build 'tesmioloader.ini'
# Replaced files are saved beside the loader folder when that is the old "build", inside
# it otherwise.
$backupRoot = if ((Split-Path -Leaf $build) -ieq 'build') { Split-Path -Parent $build } else { $build }

# ---------------------------------------------------------------- running programs
$running = @(Get-Process -Name rmm, tesmiolauncher, SOVIET64 -ErrorAction SilentlyContinue)
if ($running.Count -gt 0) {
    Fail (T 'Das Spiel oder der Republic Mod Manager läuft noch.' 'The game or Republic Mod Manager is still running.') `
         (T ('Bitte zuerst beenden: ' + (($running | ForEach-Object { $_.ProcessName } | Sort-Object -Unique) -join ', ') + '. Dateien, die gerade benutzt werden, lassen sich nicht ersetzen.') `
            ('Please close first: ' + (($running | ForEach-Object { $_.ProcessName } | Sort-Object -Unique) -join ', ') + '. Files in use cannot be replaced.'))
}

# ================================================================ uninstall
if ($Uninstall) {
    Write-Host (T 'Entferne Republic Mod Manager ...' 'Removing Republic Mod Manager ...')
    $removed = 0
    foreach ($rel in $files) {
        $target = Join-Path $build $rel
        # The loader DLL stays: without it the game has no loader at all. A file the
        # installer replaced lies in rmm_install_backup\<time>; there is none when the
        # loader already was this build.
        if ($rel -ieq 'tesmioloader.dll') {
            if (Test-Path -LiteralPath $target -PathType Leaf) {
                $saved = $null
                $backups = Join-Path $backupRoot 'rmm_install_backup'
                if (Test-Path -LiteralPath $backups) {
                    $saved = Get-ChildItem -LiteralPath $backups -Directory | Sort-Object Name |
                             Where-Object { Test-Path -LiteralPath (Join-Path $_.FullName 'tesmioloader.dll') -PathType Leaf } | Select-Object -First 1
                }
                if ($saved) { Note ((T 'tesmioloader.dll bleibt (die Fassung aus diesem Paket). Die Datei, die vorher dort lag, findest du unter ' 'tesmioloader.dll stays (the build from this package). The file that was there before is under ') + $saved.FullName) }
                else { Note (T 'tesmioloader.dll bleibt (die Fassung aus diesem Paket).' 'tesmioloader.dll stays (the build from this package).') }
            }
            continue
        }
        # rmm.ini holds the player's own settings of Republic Mod Manager.
        if ($keep -contains $rel) {
            if (Test-Path -LiteralPath $target -PathType Leaf) { Note ((T 'bleibt (deine Einstellungen): ' 'stays (your settings): ') + $rel) }
            continue
        }
        if (Test-Path -LiteralPath $target -PathType Leaf) { Remove-Item -LiteralPath $target -Force; $removed++ }
    }
    foreach ($dir in @('settings_schemas\languages', 'settings_schemas')) {
        $d = Join-Path $build $dir
        if ((Test-Path -LiteralPath $d) -and -not (Get-ChildItem -LiteralPath $d -Force | Select-Object -First 1)) { Remove-Item -LiteralPath $d -Force }
    }
    Ok ((T 'Dateien entfernt: ' 'Files removed: ') + $removed)
    RemoveShortcut
    Note (T 'Resources Plus, Needs Plus und Buildings Plus liegen in diesem Workshop-Objekt und laden weiter, solange du es abonniert hast. Abbestellen entfernt sie; ausschalten kannst du sie im TesmioLauncher.' 'Resources Plus, Needs Plus and Buildings Plus lie in this Workshop item and keep loading while you are subscribed. Unsubscribing removes them; you can switch them off in TesmioLauncher.')
    Note (T ('Deine Listen und persönlichen Einstellungen unter ' + $build + '\plugins und \user_config bleiben erhalten, ebenso rmm.ini und der Ordner rmm_install_backup.') ('Your lists and personal settings under ' + $build + '\plugins and \user_config are kept, and so are rmm.ini and the folder rmm_install_backup.'))
    Write-Host ''
    Write-Host (T 'Republic Mod Manager wurde entfernt.' 'Republic Mod Manager has been removed.') -ForegroundColor Green
    exit 0
}

# ================================================================ install
Write-Host (T 'Installiere Republic Mod Manager ...' 'Installing Republic Mod Manager ...')
$stamp = Get-Date -Format 'yyyyMMdd_HHmmss'
$backup =Join-Path $backupRoot ('rmm_install_backup\' + $stamp)
$copied = 0; $kept = 0; $replaced = 0
foreach ($rel in $files) {
    $from = Join-Path $source $rel
    $to = Join-Path $build $rel
    $exists = Test-Path -LiteralPath $to -PathType Leaf
    if ($exists -and ($keep -contains $rel)) { $kept++; Note ((T 'behalten: ' 'kept: ') + $rel); continue }
    if ($exists -and (Hash $from) -eq (Hash $to)) { Note ((T 'unverändert: ' 'unchanged: ') + $rel); continue }
    $parent = Split-Path -Parent $to
    if (-not (Test-Path -LiteralPath $parent)) { New-Item -ItemType Directory -Path $parent -Force | Out-Null }
    if ($exists) {
        $bparent = Split-Path -Parent (Join-Path $backup $rel)
        if (-not (Test-Path -LiteralPath $bparent)) { New-Item -ItemType Directory -Path $bparent -Force | Out-Null }
        Copy-Item -LiteralPath $to -Destination (Join-Path $backup $rel) -Force
        $replaced++
    }
    try {
        Copy-Item -LiteralPath $from -Destination $to -Force
    } catch {
        Fail ((T 'Datei konnte nicht kopiert werden: ' 'File could not be copied: ') + $rel) `
             ((T 'Windows meldet: ' 'Windows says: ') + $_.Exception.Message + (T ' Läuft das Spiel noch, oder fehlen Schreibrechte im Spielordner?' ' Is the game still running, or is the game folder write-protected?'))
    }
    if ((Hash $from) -ne (Hash $to)) {
        Fail ((T 'Datei ist nach dem Kopieren nicht identisch: ' 'File differs after copying: ') + $rel) `
             (T 'Der Kopiervorgang wurde gestört (Virenscanner, volle Platte?). Bitte erneut starten.' 'The copy was disturbed (antivirus, full disk?). Please run again.')
    }
    $copied++
}
Ok ((T 'Dateien kopiert: ' 'Files copied: ') + $copied + (T ', behalten: ' ', kept: ') + $kept + (T ', ersetzt: ' ', replaced: ') + $replaced)
Note (T 'tesmioloader.dll ist jetzt die Fassung aus diesem Paket (b0.4.6-rmm.1, gleiche API wie MaxLegends b0.4.6).' 'tesmioloader.dll is now the build from this package (b0.4.6-rmm.1, same API as the b0.4.6 by MaxLegend).')

# Leftovers of an earlier installation: the Workshop Bridge, and copies of the Plus
# plugins that would win over the files in this Workshop item and never get an update.
$moved = 0
# The Plus copies only go when the loader can reach the ones in this item: the script must
# run from the subscribed Workshop folder and the DLL must really be in the package.
$inWorkshop = $PSScriptRoot -match '\\workshop\\content\\784150\\'
foreach ($rel in $retired) {
    $old = Join-Path $build $rel
    if (-not (Test-Path -LiteralPath $old -PathType Leaf)) { continue }
    if ($rel -match '^plugins\\(buildings_plus|resources_plus|needs_plus)\.') {
        $name = $Matches[1]
        if (-not $inWorkshop -or -not (Test-Path -LiteralPath (Join-Path $PSScriptRoot ('plugins\' + $name + '\' + $name + '.dll')) -PathType Leaf)) {
            Note ((T 'bleibt liegen (dieses Paket liegt nicht im Workshop-Ordner): ' 'left in place (this package is not in the Workshop folder): ') + $rel); continue
        }
    }
    $bparent = Split-Path -Parent (Join-Path $backup $rel)
    if (-not (Test-Path -LiteralPath $bparent)) { New-Item -ItemType Directory -Path $bparent -Force | Out-Null }
    try { Move-Item -LiteralPath $old -Destination (Join-Path $backup $rel) -Force; $moved++; Note ((T 'in die Sicherung verschoben: ' 'moved into the backup: ') + $rel) }
    catch { Note ((T 'konnte nicht verschoben werden: ' 'could not be moved: ') + $rel + ' - ' + $_.Exception.Message) }
}
if ($moved -gt 0) { Ok (T 'Ältere Kopien entfernt: Resources Plus, Needs Plus und Buildings Plus lädt der TesmioLoader jetzt direkt aus diesem Workshop-Objekt; die Workshop Bridge wird nicht mehr gebraucht.' 'Older copies removed: the TesmioLoader now loads Resources Plus, Needs Plus and Buildings Plus straight from this Workshop item; the Workshop Bridge is no longer needed.') }
if ($replaced -gt 0 -or $moved -gt 0) { Note ((T 'Sicherung der ersetzten Dateien: ' 'Backup of the replaced files: ') + $backup) }

# ---------------------------------------------------------------- shortcut
$want = $false
if ($Shortcut) { $want = $true }
elseif ($NoShortcut) { $want = $false }
elseif (Interactive) {
    $answer = Read-Host (T 'Verknüpfung zu rmm.exe auf dem Desktop anlegen? (J/N)' 'Create a shortcut to rmm.exe on the desktop? (Y/N)')
    $want = ($answer.Trim().ToLowerInvariant() -in @('j', 'ja', 'y', 'yes'))
}
if ($want) {
    try {
        $lnk = Join-Path ([Environment]::GetFolderPath('Desktop')) 'Republic Mod Manager.lnk'
        $shell = New-Object -ComObject WScript.Shell
        $sc = $shell.CreateShortcut($lnk)
        $sc.TargetPath = Join-Path $build 'rmm.exe'
        $sc.WorkingDirectory = $build
        $sc.IconLocation = (Join-Path $build 'rmm.exe') + ',0'
        $sc.Description = 'Republic Mod Manager'
        $sc.Save()
        Ok ((T 'Desktop-Verknüpfung angelegt: ' 'Desktop shortcut created: ') + $lnk)
    } catch {
        Note ((T 'Desktop-Verknüpfung konnte nicht angelegt werden: ' 'Desktop shortcut could not be created: ') + $_.Exception.Message)
    }
}

Write-Host ''
Write-Host (T 'Republic Mod Manager ist installiert.' 'Republic Mod Manager is installed.') -ForegroundColor Green
Write-Host ((T 'Starten: ' 'Start: ') + (Join-Path $build 'rmm.exe'))
Write-Host (T 'Resources Plus, Needs Plus und Buildings Plus lädt der TesmioLoader direkt aus diesem Workshop-Objekt.' 'The TesmioLoader loads Resources Plus, Needs Plus and Buildings Plus straight from this Workshop item.')
Write-Host (T 'Nach einem Update des Workshop-Objekts einfach Install-RMM.bat erneut ausführen.' 'After an update of the Workshop item just run Install-RMM.bat again.')
exit 0
