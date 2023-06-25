using System;
using System.Diagnostics;
using NUnit.Framework;
using Tools;

namespace TestProject1;

public class Tests {
    [SetUp]
    public void Setup() {
    }

    [Test]
    public void TestTrace() {
        Trace.Listeners.Add(new DefaultTraceListener());
        Trace.WriteLine("[Test] test message");
    }

    [Test]
    public void TestMortonCodeXY() {
        Random rand = new Random();
        for (int i = 0; i < 1000; i++) {
            byte xOriginal = (byte) rand.Next(0, 256); // valeurs aléatoires entre 0 et 255
            byte yOriginal = (byte) rand.Next(0, 256);

            uint morton = MortonCode.Encode(xOriginal, yOriginal);
            MortonCode.Decode(morton, out uint xDecoded, out uint yDecoded);

            Assert.AreEqual(xOriginal, xDecoded);
            Assert.AreEqual(yOriginal, yDecoded);
        }
    }

    [Test]
    public void TestMortonCodeXYZ() {
        Random rand = new Random();
        for (int i = 0; i < 1000; i++) {
            byte xOriginal = (byte) rand.Next(0, 16); // valeurs aléatoires entre 0 et 15
            byte yOriginal = (byte) rand.Next(0, 16);
            byte zOriginal = (byte) rand.Next(0, 16);

            uint morton = MortonCode.Encode(xOriginal, yOriginal, zOriginal);
            MortonCode.Decode(morton, out uint xDecoded, out uint yDecoded, out uint zDecoded);

            Assert.AreEqual(xOriginal, xDecoded);
            Assert.AreEqual(yOriginal, yDecoded);
            Assert.AreEqual(zOriginal, zDecoded);
        }
    }
}