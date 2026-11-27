// storage_retrofit.h - give a building that already stands the storages its definition
// gained since it was built.
//
// The game keeps a list of storages in every building of a saved game; the building
// definition is only consulted when a building is built. A plugin that adds a storage line
// to a definition therefore reaches new buildings only. This header is the add-only
// reconciliation for the rest: after the game has loaded one building, the storages of its
// type that the including plugin calls its own and that the building lacks are put in, empty,
// where the definition has them.
//
// It is the routine Technical Service Storage has used for its depots (storage_migration.h
// there), made independent of that plugin: same native helpers, same checks, same staged
// swap. Nothing is resized, converted, reordered or removed. What it adds is saved with the
// next ordinary save and stays in the saved game. A building whose existing storages include
// a storage of vehicles - a vehicle factory - is refused as a whole (see NESTED_SIZE).
//
// How a plugin uses it (every plugin has its own copy - everything here is static):
//
//     #define STORAGE_RETROFIT_REPORT(level, rule, ...) <the plugin's own log call>
//     #include "../storage_retrofit.h"          // after tesmio_plugin.h
//
//     in TsmPluginStart:
//         if (storage_retrofit::Prepare())      // the native helpers are the known ones
//             subscribe an `after` callback to TSM_SERVICE_BUILDING_LOAD that calls
//             storage_retrofit::AfterLoad(building, Wanted, context) when completed != 0
//
// `Wanted` decides which definition storages are the plugin's own: it is asked for every
// single-resource import storage of the building's type with the resource name, the
// transport class number and the capacity.
//
// Game addresses: WRSR 1.2.0.9 (PE stamp 0x6B05A574). Every helper is compared with its
// expected first bytes before use; on another build Prepare() returns false and nothing
// happens. Port these together with storage_migration.h of Technical Service Storage.
#pragma once

#include <float.h>

#ifndef STORAGE_RETROFIT_REPORT
#error define STORAGE_RETROFIT_REPORT(level, rule, ...) before including storage_retrofit.h
#endif

namespace storage_retrofit
{

static const DWORD  RVA_STORAGE_PUSH     = 0x000E3D60;
static const DWORD  RVA_VECTOR_DESTROY   = 0x000C9360;
static const DWORD  RVA_STORAGE_CONTROLS = 0x002067F0;
static const DWORD  RVA_DEEP_COPY        = 0x001245E0;
static const DWORD  RVA_RESOURCE_VECTOR  = 0x00A2B270;

static const size_t RESOURCE_RECORD_SIZE = 0x0380;
static const size_t MIN_RESOURCE_RECORDS = 61;
static const size_t MAX_RESOURCE_RECORDS = 512;

static const size_t B_TYPEDESC              = 0x0318;   // building -> its type descriptor
static const size_t B_CONSTRUCTION_PROGRESS = 0x0604;   // float, 1.0 = finished
static const size_t B_STORAGE_BEGIN         = 0x0970;   // std::vector<Storage>
static const size_t B_GOING_AWAY            = 0x0EA8;   // byte: demolition or collapse
static const size_t TYPE_STORAGES           = 0x03A8;   // the type's own std::vector<Storage>
static const size_t TYPE_BUILDING_KIND      = 0x0368;   // int, the building type number
static const int    KIND_PRODUCTION_LINE    = 40;       // vehicle factories, dry dock, rocket parts

static const size_t STORAGE_SIZE     = 0x00E0;
static const size_t STORAGE_CAPACITY = 0x008C;
static const size_t STORAGE_CLASS    = 0x0090;
static const size_t SLOT_SIZE        = 0x0010;
static const size_t MAX_STORAGES          = 64;
static const size_t MAX_SLOTS_PER_STORAGE = 64;

// The transport classes the append has been used with. Other classes are left alone.
static const int TRANSPORT_COVERED = 0;
static const int TRANSPORT_OPEN    = 1;
static const int TRANSPORT_GRAVEL  = 2;

// The classes goods are stored in, by their number (the order of RESOURCE_TRANSPORT_* in the
// game's SOVIETInstructions.txt, CRYO and HYPERGOLIC behind it). Passengers, electricity,
// vehicles, the general class, heat, water, sewage and waste have storages of their own kind
// and are left alone.
static const char* const GOODS_CLASS_NAMES[] = {
    "COVERED", "OPEN", "GRAVEL", "OIL", "CEMENT", "COOLER", "LIVESTOCK", nullptr, "CONCRETE", nullptr,
    nullptr, nullptr, "NUCLEAR1", "NUCLEAR2", nullptr, nullptr, nullptr, nullptr, "CRYO", "HYPERGOLIC"
};
static bool GoodsClass(int transportClass)
{
    return transportClass >= 0 && transportClass < (int)_countof(GOODS_CLASS_NAMES) &&
        GOODS_CLASS_NAMES[transportClass] != nullptr;
}
// "RESOURCE_TRANSPORT_CEMENT" -> 4; -1 for anything that is no goods class.
static int ClassNumber(const char* token)
{
    static const char prefix[] = "RESOURCE_TRANSPORT_";
    if (!token || _strnicmp(token, prefix, sizeof(prefix) - 1) != 0) return -1;
    const char* name = token + sizeof(prefix) - 1;
    for (int i = 0; i < (int)_countof(GOODS_CLASS_NAMES); ++i)
        if (GOODS_CLASS_NAMES[i] && _stricmp(name, GOODS_CLASS_NAMES[i]) == 0) return i;
    return -1;
}
// Which way a storage goes. The building.ini parser sets +0x84 for the IMPORT tokens and
// +0x85 for the EXPORT tokens (1.2.0.9: 0x12F650..0x12F728); a storage that says neither or
// both is not one of ours.
static const int WAY_IMPORT = 1;
static const int WAY_EXPORT = 2;
static int Direction(const BYTE* storage)
{
    if (storage[0x84] == 1 && storage[0x85] == 0) return WAY_IMPORT;
    if (storage[0x84] == 0 && storage[0x85] == 1) return WAY_EXPORT;
    return 0;
}

struct Vector { BYTE* begin; BYTE* end; BYTE* capacity; };
static_assert(sizeof(Vector) == 24, "native x64 vector layout");

typedef void (__fastcall* PushFn)(Vector*, const BYTE*);
typedef void (__fastcall* DestroyFn)(Vector*);
typedef void (__fastcall* ControlsFn)(BYTE*);

// Is this definition storage one of the including plugin's own?
typedef bool (*WantedFn)(const char* resource, int transportClass, float capacity, void* context);

static PushFn     g_push     = nullptr;
static DestroyFn  g_destroy  = nullptr;
static ControlsFn g_controls = nullptr;
static volatile LONG g_faulted = 0;
static volatile LONG g_noRoomReports = 0;

static void Warn(BYTE* building, int index, const char* resource, const char* reason)
{
    STORAGE_RETROFIT_REPORT("WARN", "rejected",
        "building=%p definition_index=%d resource=%s reason=%s action=retain-existing-storage",
        building, index, resource ? resource : "<unknown>", reason);
}

static bool ReadVector(const void* address, size_t stride, size_t maximum, Vector* out, size_t* count)
{
    *out = {};
    *count = 0;
    if (!ReadablePtr(address, sizeof(*out))) return false;
    memcpy(out, address, sizeof(*out));
    uintptr_t b = (uintptr_t)out->begin, e = (uintptr_t)out->end;
    uintptr_t c = (uintptr_t)out->capacity;
    if (!b) return !e && !c;
    if (b % alignof(void*) || e < b || c < e ||
        (e - b) % stride || (c - b) % stride ||
        (e - b) / stride > maximum || (c - b) / stride > maximum * 4)
        return false;
    *count = (e - b) / stride;
    return e == b || ReadablePtr(out->begin, e - b);
}

static bool ResourceName(BYTE* resource, char name[64])
{
    name[0] = 0;
    Vector resources = {};
    size_t count = 0;
    if (!ReadVector(g_exeBase + RVA_RESOURCE_VECTOR, RESOURCE_RECORD_SIZE, MAX_RESOURCE_RECORDS,
            &resources, &count) || count < MIN_RESOURCE_RECORDS)
        return false;
    uintptr_t r = (uintptr_t)resource, b = (uintptr_t)resources.begin;
    return r >= b && r < (uintptr_t)resources.end &&
        (r - b) % RESOURCE_RECORD_SIZE == 0 &&
        SafeReadStr(resource, name, 64);
}

// All owning members used by the verified native copy constructor/destructor.
static const size_t VECTOR_OFFSETS[] = { 0, 0x18, 0x30, 0x50, 0x68, 0xC8 };
static const size_t VECTOR_STRIDES[] = { 16, 16, 4, 12, 12, 188 };

// A storage of vehicles (the VEHICLES export of a production line, for one) owns one more
// object at +0xC0: 0x90 bytes, five plain values and five vectors of plain records. The
// game's Storage copy constructor clones it (operator new(0x90), then that object's own copy
// constructor, which copies the records field by field), and when a storage vector grows the
// game itself copies every storage into the new block and destroys the old ones. The staged
// swap below therefore does to such a storage exactly what the game does; what is added here
// is only the knowledge needed to CHECK the clone. Read from the 1.2.0.8 image (copy
// constructor 0x124970, record copies 0x4F20 / 0x4EA0); a clone that does not compare equal
// stops the transaction before anything is swapped.
//
// OFF UNLESS THE PLUGIN SAYS OTHERWISE (g_serveVehicleStorages). Tried in the game on
// 2026-11-26 (Vanilla Buildings 0.4.10), when a new storage was still APPENDED: a finished
// car factory with 8 storages - its definition explains 4 - got a ninth behind all of them,
// the log said `added`, and two seconds later the game crashed at SOVIET64.exe+0x6CD691
// reading a building pointer whose upper half was zero. A production line has storages of
// its own behind the definition's. Since then a new storage is put where the definition has
// it (BuildReplacement). With that a car factory took a storage in the MIDDLE of its list
// without a crash, but in that session it reported no power until the game was saved and
// loaded again: the storages behind the new one had moved. A plugin whose storage line
// stands in the building's FILE may switch this on for a test only. A plugin that appends
// its storages to the END of the type's list in memory (Vehicle Materials) moves nothing in
// a building that already stands and switches it on.
static bool         g_serveVehicleStorages = false;
// A plugin whose storages stand at the END of the type list sets this: what a building
// lacks is then put behind everything the building has, whatever the order of the new
// storages in the type is. No storage of the building moves, ever.
static bool         g_appendOnly = false;
// A plugin that has no business with production lines sets this: AfterLoad, RemoveGone and
// Resize then leave every building of type 40 alone. A production line keeps storages of its
// own behind those of its definition (a car factory: 8 for 4), and a plain single storage
// among them would otherwise look like one whose line is gone.
static bool         g_skipProductionLines = false;

static bool SkippedKind(const BYTE* type)
{
    return g_skipProductionLines && ReadablePtr(type + TYPE_BUILDING_KIND, sizeof(int)) &&
        *(const int*)(type + TYPE_BUILDING_KIND) == KIND_PRODUCTION_LINE;
}
static const size_t NESTED_SIZE = 0x90;
static const size_t NESTED_VECTOR_OFFSETS[] = { 0x18, 0x30, 0x48, 0x60, 0x78 };
static const size_t NESTED_VECTOR_STRIDES[] = { 0x28, 0x28, 0x28, 8, 0x24 };

static bool NestedValid(BYTE* nested)
{
    if (!ReadablePtr(nested, NESTED_SIZE)) return false;
    for (size_t i = 0; i < _countof(NESTED_VECTOR_OFFSETS); ++i)
    {
        Vector view = {};
        size_t count = 0;
        if (!ReadVector(nested + NESTED_VECTOR_OFFSETS[i], NESTED_VECTOR_STRIDES[i], 65536, &view, &count))
            return false;
    }
    return true;
}

// The record copies leave the padding behind the leading byte(s) alone: a 0x28 record is two
// bytes and then values from +4, a 0x24 record one byte and values from +4; 8-byte records
// are copied whole.
static bool RecordsMatch(const BYTE* a, const BYTE* b, size_t count, size_t stride)
{
    for (size_t i = 0; i < count; ++i, a += stride, b += stride)
    {
        if (stride == 8) { if (memcmp(a, b, 8)) return false; continue; }
        if (a[0] != b[0] || (stride == 0x28 && a[1] != b[1]) || memcmp(a + 4, b + 4, stride - 4)) return false;
    }
    return true;
}

static bool NestedMatches(BYTE* a, BYTE* b)
{
    if (!a && !b) return true;
    if (!a || !b || a == b || !ReadablePtr(a, NESTED_SIZE) || !ReadablePtr(b, NESTED_SIZE)) return false;
    // One byte, then four 4-byte values from +4.
    if (a[0] != b[0] || memcmp(a + 4, b + 4, 16)) return false;
    for (size_t i = 0; i < _countof(NESTED_VECTOR_OFFSETS); ++i)
    {
        Vector va = {}, vb = {};
        size_t na = 0, nb = 0;
        if (!ReadVector(a + NESTED_VECTOR_OFFSETS[i], NESTED_VECTOR_STRIDES[i], 65536, &va, &na) ||
            !ReadVector(b + NESTED_VECTOR_OFFSETS[i], NESTED_VECTOR_STRIDES[i], 65536, &vb, &nb) ||
            na != nb || (va.begin && va.begin == vb.begin)) return false;
        if (na && !RecordsMatch(va.begin, vb.begin, na, NESTED_VECTOR_STRIDES[i])) return false;
    }
    return true;
}

static bool StorageValid(BYTE* storage, const char** reason = nullptr)
{
    const char* ignored = nullptr;
    if (!reason) reason = &ignored;
    *reason = "unreadable-storage-object";
    if (!ReadablePtr(storage, STORAGE_SIZE)) return false;
    static const char* vectorErrors[] = {
        "invalid-resource-slot-vector", "invalid-per-resource-control-vector",
        "invalid-selection-bitset-vector", "invalid-owner-vector-0x50",
        "invalid-owner-vector-0x68", "invalid-owner-vector-0xC8"
    };
    for (size_t i = 0; i < _countof(VECTOR_OFFSETS); ++i)
    {
        Vector view = {};
        size_t count = 0;
        *reason = vectorErrors[i];
        if (!ReadVector(storage + VECTOR_OFFSETS[i], VECTOR_STRIDES[i], 4096, &view, &count)) return false;
        if (i == 0)
        {
            *reason = "resource-slot-count-exceeds-64";
            if (count > MAX_SLOTS_PER_STORAGE) return false;
            for (size_t j = 0; j < count; ++j)
            {
                char name[64] = {};
                *reason = "slot-resource-outside-live-resource-table";
                if (!ResourceName(*(BYTE**)(view.begin + j * SLOT_SIZE), name)) return false;
                float amount = *(float*)(view.begin + j * SLOT_SIZE + 8);
                *reason = "non-finite-resource-stock";
                if (!_finite(amount)) return false;
            }
        }
    }
    // +0x30 is vector<bool>'s backing words, +0x48 its bit count.
    Vector bits = {};
    size_t words = 0;
    *reason = "selection-bit-count-exceeds-backing-words";
    if (!ReadVector(storage + 0x30, 4, 4096, &bits, &words) ||
        *(size_t*)(storage + 0x48) > words * 32) return false;
    // +0xC0 owns a further nested object in a storage of vehicles; see NESTED_SIZE above.
    // A building that has such a storage is not served unless g_serveVehicleStorages is set.
    BYTE* nested = *(BYTE**)(storage + 0xC0);
    if (!nested) return true;
    *reason = "building-with-a-storage-of-vehicles-is-not-served";
    if (!g_serveVehicleStorages) return false;
    *reason = "invalid-specialized-storage-owner-0xC0";
    return NestedValid(nested);
}

struct Candidate
{
    BYTE*  definition;
    int    index;       // where the definition has the storage
    int    direction;   // WAY_IMPORT or WAY_EXPORT
    size_t position;    // where it ended up in the building (set by BuildReplacement)
    char   name[64];
};

struct Plan
{
    Vector    original;
    size_t    originalCount;
    Candidate add[64];
    size_t    count;
};

// This runs for every building of a saved game. It stays silent until a building really
// lacks a storage the plugin calls its own; only then are the existing storages checked
// and findings reported.
static bool MakePlan(BYTE* building, WantedFn wanted, void* context, Plan* plan)
{
    *plan = {};
    if (!building || !wanted || !ReadablePtr(building + B_TYPEDESC, sizeof(void*))) return false;
    BYTE* type = *(BYTE**)(building + B_TYPEDESC);
    if (!type || !ReadablePtr(type + TYPE_STORAGES, sizeof(Vector)) || SkippedKind(type)) return false;
    if (!ReadablePtr(building + B_CONSTRUCTION_PROGRESS, sizeof(float)) ||
        !ReadablePtr(building + B_GOING_AWAY, 1)) return false;
    // Construction sites and buildings that are being demolished or have collapsed are no
    // targets; a site gets its storages from the definition when it is finished.
    const float progress = *(float*)(building + B_CONSTRUCTION_PROGRESS);
    if (!_finite(progress) || progress < 0.9999f || *(BYTE*)(building + B_GOING_AWAY)) return false;

    Vector definitions = {};
    size_t definitionCount = 0;
    if (!ReadVector(type + TYPE_STORAGES, STORAGE_SIZE, MAX_STORAGES, &definitions, &definitionCount) ||
        !ReadVector(building + B_STORAGE_BEGIN, STORAGE_SIZE, MAX_STORAGES, &plan->original, &plan->originalCount))
        return false;

    Candidate candidates[64] = {};
    size_t candidateCount = 0;
    for (size_t i = 0; i < definitionCount && candidateCount < _countof(candidates); ++i)
    {
        BYTE* definition = definitions.begin + i * STORAGE_SIZE;
        Vector slots = {};
        size_t slotCount = 0;
        if (!ReadVector(definition, SLOT_SIZE, MAX_SLOTS_PER_STORAGE, &slots, &slotCount) || slotCount != 1)
            continue;
        // An import or an export storage of one resource; ordinary ones keep the purpose
        // +0x88 = -1. What is ADDED is always a plain storage; only the existing ones may
        // be of the vehicle kind.
        if (!ReadablePtr(definition, STORAGE_SIZE) || !Direction(definition) ||
            *(int*)(definition + 0x88) != -1 || *(BYTE**)(definition + 0xC0) != nullptr) continue;
        const int direction = Direction(definition);
        int transport = *(int*)(definition + STORAGE_CLASS);
        float capacity = *(float*)(definition + STORAGE_CAPACITY);
        if (!GoodsClass(transport) || !_finite(capacity) || capacity <= 0) continue;
        char name[64] = {};
        if (!ResourceName(*(BYTE**)slots.begin, name)) continue;
        if (!wanted(name, transport, capacity, context)) continue;
        // The slot of a type holds what the storage takes of the resource: the capacity times
        // the resource's factor for the class. That is nothing when the line names a class
        // the resource does not have - such a storage is never given to a building.
        const float room = *(float*)(slots.begin + 8);
        if (!_finite(room) || room <= 0)
        {
            if (InterlockedIncrement(&g_noRoomReports) <= 20)
                Warn(building, (int)i, name, "the-line-names-a-class-in-which-this-resource-takes-nothing");
            continue;
        }
        Candidate& candidate = candidates[candidateCount++];
        candidate.definition = definition;
        candidate.index = (int)i;
        candidate.direction = direction;
        strcpy_s(candidate.name, name);
    }
    if (!candidateCount) return false;

    for (size_t i = 0; i < candidateCount; ++i)
    {
        const Candidate& candidate = candidates[i];
        bool present = false;
        for (size_t j = 0; j < plan->originalCount && !present; ++j)
        {
            BYTE* existing = plan->original.begin + j * STORAGE_SIZE;
            Vector slots = {};
            size_t n = 0;
            if (!ReadVector(existing, SLOT_SIZE, MAX_SLOTS_PER_STORAGE, &slots, &n)) continue;
            // A storage that goes the other way is another storage: an export of the
            // resource does not stand for the import its definition names.
            const int way = ReadablePtr(existing, STORAGE_SIZE) ? Direction(existing) : 0;
            if (way && way != candidate.direction) continue;
            for (size_t k = 0; k < n; ++k)
            {
                char name[64] = {};
                if (ResourceName(*(BYTE**)(slots.begin + k * SLOT_SIZE), name) &&
                    _stricmp(name, candidate.name) == 0) { present = true; break; }
            }
        }
        if (present) continue;
        bool duplicate = false;
        for (size_t j = 0; j < candidateCount; ++j)
            if (i != j && candidate.direction == candidates[j].direction &&
                _stricmp(candidate.name, candidates[j].name) == 0) duplicate = true;
        if (duplicate)
        {
            Warn(building, candidate.index, candidate.name, "ambiguous-duplicate-resource-in-building-definition");
            continue;
        }
        const char* detail = nullptr;
        if (!StorageValid(candidate.definition, &detail))
        {
            Warn(building, candidate.index, candidate.name, detail);
            continue;
        }
        plan->add[plan->count++] = candidate;
    }
    if (!plan->count) return false;

    // Something is missing. Preflight every existing owner BEFORE a native allocation or
    // copy occurs.
    for (size_t j = 0; j < plan->originalCount; ++j)
    {
        const char* detail = nullptr;
        if (!StorageValid(plan->original.begin + j * STORAGE_SIZE, &detail))
        {
            char reason[192];
            sprintf_s(reason, "existing-storage-index=%llu:%s", (unsigned long long)j, detail);
            Warn(building, plan->add[0].index, plan->add[0].name, reason);
            plan->count = 0;
            return false;
        }
    }
    if (plan->originalCount + plan->count > MAX_STORAGES)
    {
        Warn(building, -1, nullptr, "combined-storage-count-exceeds-64:no-partial-addition");
        plan->count = 0;
        return false;
    }
    return true;
}

static bool CopyMatches(BYTE* source, BYTE* copy)
{
    // The native copy invalidates its +0xA4 cache. Preserve that value too for existing
    // objects after the final push (which can itself reallocate).
    memcpy(copy + 0xA4, source + 0xA4, 4);
    // +0x86/+0x87 are padding, not initialized by the native copy constructor.
    if (memcmp(source + 0x80, copy + 0x80, 6) ||
        memcmp(source + 0x88, copy + 0x88, 0x38) ||
        *(size_t*)(source + 0x48) != *(size_t*)(copy + 0x48)) return false;
    for (size_t i = 0; i < _countof(VECTOR_OFFSETS); ++i)
    {
        size_t offset = VECTOR_OFFSETS[i];
        Vector a = {}, b = {};
        size_t na = 0, nb = 0;
        if (!ReadVector(source + offset, VECTOR_STRIDES[i], 4096, &a, &na) ||
            !ReadVector(copy + offset, VECTOR_STRIDES[i], 4096, &b, &nb) ||
            na != nb || (a.begin && a.begin == b.begin)) return false;
        if (na && memcmp(a.begin, b.begin, na * VECTOR_STRIDES[i])) return false;
    }
    return NestedMatches(*(BYTE**)(source + 0xC0), *(BYTE**)(copy + 0xC0));
}

static bool PrepareEmpty(BYTE* storage, BYTE* definition)
{
    // Type slots hold capacity, not stock. Never grant that amount as cargo.
    if (!CopyMatches(definition, storage)) return false;
    BYTE* slot = *(BYTE**)storage;
    memset(slot + 8, 0, 8);          // actual stock and transient per-slot quantity
    g_controls(storage);             // native per-resource controls + bit mask
    Vector controls = {};
    size_t count = 0;
    if (!ReadVector(storage + 0x18, SLOT_SIZE, MAX_SLOTS_PER_STORAGE, &controls, &count) || count != 1)
        return false;
    if (*(BYTE**)controls.begin != *(BYTE**)slot) return false;
    memset(controls.begin + 8, 0, 8);
    Vector bits = {};
    if (!ReadVector(storage + 0x30, 4, 4096, &bits, &count)) return false;
    if (count) memset(bits.begin, 0, count * 4);
    *(float*)(storage + 0xA4) = -1.0f;   // native cache invalidation
    return StorageValid(storage);
}

// A new storage goes where the building's definition has it, as far as the building has
// storages in front of that place. That is what the game does itself: its building loader
// gives a building the water storage its definition has and it lacks by inserting it at the
// definition's index, clamped to the number of storages the building has (1.2.0.8 image,
// 0x20B890, the block that tests transport class 0xF). It is also where a newly built
// building has the storage. What a building has beyond its definition - the storages a
// production line makes for itself - stays behind the new one.
// plan.add is in the order of the definition.
static bool BuildReplacement(Plan& plan, Vector* staged)
{
    // Exceptions leave the original untouched; the caller disposes staged.
    size_t next = 0, size = 0;
    for (size_t i = 0; i < plan.count; ++i)
    {
        const size_t wanted = g_appendOnly ? plan.originalCount : (size_t)plan.add[i].index;
        while (size < wanted && next < plan.originalCount)
        {
            g_push(staged, plan.original.begin + next * STORAGE_SIZE);
            ++next; ++size;
        }
        g_push(staged, plan.add[i].definition);
        plan.add[i].position = size++;
        if (!PrepareEmpty(staged->end - STORAGE_SIZE, plan.add[i].definition)) return false;
    }
    for (; next < plan.originalCount; ++next)
        g_push(staged, plan.original.begin + next * STORAGE_SIZE);

    Vector checked = {};
    size_t count = 0;
    if (!ReadVector(staged, STORAGE_SIZE, MAX_STORAGES, &checked, &count) ||
        count != plan.originalCount + plan.count) return false;
    // Every existing storage must be an equal copy, in its old order, around the new ones.
    size_t at = 0, a = 0;
    for (size_t i = 0; i < plan.originalCount; ++i)
    {
        while (a < plan.count && plan.add[a].position == at) { ++a; ++at; }
        if (at >= count ||
            !CopyMatches(plan.original.begin + i * STORAGE_SIZE, staged->begin + at * STORAGE_SIZE))
            return false;
        ++at;
    }
    for (size_t i = 0; i < plan.count; ++i)
    {
        if (plan.add[i].position >= count) return false;
        BYTE* added = staged->begin + plan.add[i].position * STORAGE_SIZE;
        if (*(float*)(*(BYTE**)added + 8) != 0.0f) return false;
    }
    return true;
}

// ---- the other way to put storages in: nothing that exists is cloned or freed ------------
//
// g_keepExisting. The staged swap above replaces every storage of the building by a clone and
// frees the originals: the storage objects, their slot lists and, in a storage of vehicles,
// the object at +0xC0. Tried three times on a car factory (2026-11-26), each time the factory
// reported "no power" for the rest of the session, and twice the game crashed minutes later
// on a pointer whose upper half was zero - once at SOVIET64.exe+0x6CD691, once at +0x20759D
// while saving. Something outside the building (the power grid is the obvious candidate)
// keeps addresses into what the swap frees and goes on writing there.
//
// So here the storages the building has are MOVED, bit by bit, into a block that has room
// for the new ones behind them:
//   - the block is made by the game (pushes of plain definition storages), so the game can
//     free it one day like any other;
//   - the first originalCount places are then overwritten with the bytes of the building's
//     own storages. Their slot lists, control lists and the object at +0xC0 stay where they
//     are and stay owned once - by the storage at its new place;
//   - the old block is NOT freed and its storages are NOT destroyed. It keeps the same bytes,
//     so an address into it still leads to the same slot lists. That is a leak of
//     originalCount * 0xE0 bytes per building, once;
//   - the placeholders that are overwritten leak their one-slot list (16 bytes each).
// What cannot be kept is the address of a storage object itself for the new block; whoever
// holds one reads the old block, which says the same as long as the lists inside a storage
// are not reallocated.
static bool g_keepExisting = false;

static bool PlainOneSlot(BYTE* storage)
{
    if (!ReadablePtr(storage, STORAGE_SIZE) || *(BYTE**)(storage + 0xC0) != nullptr) return false;
    for (size_t i = 0; i < _countof(VECTOR_OFFSETS); ++i)
    {
        Vector view = {};
        size_t count = 0;
        if (!ReadVector(storage + VECTOR_OFFSETS[i], VECTOR_STRIDES[i], 4096, &view, &count)) return false;
        if (i == 0 ? count != 1 : (i != 2 && count != 0)) return false;
    }
    return true;
}

static bool BuildRelocated(Plan& plan, Vector* staged)
{
    const size_t kept = plan.originalCount;
    // Placeholders for the places the building's own storages will take, then the new ones.
    for (size_t i = 0; i < kept; ++i) g_push(staged, plan.add[0].definition);
    for (size_t i = 0; i < plan.count; ++i)
    {
        g_push(staged, plan.add[i].definition);
        plan.add[i].position = kept + i;
    }
    Vector checked = {};
    size_t count = 0;
    if (!ReadVector(staged, STORAGE_SIZE, MAX_STORAGES, &checked, &count) || count != kept + plan.count)
        return false;
    // No push after this point: the block does not move any more.
    for (size_t i = 0; i < kept; ++i)
        if (!PlainOneSlot(staged->begin + i * STORAGE_SIZE)) return false;
    for (size_t i = 0; i < plan.count; ++i)
    {
        BYTE* added = staged->begin + (kept + i) * STORAGE_SIZE;
        if (!PrepareEmpty(added, plan.add[i].definition) || *(float*)(*(BYTE**)added + 8) != 0.0f)
            return false;
    }
    // Last and unconditional: from here on the staged block shares what the old one owns
    // and must never be destroyed element by element.
    memcpy(staged->begin, plan.original.begin, kept * STORAGE_SIZE);
    return true;
}

// Keep C++ exception handling separate from the outer SEH function (MSVC).
static bool TryBuild(Plan& plan, Vector* staged)
{
    try { return g_keepExisting ? BuildRelocated(plan, staged) : BuildReplacement(plan, staged); }
    catch (...) { return false; }
}

// Call when the game has finished loading one building. Returns the number of storages that
// were appended (0 = the building lacks nothing, or it was left alone - the log says why).
static int AfterLoad(BYTE* building, WantedFn wanted, void* context)
{
    if (!g_push || !g_destroy || !g_controls || InterlockedCompareExchange(&g_faulted, 0, 0)) return 0;
    Plan plan = {};
    Vector staged = {};
    bool committed = false;
    int added = 0;
    __try
    {
        if (!MakePlan(building, wanted, context, &plan)) return 0;
        // BuildRelocated fails only before it shares anything, so staged may be destroyed.
        if (!TryBuild(plan, &staged))
        {
            Warn(building, plan.add[0].index, plan.add[0].name,
                "native-copy-or-empty-initialization-failed:transaction-not-committed");
            g_destroy(&staged);
            return 0;
        }
        // Swap only vector ownership; the individual storages were cloned by the game -
        // or, with g_keepExisting, moved: then the old block stays as it is (see above).
        memcpy(building + B_STORAGE_BEGIN, &staged, sizeof(staged));
        staged = plan.original;
        committed = true;
        if (!g_keepExisting) g_destroy(&staged);
        for (size_t i = 0; i < plan.count; ++i)
            STORAGE_RETROFIT_REPORT("INFO", "added",
                "building=%p definition_index=%d storage_index=%llu of=%llu resource=%s capacity=%.3f stock=0 existing_storages=preserved persistence=native-save",
                building, plan.add[i].index, (unsigned long long)plan.add[i].position,
                (unsigned long long)(plan.originalCount + plan.count),
                plan.add[i].name, *(float*)(plan.add[i].definition + STORAGE_CAPACITY));
        added = (int)plan.count;
    }
    __except (FaultFilter("storage retrofit", GetExceptionInformation()))
    {
        // Do not traverse partially faulting ownership again: stop for this session and
        // leave the native load and the rest of the plugin alone.
        InterlockedExchange(&g_faulted, 1);
        STORAGE_RETROFIT_REPORT("WARN", "fault",
            "building=%p committed=%d retrofit-disabled-for-session=1 reason=native-memory-fault further-cleanup-skipped=1",
            building, committed ? 1 : 0);
    }
    return added;
}

// ---- storages that leave again ---------------------------------------------------------
//
// RemoveGone: the counterpart of AfterLoad. A storage of the building goes when
//   - it is a plain import or export storage of one resource in a goods class,
//   - the plugin says it once gave a storage for that resource (GoneFn),
//   - the building's definition has no storage for that resource any more, or only a
//     plain one of another class (then AfterLoad puts that one in afterwards),
//   - it is empty, nothing is on its way to it and nobody waits for it.
// A storage with stock stays until it is empty (reported, a limited number of times).
//
// Nothing of what the building keeps is cloned, destroyed or freed:
//   - when everything that goes stands at the END of the list, the vector just ends earlier;
//   - otherwise the storages that stay are moved together, bit by bit, into a block the game
//     made (placeholders pushed by the game's own function), and the old block is left as it
//     is. The storages behind a gap stand one place earlier afterwards.
// Seen in the game with Vehicle Materials (2026-11-27) on car and train factories, at the
// end of the list and in front of another storage: the building keeps its power and works.
// A building with a storage of vehicles is only served when g_serveVehicleStorages is set.
typedef bool (*GoneFn)(const char* resource, int transportClass, void* context);
static volatile LONG g_keptReports = 0;

// A plain import or export storage of one resource in a goods class: what a line
// $STORAGE_IMPORT_SPECIAL or $STORAGE_EXPORT_SPECIAL <class> <tons> <resource> makes.
// Gives its resource and its slot.
static bool PlainSingle(BYTE* storage, BYTE** resource, BYTE** slot)
{
    if (!ReadablePtr(storage, STORAGE_SIZE) || !Direction(storage) ||
        *(int*)(storage + 0x88) != -1 || *(BYTE**)(storage + 0xC0) != nullptr) return false;
    const int transport = *(int*)(storage + STORAGE_CLASS);
    if (!GoodsClass(transport)) return false;
    Vector slots = {};
    size_t n = 0;
    if (!ReadVector(storage, SLOT_SIZE, MAX_SLOTS_PER_STORAGE, &slots, &n) || n != 1) return false;
    *resource = *(BYTE**)slots.begin;
    *slot = slots.begin;
    return *resource != nullptr;
}

// Does the definition still have a place for this resource that a plain storage of this
// class stands for? A shared or special storage that holds the resource counts as before;
// a plain single-resource storage only when it is of the same class - a line that names
// another class asks for another storage.
// A storage that goes the other way does not count, and a plain one of another class in
// which the resource takes nothing is a slip in the definition, not a wish: the storage
// that works stays.
static bool DefinitionKeeps(const Vector& storages, size_t count, BYTE* resource, int transport, int direction)
{
    for (size_t i = 0; i < count; ++i)
    {
        BYTE* storage = storages.begin + i * STORAGE_SIZE;
        Vector slots = {};
        size_t n = 0;
        if (!ReadVector(storage, SLOT_SIZE, MAX_SLOTS_PER_STORAGE, &slots, &n)) continue;
        bool holds = false;
        for (size_t k = 0; k < n && !holds; ++k)
            if (*(BYTE**)(slots.begin + k * SLOT_SIZE) == resource) holds = true;
        if (!holds) continue;
        const int way = ReadablePtr(storage, STORAGE_SIZE) ? Direction(storage) : 0;
        if (way && way != direction) continue;
        BYTE* single = nullptr;
        BYTE* slot = nullptr;
        if (!PlainSingle(storage, &single, &slot)) return true;
        if (*(int*)(storage + STORAGE_CLASS) == transport) return true;
        const float room = *(float*)(slot + 8);
        if (!_finite(room) || room <= 0) return true;
    }
    return false;
}

static bool StorageGone(BYTE* storage, const Vector& definition, size_t definitionCount,
                        GoneFn gone, void* context, char name[64], bool* heldBack, float* stockOut)
{
    *heldBack = false;
    *stockOut = 0.0f;
    name[0] = 0;
    if (!ReadablePtr(storage, STORAGE_SIZE) || !Direction(storage) ||
        *(int*)(storage + 0x88) != -1 || *(BYTE**)(storage + 0xC0) != nullptr) return false;
    const int transport = *(int*)(storage + STORAGE_CLASS);
    if (!GoodsClass(transport)) return false;
    Vector slots = {};
    size_t slotCount = 0;
    if (!ReadVector(storage, SLOT_SIZE, MAX_SLOTS_PER_STORAGE, &slots, &slotCount) || slotCount != 1) return false;
    BYTE* resource = *(BYTE**)slots.begin;
    if (!ResourceName(resource, name)) return false;
    if (!gone(name, transport, context)) return false;
    // The definition has it - in a storage of this class, or in a shared one.
    if (DefinitionKeeps(definition, definitionCount, resource, transport, Direction(storage))) return false;
    bool waitedFor = false;
    for (size_t i = 3; i < _countof(VECTOR_OFFSETS); ++i)
    {
        Vector view = {};
        size_t count = 0;
        if (!ReadVector(storage + VECTOR_OFFSETS[i], VECTOR_STRIDES[i], 4096, &view, &count) || count)
            waitedFor = true;
    }
    const float stock = *(float*)(slots.begin + 8);
    const float coming = *(float*)(slots.begin + 12);
    *stockOut = stock;
    if (waitedFor || !_finite(stock) || stock > 0.0005f || !_finite(coming) || coming > 0.0005f)
    {
        *heldBack = true;
        return false;
    }
    return true;
}

// The bytes are copied last: a failure before that leaves a block of placeholders the
// caller may destroy.
static bool TryBuildKept(const Vector& own, size_t ownCount, const bool* leaves, size_t kept, Vector* staged)
{
    try
    {
        BYTE* placeholder = nullptr;
        for (size_t i = 0; i < ownCount && !placeholder; ++i)
            if (leaves[i]) placeholder = own.begin + i * STORAGE_SIZE;
        if (!placeholder || !kept) return false;
        for (size_t i = 0; i < kept; ++i) g_push(staged, placeholder);
        Vector checked = {};
        size_t count = 0;
        if (!ReadVector(staged, STORAGE_SIZE, MAX_STORAGES, &checked, &count) || count != kept) return false;
        size_t at = 0;
        for (size_t i = 0; i < ownCount; ++i)
            if (!leaves[i]) memcpy(staged->begin + at++ * STORAGE_SIZE, own.begin + i * STORAGE_SIZE, STORAGE_SIZE);
        return at == kept;
    }
    catch (...) { return false; }
}

// Call when the game has finished loading one building, before AfterLoad. Returns the
// number of storages that left.
static int RemoveGone(BYTE* building, GoneFn gone, void* context)
{
    if (!g_push || !g_destroy || !gone || InterlockedCompareExchange(&g_faulted, 0, 0)) return 0;
    int removedCount = 0;
    __try
    {
        if (!building || !ReadablePtr(building + B_TYPEDESC, sizeof(void*))) return 0;
        BYTE* type = *(BYTE**)(building + B_TYPEDESC);
        if (!type || !ReadablePtr(type + TYPE_STORAGES, sizeof(Vector)) || SkippedKind(type)) return 0;
        if (!ReadablePtr(building + B_CONSTRUCTION_PROGRESS, sizeof(float)) ||
            !ReadablePtr(building + B_GOING_AWAY, 1)) return 0;
        const float progress = *(float*)(building + B_CONSTRUCTION_PROGRESS);
        if (!_finite(progress) || progress < 0.9999f || *(BYTE*)(building + B_GOING_AWAY)) return 0;
        Vector definition = {}, own = {};
        size_t definitionCount = 0, ownCount = 0;
        if (!ReadVector(type + TYPE_STORAGES, STORAGE_SIZE, MAX_STORAGES, &definition, &definitionCount) ||
            !ReadVector(building + B_STORAGE_BEGIN, STORAGE_SIZE, MAX_STORAGES, &own, &ownCount) ||
            ownCount < 2) return 0;

        bool leaves[MAX_STORAGES] = {};
        char names[MAX_STORAGES][64] = {};
        size_t removed = 0, first = ownCount;
        bool vehicles = false;
        for (size_t i = 0; i < ownCount; ++i)
        {
            BYTE* storage = own.begin + i * STORAGE_SIZE;
            if (ReadablePtr(storage, STORAGE_SIZE) && *(BYTE**)(storage + 0xC0) != nullptr) vehicles = true;
            bool heldBack = false;
            float stock = 0.0f;
            if (StorageGone(storage, definition, definitionCount, gone, context, names[i], &heldBack, &stock))
            {
                leaves[i] = true;
                ++removed;
                if (i < first) first = i;
            }
            else if (heldBack && InterlockedIncrement(&g_keptReports) <= 40)
                STORAGE_RETROFIT_REPORT("INFO", "kept",
                    "building=%p storage_index=%llu resource=%s stock=%.3f: its line is gone, but the storage is not empty or a delivery is on its way; it stays until it is empty",
                    building, (unsigned long long)i, names[i], stock);
        }
        if (!removed || removed >= ownCount) return 0;
        if (vehicles && !g_serveVehicleStorages) return 0;
        const size_t kept = ownCount - removed;
        const bool atTheEnd = first + removed == ownCount;
        if (atTheEnd)
        {
            BYTE* newEnd = own.begin + kept * STORAGE_SIZE;
            memcpy(building + B_STORAGE_BEGIN + sizeof(void*), &newEnd, sizeof(newEnd));
        }
        else
        {
            Vector staged = {};
            if (!TryBuildKept(own, ownCount, leaves, kept, &staged))
            {
                STORAGE_RETROFIT_REPORT("WARN", "rejected",
                    "building=%p: the storages that stay could not be moved together; nothing was changed", building);
                if (staged.begin) g_destroy(&staged);
                return 0;
            }
            // Only the vector's three pointers change hands; the old block stays as it is.
            memcpy(building + B_STORAGE_BEGIN, &staged, sizeof(staged));
        }
        for (size_t i = 0; i < ownCount; ++i)
            if (leaves[i])
                STORAGE_RETROFIT_REPORT("INFO", "removed",
                    "building=%p storage_index=%llu of=%llu resource=%s: its line is gone and the storage was empty; %s persistence=native-save",
                    building, (unsigned long long)i, (unsigned long long)ownCount, names[i],
                    atTheEnd ? "no other storage moved" : "the storages that stay were moved together, none cloned or freed");
        removedCount = (int)removed;
    }
    __except (FaultFilter("storage removal", GetExceptionInformation()))
    {
        InterlockedExchange(&g_faulted, 1);
        STORAGE_RETROFIT_REPORT("WARN", "fault",
            "building=%p removal-disabled-for-session=1 reason=native-memory-fault", building);
    }
    return removedCount;
}

// ---- storages that follow their line ---------------------------------------------------
//
// Resize: a plain import or export storage of one resource takes the size its definition
// names now.
// Only the capacity value of the storage is written (Storage +0x8C - the value the game
// writes into a saved game for every storage, see its routine at 0x2068E0); nothing is
// moved, cloned or freed.
//   - the definition has exactly one plain single-resource storage for the resource and
//     the plugin calls it its own (WantedFn);
//   - the building has exactly one such storage for it, of the same class (another class is
//     the business of RemoveGone and AfterLoad: the old one leaves when empty, the new one
//     comes);
//   - a bigger size is taken at once; a smaller one waits until the stock and what is on
//     its way fit (reported, a limited number of times).
// Call it between RemoveGone and AfterLoad. Returns the number of storages that changed.
static volatile LONG g_resizeHeldReports = 0;

static int Resize(BYTE* building, WantedFn wanted, void* context)
{
    if (!wanted || InterlockedCompareExchange(&g_faulted, 0, 0)) return 0;
    int resized = 0;
    __try
    {
        if (!building || !ReadablePtr(building + B_TYPEDESC, sizeof(void*))) return 0;
        BYTE* type = *(BYTE**)(building + B_TYPEDESC);
        if (!type || !ReadablePtr(type + TYPE_STORAGES, sizeof(Vector)) || SkippedKind(type)) return 0;
        if (!ReadablePtr(building + B_CONSTRUCTION_PROGRESS, sizeof(float)) ||
            !ReadablePtr(building + B_GOING_AWAY, 1)) return 0;
        const float progress = *(float*)(building + B_CONSTRUCTION_PROGRESS);
        if (!_finite(progress) || progress < 0.9999f || *(BYTE*)(building + B_GOING_AWAY)) return 0;
        Vector definition = {}, own = {};
        size_t definitionCount = 0, ownCount = 0;
        if (!ReadVector(type + TYPE_STORAGES, STORAGE_SIZE, MAX_STORAGES, &definition, &definitionCount) ||
            !ReadVector(building + B_STORAGE_BEGIN, STORAGE_SIZE, MAX_STORAGES, &own, &ownCount) ||
            !ownCount) return 0;
        if (!g_serveVehicleStorages)
            for (size_t i = 0; i < ownCount; ++i)
            {
                BYTE* storage = own.begin + i * STORAGE_SIZE;
                if (ReadablePtr(storage, STORAGE_SIZE) && *(BYTE**)(storage + 0xC0) != nullptr) return 0;
            }
        for (size_t i = 0; i < definitionCount; ++i)
        {
            BYTE* line = definition.begin + i * STORAGE_SIZE;
            BYTE* resource = nullptr;
            BYTE* lineSlot = nullptr;
            if (!PlainSingle(line, &resource, &lineSlot)) continue;
            const int transport = *(int*)(line + STORAGE_CLASS);
            const int way = Direction(line);
            const float capacity = *(float*)(line + STORAGE_CAPACITY);
            char name[64] = {};
            if (!_finite(capacity) || capacity <= 0 || !ResourceName(resource, name) ||
                !wanted(name, transport, capacity, context)) continue;
            size_t lines = 0;
            for (size_t j = 0; j < definitionCount; ++j)
            {
                BYTE* other = nullptr;
                BYTE* otherSlot = nullptr;
                if (PlainSingle(definition.begin + j * STORAGE_SIZE, &other, &otherSlot) && other == resource &&
                    Direction(definition.begin + j * STORAGE_SIZE) == way) ++lines;
            }
            if (lines != 1) continue;
            BYTE* target = nullptr;
            BYTE* targetSlot = nullptr;
            size_t found = 0, index = 0;
            for (size_t j = 0; j < ownCount; ++j)
            {
                BYTE* other = nullptr;
                BYTE* otherSlot = nullptr;
                if (!PlainSingle(own.begin + j * STORAGE_SIZE, &other, &otherSlot) || other != resource ||
                    Direction(own.begin + j * STORAGE_SIZE) != way) continue;
                ++found;
                target = own.begin + j * STORAGE_SIZE;
                targetSlot = otherSlot;
                index = j;
            }
            if (found != 1 || *(int*)(target + STORAGE_CLASS) != transport) continue;
            const float old = *(float*)(target + STORAGE_CAPACITY);
            float difference = old - capacity;
            if (difference < 0) difference = -difference;
            if (!_finite(old) || difference < 0.0005f) continue;
            if (capacity < old)
            {
                // A type's slot holds what the storage takes of this resource: the capacity
                // times the resource's factor for the class.
                const float room = *(float*)(lineSlot + 8);
                const float stock = *(float*)(targetSlot + 8);
                const float coming = *(float*)(targetSlot + 12);
                const bool known = _finite(stock) && _finite(coming) && stock >= 0 && coming >= 0;
                const bool fits = known && ((_finite(room) && room > 0)
                    ? stock + coming <= room + 0.0005f : stock + coming <= 0.0005f);
                if (!fits)
                {
                    if (InterlockedIncrement(&g_resizeHeldReports) <= 40)
                        STORAGE_RETROFIT_REPORT("INFO", "kept",
                            "building=%p storage_index=%llu resource=%s capacity=%.3f wanted=%.3f stock=%.3f: its line names a smaller size, but the stock does not fit yet; the storage keeps its size until it does",
                            building, (unsigned long long)index, name, old, capacity, stock);
                    continue;
                }
            }
            *(float*)(target + STORAGE_CAPACITY) = capacity;
            STORAGE_RETROFIT_REPORT("INFO", "resized",
                "building=%p storage_index=%llu of=%llu resource=%s capacity=%.3f was=%.3f: the storage follows its line; nothing moved persistence=native-save",
                building, (unsigned long long)index, (unsigned long long)ownCount, name, capacity, old);
            ++resized;
        }
    }
    __except (FaultFilter("storage resize", GetExceptionInformation()))
    {
        InterlockedExchange(&g_faulted, 1);
        STORAGE_RETROFIT_REPORT("WARN", "fault",
            "building=%p resize-disabled-for-session=1 reason=native-memory-fault", building);
    }
    return resized;
}

// ---- what a definition holds -----------------------------------------------------------
//
// Every plain single storage of the building's definition (one resource, a goods class,
// import or export - what a $STORAGE_IMPORT_SPECIAL / $STORAGE_EXPORT_SPECIAL line makes, or
// a plain line of a class with one resource), for a plugin that recognises a definition by
// it. Returns their number, or -1 when the definition cannot be read.
typedef void (*SingleFn)(const char* resource, int transportClass, float capacity, int direction, void* context);

static int DefinitionSingles(BYTE* building, SingleFn fn, void* context)
{
    int found = -1;
    __try
    {
        if (!building || !ReadablePtr(building + B_TYPEDESC, sizeof(void*))) return -1;
        BYTE* type = *(BYTE**)(building + B_TYPEDESC);
        if (!type || !ReadablePtr(type + TYPE_STORAGES, sizeof(Vector))) return -1;
        Vector definition = {};
        size_t count = 0;
        if (!ReadVector(type + TYPE_STORAGES, STORAGE_SIZE, MAX_STORAGES, &definition, &count)) return -1;
        found = 0;
        for (size_t i = 0; i < count; ++i)
        {
            BYTE* storage = definition.begin + i * STORAGE_SIZE;
            BYTE* resource = nullptr;
            BYTE* slot = nullptr;
            char name[64] = {};
            if (!PlainSingle(storage, &resource, &slot) || !ResourceName(resource, name)) continue;
            if (fn) fn(name, *(int*)(storage + STORAGE_CLASS), *(float*)(storage + STORAGE_CAPACITY), Direction(storage), context);
            ++found;
        }
    }
    __except (FaultFilter("storage definition", GetExceptionInformation())) { found = -1; }
    return found;
}

// ---- read-only description, for the log ------------------------------------------------
//
// One line per storage: where it stands, class, capacity, the parser's flags, whether it is
// a storage of vehicles, and what it holds. Nothing is changed. A plugin calls Describe when
// its detailed log is on, for the buildings whose definition carries one of its storages.
static void DescribeList(BYTE* building, const char* list, const void* vectorAddress)
{
    Vector storages = {};
    size_t count = 0;
    if (!ReadVector(vectorAddress, STORAGE_SIZE, MAX_STORAGES, &storages, &count))
    {
        STORAGE_RETROFIT_REPORT("DEBUG", "storage", "building=%p list=%s unreadable", building, list);
        return;
    }
    for (size_t i = 0; i < count; ++i)
    {
        BYTE* storage = storages.begin + i * STORAGE_SIZE;
        if (!ReadablePtr(storage, STORAGE_SIZE)) continue;
        char held[400] = "";
        Vector slots = {};
        size_t slotCount = 0;
        if (ReadVector(storage, SLOT_SIZE, 4096, &slots, &slotCount))
        {
            size_t shown = 0;
            for (size_t k = 0; k < slotCount && shown < 10; ++k)
            {
                char name[64] = {};
                if (!ResourceName(*(BYTE**)(slots.begin + k * SLOT_SIZE), name)) strcpy_s(name, "?");
                float amount = *(float*)(slots.begin + k * SLOT_SIZE + 8);
                // A long list (a storage for a whole class): only what is in stock, then a few names.
                if (slotCount > 10 && amount == 0.0f && shown >= 4) continue;
                char piece[96];
                sprintf_s(piece, "%s%s=%.2f", shown ? " " : "", name, amount);
                strcat_s(held, piece);
                ++shown;
            }
        }
        STORAGE_RETROFIT_REPORT("DEBUG", "storage",
            "building=%p list=%s index=%llu of=%llu class=%d capacity=%.2f import_special=%u export=%u purpose=%d w80=%d vehicles=%d slots=%llu: %s",
            building, list, (unsigned long long)i, (unsigned long long)count,
            *(int*)(storage + STORAGE_CLASS), *(float*)(storage + STORAGE_CAPACITY),
            storage[0x84], storage[0x85], *(int*)(storage + 0x88), *(int*)(storage + 0x80),
            *(BYTE**)(storage + 0xC0) ? 1 : 0, (unsigned long long)slotCount, held);
    }
}

// True when the definition of this building has a storage the plugin calls its own.
static bool DefinitionHasWanted(BYTE* building, WantedFn wanted, void* context)
{
    if (!building || !wanted || !ReadablePtr(building + B_TYPEDESC, sizeof(void*))) return false;
    BYTE* type = *(BYTE**)(building + B_TYPEDESC);
    Vector definitions = {};
    size_t count = 0;
    if (!type || !ReadVector(type + TYPE_STORAGES, STORAGE_SIZE, MAX_STORAGES, &definitions, &count)) return false;
    for (size_t i = 0; i < count; ++i)
    {
        BYTE* definition = definitions.begin + i * STORAGE_SIZE;
        Vector slots = {};
        size_t slotCount = 0;
        if (!ReadablePtr(definition, STORAGE_SIZE) ||
            !ReadVector(definition, SLOT_SIZE, MAX_SLOTS_PER_STORAGE, &slots, &slotCount) || slotCount != 1) continue;
        char name[64] = {};
        if (!ResourceName(*(BYTE**)slots.begin, name)) continue;
        if (wanted(name, *(int*)(definition + STORAGE_CLASS), *(float*)(definition + STORAGE_CAPACITY), context)) return true;
    }
    return false;
}

static void Describe(BYTE* building, WantedFn wanted, void* context, const char* when)
{
    __try
    {
        if (!DefinitionHasWanted(building, wanted, context)) return;
        BYTE* type = *(BYTE**)(building + B_TYPEDESC);
        STORAGE_RETROFIT_REPORT("DEBUG", "storage",
            "building=%p %s type=%p type_number=%d type_0x3C0=%d type_0x3C4=%d type_0xC58=%d progress=%.3f",
            building, when, type, *(int*)(type + 0x368), *(int*)(type + 0x3C0), *(int*)(type + 0x3C4),
            ReadablePtr(type + 0xC58, 4) ? *(int*)(type + 0xC58) : -9999,
            *(float*)(building + B_CONSTRUCTION_PROGRESS));
        DescribeList(building, "definition", type + TYPE_STORAGES);
        DescribeList(building, "building", building + B_STORAGE_BEGIN);
    }
    __except (FaultFilter("storage description", GetExceptionInformation())) {}
}

static bool Verify(DWORD rva, const BYTE* expected, size_t count, const char* label)
{
    if (g_exeBase && rva <= g_exeSize && count <= g_exeSize - rva &&
        ReadablePtr(g_exeBase + rva, count) &&
        !memcmp(g_exeBase + rva, expected, count)) return true;
    STORAGE_RETROFIT_REPORT("WARN", "signature-rejected",
        "helper=%s rva=0x%X reason=unreadable-or-changed-native-code retrofit-disabled=1", label, rva);
    return false;
}

// True when the native helpers are the ones these addresses were taken from. Call once from
// TsmPluginStart, before subscribing to the building-load event.
static bool Prepare()
{
    static const BYTE push[]     = { 0x40,0x57,0x48,0x83,0xEC,0x30,0x48,0xC7,0x44,0x24,0x20,0xFE,0xFF,0xFF,0xFF };
    static const BYTE copy[]     = { 0x48,0x8B,0xC4,0x48,0x89,0x48,0x08,0x55,0x56,0x57,0x41,0x54,0x41,0x55,0x41,0x56,0x41,0x57 };
    static const BYTE controls[] = { 0x48,0x89,0x5C,0x24,0x10,0x57,0x48,0x83,0xEC,0x20,0x48,0x8B,0x51,0x08 };
    static const BYTE destroy[]  = { 0x48,0x89,0x5C,0x24,0x10,0x56,0x48,0x83,0xEC,0x20,0x48,0x8B,0x19,0x48,0x8B,0xF1 };
    if (!Verify(RVA_STORAGE_PUSH, push, sizeof(push), "push") ||
        !Verify(RVA_DEEP_COPY, copy, sizeof(copy), "deep copy") ||
        !Verify(RVA_VECTOR_DESTROY, destroy, sizeof(destroy), "vector destructor") ||
        !Verify(RVA_STORAGE_CONTROLS, controls, sizeof(controls), "controls"))
        return false;
    g_push     = (PushFn)(g_exeBase + RVA_STORAGE_PUSH);
    g_destroy  = (DestroyFn)(g_exeBase + RVA_VECTOR_DESTROY);
    g_controls = (ControlsFn)(g_exeBase + RVA_STORAGE_CONTROLS);
    return true;
}

} // namespace storage_retrofit
