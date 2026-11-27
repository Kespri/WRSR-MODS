// vehicle_materials.cpp - Vehicle Materials for WRSR.
//
// The plugin adds explicitly configured mod resources to the material list of
// newly produced vehicles. resources.ini remains the authoritative registry;
// vehicle_materials.ini only selects resources and their coefficients.
// The game files are never modified and configuration changes need no rebuild.
// The production lines get the consumption entry and the storage each material needs
// from the plugin as well, in memory (see "factories" below).
//
// Configuration follows the rule shared by every plugin in my_plugins
// (tesmio_config.h): the base INI is <loader>\plugins\vehicle_materials.ini if
// it exists, otherwise the INI beside this DLL (the Workshop package under SML
// or the Workshop Bridge). <loader>\user_config\vehicle_materials.ini, written
// by Tesmio Settings, is laid over it key by key. Both files go through the
// same strict reader; the merged result is validated as one configuration.
//
// Supported game build:
//   version     WRSR 1.2.0.9
//   SHA256      91a2ff66fd02e02a51f38f4d2106ccb56af9510af9a6fb7e7b93ae4249358ea7
//   timestamp   0x6B05A574
//   image size  0xAE9000
//
// Verified hook and helpers:
//   vehicle requirement builder RVA 0x419EC0  (0x3F8C50 in 1.1.1.13)
//   requirement vector           vehicle + 0x8618  (+0x85C8 in 1.1.1.13)
//   merge helper                 RVA 0x2BD870  (0x2A1840 in 1.1.1.13)
//   ResourceGet                  RVA 0x2C6380  (0x2AA7B0 in 1.1.1.13)

#include "../../src/tesmio_plugin.h"
#include "../tesmio_config.h"

#include <errno.h>
#include <float.h>
#include <limits.h>
#include <math.h>
#include <stdint.h>
#include <stdarg.h>
#include <stdio.h>
#include <string.h>

#include <algorithm>
#include <exception>
#include <map>
#include <vector>
#include <set>
#include <sstream>
#include <string>

#define PLUGIN_INI      "vehicle_materials.ini"
#define PLUGIN_OVERLAY  "user_config\\vehicle_materials.ini"
#define PLUGIN_VERSION  "0.4.17"
#define PLUGIN_LOG_NAME "tesmioloader.vehicle_materials.log"

static const size_t MAX_INI_BYTES = 1u * 1024u * 1024u;
static const size_t MAX_CONFIG_VALUE = 63u;
static const int MAX_MATERIALS = 32;
static const int MAX_NAME = 64;
static const int MAX_DEBUG_LIMIT = 10000;
static const float MAX_COEFFICIENT = 1000000.0f;

static const DWORD RVA_VEHICLE_MATERIAL_BUILDER = 0x00419EC0;
static const DWORD RVA_MERGE_REQUIREMENTS        = 0x002BD870;
static const DWORD RVA_RESOURCE_GET              = 0x002C6380;
static const DWORD RVA_RESOURCE_MANAGER          = 0x00A1EF20;

static const DWORD EXPECTED_TIMESTAMP = 0x6B05A574;
static const size_t EXPECTED_IMAGE_SIZE = 0xAE9000;

// Vehicle object fields, read from the builder itself. v1.2.0.9; the three behind
// the type moved by 0x50 (0x85C8, 0x867C before), the type from 0x294.
#define VEHICLE_TYPE          0x2B8
#define VEHICLE_REQUIREMENTS  0x8618
#define VEHICLE_SCALE         0x86CC
// std::vector of the horses that pull the vehicle (one per $HORSE line of its script.ini, 0x110
// bytes each); the game's own requirement builder (0x419EC0) asks the same vector.
#define VEHICLE_HORSES        0x7CC8

static const unsigned char EXPECTED_PROLOGUE[15] =
{
    0x48,0x8B,0xC4,
    0x55,
    0x41,0x56,
    0x41,0x57,
    0x48,0x8B,0xEC,
    0x48,0x83,0xEC,0x70
};

struct MaterialEntry
{
    void* resource;
    float amount0;
    float amount1;
};

struct EntryVector
{
    MaterialEntry* begin;
    MaterialEntry* end;
    MaterialEntry* capacity;
};

typedef void (__fastcall *VehicleMaterialBuilderFn)(void* rcx, void* vehicle);
typedef void (__fastcall *MergeRequirementsFn)(void* dstVector,
                                               unsigned char useAmount1,
                                               EntryVector* srcVector,
                                               unsigned char srcUseAmount1);
typedef void* (__fastcall *ResourceGetFn)(void* resourceManager,
                                         const char* name);

enum Category
{
    CAT_ROAD = 0,
    CAT_RAIL = 1,
    CAT_SHIP = 2,
    CAT_AIRPLANE = 3,
    CAT_ROCKET = 4,
    CAT_COUNT = 5
};

struct ConfigMaterial
{
    char name[MAX_NAME];
    float coeff[CAT_COUNT];
};

struct PluginConfig
{
    int enabled;
    int debug;
    int debugLimit;
    ConfigMaterial materials[MAX_MATERIALS];
    int materialCount;
    int typeOverride[16];
    int factoryStorages;
    int factoryCapacity;
};

struct IniValue
{
    std::string text;
    size_t line;
    const char* origin;     // PLUGIN_INI or PLUGIN_OVERLAY, for messages
};

typedef std::map<std::string, IniValue> IniSection;
typedef std::map<std::string, IniSection> IniData;

static VehicleMaterialBuilderFn g_original;
static MergeRequirementsFn g_merge;
static ResourceGetFn g_resourceGet;
static const TsmResourceApi* g_resources;

static ConfigMaterial g_materials[MAX_MATERIALS];
static int g_materialCount;
static int g_typeOverride[16];
static int g_enabled = 1;
static int g_debug;
static int g_debugLimit = 80;
static int g_factoryStorages = 1;
// Loaded only to take storages out of factories again: no material is active.
static bool g_cleanupOnly;
// The materials this plugin ever gave a factory a storage for (a small file in the loader
// folder). Only such a storage is taken out again.
static char g_factoryGiven[64][MAX_NAME];
static int g_factoryGivenCount;
static bool g_factoryGivenDirty;
static std::string g_factoryGivenPath;
static float g_factoryCapacity = 50.0f;

static HANDLE g_detail = INVALID_HANDLE_VALUE;
static bool g_bound;
static volatile LONG g_initialized;
static volatile LONG g_runtimeEnabled;
static volatile LONG g_faultLogged;
static volatile LONG g_debugCount;
static volatile LONG g_resourceManagerErrorLogged;
static volatile LONG g_vectorErrorLogged;
static volatile LONG g_logWarnings;
static volatile LONG g_logErrors;
static volatile LONG g_logFatals;
static ULONGLONG g_phaseStarted;
static LONG g_phaseWarnings;
static LONG g_phaseErrors;
static LONG g_phaseFatals;
static DWORD g_lastWindowsError;
static std::string g_iniPath;

// ---------------------------------------------------------------- diagnostics

static void DetailWriteLine(const char* text)
{
    if (g_detail == INVALID_HANDLE_VALUE || !text) return;

    if (g_bound) EnterCriticalSection(&g_lock);

    SYSTEMTIME t;
    GetLocalTime(&t);
    char stamp[64];
    _snprintf_s(stamp, sizeof(stamp), _TRUNCATE,
        "[%04u-%02u-%02u %02u:%02u:%02u.%03u] ",
        t.wYear, t.wMonth, t.wDay, t.wHour, t.wMinute, t.wSecond,
        t.wMilliseconds);
    DWORD put = 0;
    WriteFile(g_detail, stamp, (DWORD)strlen(stamp), &put, NULL);
    WriteFile(g_detail, text, (DWORD)strlen(text), &put, NULL);
    WriteFile(g_detail, "\r\n", 2, &put, NULL);

    if (g_bound) LeaveCriticalSection(&g_lock);
}

static void CountLevel(const char* level)
{
    if (!level) return;
    if (_stricmp(level, "WARN") == 0) InterlockedIncrement(&g_logWarnings);
    else if (_stricmp(level, "ERROR") == 0) InterlockedIncrement(&g_logErrors);
    else if (_stricmp(level, "FATAL") == 0) InterlockedIncrement(&g_logFatals);
}

static void ReportV(const char* level, const char* where, const char* rule,
    const char* fmt, va_list ap)
{
    char body[3072];
    _vsnprintf_s(body, sizeof(body), _TRUNCATE, fmt, ap);
    char full[4096];
    _snprintf_s(full, sizeof(full), _TRUNCATE, "%s: %s [%s] %s",
        level, where && where[0] ? where : "-",
        rule && rule[0] ? rule : "general", body);
    CountLevel(level);
    if (H) Logf("vehicle_materials  %s", full);
    DetailWriteLine(full);
}

static void Report(const char* level, const char* where, const char* rule,
    const char* fmt, ...)
{
    va_list ap;
    va_start(ap, fmt);
    ReportV(level, where, rule, fmt, ap);
    va_end(ap);
}

static void Info(const char* fmt, ...)
{
    va_list ap;
    va_start(ap, fmt);
    ReportV("INFO", NULL, "status", fmt, ap);
    va_end(ap);
}

static void Debug(const char* fmt, ...)
{
    if (!g_debug) return;
    va_list ap;
    va_start(ap, fmt);
    ReportV("DEBUG", NULL, "diagnostic", fmt, ap);
    va_end(ap);
}

static void ReportWindows(const char* level, const char* where,
    const char* rule, const char* action, DWORD error, const char* remedy)
{
    char systemText[512] = {};
    DWORD n = FormatMessageA(FORMAT_MESSAGE_FROM_SYSTEM |
        FORMAT_MESSAGE_IGNORE_INSERTS, NULL, error, 0,
        systemText, (DWORD)sizeof(systemText), NULL);
    while (n && (systemText[n - 1] == '\r' || systemText[n - 1] == '\n' ||
                 systemText[n - 1] == ' ' || systemText[n - 1] == '.'))
        systemText[--n] = 0;
    if (!n) strcpy_s(systemText, sizeof(systemText), "Unknown Windows error");
    Report(level, where, rule, "%s (Windows error %lu: %s). Action: %s",
        action, error, systemText,
        remedy ? remedy : "Review the preceding context and retry");
}

static LONG AtomicRead(volatile LONG* value)
{
    return InterlockedExchangeAdd(value, 0);
}

static void BeginLogPhase()
{
    g_phaseStarted = GetTickCount64();
    g_phaseWarnings = AtomicRead(&g_logWarnings);
    g_phaseErrors = AtomicRead(&g_logErrors);
    g_phaseFatals = AtomicRead(&g_logFatals);
}

static void LogSummaryStatus(const char* phase, const char* status)
{
    ULONGLONG elapsed = g_phaseStarted ? GetTickCount64() - g_phaseStarted : 0;
    Info("%s %s after %llu ms; %ld warning(s), %ld error(s), %ld fatal error(s)",
        phase, status, (unsigned long long)elapsed,
        AtomicRead(&g_logWarnings) - g_phaseWarnings,
        AtomicRead(&g_logErrors) - g_phaseErrors,
        AtomicRead(&g_logFatals) - g_phaseFatals);
}

static void LogSummary(const char* phase, bool success)
{
    LogSummaryStatus(phase, success ? "completed successfully" : "failed");
}

// ---------------------------------------------------------------- config

static std::string TrimString(const std::string& input)
{
    size_t begin = 0;
    while (begin < input.size() &&
           (input[begin] == ' ' || input[begin] == '\t' ||
            input[begin] == '\r' || input[begin] == '\n'))
        ++begin;
    size_t end = input.size();
    while (end > begin &&
           (input[end - 1] == ' ' || input[end - 1] == '\t' ||
            input[end - 1] == '\r' || input[end - 1] == '\n'))
        --end;
    return input.substr(begin, end - begin);
}

static std::string LowerAscii(std::string value)
{
    for (size_t i = 0; i < value.size(); ++i)
        if (value[i] >= 'A' && value[i] <= 'Z')
            value[i] = (char)(value[i] - 'A' + 'a');
    return value;
}

static bool IsOnlyDashes(const std::string& value)
{
    if (value.empty()) return false;
    for (size_t i = 0; i < value.size(); ++i)
        if (value[i] != '-') return false;
    return true;
}

static bool IsAbsoluteWindowsPath(const std::string& path)
{
    if (path.size() >= 4 && path[0] == '\\' && path[1] == '\\' &&
        (path[2] == '?' || path[2] == '.') && path[3] == '\\')
        return true;
    if (path.size() >= 3 &&
        ((path[0] >= 'A' && path[0] <= 'Z') ||
         (path[0] >= 'a' && path[0] <= 'z')) &&
        path[1] == ':' && (path[2] == '\\' || path[2] == '/'))
        return true;
    return path.size() >= 2 &&
           (path[0] == '\\' || path[0] == '/') &&
           (path[1] == '\\' || path[1] == '/');
}

static std::string JoinPath(const std::string& left,
                            const std::string& right)
{
    if (left.empty()) return right;
    if (right.empty()) return left;
    char last = left[left.size() - 1];
    return (last == '\\' || last == '/') ? left + right
                                          : left + "\\" + right;
}

static bool ParseIntStrict(const std::string& text, int* output)
{
    if (!output || text.empty()) return false;
    errno = 0;
    char* end = NULL;
    long parsed = strtol(text.c_str(), &end, 10);
    if (errno == ERANGE || end == text.c_str() || !end || *end ||
        parsed < INT_MIN || parsed > INT_MAX)
        return false;
    *output = (int)parsed;
    return true;
}

static bool ParseFloatStrict(const std::string& text, float* output)
{
    if (!output || text.empty()) return false;
    errno = 0;
    char* end = NULL;
    double parsed = strtod(text.c_str(), &end);
    if (errno == ERANGE || end == text.c_str() || !end || *end ||
        !_finite(parsed) || parsed < -FLT_MAX || parsed > FLT_MAX)
        return false;
    *output = (float)parsed;
    return _finite(*output) != 0;
}

static bool IsKnownSection(const std::string& section)
{
    return section == "general" || section == "resources" ||
           section == "road" || section == "rail" ||
           section == "ship" || section == "airplane" || section == "rocket" ||
           section == "mapping";
}

static bool IsSafeResourceName(const std::string& name)
{
    if (name.empty() || name.size() >= MAX_NAME) return false;
    for (size_t i = 0; i < name.size(); ++i)
    {
        unsigned char c = (unsigned char)name[i];
        if (c < 33 || c > 126 || c == '=' || c == ';' || c == '#' ||
            c == '[' || c == ']' || c == ':' || c == '/' || c == '\\' ||
            c == '"' || c == '\'')
            return false;
    }
    return true;
}

static bool ReadTextFile(const std::string& path, std::string& output)
{
    output.clear();
    HANDLE file = CreateFileA(path.c_str(), GENERIC_READ,
        FILE_SHARE_READ | FILE_SHARE_WRITE | FILE_SHARE_DELETE,
        NULL, OPEN_EXISTING, FILE_ATTRIBUTE_NORMAL, NULL);
    if (file == INVALID_HANDLE_VALUE)
    {
        g_lastWindowsError = GetLastError();
        return false;
    }

    LARGE_INTEGER size;
    BOOL sizeOk = GetFileSizeEx(file, &size);
    if (!sizeOk || size.QuadPart < 0 ||
        (unsigned long long)size.QuadPart > MAX_INI_BYTES)
    {
        g_lastWindowsError = sizeOk ? ERROR_FILE_TOO_LARGE : GetLastError();
        CloseHandle(file);
        return false;
    }

    output.resize((size_t)size.QuadPart);
    size_t done = 0;
    while (done < output.size())
    {
        size_t remaining = output.size() - done;
        DWORD wanted = (DWORD)(remaining > (1u << 20) ? (1u << 20)
                                                        : remaining);
        DWORD received = 0;
        if (!ReadFile(file, &output[done], wanted, &received, NULL))
        {
            g_lastWindowsError = GetLastError();
            CloseHandle(file);
            output.clear();
            return false;
        }
        if (!received)
        {
            g_lastWindowsError = ERROR_HANDLE_EOF;
            CloseHandle(file);
            output.clear();
            return false;
        }
        done += received;
    }
    CloseHandle(file);
    return true;
}

static bool ValidateUtf8WithoutBom(const std::string& path,
                                   const std::string& text)
{
    if (text.size() >= 3 &&
        (unsigned char)text[0] == 0xEF &&
        (unsigned char)text[1] == 0xBB &&
        (unsigned char)text[2] == 0xBF)
    {
        Report("ERROR", path.c_str(), "utf8-bom",
            "The plugin INI must be UTF-8 without a BOM");
        return false;
    }
    if (text.empty()) return true;
    if (text.size() > (size_t)INT_MAX ||
        MultiByteToWideChar(CP_UTF8, MB_ERR_INVALID_CHARS,
            text.data(), (int)text.size(), NULL, 0) <= 0)
    {
        Report("ERROR", path.c_str(), "utf8",
            "The configuration is not valid UTF-8 without a BOM");
        return false;
    }
    return true;
}

// One file into (section, key) -> value. The base INI must carry every known
// section; the overlay may carry any subset, and its values are laid over the
// base by MergeOverlay. Sections and keys may repeat across the two files,
// never within one.
static bool ParseIniLayout(const std::string& text, IniData* output,
                           bool requireSections, const char* origin)
{
    if (!output) return false;
    output->clear();
    std::istringstream input(text);
    std::string rawLine;
    std::string currentSection;
    std::set<std::string> sections;
    size_t line = 0;

    while (std::getline(input, rawLine))
    {
        ++line;
        std::string content = TrimString(rawLine);
        if (content.empty() || content[0] == ';' || content[0] == '#' ||
            IsOnlyDashes(content))
            continue;

        if (content[0] == '[')
        {
            if (content.size() < 3 || content[content.size() - 1] != ']' ||
                content.find(']') != content.size() - 1)
            {
                Report("ERROR", origin, "malformed-section",
                    "Malformed section header at line %Iu: %s",
                    line, content.c_str());
                return false;
            }
            std::string section = LowerAscii(TrimString(
                content.substr(1, content.size() - 2)));
            if (!IsKnownSection(section))
            {
                Report("ERROR", origin, "unknown-section",
                    "Unknown section '%s' at line %Iu",
                    section.empty() ? "<empty>" : section.c_str(), line);
                return false;
            }
            if (!sections.insert(section).second)
            {
                Report("ERROR", origin, "duplicate-section",
                    "Section [%s] appears more than once (line %Iu)",
                    section.c_str(), line);
                return false;
            }
            (*output)[section] = IniSection();
            currentSection = section;
            continue;
        }

        size_t equals = content.find('=');
        if (equals == std::string::npos)
        {
            Report("ERROR", origin, "missing-assignment",
                "Expected key = value at line %Iu: %s", line,
                content.c_str());
            return false;
        }
        if (currentSection.empty())
        {
            Report("ERROR", origin, "assignment-outside-section",
                "Configuration assignment outside a section at line %Iu",
                line);
            return false;
        }

        std::string key = LowerAscii(TrimString(content.substr(0, equals)));
        std::string value = TrimString(content.substr(equals + 1));
        if (key.empty() || value.empty())
        {
            Report("ERROR", origin, "missing-value",
                "[%s] contains an empty key or value at line %Iu",
                currentSection.c_str(), line);
            return false;
        }
        if (key.size() > MAX_CONFIG_VALUE || value.size() > MAX_CONFIG_VALUE)
        {
            Report("ERROR", origin, "value-too-long",
                "[%s] key or value exceeds %Iu characters at line %Iu",
                currentSection.c_str(), MAX_CONFIG_VALUE, line);
            return false;
        }

        IniSection& target = (*output)[currentSection];
        if (target.find(key) != target.end())
        {
            Report("ERROR", origin, "duplicate-key",
                "[%s] %s appears more than once (line %Iu)",
                currentSection.c_str(), key.c_str(), line);
            return false;
        }
        IniValue iniValue = { value, line, origin };
        target[key] = iniValue;
    }

    if (!requireSections) return true;
    static const char* const required[] = {
        "general", "resources", "road", "rail", "ship", "airplane",
        "mapping"
    };
    for (size_t i = 0; i < sizeof(required) / sizeof(required[0]); ++i)
    {
        if (sections.find(required[i]) == sections.end())
        {
            Report("ERROR", origin, "missing-section",
                "Required section [%s] is missing", required[i]);
            return false;
        }
    }
    return true;
}

// Every overlay value replaces or adds the same (section, key) in the base.
// Returns how many values came from the overlay.
static int MergeOverlay(IniData* base, const IniData& overlay)
{
    int applied = 0;
    for (IniData::const_iterator s = overlay.begin(); s != overlay.end(); ++s)
    {
        IniSection& target = (*base)[s->first];
        for (IniSection::const_iterator k = s->second.begin();
             k != s->second.end(); ++k)
        {
            target[k->first] = k->second;
            ++applied;
        }
    }
    return applied;
}

static bool FromOverlay(const IniValue& value)
{
    return value.origin && strcmp(value.origin, PLUGIN_OVERLAY) == 0;
}

static const IniValue* FindIniValue(const IniData& data,
                                    const char* section,
                                    const std::string& key)
{
    IniData::const_iterator sectionIt = data.find(section);
    if (sectionIt == data.end()) return NULL;
    IniSection::const_iterator valueIt = sectionIt->second.find(key);
    return valueIt == sectionIt->second.end() ? NULL : &valueIt->second;
}

static bool ParseRequiredInt(const IniData& data, const char* section,
    const char* key, int minimum, int maximum, int* output)
{
    const IniValue* value = FindIniValue(data, section, key);
    int parsed = 0;
    if (value && ParseIntStrict(value->text, &parsed) &&
        parsed >= minimum && parsed <= maximum)
    {
        *output = parsed;
        return true;
    }
    if (!value)
    {
        Report("ERROR", PLUGIN_INI, "missing-key",
            "Required key [%s] %s is missing", section, key);
    }
    else
    {
        Report("ERROR", value->origin, "config-range",
            "[%s] %s must be an integer from %d to %d at line %Iu",
            section, key, minimum, maximum, value->line);
    }
    return false;
}

static bool ParseOptionalInt(const IniData& data, const char* section,
    const char* key, int minimum, int maximum, int* output)
{
    return !FindIniValue(data, section, key) ||
           ParseRequiredInt(data, section, key, minimum, maximum, output);
}

static bool ValidateFixedKeys(const IniSection& section,
                              const char* sectionName,
                              const std::set<std::string>& allowed)
{
    for (IniSection::const_iterator it = section.begin(); it != section.end();
         ++it)
    {
        if (allowed.find(it->first) == allowed.end())
        {
            Report("ERROR", it->second.origin, "unknown-key",
                "Unknown key '%s' in section [%s] at line %Iu",
                it->first.c_str(), sectionName, it->second.line);
            return false;
        }
    }
    return true;
}

static bool ParseCoefficient(const IniData& data, const char* section,
    const std::string& resourceKey, float* output)
{
    const IniValue* value = FindIniValue(data, section, resourceKey);
    if (!value)
    {
        *output = 0.0f;
        return true;
    }
    float parsed = 0.0f;
    if (!ParseFloatStrict(value->text, &parsed) || parsed < 0.0f ||
        parsed > MAX_COEFFICIENT)
    {
        Report("ERROR", value->origin, "coefficient-range",
            "[%s] %s must be a finite number from 0 to %.0f at line %Iu",
            section, resourceKey.c_str(), MAX_COEFFICIENT, value->line);
        return false;
    }
    *output = parsed;
    return true;
}

// Validates the merged (section, key) data as one configuration.
// `overlayListsResources` is true when the overlay carried [resources] count:
// the material list then belongs to the overlay, and a category coefficient
// the base still holds for a material no longer listed is dropped with a
// warning instead of rejecting everything.
static bool ParseConfigData(IniData& data, bool overlayListsResources,
                            PluginConfig* output)
{
    if (!output) return false;

    std::set<std::string> generalKeys;
    generalKeys.insert("enabled");
    generalKeys.insert("debug");
    generalKeys.insert("debug_limit");
    generalKeys.insert("factory_storages");
    generalKeys.insert("factory_storage_capacity");
    if (!ValidateFixedKeys(data["general"], "general", generalKeys))
        return false;

    PluginConfig parsed = {};
    for (int i = 0; i < 16; ++i) parsed.typeOverride[i] = -1;

    if (!ParseRequiredInt(data, "general", "enabled", 0, 1,
                          &parsed.enabled) ||
        !ParseRequiredInt(data, "general", "debug", 0, 1,
                          &parsed.debug) ||
        !ParseRequiredInt(data, "general", "debug_limit", 0,
                          MAX_DEBUG_LIMIT, &parsed.debugLimit))
        return false;

    // Both are optional: an INI from before them means "storages on, 50 t".
    parsed.factoryStorages = 1;
    parsed.factoryCapacity = 50;
    if (!ParseOptionalInt(data, "general", "factory_storages", 0, 1,
                          &parsed.factoryStorages) ||
        !ParseOptionalInt(data, "general", "factory_storage_capacity", 1,
                          10000, &parsed.factoryCapacity))
        return false;

    int requested = 0;
    if (!ParseRequiredInt(data, "resources", "count", 0, MAX_MATERIALS,
                          &requested))
        return false;

    std::set<std::string> resourceKeys;
    resourceKeys.insert("count");
    for (int i = 0; i < MAX_MATERIALS; ++i)
    {
        char key[32];
        _snprintf_s(key, sizeof(key), _TRUNCATE, "resource%d", i);
        resourceKeys.insert(key);
    }
    if (!ValidateFixedKeys(data["resources"], "resources", resourceKeys))
        return false;

    std::string resourceNames[MAX_MATERIALS];
    std::string resourceCanonical[MAX_MATERIALS];
    std::set<std::string> uniqueResources;
    for (int i = 0; i < requested; ++i)
    {
        char key[32];
        _snprintf_s(key, sizeof(key), _TRUNCATE, "resource%d", i);
        const IniValue* value = FindIniValue(data, "resources", key);
        if (!value)
        {
            Report("ERROR", PLUGIN_INI, "missing-resource",
                "[resources] %s is required because count=%d", key,
                requested);
            return false;
        }
        if (!IsSafeResourceName(value->text))
        {
            Report("ERROR", value->origin, "resource-name",
                "[resources] %s has an invalid resource name at line %Iu",
                key, value->line);
            return false;
        }
        resourceNames[i] = value->text;
        resourceCanonical[i] = LowerAscii(value->text);
        if (!uniqueResources.insert(resourceCanonical[i]).second)
        {
            Report("ERROR", value->origin, "duplicate-resource",
                "Resource '%s' is configured more than once (line %Iu)",
                value->text.c_str(), value->line);
            return false;
        }
    }

    for (int i = requested; i < MAX_MATERIALS; ++i)
    {
        char key[32];
        _snprintf_s(key, sizeof(key), _TRUNCATE, "resource%d", i);
        const IniValue* extra = FindIniValue(data, "resources", key);
        if (!extra) continue;
        if (overlayListsResources && !FromOverlay(*extra))
        {
            // The overlay shortened the list; the base entry is superseded.
            Report("WARN", PLUGIN_INI, "resource-superseded",
                "[resources] %s at line %Iu is beyond the overlay's count=%d and is ignored",
                key, extra->line, requested);
            data["resources"].erase(key);
            continue;
        }
        Report("ERROR", extra->origin, "resource-count",
            "[resources] %s is present at line %Iu but count=%d",
            key, extra->line, requested);
        return false;
    }

    // [rocket] is optional: a configuration written before the class existed has none, and a
    // material without a [rocket] value asks nothing of rocket parts.
    static const char* const categoryNames[CAT_COUNT] = {
        "road", "rail", "ship", "airplane", "rocket"
    };
    for (int c = 0; c < CAT_COUNT; ++c)
    {
        IniSection& section = data[categoryNames[c]];
        for (IniSection::iterator it = section.begin(); it != section.end();)
        {
            if (uniqueResources.find(it->first) != uniqueResources.end())
            {
                ++it;
                continue;
            }
            if (overlayListsResources && !FromOverlay(it->second))
            {
                Report("WARN", PLUGIN_INI, "resource-superseded",
                    "[%s] key '%s' at line %Iu names a material the overlay no longer lists and is ignored",
                    categoryNames[c], it->first.c_str(), it->second.line);
                section.erase(it++);
                continue;
            }
            Report("ERROR", it->second.origin, "unknown-resource",
                "[%s] key '%s' is not listed in [resources] (line %Iu)",
                categoryNames[c], it->first.c_str(), it->second.line);
            return false;
        }
    }

    for (int i = 0; i < requested; ++i)
    {
        ConfigMaterial material = {};
        strncpy_s(material.name, sizeof(material.name),
                  resourceNames[i].c_str(), _TRUNCATE);
        bool active = false;
        for (int c = 0; c < CAT_COUNT; ++c)
        {
            if (!ParseCoefficient(data, categoryNames[c],
                                  resourceCanonical[i], &material.coeff[c]))
                return false;
            if (material.coeff[c] > 0.0f) active = true;
        }
        if (!active)
        {
            Report("WARN", PLUGIN_INI, "zero-coefficient",
                "Resource '%s' has coefficient 0 in every category and is ignored",
                material.name);
            continue;
        }
        parsed.materials[parsed.materialCount++] = material;
    }

    std::set<std::string> mappingKeys;
    for (int i = 0; i < 16; ++i)
    {
        char key[32];
        _snprintf_s(key, sizeof(key), _TRUNCATE, "type%d", i);
        mappingKeys.insert(key);
    }
    if (!ValidateFixedKeys(data["mapping"], "mapping", mappingKeys))
        return false;

    for (int i = 0; i < 16; ++i)
    {
        char key[32];
        _snprintf_s(key, sizeof(key), _TRUNCATE, "type%d", i);
        const IniValue* value = FindIniValue(data, "mapping", key);
        if (!value) continue;
        int mapping = 0;
        if (!ParseIntStrict(value->text, &mapping) || mapping < -1 ||
            mapping >= CAT_COUNT)
        {
            Report("ERROR", value->origin, "mapping-range",
                "[mapping] %s must be an integer from -1 to 4 at line %Iu",
                key, value->line);
            return false;
        }
        parsed.typeOverride[i] = mapping;
    }

    *output = parsed;
    return true;
}

static bool ReadIniFile(const std::string& path, bool requireSections,
                        const char* origin, IniData* output)
{
    std::string text;
    if (!ReadTextFile(path, text))
    {
        ReportWindows("ERROR", path.c_str(), "config-read",
            "Configuration could not be read", g_lastWindowsError,
            "Verify that the file exists, is readable, and is not oversized");
        return false;
    }
    return ValidateUtf8WithoutBom(path, text) &&
           ParseIniLayout(text, output, requireSections, origin);
}

// Base INI, then the personal overlay on top, then one validation pass.
static bool LoadConfig(const std::string& basePath,
                       const std::string& overlayPath, PluginConfig* output)
{
    IniData data;
    if (!ReadIniFile(basePath, true, PLUGIN_INI, &data)) return false;

    bool overlayListsResources = false;
    if (!overlayPath.empty())
    {
        IniData overlay;
        if (!ReadIniFile(overlayPath, false, PLUGIN_OVERLAY, &overlay))
            return false;
        overlayListsResources =
            FindIniValue(overlay, "resources", "count") != NULL;
        int applied = MergeOverlay(&data, overlay);
        Info("Personal overlay applied: %d value(s) from %s", applied,
             overlayPath.c_str());
    }
    return ParseConfigData(data, overlayListsResources, output);
}

static void ApplyConfig(const PluginConfig& config)
{
    g_enabled = config.enabled;
    g_debug = config.debug;
    g_debugLimit = config.debugLimit;
    g_factoryStorages = config.factoryStorages;
    g_factoryCapacity = (float)config.factoryCapacity;
    g_materialCount = config.materialCount;
    memcpy(g_materials, config.materials, sizeof(g_materials));
    memcpy(g_typeOverride, config.typeOverride, sizeof(g_typeOverride));
}

static void LogConfig()
{
    Info("Configuration: enabled=%d debug=%d debug_limit=%d factory_storages=%d factory_storage_capacity=%.0f active_materials=%d",
        g_enabled, g_debug, g_debugLimit, g_factoryStorages, g_factoryCapacity, g_materialCount);
    for (int i = 0; i < g_materialCount; ++i)
    {
        const ConfigMaterial& m = g_materials[i];
        Debug("material=%s road=%.4f rail=%.4f ship=%.4f airplane=%.4f rocket=%.4f",
            m.name, m.coeff[CAT_ROAD], m.coeff[CAT_RAIL],
            m.coeff[CAT_SHIP], m.coeff[CAT_AIRPLANE], m.coeff[CAT_ROCKET]);
    }
}

// ---------------------------------------------------------------- game validation and helpers

static bool HostIsUsable(const TsmHost* host)
{
    if (!host) return false;
    const size_t requiredSize = offsetof(TsmHost, consume) +
                                sizeof(host->consume);
    return host->structSize >= requiredSize && host->exeModule &&
           host->exeBase && host->exeSize && host->baseDir &&
           host->baseDir[0] && host->log && host->installInlineHook &&
           host->readablePtr && host->faultFilter && host->consume;
}

static bool ReadPeIdentity(DWORD* timestamp, DWORD* imageSize)
{
    if (!timestamp || !imageSize || !g_exeBase ||
        g_exeSize < sizeof(IMAGE_DOS_HEADER) ||
        !ReadablePtr(g_exeBase, sizeof(IMAGE_DOS_HEADER)))
        return false;

    const IMAGE_DOS_HEADER* dos = (const IMAGE_DOS_HEADER*)g_exeBase;
    if (dos->e_magic != IMAGE_DOS_SIGNATURE ||
        dos->e_lfanew < (LONG)sizeof(IMAGE_DOS_HEADER))
        return false;

    size_t ntOffset = (size_t)dos->e_lfanew;
    if (ntOffset > g_exeSize ||
        g_exeSize - ntOffset < sizeof(IMAGE_NT_HEADERS64))
        return false;

    const IMAGE_NT_HEADERS64* nt =
        (const IMAGE_NT_HEADERS64*)(g_exeBase + ntOffset);
    if (!ReadablePtr(nt, sizeof(*nt)) || nt->Signature != IMAGE_NT_SIGNATURE ||
        nt->FileHeader.Machine != IMAGE_FILE_MACHINE_AMD64 ||
        nt->OptionalHeader.Magic != IMAGE_NT_OPTIONAL_HDR64_MAGIC)
        return false;

    *timestamp = nt->FileHeader.TimeDateStamp;
    *imageSize = nt->OptionalHeader.SizeOfImage;
    return true;
}

static bool RvaRangeIsValid(DWORD rva, size_t length)
{
    return (size_t)rva <= g_exeSize && length <= g_exeSize - (size_t)rva;
}

static bool BuildMatches()
{
    DWORD timestamp = 0;
    DWORD imageSize = 0;
    if (!ReadPeIdentity(&timestamp, &imageSize))
    {
        Report("FATAL", "SOVIET64.exe", "pe-identity",
            "The executable has no readable 64-bit PE identity. Action: verify the game executable and loader");
        return false;
    }
    if (g_exeSize != EXPECTED_IMAGE_SIZE || imageSize != EXPECTED_IMAGE_SIZE ||
        timestamp != EXPECTED_TIMESTAMP)
    {
        Report("FATAL", "SOVIET64.exe", "unsupported-build",
            "Build mismatch: mapped=0x%llX PE-size=0x%08X timestamp=0x%08X; expected size=0x%llX timestamp=0x%08X. Action: use WRSR 1.2.0.9",
            (unsigned long long)g_exeSize, imageSize, timestamp,
            (unsigned long long)EXPECTED_IMAGE_SIZE, EXPECTED_TIMESTAMP);
        return false;
    }
    if (!RvaRangeIsValid(RVA_VEHICLE_MATERIAL_BUILDER,
                         sizeof(EXPECTED_PROLOGUE)) ||
        !RvaRangeIsValid(RVA_MERGE_REQUIREMENTS, 16) ||
        !RvaRangeIsValid(RVA_RESOURCE_GET, 16) ||
        !RvaRangeIsValid(RVA_RESOURCE_MANAGER, sizeof(void*)))
    {
        Report("FATAL", "SOVIET64.exe", "rva-range",
            "A required game address lies outside the verified image. Action: verify plugin compatibility");
        return false;
    }

    BYTE* site = g_exeBase + RVA_VEHICLE_MATERIAL_BUILDER;
    if (!ReadablePtr(site, sizeof(EXPECTED_PROLOGUE)) ||
        memcmp(site, EXPECTED_PROLOGUE, sizeof(EXPECTED_PROLOGUE)) != 0)
    {
        Report("FATAL", "SOVIET64.exe", "builder-prologue",
            "Material-builder prologue changed. Action: verify WRSR 1.2.0.9 and check for a conflicting plugin");
        return false;
    }

    // The two helpers are called, not hooked, so no hook installer checks
    // their bytes. A matching time stamp does not prove an unchanged binary,
    // and calling into whatever else lies there would be a crash far from its
    // cause. ResourceGet may already carry another plugin's inline hook (a
    // jump over its first bytes), so only the merge helper is compared in
    // full and ResourceGet is accepted with either its own prologue or a jump.
    static const unsigned char kMergePrologue[16] =
    {
        0x48,0x89,0x5C,0x24,0x10,
        0x48,0x89,0x6C,0x24,0x18,
        0x56,
        0x41,0x56,
        0x41,0x57,
        0x48
    };
    static const unsigned char kResourceGetPrologue[12] =
    {
        0x40,0x55,
        0x57,
        0x41,0x56,
        0x48,0x8B,0xEC,
        0x48,0x83,0xEC,0x40
    };
    BYTE* merge = g_exeBase + RVA_MERGE_REQUIREMENTS;
    BYTE* get = g_exeBase + RVA_RESOURCE_GET;
    if (!ReadablePtr(merge, sizeof(kMergePrologue)) ||
        memcmp(merge, kMergePrologue, sizeof(kMergePrologue)) != 0)
    {
        Report("FATAL", "SOVIET64.exe", "merge-prologue",
            "The requirement merge helper is not where WRSR 1.2.0.9 has it. Action: verify the game version; no hook was installed");
        return false;
    }
    if (!ReadablePtr(get, sizeof(kResourceGetPrologue)) ||
        (memcmp(get, kResourceGetPrologue, sizeof(kResourceGetPrologue)) != 0 &&
         get[0] != 0xE9 && !(get[0] == 0xFF && get[1] == 0x25) && !(get[0] == 0x48 && get[1] == 0xB8)))
    {
        Report("FATAL", "SOVIET64.exe", "resourceget-prologue",
            "ResourceGet is not where WRSR 1.2.0.9 has it. Action: verify the game version; no hook was installed");
        return false;
    }
    return true;
}

static bool ResourceServiceContains(const char* name)
{
    if (!g_resources || !g_resources->count || !g_resources->name || !name)
        return false;
    int count = g_resources->count();
    if (count < 0 || count > 4096) return false;
    for (int i = 0; i < count; ++i)
    {
        const char* candidate = g_resources->name(i);
        if (candidate && _stricmp(candidate, name) == 0) return true;
    }
    return false;
}

static void* GetResource(const char* name)
{
    if (!g_resourceGet || !name || !name[0]) return NULL;
    void* manager = g_exeBase + RVA_RESOURCE_MANAGER;
    if (!ReadablePtr(manager, sizeof(void*)))
    {
        if (InterlockedCompareExchange(&g_resourceManagerErrorLogged, 1, 0) == 0)
            Report("ERROR", "SOVIET64.exe", "resource-manager",
                "Resource manager address became unreadable; custom materials are skipped for this session");
        InterlockedExchange(&g_runtimeEnabled, 0);
        return NULL;
    }
    return g_resourceGet(manager, name);
}

enum VectorResourceState
{
    VECTOR_INVALID = -1,
    VECTOR_ABSENT = 0,
    VECTOR_PRESENT = 1
};

static VectorResourceState FindResourceInVector(void* vehicle, void* resource)
{
    if (!vehicle || !resource) return VECTOR_INVALID;
    EntryVector* vector = (EntryVector*)((BYTE*)vehicle + VEHICLE_REQUIREMENTS);
    if (!ReadablePtr(vector, sizeof(*vector))) return VECTOR_INVALID;

    uintptr_t begin = (uintptr_t)vector->begin;
    uintptr_t end = (uintptr_t)vector->end;
    uintptr_t capacity = (uintptr_t)vector->capacity;
    if (!begin && !end && !capacity) return VECTOR_ABSENT;
    if (!begin || !end || !capacity || end < begin || capacity < end)
        return VECTOR_INVALID;
    size_t span = (size_t)(end - begin);
    size_t capacitySpan = (size_t)(capacity - begin);
    if (span % sizeof(MaterialEntry) != 0 ||
        capacitySpan % sizeof(MaterialEntry) != 0)
        return VECTOR_INVALID;
    size_t count = span / sizeof(MaterialEntry);
    size_t capacityCount = capacitySpan / sizeof(MaterialEntry);
    if (count > 256 || capacityCount > 4096 ||
        (count && !ReadablePtr((void*)begin, span)))
        return VECTOR_INVALID;

    const MaterialEntry* entries = (const MaterialEntry*)begin;
    for (size_t i = 0; i < count; ++i)
        if (entries[i].resource == resource) return VECTOR_PRESENT;
    return VECTOR_ABSENT;
}

static Category AutomaticCategory(int rawType)
{
    // The vehicle kind as 1.2.0.9 writes it to vehicle type +0x2B8 (0x68B070 turns the
    // $TYPE token into the number): 1 ROAD, 2 ROAD_SERVICE, 3 RAIL_VAGON, 4 RAIL_LOCOMOTIVE,
    // 5 RAIL_SERVICE, 6 SHIP, 7 CABIN, 8 AIRPLANE, 9 CONTAINER (the rocket parts of the
    // rocket DLC), 10 HELICOPTER. The rocket DLC put CABIN at 7; before it the airplane was 7.
    switch (rawType)
    {
        case 1: case 2: return CAT_ROAD;
        case 6:         return CAT_SHIP;
        case 8: case 10: return CAT_AIRPLANE;
        case 7: case 9: return CAT_ROCKET;
        default:        return CAT_RAIL;
    }
}

static Category CategoryForType(int rawType)
{
    if (rawType >= 0 && rawType < 16)
    {
        int mapping = g_typeOverride[rawType];
        if (mapping >= CAT_ROAD && mapping < CAT_COUNT)
            return (Category)mapping;
    }
    return AutomaticCategory(rawType);
}

static const char* CategoryName(Category category)
{
    switch (category)
    {
        case CAT_ROAD: return "road";
        case CAT_RAIL: return "rail";
        case CAT_SHIP: return "ship";
        case CAT_AIRPLANE: return "airplane";
        case CAT_ROCKET: return "rocket";
        default: return "unknown";
    }
}

static bool ReserveLimitedLog()
{
    if (g_debugLimit <= 0) return false;
    return InterlockedIncrement(&g_debugCount) <= g_debugLimit;
}

// ---------------------------------------------------------------- factories
//
// A vehicle factory uses a material only when two things are true for its building type:
//
//   1. the material stands in the type's consumption list. The game's storage finder -
//      the one behind deliveries and behind the production line itself - skips every
//      storage slot of a production line whose resource is neither consumed nor produced
//      by the type (mask 0x100000202C0 at SOVIET64.exe+0x448A57; type number 40). Without
//      the entry a factory shows the storage and its stock, lorries do not unload and
//      nothing is built. The game's own definitions carry "$CONSUMPTION steel 1.0" and so
//      on for exactly this reason.
//   2. the type has a storage for it.
//
// Both are given here, in memory, to every production line that takes materials (a storage
// of the CARPLANT kind): the game's, the DLCs' and the Workshop's. Nothing is written to a
// file. The consumption entry is the pair the parser makes of "$CONSUMPTION <resource> 1.0".
// The storage is what the parser makes of "$STORAGE_IMPORT_SPECIAL <class> <tons>
// <resource>": a copy, by the game's own copy constructor, of a storage the parser made of
// such a line for some other building, with the class, the capacity and the one slot set.
//
// THE STORAGE GOES TO THE END OF THE TYPE'S LIST, behind the electricity, water, sewage and
// waste storages the parser appends. A building keeps its storages in the order of its
// type, and the game counts on that. A factory that already stands has every storage of
// the type except the new ones, so appending them to the building (storage_retrofit.h,
// when a saved game is loaded; g_keepExisting - the storages it has are moved, not cloned,
// and nothing is freed) leaves no storage at another place than before - which a
// storage line in the building's file cannot do: it lands in front of the appended ones
// and shifts them.
//
// A type that already has a storage for the material - from its own file or from a rule
// set - is left as it is; it only gets the consumption entry.
#define STORAGE_RETROFIT_REPORT(level, rule, ...) Report(level, "Factory storage", rule, __VA_ARGS__)
#include "../storage_retrofit.h"

static const DWORD  RVA_BUILDING_TYPES   = 0x00A30AF0;   // std::vector<TYPE>, the objects themselves
static const size_t TYPE_SIZE            = 0x0E98;
static const size_t TYPE_NUMBER          = 0x0368;       // int; 40 = production line
static const size_t TYPE_CONSUMPTION     = 0x0898;       // std::vector<{ resource*, float, 0 }>
static const int    TYPE_PRODUCTION_LINE = 40;
// Building subtype ($SUBTYPE_* in building.ini, 1.2.0.9 parser: rbp+0x1E1C, base rbp+0x1AB0).
// The works of horse-drawn vehicles and the stud farm build nothing this plugin asks for.
static const size_t TYPE_SUBTYPE         = 0x036C;
static const int    SUBTYPE_HORSE_WAGON  = 42;
static const int    SUBTYPE_HORSE_STUD   = 43;

static bool HorseWorks(const BYTE* type)
{
    const int subtype = *(const int*)(type + TYPE_SUBTYPE);
    return subtype == SUBTYPE_HORSE_WAGON || subtype == SUBTYPE_HORSE_STUD;
}
static const int    PURPOSE_CARPLANT     = 10;           // storage +0x88
static const size_t MAX_BUILDING_TYPES   = 20000;
// Per transport class inside a resource record: the factor a storage of that class
// multiplies its capacity by, and a byte that rules the pair out.
static const size_t RES_CLASS_FACTOR     = 0x00CC;
static const size_t RES_CLASS_BLOCKED    = 0x00E8;
static const size_t RES_CLASS_STRIDE     = 0x0020;

static bool  g_factoryHelpers;                 // push, copy and destructor are the known ones
static volatile LONG g_factoryBusy;
static volatile LONG g_factoryFaulted;
static volatile LONG g_factoryEmptyLogged;
static volatile LONG g_factoryClassWarned;
static volatile LONG g_factoryRetrofitted;

// The storages this plugin appended, by type. Valid for the type list they were found in.
struct FactoryOwn { BYTE* type; int material; };
static FactoryOwn g_factoryOwn[4096];
static int   g_factoryOwnCount;
static BYTE* g_factoryTypesBegin;
static BYTE* g_factoryTypesEnd;
static bool  g_factoryScanned;
static BYTE* g_factoryWitnessType;
static void* g_factoryWitnessResource;
static BYTE* g_factoryCurrentType;

// ---- the record of what was given ------------------------------------------------------
//
// <loader>\plugins\vehicle_materials.storages.txt: one resource per line. A storage in a
// factory counts as this plugin's own only when its resource stands here; storages from a
// building's file, from a rule set or from another plugin are never touched.
static bool FactoryGiven(const char* name)
{
    for (int i = 0; i < g_factoryGivenCount; ++i)
        if (_stricmp(g_factoryGiven[i], name) == 0) return true;
    return false;
}

static void FactoryRememberGiven(const char* name)
{
    if (!name || !name[0] || FactoryGiven(name)) return;
    if (g_factoryGivenCount >= (int)_countof(g_factoryGiven)) return;
    strcpy_s(g_factoryGiven[g_factoryGivenCount++], name);
    g_factoryGivenDirty = true;
}

static void FactoryLoadGiven(const std::string& baseDir)
{
    g_factoryGivenCount = 0;
    g_factoryGivenDirty = false;
    g_factoryGivenPath = baseDir + "\\plugins\\vehicle_materials.storages.txt";
    FILE* file = nullptr;
    if (fopen_s(&file, g_factoryGivenPath.c_str(), "rb") != 0 || !file) return;
    char line[256];
    while (fgets(line, sizeof(line), file))
    {
        std::string name = TrimString(line);
        if (name.empty() || name[0] == ';' || !IsSafeResourceName(name)) continue;
        if (name.size() >= (size_t)MAX_NAME || FactoryGiven(name.c_str())) continue;
        if (g_factoryGivenCount >= (int)_countof(g_factoryGiven)) break;
        strcpy_s(g_factoryGiven[g_factoryGivenCount++], name.c_str());
    }
    fclose(file);
}

static void FactorySaveGiven()
{
    if (!g_factoryGivenDirty || g_factoryGivenPath.empty()) return;
    FILE* file = nullptr;
    if (fopen_s(&file, g_factoryGivenPath.c_str(), "wb") != 0 || !file) return;
    fputs("; Written by the plugin Vehicle Materials: the materials it gave the vehicle\r\n"
          "; factories a storage for. A storage of a material that is no longer asked for\r\n"
          "; is taken out of the factories again when it is empty. Do not edit.\r\n", file);
    for (int i = 0; i < g_factoryGivenCount; ++i)
    {
        fputs(g_factoryGiven[i], file);
        fputs("\r\n", file);
    }
    fclose(file);
    g_factoryGivenDirty = false;
}

static bool FactoryActive(const char* name)
{
    if (g_cleanupOnly) return false;
    for (int i = 0; i < g_materialCount; ++i)
        if (_stricmp(g_materials[i].name, name) == 0) return true;
    return false;
}

static bool FactoryOwns(BYTE* type, int material)
{
    for (int i = 0; i < g_factoryOwnCount; ++i)
        if (g_factoryOwn[i].type == type && g_factoryOwn[i].material == material) return true;
    return false;
}

static void FactoryRemember(BYTE* type, int material)
{
    if (FactoryOwns(type, material)) return;
    if (g_factoryOwnCount >= (int)_countof(g_factoryOwn)) return;
    g_factoryOwn[g_factoryOwnCount].type = type;
    g_factoryOwn[g_factoryOwnCount].material = material;
    ++g_factoryOwnCount;
}

static bool PairListHas(const void* address, void* resource, bool* valid)
{
    storage_retrofit::Vector list = {};
    size_t count = 0;
    *valid = storage_retrofit::ReadVector(address, 16, 4096, &list, &count);
    if (!*valid) return false;
    for (size_t i = 0; i < count; ++i)
        if (*(void**)(list.begin + i * 16) == resource) return true;
    return false;
}

static bool StoragesHold(const storage_retrofit::Vector& storages, size_t count, void* resource)
{
    using namespace storage_retrofit;
    for (size_t i = 0; i < count; ++i)
    {
        Vector slots = {};
        size_t n = 0;
        if (!ReadVector(storages.begin + i * STORAGE_SIZE, SLOT_SIZE, MAX_SLOTS_PER_STORAGE, &slots, &n)) continue;
        for (size_t k = 0; k < n; ++k)
            if (*(void**)(slots.begin + k * SLOT_SIZE) == resource) return true;
    }
    return false;
}

// What the parser makes of "$STORAGE_IMPORT_SPECIAL <covered|open|gravel> <tons> <resource>":
// one slot, import, no purpose, no further owner.
static bool IsPlainSpecialImport(BYTE* storage)
{
    using namespace storage_retrofit;
    if (!ReadablePtr(storage, STORAGE_SIZE) || storage[0x84] != 1 || storage[0x85] != 0 ||
        *(int*)(storage + 0x88) != -1 || *(BYTE**)(storage + 0xC0) != nullptr) return false;
    const int transport = *(int*)(storage + STORAGE_CLASS);
    if (transport != TRANSPORT_COVERED && transport != TRANSPORT_OPEN && transport != TRANSPORT_GRAVEL)
        return false;
    for (size_t i = 0; i < _countof(VECTOR_OFFSETS); ++i)
    {
        Vector view = {};
        size_t count = 0;
        if (!ReadVector(storage + VECTOR_OFFSETS[i], VECTOR_STRIDES[i], 4096, &view, &count)) return false;
        // The slot list has the one resource; the per-resource controls may exist for it;
        // the owner lists of a storage in use are empty in a definition.
        if (i == 0 ? count != 1 : (i == 1 ? count > 1 : (i >= 3 && count != 0))) return false;
    }
    return StorageValid(storage);
}

static BYTE* FindSpecialImportTemplate(const storage_retrofit::Vector& types, size_t typeCount)
{
    using namespace storage_retrofit;
    for (size_t t = 0; t < typeCount; ++t)
    {
        BYTE* type = types.begin + t * TYPE_SIZE;
        Vector list = {};
        size_t count = 0;
        if (!ReadVector(type + TYPE_STORAGES, STORAGE_SIZE, MAX_STORAGES, &list, &count)) continue;
        for (size_t i = 0; i < count; ++i)
            if (IsPlainSpecialImport(list.begin + i * STORAGE_SIZE)) return list.begin + i * STORAGE_SIZE;
    }
    return nullptr;
}

// The class a storage for this resource gets: open, covered or gravel, whichever the
// resource fills best. Liquids and the like get no storage here.
static bool PickStorageClass(void* resource, int* transport, float* factor)
{
    using namespace storage_retrofit;
    static const int order[] = { TRANSPORT_OPEN, TRANSPORT_COVERED, TRANSPORT_GRAVEL };
    *transport = -1;
    *factor = 0.0f;
    BYTE* record = (BYTE*)resource;
    if (!ReadablePtr(record, RESOURCE_RECORD_SIZE)) return false;
    for (size_t i = 0; i < _countof(order); ++i)
    {
        const float f = *(float*)(record + RES_CLASS_FACTOR + order[i] * RES_CLASS_STRIDE);
        const BYTE blocked = record[RES_CLASS_BLOCKED + order[i] * RES_CLASS_STRIDE];
        if (!blocked && _finite(f) && f > *factor && f < 1000000.0f)
        {
            *factor = f;
            *transport = order[i];
        }
    }
    return *transport >= 0;
}

// A private copy of the template, so that no push into a type's list can move it.
static bool TryCopyTemplate(BYTE* source, storage_retrofit::Vector* copy)
{
    using namespace storage_retrofit;
    try
    {
        g_push(copy, source);
        return copy->begin && copy->end == copy->begin + STORAGE_SIZE && CopyMatches(source, copy->begin);
    }
    catch (...) { return false; }
}

static bool TryAppendTypeStorage(BYTE* type, BYTE* templateStorage, void* resource,
                                 int transport, float factor)
{
    using namespace storage_retrofit;
    Vector temp = {};
    bool ok = false;
    try
    {
        g_push(&temp, templateStorage);
        BYTE* s = temp.begin;
        if (s && temp.end == s + STORAGE_SIZE)
        {
            BYTE* slot = *(BYTE**)s;
            *(void**)slot = resource;
            *(float*)(slot + 8) = g_factoryCapacity * factor;   // a type's slot holds capacity
            *(DWORD*)(slot + 12) = 0;
            Vector controls = {};
            size_t controlCount = 0;
            if (ReadVector(s + 0x18, SLOT_SIZE, MAX_SLOTS_PER_STORAGE, &controls, &controlCount) &&
                controlCount == 1)
                *(void**)controls.begin = resource;
            *(float*)(s + STORAGE_CAPACITY) = g_factoryCapacity;
            *(int*)(s + STORAGE_CLASS) = transport;
            Vector* list = (Vector*)(type + TYPE_STORAGES);
            Vector before = {};
            size_t had = 0;
            if (IsPlainSpecialImport(s) &&
                ReadVector(list, STORAGE_SIZE, MAX_STORAGES - 1, &before, &had))
            {
                g_push(list, s);
                Vector after = {};
                size_t has = 0;
                ok = ReadVector(list, STORAGE_SIZE, MAX_STORAGES, &after, &has) && has == had + 1 &&
                     CopyMatches(s, after.begin + had * STORAGE_SIZE);
            }
        }
    }
    catch (...) { ok = false; }
    if (temp.begin) g_destroy(&temp);
    return ok;
}

static void ScanFactoryTypes(const char* when)
{
    using namespace storage_retrofit;
    Vector types = {};
    size_t typeCount = 0;
    if (!ReadVector(g_exeBase + RVA_BUILDING_TYPES, TYPE_SIZE, MAX_BUILDING_TYPES, &types, &typeCount) ||
        !typeCount)
    {
        if (InterlockedCompareExchange(&g_factoryEmptyLogged, 1, 0) == 0)
            Info("Factories (%s): the game has no building types yet; they are looked at again when a saved game is loaded", when);
        return;
    }
    if (types.begin != g_factoryTypesBegin) g_factoryOwnCount = 0;   // another list: old pointers mean nothing

    void* resources[MAX_MATERIALS] = {};
    int   transports[MAX_MATERIALS];
    float factors[MAX_MATERIALS];
    for (int m = 0; m < g_materialCount; ++m)
    {
        transports[m] = -1;
        factors[m] = 0.0f;
        resources[m] = GetResource(g_materials[m].name);
        if (resources[m]) PickStorageClass(resources[m], &transports[m], &factors[m]);
    }

    Vector templateCopy = {};
    bool templateSearched = false;
    int lines = 0, consumption = 0, storages = 0, present = 0, failed = 0;
    g_factoryWitnessType = nullptr;
    g_factoryWitnessResource = nullptr;
    for (size_t t = 0; t < typeCount; ++t)
    {
        BYTE* type = types.begin + t * TYPE_SIZE;
        if (*(int*)(type + TYPE_NUMBER) != TYPE_PRODUCTION_LINE || HorseWorks(type)) continue;
        Vector list = {};
        size_t count = 0;
        if (!ReadVector(type + TYPE_STORAGES, STORAGE_SIZE, MAX_STORAGES, &list, &count)) continue;
        bool takesMaterials = false;
        for (size_t i = 0; i < count && !takesMaterials; ++i)
            takesMaterials = *(int*)(list.begin + i * STORAGE_SIZE + 0x88) == PURPOSE_CARPLANT;
        if (!takesMaterials) continue;
        ++lines;
        int typeConsumption = 0, typeStorages = 0;
        for (int m = 0; m < g_materialCount; ++m)
        {
            void* resource = resources[m];
            if (!resource) continue;
            bool valid = false;
            if (!PairListHas(type + TYPE_CONSUMPTION, resource, &valid))
            {
                if (!valid) { ++failed; continue; }
                MaterialEntry entry = { resource, 1.0f, 0.0f };
                EntryVector source = { &entry, &entry + 1, &entry + 1 };
                g_merge(type + TYPE_CONSUMPTION, 0, &source, 0);
                if (!PairListHas(type + TYPE_CONSUMPTION, resource, &valid)) { ++failed; continue; }
                ++consumption; ++typeConsumption;
            }
            if (!g_factoryWitnessType)
            {
                g_factoryWitnessType = type;
                g_factoryWitnessResource = resource;
            }
            if (!g_factoryStorages || !g_factoryHelpers) continue;
            // An earlier push may have moved the list.
            if (!ReadVector(type + TYPE_STORAGES, STORAGE_SIZE, MAX_STORAGES, &list, &count)) { ++failed; continue; }
            if (StoragesHold(list, count, resource)) { ++present; continue; }
            if (transports[m] < 0)
            {
                if (InterlockedCompareExchange(&g_factoryClassWarned, 1, 0) == 0)
                    Report("WARN", PLUGIN_INI, "factory-storage-class",
                        "Resource '%s' fits no covered, open or gravel storage; the factories get no storage for it. Action: give them one in their building file or with a rule set",
                        g_materials[m].name);
                ++failed;
                continue;
            }
            if (!templateSearched)
            {
                templateSearched = true;
                BYTE* found = FindSpecialImportTemplate(types, typeCount);
                if (!found || !TryCopyTemplate(found, &templateCopy))
                {
                    if (templateCopy.begin) g_destroy(&templateCopy);
                    templateCopy = {};
                    Report("WARN", "SOVIET64.exe", "factory-storage-template",
                        "No building of the game has a storage made of a $STORAGE_IMPORT_SPECIAL line to model the new storages on; the factories get no storage. Action: give them one in their building file or with a rule set");
                }
            }
            if (!templateCopy.begin) { ++failed; continue; }
            if (TryAppendTypeStorage(type, templateCopy.begin, resource, transports[m], factors[m]))
            {
                ++storages; ++typeStorages;
                FactoryRemember(type, m);
                FactoryRememberGiven(g_materials[m].name);
            }
            else ++failed;
        }
        if (g_debug && (typeConsumption || typeStorages) && ReserveLimitedLog())
            Report("DEBUG", NULL, "factory-type",
                "type=%p index=%Iu consumption_entries_added=%d storages_appended=%d storages_now=%Iu",
                type, t, typeConsumption, typeStorages,
                (size_t)(((Vector*)(type + TYPE_STORAGES))->end - ((Vector*)(type + TYPE_STORAGES))->begin) / STORAGE_SIZE);
    }
    if (templateCopy.begin) g_destroy(&templateCopy);
    FactorySaveGiven();
    g_factoryTypesBegin = types.begin;
    g_factoryTypesEnd = types.end;
    g_factoryScanned = true;
    if (consumption || storages || failed || InterlockedCompareExchange(&g_factoryEmptyLogged, 2, 0) != 2)
        Info("Factories (%s): %d production line(s) that take materials among %Iu building types; %d consumption entr(ies) and %d storage(s) of %.0f t added, %d storage(s) were there already, %d could not be added",
            when, lines, typeCount, consumption, storages, g_factoryCapacity, present, failed);
    InterlockedExchange(&g_factoryEmptyLogged, 2);
}

// Cheap unless the game's type list changed: called for every vehicle type the game builds
// at its start and before every building of a saved game.
static void EnsureFactoryTypes(const char* when)
{
    if (InterlockedCompareExchange(&g_factoryFaulted, 0, 0)) return;
    if (InterlockedCompareExchange(&g_factoryBusy, 1, 0) != 0) return;
    __try
    {
        storage_retrofit::Vector raw = {};
        if (ReadablePtr(g_exeBase + RVA_BUILDING_TYPES, sizeof(raw)))
        {
            memcpy(&raw, g_exeBase + RVA_BUILDING_TYPES, sizeof(raw));
            bool current = g_factoryScanned && raw.begin == g_factoryTypesBegin && raw.end == g_factoryTypesEnd;
            if (current && g_factoryWitnessType)
            {
                // The same block can hold types that were read again.
                bool valid = false;
                current = PairListHas(g_factoryWitnessType + TYPE_CONSUMPTION, g_factoryWitnessResource, &valid);
            }
            if (!current) ScanFactoryTypes(when);
        }
    }
    __except (FaultFilter("vehicle_materials factory types", GetExceptionInformation()))
    {
        InterlockedExchange(&g_factoryFaulted, 1);
        Report("WARN", "vehicle_materials", "factory-fault",
            "Giving the factories their consumption entries and storages faulted and is off for the rest of this session; vehicles keep their materials. Action: preserve this log and report it");
    }
    InterlockedExchange(&g_factoryBusy, 0);
}

static bool FactoryWanted(const char* resource, int, float, void*)
{
    for (int i = 0; i < g_factoryOwnCount; ++i)
        if (g_factoryOwn[i].type == g_factoryCurrentType &&
            _stricmp(g_materials[g_factoryOwn[i].material].name, resource) == 0) return true;
    return false;
}

// 1 = the building has every storage of its type in front of the appended ones, in the
// type's order; 0 = it has not; -1 = not a building to look at.
static int FactoryMatchesType(BYTE* building, BYTE* type)
{
    using namespace storage_retrofit;
    int mine = 0;
    for (int i = 0; i < g_factoryOwnCount; ++i)
        if (g_factoryOwn[i].type == type) ++mine;
    if (mine <= 0) return -1;
    if (!ReadablePtr(building + B_CONSTRUCTION_PROGRESS, sizeof(float)) ||
        !ReadablePtr(building + B_GOING_AWAY, 1)) return -1;
    const float progress = *(float*)(building + B_CONSTRUCTION_PROGRESS);
    if (!_finite(progress) || progress < 0.9999f || *(BYTE*)(building + B_GOING_AWAY)) return -1;
    Vector definition = {}, own = {};
    size_t definitionCount = 0, ownCount = 0;
    if (!ReadVector(type + TYPE_STORAGES, STORAGE_SIZE, MAX_STORAGES, &definition, &definitionCount) ||
        !ReadVector(building + B_STORAGE_BEGIN, STORAGE_SIZE, MAX_STORAGES, &own, &ownCount)) return -1;
    if ((size_t)mine > definitionCount) return -1;
    const size_t base = definitionCount - (size_t)mine;
    if (ownCount < base) return 0;
    for (size_t i = 0; i < base; ++i)
    {
        BYTE* d = definition.begin + i * STORAGE_SIZE;
        BYTE* s = own.begin + i * STORAGE_SIZE;
        if (*(int*)(d + STORAGE_CLASS) != *(int*)(s + STORAGE_CLASS) ||
            *(int*)(d + 0x88) != *(int*)(s + 0x88)) return 0;
    }
    return 1;
}

static void __cdecl FactoryBeforeLoad(void*, void*, int)
{
    EnsureFactoryTypes("saved game");
}

// The storages of materials that are no longer asked for leave a factory again
// (storage_retrofit::RemoveGone): a storage this plugin once gave - its resource is on
// record - whose material is not active any more, as soon as it is empty.
static bool FactoryGone(const char* resource, int, void*)
{
    return FactoryGiven(resource) && !FactoryActive(resource);
}

// Every storage this plugin once gave, active or not (for the works of horse-drawn vehicles).
static bool FactoryGivenAny(const char* resource, int, void*)
{
    return FactoryGiven(resource);
}

static void FactoryGiveMissing(BYTE* building, BYTE* type)
{
    const int matches = FactoryMatchesType(building, type);
    if (matches < 0) return;
    if (matches == 0)
    {
        if (ReserveLimitedLog())
            Report("WARN", NULL, "factory-differs",
                "building=%p keeps its storages in another order than its definition (it was built while a storage line stood in the definition, or another mod changed it); it is left as it is. Action: if it lacks the storage for a material, demolish it and build it again",
                building);
        return;
    }
    g_factoryCurrentType = type;
    const int added = storage_retrofit::AfterLoad(building, FactoryWanted, nullptr);
    g_factoryCurrentType = nullptr;
    if (added > 0) InterlockedExchangeAdd(&g_factoryRetrofitted, added);
}

// The storages this plugin gave follow factory_storage_capacity.
static bool FactoryResizeWanted(const char* resource, int, float, void*)
{
    return FactoryGiven(resource) && FactoryActive(resource);
}

static void __cdecl FactoryAfterLoad(void* loaded, void*, int completed)
{
    if (!completed || InterlockedCompareExchange(&g_factoryFaulted, 0, 0)) return;
    __try
    {
        BYTE* building = (BYTE*)loaded;
        if (!building || !ReadablePtr(building + storage_retrofit::B_TYPEDESC, sizeof(void*))) return;
        BYTE* type = *(BYTE**)(building + storage_retrofit::B_TYPEDESC);
        if (!type || !ReadablePtr(type, TYPE_SIZE) ||
            *(int*)(type + TYPE_NUMBER) != TYPE_PRODUCTION_LINE) return;
        // The works of horse-drawn vehicles get nothing; what an earlier version gave them leaves
        // again as soon as it is empty.
        if (HorseWorks(type))
        {
            if (g_factoryGivenCount > 0) storage_retrofit::RemoveGone(building, FactoryGivenAny, nullptr);
            return;
        }
        // First what is gone, then what is missing: a storage that leaves stands at the end,
        // and a new one is put behind everything that stays.
        if (g_factoryGivenCount > 0) storage_retrofit::RemoveGone(building, FactoryGone, nullptr);
        if (!g_cleanupOnly && g_factoryStorages && g_factoryHelpers)
            storage_retrofit::Resize(building, FactoryResizeWanted, nullptr);
        if (!g_cleanupOnly && g_factoryStorages && g_factoryHelpers) FactoryGiveMissing(building, type);
    }
    __except (FaultFilter("vehicle_materials factory retrofit", GetExceptionInformation()))
    {
        g_factoryCurrentType = nullptr;
        InterlockedExchange(&g_factoryFaulted, 1);
        Report("WARN", "vehicle_materials", "factory-fault",
            "Looking at a loaded factory faulted; the factory part is off for the rest of this session. Action: preserve this log and report it");
    }
}

static void InstallFactorySupport()
{
    const bool give = !g_cleanupOnly && g_factoryStorages != 0;
    if (!g_cleanupOnly && !g_factoryStorages)
        Info("Factories: factory_storages = 0 - the plugin enters the materials as consumption only; every factory needs a storage line of its own for each material");
    // Needed for giving a storage and for moving storages together when one leaves.
    g_factoryHelpers = storage_retrofit::Prepare();
    // A production line has a storage of vehicles among its own - in every mode.
    storage_retrofit::g_serveVehicleStorages = true;
    if (give)
    {
        if (!g_factoryHelpers)
            Report("WARN", "SOVIET64.exe", "factory-storages-unavailable",
                "The game functions for making a storage are not the known ones; the factories get the consumption entries only. Action: update the plugin for this game build, or give the factories a storage line for each material");
        // Behind everything the factory has: the order of the materials may change between two
        // sessions, the place of a storage in a building that stands may not.
        storage_retrofit::g_appendOnly = true;
        // And nothing the factory has is cloned or freed: the game keeps addresses into a
        // factory's storages (cloning them cost the factory its power and crashed the game).
        storage_retrofit::g_keepExisting = true;
    }
    const TsmBuildingLoadApi* events = nullptr;
    if (H && H->structSize >= offsetof(TsmHost, consume) + sizeof(H->consume) && H->consume)
        events = (const TsmBuildingLoadApi*)H->consume(TSM_SERVICE_BUILDING_LOAD, TSM_BUILDING_LOAD_VERSION);
    if (!events || !events->subscribe ||
        !events->subscribe(g_cleanupOnly ? nullptr : FactoryBeforeLoad, FactoryAfterLoad, nullptr, 0))
    {
        Info("Factories: this loader does not report loaded buildings (the loader from Republic Mod Manager, b0.4.5-rmm.3 or newer, does); factories that already stand are left as they are - only factories built from now on get the storages");
        return;
    }
    if (g_cleanupOnly)
        Info("Factories: storages this plugin gave for %d material(s) are taken out of the factories again, when empty, as a saved game is loaded",
            g_factoryGivenCount);
    else if (give && g_factoryHelpers)
        Info("Factories: every production line that takes materials gets a storage of %.0f t and a consumption entry for each material; factories that already stand get the storages, empty, when a saved game is loaded, and lose the empty storage of a material that is no longer listed",
            g_factoryCapacity);
    else
        Info("Factories: the empty storage of a material that is no longer listed is taken out of the factories when a saved game is loaded");
}

// ---------------------------------------------------------------- hook

static void __fastcall HookVehicleMaterials(void* rcx, void* vehicle)
{
    if (!g_original) return;

    // Native requirements are always built first and remain untouched.
    g_original(rcx, vehicle);
    if (!InterlockedExchangeAdd(&g_runtimeEnabled, 0)) return;

    // The game reads every building type before it builds its vehicle types, so this is
    // the first moment the factories can be given what the materials need.
    EnsureFactoryTypes("game start");

    LONG faulted = 0;
    __try
    {
        if (!vehicle || !ReadablePtr(vehicle, VEHICLE_SCALE + sizeof(float))) return;

        int rawType = *(int*)((BYTE*)vehicle + VEHICLE_TYPE);
        float scale = *(float*)((BYTE*)vehicle + VEHICLE_SCALE);
        if (!_finite(scale) || !(scale > 0.0f) || scale > 1000000.0f)
            return;

        // A vehicle pulled by horses is built from what the game asks for it (wood) and from
        // nothing of this plugin.
        if (ReadablePtr((BYTE*)vehicle + VEHICLE_HORSES, 2 * sizeof(void*)))
        {
            BYTE* first = *(BYTE**)((BYTE*)vehicle + VEHICLE_HORSES);
            BYTE* last = *(BYTE**)((BYTE*)vehicle + VEHICLE_HORSES + sizeof(void*));
            if (first && last > first)
            {
                if (g_debug && ReserveLimitedLog())
                    Report("DEBUG", NULL, "vehicle-horses", "type=%d pulled by horses - no custom materials", rawType);
                return;
            }
        }

        Category category = CategoryForType(rawType);
        MaterialEntry entries[MAX_MATERIALS] = {};
        int count = 0;

        for (int i = 0; i < g_materialCount; ++i)
        {
            const ConfigMaterial& material = g_materials[i];
            float coefficient = material.coeff[(int)category];
            if (!(coefficient > 0.0f)) continue;

            void* resource = GetResource(material.name);
            if (!resource)
            {
                if (!InterlockedExchangeAdd(&g_runtimeEnabled, 0)) return;
                if (ReserveLimitedLog())
                    Report("WARN", NULL, "resource-unavailable",
                        "Resource '%s' is unavailable at vehicle-build time. Action: verify resources.ini and plugin load order",
                        material.name);
                continue;
            }
            VectorResourceState state = FindResourceInVector(vehicle, resource);
            if (state == VECTOR_INVALID)
            {
                if (InterlockedCompareExchange(&g_vectorErrorLogged, 1, 0) == 0)
                    Report("ERROR", NULL, "requirement-vector",
                        "The vehicle requirement vector is invalid; custom materials are skipped for this vehicle");
                return;
            }
            if (state == VECTOR_PRESENT) continue;

            float amount = scale * coefficient;
            if (!_finite(amount) || !(amount > 0.000001f)) continue;

            entries[count].resource = resource;
            entries[count].amount0 = amount;
            entries[count].amount1 = 0.0f;
            if (++count >= MAX_MATERIALS) break;
        }

        if (!count) return;
        EntryVector source = { entries, entries + count, entries + count };
        g_merge((BYTE*)vehicle + VEHICLE_REQUIREMENTS, 0, &source, 0);

        if (g_debug && ReserveLimitedLog())
            Report("DEBUG", NULL, "vehicle-build",
                "type=%d category=%s scale=%.4f custom_materials_added=%d",
                rawType, CategoryName(category), scale, count);
    }
    __except(FaultFilter("vehicle_materials vehicle requirement hook",
                         GetExceptionInformation()))
    {
        faulted = 1;
    }

    if (faulted)
    {
        InterlockedExchange(&g_runtimeEnabled, 0);
        if (InterlockedCompareExchange(&g_faultLogged, 1, 0) == 0)
            Report("FATAL", "vehicle_materials", "runtime-fault",
                "The custom-material hook faulted and was disabled for the rest of this session; native vehicle materials remain active");
    }
}

// ---------------------------------------------------------------- plugin entry

extern "C" __declspec(dllexport) unsigned TsmPluginApiVersion(void)
{
    return TSM_API_VERSION;
}

extern "C" __declspec(dllexport) int TsmPluginInit(const TsmHost* host,
                                                   TsmPluginInfo* info)
{
    if (!info || !HostIsUsable(host)) return 1;
    TsmBind(host);
    g_bound = true;
    info->name = "vehicle_materials";
    info->version = PLUGIN_VERSION;

    BeginLogPhase();
    g_detail = TsmOpenLog(PLUGIN_LOG_NAME);
    if (g_detail == INVALID_HANDLE_VALUE)
        ReportWindows("WARN", PLUGIN_LOG_NAME, "log-open",
            "Detail log could not be opened", GetLastError(),
            "Verify write access to the TesmioLoader directory");
    Info("TesmioLoader vehicle_materials %s starting; loader API=%u; detail log: %s",
        PLUGIN_VERSION, TSM_API_VERSION, PLUGIN_LOG_NAME);

    try
    {
        std::string baseDir = g_baseDir ? g_baseDir : "";
        if (!IsAbsoluteWindowsPath(baseDir))
        {
            Report("FATAL", NULL, "base-directory",
                "TesmioLoader did not provide an absolute base directory. Action: verify the loader installation");
            LogSummary("Initialization", false);
            return 1;
        }
        // Base: <loader>\plugins\vehicle_materials.ini, else the INI beside
        // this DLL. Overlay: <loader>\user_config\vehicle_materials.ini.
        if (!TsmConfigInit("vehicle_materials"))
        {
            Report("FATAL", PLUGIN_INI, "configuration-missing",
                "No vehicle_materials.ini in %s\\plugins and none beside this DLL. Action: keep the INI beside the DLL; no hook was installed",
                baseDir.c_str());
            LogSummary("Initialization", false);
            return 1;
        }
        g_iniPath = g_tsmConfig.base;
        Info("Configuration %s: %s", g_tsmConfig.baseIsLocal ? "in the loader folder" : "beside plugin DLL", g_iniPath.c_str());
        std::string overlayPath = g_tsmConfig.hasOverlay ? g_tsmConfig.overlay : "";
        if (!overlayPath.empty()) Info("Personal overlay: %s", overlayPath.c_str());

        PluginConfig config;
        if (!LoadConfig(g_iniPath, overlayPath, &config))
        {
            Report("FATAL", PLUGIN_INI, "configuration",
                "Configuration is invalid. Action: correct the preceding error; no hook was installed");
            LogSummary("Initialization", false);
            return 1;
        }
        ApplyConfig(config);
        LogConfig();

        // Storages this plugin gave to factories earlier have to leave them again when their
        // material is no longer asked for - also when no material is left at all or the
        // plugin is switched off in its INI. For that alone it stays loaded.
        FactoryLoadGiven(baseDir);
        if ((!g_enabled || g_materialCount <= 0) && g_factoryGivenCount > 0)
        {
            g_cleanupOnly = true;
            g_materialCount = 0;
            Info("%s; the plugin stays loaded only to take the storages it gave to factories out again (%d material(s) on record)",
                 !g_enabled ? "enabled = 0" : "No material is listed", g_factoryGivenCount);
            InterlockedExchange(&g_initialized, 1);
            LogSummary("Initialization", true);
            return 0;
        }

        if (!g_enabled)
        {
            Info("enabled = 0; plugin remains inactive");
            LogSummaryStatus("Initialization",
                             "was skipped because the plugin is disabled");
            return 1;
        }
        if (g_materialCount <= 0)
        {
            Report("FATAL", PLUGIN_INI, "no-materials",
                "No active material is configured. Action: assign a positive coefficient to at least one listed resource");
            LogSummary("Initialization", false);
            return 1;
        }

        InterlockedExchange(&g_initialized, 1);
        Info("%d active custom vehicle material(s) configured",
             g_materialCount);
        LogSummary("Initialization", true);
        return 0;
    }
    catch (const std::exception& e)
    {
        Report("FATAL", "vehicle_materials", "exception",
            "C++ exception in TsmPluginInit: %s. Action: preserve this log and report the failure; no hook was installed",
            e.what());
        LogSummary("Initialization", false);
        return 1;
    }
    catch (...)
    {
        Report("FATAL", "vehicle_materials", "exception",
            "Unknown C++ exception in TsmPluginInit. Action: preserve this log and report the failure; no hook was installed");
        LogSummary("Initialization", false);
        return 1;
    }
}

extern "C" __declspec(dllexport) int TsmPluginStart(void)
{
    BeginLogPhase();
    try
    {
        if (!H || !g_bound || !InterlockedExchangeAdd(&g_initialized, 0) ||
            (!g_cleanupOnly && g_materialCount <= 0))
        {
            Report("FATAL", NULL, "startup-state",
                "Required initialized plugin state is unavailable. Action: review the initialization log; no hook was installed");
            LogSummary("Startup", false);
            return 1;
        }

        if (g_cleanupOnly)
        {
            // The offsets of the factory part belong to one game build.
            if (!BuildMatches())
            {
                LogSummary("Startup", false);
                return 1;
            }
            InstallFactorySupport();
            Info("v%s ready (taking storages out only)", PLUGIN_VERSION);
            LogSummary("Startup", true);
            return 0;
        }

        g_resources = (const TsmResourceApi*)H->consume(
            TSM_SERVICE_RESOURCES, TSM_RESOURCES_VERSION);
        if (!g_resources || !ReadablePtr(g_resources, sizeof(*g_resources)) ||
            !g_resources->count || !g_resources->name)
        {
            Report("FATAL", NULL, "resources-service",
                "Resources service is unavailable or invalid. Action: enable the Resources plugin; no hook was installed");
            LogSummary("Startup", false);
            return 1;
        }
        for (int i = 0; i < g_materialCount; ++i)
        {
            if (!ResourceServiceContains(g_materials[i].name))
            {
                Report("FATAL", "plugins\\resources.ini", "resource-not-registered",
                    "Configured resource '%s' is not published by the Resources plugin. Action: add or correct it in resources.ini; no hook was installed",
                    g_materials[i].name);
                LogSummary("Startup", false);
                return 1;
            }
        }

        if (!BuildMatches())
        {
            LogSummary("Startup", false);
            return 1;
        }

        g_merge = (MergeRequirementsFn)(g_exeBase + RVA_MERGE_REQUIREMENTS);
        g_resourceGet = (ResourceGetFn)(g_exeBase + RVA_RESOURCE_GET);
        void* resourceManager = g_exeBase + RVA_RESOURCE_MANAGER;
        if (!ReadablePtr((void*)g_merge, 16) ||
            !ReadablePtr((void*)g_resourceGet, 16) ||
            !ReadablePtr(resourceManager, sizeof(void*)))
        {
            Report("FATAL", "SOVIET64.exe", "helper-validation",
                "A required helper or the resource manager is unreadable. Action: verify WRSR 1.2.0.9; no hook was installed");
            LogSummary("Startup", false);
            return 1;
        }

        BYTE* target = g_exeBase + RVA_VEHICLE_MATERIAL_BUILDER;
        void* trampoline = NULL;
        if (!InstallInlineHook(target, (void*)&HookVehicleMaterials,
                               &trampoline, EXPECTED_PROLOGUE,
                               sizeof(EXPECTED_PROLOGUE),
                               "vehicle_materials vehicle requirement builder") ||
            !trampoline)
        {
            Report("FATAL", "SOVIET64.exe", "hook-install",
                "Vehicle-material hook was refused. Action: check for a conflicting plugin or game update; no hook was installed");
            LogSummary("Startup", false);
            return 1;
        }

        // From this point the DLL must remain loaded: the game now branches
        // into HookVehicleMaterials. Runtime faults switch the custom part to
        // pass-through mode but never return a failure that could unload it.
        g_original = (VehicleMaterialBuilderFn)trampoline;
        InterlockedExchange(&g_runtimeEnabled, 1);

        Info("Hook active at SOVIET64.exe+0x%X", RVA_VEHICLE_MATERIAL_BUILDER);
        InstallFactorySupport();
        Info("Automatic mapping: type1/2=road, type6=ship, type8/10=airplane, type7/9=rocket, other=rail; horse-drawn vehicles take no extra material");
        Info("v%s ready", PLUGIN_VERSION);
        LogSummary("Startup", true);
        return 0;
    }
    catch (const std::exception& e)
    {
        if (g_original)
        {
            InterlockedExchange(&g_runtimeEnabled, 0);
            Report("FATAL", "vehicle_materials", "exception",
                "C++ exception after hook installation: %s. Custom additions are disabled; native vehicle materials remain active",
                e.what());
            LogSummary("Startup", false);
            return 0;
        }
        Report("FATAL", "vehicle_materials", "exception",
            "C++ exception in TsmPluginStart: %s. Action: preserve this log and report the failure; no hook was installed",
            e.what());
        LogSummary("Startup", false);
        return 1;
    }
    catch (...)
    {
        if (g_original)
        {
            InterlockedExchange(&g_runtimeEnabled, 0);
            Report("FATAL", "vehicle_materials", "exception",
                "Unknown exception after hook installation. Custom additions are disabled; native vehicle materials remain active");
            LogSummary("Startup", false);
            return 0;
        }
        Report("FATAL", "vehicle_materials", "exception",
            "Unknown exception in TsmPluginStart. Action: preserve this log and report the failure; no hook was installed");
        LogSummary("Startup", false);
        return 1;
    }
}

BOOL APIENTRY DllMain(HMODULE, DWORD reason, LPVOID)
{
    if (reason == DLL_PROCESS_DETACH)
    {
        if (g_detail != INVALID_HANDLE_VALUE)
        {
            CloseHandle(g_detail);
            g_detail = INVALID_HANDLE_VALUE;
        }
        if (g_bound) DeleteCriticalSection(&g_lock);
    }
    return TRUE;
}
