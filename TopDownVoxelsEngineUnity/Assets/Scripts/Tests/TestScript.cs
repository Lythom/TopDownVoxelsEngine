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