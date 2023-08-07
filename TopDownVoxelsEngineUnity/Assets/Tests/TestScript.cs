using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Shared;
using VoxelsEngine;

public class NewTestScript {
    [Test]
    public void NewTestScriptSimplePasses() {
        // Use the Assert class to test conditions
        int bitmask = 0;
        for (var i = 0; i <= 7; i++) {
            int isSameBlock = i % 2;
            bitmask |= isSameBlock << i;
        }

        Debug.Log(bitmask);
    }

    [Test]
    public void TestBitmaskTables() {
        for (var i = 0; i < 256; i++) {
            Assert.AreEqual(AutoTile48Blob.BitmaskToBlobMappingsDic[i], AutoTile48Blob.BitmaskToBlobMappings[i], "Error at index " + i);
        }
    }

    [Test]
    public void TestDirections() {
        Assert.AreEqual(DirectionFlag.Down, Direction.Down.ToFlag());
        Assert.AreEqual(DirectionFlag.Up, Direction.Up.ToFlag());
        Assert.AreEqual(DirectionFlag.North, Direction.North.ToFlag());
        Assert.AreEqual(DirectionFlag.South, Direction.South.ToFlag());
        Assert.AreEqual(DirectionFlag.East, Direction.East.ToFlag());
        Assert.AreEqual(DirectionFlag.West, Direction.West.ToFlag());
    }

    [Test]
    public void TestInt3Packing() {
        for (var i = 0; i < 254; i += 5) {
            for (var j = 0; j < 254; j += 8) {
                for (var k = 0; k < 254; k += 13) {
                    var packed = CSharpToShaderPacking.PackThree(i, j, k);
                    var unpacked = CSharpToShaderPacking.UnpackThree(packed);
                    Assert.AreEqual(i, unpacked.Item1, $"It didn't work for {i}, {j}, {k} (float={packed})");
                    Assert.AreEqual(j, unpacked.Item2, $"It didn't work for {i}, {j}, {k} (float={packed})");
                    Assert.AreEqual(k, unpacked.Item3, $"It didn't work for {i}, {j}, {k} (float={packed})");
                }
            }
        }

        var max = 254;
        var maxPacked = CSharpToShaderPacking.PackThree(max, max, max);
        var maxUnpacked = CSharpToShaderPacking.UnpackThree(maxPacked);
        Assert.AreEqual(max, maxUnpacked.Item1);
        Assert.AreEqual(max, maxUnpacked.Item2);
        Assert.AreEqual(max, maxUnpacked.Item3);
    }

    [Test]
    public void TestInt2Packing() {
        var max = 4094;
        for (var i = 0; i < max; i += 51) {
            for (var j = 0; j < max; j += 82) {
                var packed = CSharpToShaderPacking.PackTwo(i, j);
                var unpacked = CSharpToShaderPacking.UnpackTwo(packed);
                Assert.AreEqual(i, unpacked.Item1, $"It didn't work for {i}, {j} (float={packed})");
                Assert.AreEqual(j, unpacked.Item2, $"It didn't work for {i}, {j} (float={packed})");
            }
        }

        var maxPacked = CSharpToShaderPacking.PackTwo(max, max);
        var maxUnpacked = CSharpToShaderPacking.UnpackTwo(maxPacked);
        Assert.AreEqual(max, maxUnpacked.Item1);
        Assert.AreEqual(max, maxUnpacked.Item2);
    }

    // A UnityTest behaves like a coroutine in Play Mode. In Edit Mode you can use
    // `yield return null;` to skip a frame.
    [UnityTest]
    public IEnumerator NewTestScriptWithEnumeratorPasses() {
        // Use the Assert class to test conditions.
        // Use yield to skip a frame.
        yield return null;
    }
}