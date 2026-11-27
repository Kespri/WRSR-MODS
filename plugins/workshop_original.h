// workshop_original.h - shared by Resources Plus and Needs Plus (GPL v3, Kespri).
//
// The INI of the original plugin when the loader takes its DLL from a Workshop
// item. The loader finds plugin DLLs in <loader>\plugins and, in any depth,
// below the Workshop folder; a DLL in plugins\ wins, and of several in the
// Workshop the one with the smallest path. The Workshop folder is
// tesmioloader.ini [tesmioloader] workshop_path when that is set, otherwise
// <...>\steamapps\workshop\content\784150, found by walking up from the loader
// folder. Republic Mod Manager seeds the list file by the same rule (ForkSeed).
// `out` stays empty when the original is not there, lies in plugins\ (then its
// INI is plugins\<ini>, which the caller has looked at already), or has no INI
// beside it.
//
// Both plugins carried this code as a copy of its own until the code review of
// 2026-11-19 asked for one place; the behaviour is unchanged.
#pragma once
#include <windows.h>
#include <stdio.h>
#include <string.h>

static bool WorkshopOriginalFileExists(const char* path)
{
    DWORD a = GetFileAttributesA(path);
    return a != INVALID_FILE_ATTRIBUTES && !(a & FILE_ATTRIBUTE_DIRECTORY);
}

static void WorkshopOriginalSmallestDll(const char* dir, int depth, const char* dllName, char* best, size_t bestSize)
{
    char pattern[MAX_PATH];
    if (_snprintf_s(pattern, sizeof(pattern), _TRUNCATE, "%s\\*", dir) < 0) return;
    WIN32_FIND_DATAA fd;
    HANDLE h = FindFirstFileA(pattern, &fd);
    if (h == INVALID_HANDLE_VALUE) return;
    do
    {
        if (fd.dwFileAttributes & FILE_ATTRIBUTE_REPARSE_POINT) continue;
        char path[MAX_PATH];
        if (_snprintf_s(path, sizeof(path), _TRUNCATE, "%s\\%s", dir, fd.cFileName) < 0) continue;
        if (fd.dwFileAttributes & FILE_ATTRIBUTE_DIRECTORY)
        {
            if (depth < 8 && strcmp(fd.cFileName, ".") != 0 && strcmp(fd.cFileName, "..") != 0)
                WorkshopOriginalSmallestDll(path, depth + 1, dllName, best, bestSize);
            continue;
        }
        if (_stricmp(fd.cFileName, dllName) != 0) continue;
        if (!best[0] || strcmp(path, best) < 0) strncpy_s(best, bestSize, path, _TRUNCATE);
    } while (FindNextFileA(h, &fd));
    FindClose(h);
}

// baseDir: the loader folder (tesmioloader.ini and plugins\ lie in it).
static void WorkshopOriginalIni(const char* baseDir, const char* key, char* out, size_t outSize)
{
    out[0] = 0;
    char dllName[64], local[MAX_PATH];
    _snprintf_s(dllName, sizeof(dllName), _TRUNCATE, "%s.dll", key);
    _snprintf_s(local, sizeof(local), _TRUNCATE, "%s\\plugins\\%s", baseDir, dllName);
    if (WorkshopOriginalFileExists(local)) return;       // the loader takes that one

    char root[MAX_PATH] = "", loaderIni[MAX_PATH];
    _snprintf_s(loaderIni, sizeof(loaderIni), _TRUNCATE, "%s\\tesmioloader.ini", baseDir);
    GetPrivateProfileStringA("tesmioloader", "workshop_path", "", root, sizeof(root), loaderIni);
    if (!root[0])
    {
        char dir[MAX_PATH];
        strncpy_s(dir, sizeof(dir), baseDir, _TRUNCATE);
        for (;;)
        {
            char* slash = strrchr(dir, '\\');
            if (_stricmp(slash ? slash + 1 : dir, "steamapps") == 0)
            {
                _snprintf_s(root, sizeof(root), _TRUNCATE, "%s\\workshop\\content\\784150", dir);
                break;
            }
            if (!slash) break;
            *slash = 0;
        }
    }
    if (!root[0]) return;

    char dll[MAX_PATH] = "";
    WorkshopOriginalSmallestDll(root, 0, dllName, dll, sizeof(dll));
    size_t len = strlen(dll);
    if (len < 5) return;
    char ini[MAX_PATH];
    strncpy_s(ini, sizeof(ini), dll, _TRUNCATE);
    memcpy(ini + len - 3, "ini", 3);
    if (WorkshopOriginalFileExists(ini)) strncpy_s(out, outSize, ini, _TRUNCATE);
}
