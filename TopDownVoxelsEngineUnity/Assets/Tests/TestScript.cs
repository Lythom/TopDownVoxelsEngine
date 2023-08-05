using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Shared;

public class NewTestScript
{
    [Test]
    public void NewTestScriptSimplePasses()
    {
        // Use the Assert class to test conditions
        int bitmask = 0;
        for (var i = 0; i <= 7; i++)
        {
            int isSameBlock = i % 2;
            bitmask |= isSameBlock << i;
        }

        Debug.Log(bitmask);
    }

    [Test]
    public void TestBitmaskTables()
    {
        for (var i = 0; i < 256; i++)
        {
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
    public void TestIntPacking()
    {
        for (var i = 0; i < 500; i+=7)
        {
            for (var j = 0; j < 500; j+=110)
            {
                for (var k = 0; k < 500; k+=130) {
                    var packed = BlockRenderingSide.Pack(i, j, k);
                    var unpacked = BlockRenderingSide.Unpack(packed);
                    Assert.AreEqual(i, unpacked.X, $"It didn't work for {i}, {j}, {k} (float={packed})");
                    Assert.AreEqual(j, unpacked.Y, $"It didn't work for {i}, {j}, {k} (float={packed})");
                    Assert.AreEqual(k, unpacked.Z, $"It didn't work for {i}, {j}, {k} (float={packed})");
                }
            }
        }
        var maxPacked = BlockRenderingSide.Pack(1023, 1023, 1023);
        var maxUnpacked = BlockRenderingSide.Unpack(maxPacked);
        Assert.AreEqual(1023, maxUnpacked.X);
        Assert.AreEqual(1023, maxUnpacked.Y);
        Assert.AreEqual(1023, maxUnpacked.Z);
    }

    // A UnityTest behaves like a coroutine in Play Mode. In Edit Mode you can use
    // `yield return null;` to skip a frame.
    [UnityTest]
    public IEnumerator NewTestScriptWithEnumeratorPasses()
    {
        // Use the Assert class to test conditions.
        // Use yield to skip a frame.
        yield return null;
    }
}