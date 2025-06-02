using System.Collections.Generic;
using NUnit.Framework;
using Shared.Signals;

namespace SignalsTests
{
    [TestFixture]
    class SignalListTests
    {
        [Test]
        public void TestIsReadonly()
        {
            var list = new SignalList<string>();
            Assert.That(list.IsReadOnly, Is.False);
        }
        
        [Test]
        public void TestAddAndCount()
        {
            var list = new SignalList<int>();
            Assert.That(list.Count, Is.EqualTo(0));
            
            list.Add(1);
            Assert.That(list.Count, Is.EqualTo(1));
            
            list.Add(2);
            list.Add(3);
            Assert.That(list.Count, Is.EqualTo(3));
        }
        
        [Test]
        public void TestGetSetItem()
        {
            var list = new SignalList<string>();
            list.Add("one");
            list.Add("two");
            
            Assert.That(list[0], Is.EqualTo("one"));
            Assert.That(list[1], Is.EqualTo("two"));
            
            list[0] = "updated";
            Assert.That(list[0], Is.EqualTo("updated"));
        }
        
        [Test]
        public void TestClear()
        {
            var list = new SignalList<int>();
            list.Add(1);
            list.Add(2);
            list.Add(3);
            
            Assert.That(list.Count, Is.EqualTo(3));
            
            list.Clear();
            Assert.That(list.Count, Is.EqualTo(0));
        }
        
        [Test]
        public void TestContains()
        {
            var list = new SignalList<string>();
            list.Add("one");
            list.Add("two");
            
            Assert.That(list.Contains("one"), Is.True);
            Assert.That(list.Contains("two"), Is.True);
            Assert.That(list.Contains("three"), Is.False);
        }
        
        [Test]
        public void TestRemove()
        {
            var list = new SignalList<int>();
            list.Add(1);
            list.Add(2);
            list.Add(3);
            
            Assert.That(list.Count, Is.EqualTo(3));
            
            // Remove existing item
            var removed = list.Remove(2);
            Assert.That(removed, Is.True);
            Assert.That(list.Count, Is.EqualTo(2));
            Assert.That(list.Contains(2), Is.False);
            
            // Remove non-existing item
            removed = list.Remove(10);
            Assert.That(removed, Is.False);
            Assert.That(list.Count, Is.EqualTo(2));
        }
        
        [Test]
        public void TestRemoveAt()
        {
            var list = new SignalList<string>();
            list.Add("one");
            list.Add("two");
            list.Add("three");
            
            list.RemoveAt(1);
            Assert.That(list.Count, Is.EqualTo(2));
            Assert.That(list[0], Is.EqualTo("one"));
            Assert.That(list[1], Is.EqualTo("three"));
        }
        
        [Test]
        public void TestIndexOf()
        {
            var list = new SignalList<string>();
            list.Add("one");
            list.Add("two");
            list.Add("three");
            
            Assert.That(list.IndexOf("one"), Is.EqualTo(0));
            Assert.That(list.IndexOf("two"), Is.EqualTo(1));
            Assert.That(list.IndexOf("three"), Is.EqualTo(2));
            Assert.That(list.IndexOf("four"), Is.EqualTo(-1));
        }
        
        [Test]
        public void TestInsert()
        {
            var list = new SignalList<int>();
            list.Add(1);
            list.Add(3);
            
            list.Insert(1, 2);
            Assert.That(list.Count, Is.EqualTo(3));
            Assert.That(list[0], Is.EqualTo(1));
            Assert.That(list[1], Is.EqualTo(2));
            Assert.That(list[2], Is.EqualTo(3));
        }
        
        [Test]
        public void TestCopyTo()
        {
            var list = new SignalList<int>();
            list.Add(1);
            list.Add(2);
            list.Add(3);
            
            var array = new int[3];
            list.CopyTo(array, 0);
            
            Assert.That(array[0], Is.EqualTo(1));
            Assert.That(array[1], Is.EqualTo(2));
            Assert.That(array[2], Is.EqualTo(3));
        }
        
        [Test]
        public void TestEnumeration()
        {
            var list = new SignalList<int>();
            list.Add(1);
            list.Add(2);
            list.Add(3);
            
            var sum = 0;
            foreach (var item in list)
            {
                sum += item;
            }
            
            Assert.That(sum, Is.EqualTo(6));
        }
        
        [Test]
        public void TestObserve()
        {
            var list = new SignalList<string>();
            list.Add("one");
            list.Add("two");
            
            var observable = list.Observe();
            Assert.That(observable.Value.Count, Is.EqualTo(2));
            
            var bindingCalls = 0;
            var binding = observable.Bind(items =>
            {
                bindingCalls++;
                Assert.That(items.Count, Is.EqualTo(list.Count));
            });
            
            // Initial binding
            Assert.That(bindingCalls, Is.EqualTo(1));
            
            // Adding an item should trigger binding
            list.Add("three");
            Assert.That(bindingCalls, Is.EqualTo(2));
            
            // Removing an item should trigger binding
            list.Remove("two");
            Assert.That(bindingCalls, Is.EqualTo(3));
            
            // Clearing the list should trigger binding
            list.Clear();
            Assert.That(bindingCalls, Is.EqualTo(4));
            
            binding.Dispose();
            
            // After disposing binding, changes should not trigger binding
            list.Add("new");
            Assert.That(bindingCalls, Is.EqualTo(4));
        }
        
        [Test]
        public void TestSynchronizeToTarget()
        {
            var list = new SignalList<string>();
            list.Add("one");
            list.Add("two");
            list.Add("three");
            
            var target = new List<string> { "two", "three", "four" };
            
            list.SynchronizeToTarget(target);
            
            // Should remove "one" (not in target)
            // Should keep "two" and "three" (in both)
            // Should add "four" (in target but not in list)
            
            Assert.That(list.Count, Is.EqualTo(3));
            Assert.That(list.Contains("one"), Is.False);
            Assert.That(list.Contains("two"), Is.True);
            Assert.That(list.Contains("three"), Is.True);
            Assert.That(list.Contains("four"), Is.True);
        }
        
        [Test]
        public void TestToString()
        {
            var list = new SignalList<int>();
            list.Add(1);
            list.Add(2);
            
            var str = list.ToString();
            Assert.That(str, Is.Not.Null);
        }
        
        [Test]
        public void TestSerialization()
        {
            var list = new SignalList<string>();
            list.Add("one");
            list.Add("two");
            
            // Simulate serialization/deserialization cycle
            list.OnBeforeSerialize();
            list.OnAfterDeserialize();
            
            // Data should be preserved
            Assert.That(list.Count, Is.EqualTo(2));
            Assert.That(list[0], Is.EqualTo("one"));
            Assert.That(list[1], Is.EqualTo("two"));
        }
    }
}
