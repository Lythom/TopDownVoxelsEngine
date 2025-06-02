using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using LoneStoneStudio.Tools;
using NUnit.Framework;

namespace SignalsTests
{
    [TestFixture]
    class SignalDictionaryTests
    {
        [Test]
        public void TestIsReadonly()
        {
            var dict = new SignalDictionary<int, string>();
            Assert.That(dict.IsReadOnly, Is.False);
        }
        
        [Test]
        public void TestGetSetItem()
        {
            var dict = new SignalDictionary<string, int>();
            dict["foo"] = 15;
            
            Assert.That(dict["foo"], Is.EqualTo(15));
            
            dict["foo"] = 42;
            Assert.That(dict["foo"], Is.EqualTo(42));
        }
        
        [Test]
        public void TestCount()
        {
            var dict = new SignalDictionary<string, int>();
            Assert.That(dict.Count, Is.EqualTo(0));
            
            dict["foo"] = 15;
            Assert.That(dict.Count, Is.EqualTo(1));
            
            dict.Add("bar", 2);
            Assert.That(dict.Count, Is.EqualTo(2));
            
            dict.Remove("foo");
            Assert.That(dict.Count, Is.EqualTo(1));
            
            dict.Clear();
            Assert.That(dict.Count, Is.EqualTo(0));
        }
        
        [Test]
        public void TestTryGetValue()
        {
            var dict = new SignalDictionary<string, int>();
            
            bool success = dict.TryGetValue("foo", out var result);
            Assert.That(success, Is.False);
            Assert.That(result, Is.EqualTo(0)); // Default value for int
            
            dict["foo"] = 42;
            success = dict.TryGetValue("foo", out result);
            Assert.That(success, Is.True);
            Assert.That(result, Is.EqualTo(42));
            
            dict.Clear();
            success = dict.TryGetValue("foo", out result);
            Assert.That(success, Is.False);
        }
        
        [Test]
        public void TestContainsKey()
        {
            var dict = new SignalDictionary<string, int>();
            Assert.That(dict.ContainsKey("foo"), Is.False);
            
            dict["foo"] = 42;
            Assert.That(dict.ContainsKey("foo"), Is.True);
            
            dict.Clear();
            Assert.That(dict.ContainsKey("foo"), Is.False);
            
            dict.Add("foo", 10);
            Assert.That(dict.ContainsKey("foo"), Is.True);
            
            dict.Remove("foo");
            Assert.That(dict.ContainsKey("foo"), Is.False);
        }
        
        [Test]
        public void TestContains()
        {
            var dict = new SignalDictionary<string, int>();
            Assert.That(dict.Contains(new KeyValuePair<string, int>("foo", 42)), Is.False);
            
            dict["foo"] = 42;
            Assert.That(dict.Contains(new KeyValuePair<string, int>("foo", 42)), Is.True);
            
            // Wrong value, should not contain
            Assert.That(dict.Contains(new KeyValuePair<string, int>("foo", 43)), Is.False);
            
            dict.Clear();
            Assert.That(dict.Contains(new KeyValuePair<string, int>("foo", 42)), Is.False);
        }
        
        [Test]
        public void TestRemove()
        {
            var dict = new SignalDictionary<string, int>();
            dict["foo"] = 42;
            
            // Try to remove with wrong value (should not remove)
            var removed = dict.Remove(new KeyValuePair<string, int>("foo", 43));
            Assert.That(removed, Is.False);
            Assert.That(dict.ContainsKey("foo"), Is.True);
            
            // Remove with correct key/value
            removed = dict.Remove(new KeyValuePair<string, int>("foo", 42));
            Assert.That(removed, Is.True);
            Assert.That(dict.ContainsKey("foo"), Is.False);
            
            // Remove by key
            dict["foo"] = 42;
            removed = dict.Remove("foo");
            Assert.That(removed, Is.True);
            Assert.That(dict.ContainsKey("foo"), Is.False);
        }
        
        [Test]
        public void TestKeys()
        {
            var dict = new SignalDictionary<string, int>();
            Assert.That(dict.Keys.Count, Is.EqualTo(0));
            
            dict["foo"] = 42;
            Assert.That(dict.Keys.Count, Is.EqualTo(1));
            Assert.That(dict.Keys.Contains("foo"), Is.True);
            
            dict.Add("bar", 10);
            Assert.That(dict.Keys.Count, Is.EqualTo(2));
            Assert.That(dict.Keys.Contains("bar"), Is.True);
            
            dict.Remove("foo");
            Assert.That(dict.Keys.Count, Is.EqualTo(1));
            Assert.That(dict.Keys.Contains("foo"), Is.False);
        }
        
        [Test]
        public void TestValues()
        {
            var dict = new SignalDictionary<int, string>();
            Assert.That(dict.Values.Count, Is.EqualTo(0));
            
            dict[1] = "foo";
            Assert.That(dict.Values.Count, Is.EqualTo(1));
            Assert.That(dict.Values.Contains("foo"), Is.True);
            
            dict[2] = "bar";
            Assert.That(dict.Values.Count, Is.EqualTo(2));
            Assert.That(dict.Values.Contains("bar"), Is.True);
            
            dict.Remove(1);
            Assert.That(dict.Values.Count, Is.EqualTo(1));
            Assert.That(dict.Values.Contains("foo"), Is.False);
        }
        
        [Test]
        public void TestEnumeration()
        {
            var dict = new SignalDictionary<int, string>();
            dict[1] = "foo";
            dict[2] = "bar";
            
            var keys = new List<int>();
            var values = new List<string>();
            
            foreach (var kvp in dict)
            {
                keys.Add(kvp.Key);
                values.Add(kvp.Value);
            }
            
            Assert.That(keys.Count, Is.EqualTo(2));
            Assert.That(keys.Contains(1), Is.True);
            Assert.That(keys.Contains(2), Is.True);
            
            Assert.That(values.Count, Is.EqualTo(2));
            Assert.That(values.Contains("foo"), Is.True);
            Assert.That(values.Contains("bar"), Is.True);
        }
        
        [Test]
        public void TestCopyTo()
        {
            var dict = new SignalDictionary<int, string>();
            dict[1] = "foo";
            dict[2] = "bar";
            
            var array = new KeyValuePair<int, string>[2];
            dict.CopyTo(array, 0);
            
            // Sort by key to ensure consistent test
            Array.Sort(array, (a, b) => a.Key.CompareTo(b.Key));
            
            Assert.That(array[0].Key, Is.EqualTo(1));
            Assert.That(array[0].Value, Is.EqualTo("foo"));
            Assert.That(array[1].Key, Is.EqualTo(2));
            Assert.That(array[1].Value, Is.EqualTo("bar"));
        }
        
        [Test]
        public void TestObserve()
        {
            var dict = new SignalDictionary<int, string>();
            dict[1] = "a";
            dict[2] = "b";
            
            var observable = dict.Observe();
            
            // Test initial values
            var initialItems = observable.Value;
            Assert.That(initialItems.Count, Is.EqualTo(2));
            Assert.That(initialItems[1], Is.EqualTo("a"));
            Assert.That(initialItems[2], Is.EqualTo("b"));
            
            var bindingCalls = 0;
            var binding = observable.Bind(d =>
            {
                bindingCalls++;
                Assert.That(d.Count, Is.EqualTo(dict.Count));
            });
            
            // Initial binding call
            Assert.That(bindingCalls, Is.EqualTo(1));
            
            // Adding new item should trigger binding
            dict[3] = "c";
            Assert.That(bindingCalls, Is.EqualTo(2));
            
            binding.Dispose();
            
            // After disposing, changes should not trigger binding
            dict[4] = "d";
            Assert.That(bindingCalls, Is.EqualTo(2));
        }
        
        [Test]
        public void TestAsyncFunctionality()
        {
            var dict = new SignalDictionary<string, int>();
            dict["initial"] = 0;
            
            bool changeReceived = false;
            
            async UniTask TestAsync()
            {
                await using var enumerator = dict.GetAsyncEnumerator();
                
                // Get the initial value
                await enumerator.MoveNextAsync();
                var initialEvent = enumerator.Current;
                Assert.That(initialEvent.Dictionary.Count, Is.EqualTo(1));
                
                // Now wait for the next change
                var moveNextTask = enumerator.MoveNextAsync();
                
                // Make a change to trigger the async notification
                dict["new"] = 42;
                
                // Wait for the next event
                await moveNextTask;
                var changeEvent = enumerator.Current;
                
                Assert.That(changeEvent.Dictionary.Count, Is.EqualTo(2));
                Assert.That(changeEvent.Dictionary.ContainsKey("new"), Is.True);
                Assert.That(changeEvent.Dictionary["new"], Is.EqualTo(42));
                
                changeReceived = true;
            }
            
            TestAsync().Forget();
            
            // Give time for async operations to complete
            Thread.Sleep(100);
            
            Assert.That(changeReceived, Is.True);
        }
        
        [Test]
        public void TestSynchronizeToTarget()
        {
            var dict = new SignalDictionary<string, int>();
            dict["a"] = 1;
            dict["b"] = 2;
            dict["c"] = 3;
            
            var target = new Dictionary<string, int>
            {
                ["b"] = 2,
                ["c"] = 3,
                ["d"] = 4
            };
            
            dict.SynchronizeToTarget(target);
            
            // Should remove "a" (not in target)
            // Should keep "b" and "c" (in both)
            // Should add "d" (in target but not in dict)
            
            Assert.That(dict.Count, Is.EqualTo(3));
            Assert.That(dict.ContainsKey("a"), Is.False);
            Assert.That(dict.ContainsKey("b"), Is.True);
            Assert.That(dict.ContainsKey("c"), Is.True);
            Assert.That(dict.ContainsKey("d"), Is.True);
            Assert.That(dict["d"], Is.EqualTo(4));
        }
        
        [Test]
        public void TestSerialization()
        {
            var dict = new SignalDictionary<string, int>();
            dict["a"] = 1;
            dict["b"] = 2;
            
            // Simulate serialization/deserialization cycle
            dict.OnBeforeSerialize();
            dict.OnAfterDeserialize();
            
            // Data should be preserved
            Assert.That(dict.Count, Is.EqualTo(2));
            Assert.That(dict["a"], Is.EqualTo(1));
            Assert.That(dict["b"], Is.EqualTo(2));
        }
    }
}
