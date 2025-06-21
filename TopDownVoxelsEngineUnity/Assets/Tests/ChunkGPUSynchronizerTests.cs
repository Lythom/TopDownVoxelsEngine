using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using VoxelsEngine; // <- namespace that contains ChunkGPUSynchronizer

// -----------------------------------------------------------------------------
//  A minimal, pure-C# chunk surrogate that satisfies the synchroniser.
//  (No mesh, no renderer – only the few fields & methods required.)
class FakeChunkRenderer : MonoBehaviour, IChunkRenderer {
    public const int Size = 16; // Chunk.Size in the real code
    private const int VoxelsPerChunk = 16 * 64 * 16; // = x * y * z from ChunkDimensions

    public uint[] BlockData { get; set; } = new uint[VoxelsPerChunk];
    public int GpuSlotID { get; set; } = -1;

    public Vector3Int Coords; // “chunk coords” inside the world

    public int GetFlatIndex() {
        var w = ChunkGPUSynchronizer.Instance.WorldDimensionsInChunks;
        return Coords.x + Coords.y * w.x + Coords.z * w.x * w.y;
    }

    // Convenience ctor for test code
    public static FakeChunkRenderer Create(Vector3Int coords) {
        var go = new GameObject($"FakeChunk {coords}");
        var chr = go.AddComponent<FakeChunkRenderer>();
        chr.Coords = coords;
        go.transform.position = (Vector3) coords * Size;
        return chr;
    }
}

// -----------------------------------------------------------------------------
//  Reflection helpers to peek at the private synchroniser state (test-only)
static class SyncInspector {
    private static BindingFlags F = BindingFlags.Instance | BindingFlags.NonPublic;

    public static HashSet<int> Active(ChunkGPUSynchronizer s)
        => GetField<HashSet<int>>(s, "_activeChunks");

    public static Stack<int> FreeSlots(ChunkGPUSynchronizer s)
        => GetField<Stack<int>>(s, "_freeSlotsInSsbo");

    public static int[] Indirection(ChunkGPUSynchronizer s)
        => GetField<int[]>(s, "_ssboSlotIdByChunkId");

    private static T GetField<T>(object obj, string name)
        => (T) obj.GetType().GetField(name, F)!.GetValue(obj);
}

// -----------------------------------------------------------------------------
//                           TEST-SUITE
// -----------------------------------------------------------------------------
public class ChunkGPUSynchronizerTests {
    [SetUp]
    public void SetUp()
    {
        // Clean setup before each test
        ChunkGPUSynchronizer.Instance.Dispose();
        // Initialize fresh instance
    }

    [TearDown] 
    public void TearDown()
    {
        // Clean up after each test
        ChunkGPUSynchronizer.Instance.Dispose();
    }
    
    // -------------------------------------------------------------------------
    // 2. Upload  N  chunks → slotIds must be unique, free-list length accurate
    // -------------------------------------------------------------------------
    [UnityTest]
    public System.Collections.IEnumerator Upload_Many_Chunks_Yields_Unique_Slots() {
        var sync = ChunkGPUSynchronizer.Instance;
        int chunkCount = 128; // arbitrary stress number
        var chunks = new List<FakeChunkRenderer>();
        var slots = new HashSet<int>();

        // init

        // 2.1 Upload -----------------------------------------------------------
        for (int i = 0; i < chunkCount; i++) {
            var coords = new Vector3Int(i, 0, 0); // guarantee unique indices
            var chr = FakeChunkRenderer.Create(coords);

            sync.UploadChunkData(chr);

            Assert.That(chr.GpuSlotID, Is.GreaterThanOrEqualTo(0), "Slot not assigned");
            Assert.That(slots.Add(chr.GpuSlotID), Is.True, "Duplicate slot detected");

            chunks.Add(chr);
        }

        // 2.2  Validate internal state ----------------------------------------
        Assert.That(SyncInspector.Active(sync).Count, Is.EqualTo(chunkCount));
        Assert.That(SyncInspector.FreeSlots(sync).Count,
            Is.EqualTo(ChunkGPUSynchronizerTestsHelpers.MaxActiveChunks - chunkCount));

        yield return null; // 1 frame for GPU upload (paranoia)
    }

    // -------------------------------------------------------------------------
    // 3. Unload ‑> slot must come back to the free-list and indirection set to -1
    // -------------------------------------------------------------------------
    [UnityTest]
    public System.Collections.IEnumerator Unload_Releases_Slot_And_Indirection() {
        var sync = ChunkGPUSynchronizer.Instance;

        var chr = FakeChunkRenderer.Create(new Vector3Int(31, 0, 7));
        sync.UploadChunkData(chr);

        int slotBefore = chr.GpuSlotID;
        int indexBefore = chr.GetFlatIndex();

        sync.UnloadChunkData(chr);

        //  Check indirection table
        int value = SyncInspector.Indirection(sync)[indexBefore];
        Assert.That(value, Is.EqualTo(-1), "Indirection table not cleared");

        //  Check free-list
        Assert.That(SyncInspector.FreeSlots(sync), Contains.Item(slotBefore),
            "Slot not returned to free-list");

        yield return null;
    }

    // -------------------------------------------------------------------------
    // 4. Slot reuse after unload (sanity: we _should_ get the same id back)
    // -------------------------------------------------------------------------
    [UnityTest]
    public System.Collections.IEnumerator Slot_Is_Reused_After_Unload() {
        var sync = ChunkGPUSynchronizer.Instance;

        // First chunk
        var c1 = FakeChunkRenderer.Create(new Vector3Int(5, 0, 0));
        sync.UploadChunkData(c1);
        int slot = c1.GpuSlotID;

        sync.UnloadChunkData(c1);

        // Second chunk
        var c2 = FakeChunkRenderer.Create(new Vector3Int(6, 0, 0));
        sync.UploadChunkData(c2);

        Assert.That(c2.GpuSlotID, Is.EqualTo(slot), "Slot should have been reused");
        yield return null;
    }
}
// -----------------------------------------------------------------------------

// Helper class containing values that otherwise are private.
// (Only here to keep magic numbers out of the assertions.)
static class ChunkGPUSynchronizerTestsHelpers {
    // This mirrors the constant inside the prod code. Keep in sync if you change it.
    public const int MaxActiveChunks = 1024;
}