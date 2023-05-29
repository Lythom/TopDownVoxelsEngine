using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class NewTestScript {
    // A Test behaves as an ordinary method
    [Test]
    public void NewTestScriptSimplePasses() {
        // Use the Assert class to test conditions
        int bitmask = 0;
        for (var i = 0; i <= 7; i++) {
            int isSameBlock = i%2;
            bitmask |= isSameBlock << i;
        }

        Debug.Log(bitmask);
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