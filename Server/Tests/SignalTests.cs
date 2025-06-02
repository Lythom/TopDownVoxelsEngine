using NUnit.Framework;
using Shared.Signals;

namespace SignalsTests {
    [TestFixture]
    class SignalTests {
        [Test]
        public void TestCreation() {
            var signal = new Signal<int>(0);
            Assert.That(signal.Value, Is.EqualTo(0));

            var initialValue = 42;
            var signalWithValue = new Signal<int>(initialValue);
            Assert.That(signalWithValue.Value, Is.EqualTo(initialValue));
        }

        [Test]
        public void TestSetValue() {
            var signal = new Signal<int>(0);
            Assert.That(signal.Value, Is.EqualTo(0));

            signal.Value = 42;
            Assert.That(signal.Value, Is.EqualTo(42));

            signal.Value = 100;
            Assert.That(signal.Value, Is.EqualTo(100));
        }

        [Test]
        public void TestObserveValue() {
            var signal = new Signal<string>("initial");
            var observed = false;

            var disposable = signal.Bind(value => {
                if (!observed) Assert.That(value, Is.EqualTo("initial"));
                if (observed) Assert.That(value, Is.EqualTo("updated"));
                observed = true;
            });

            // Initial binding triggers with current value
            Assert.That(observed, Is.True);

            // Updating value should trigger the binding
            signal.Value = "updated";
            Assert.That(observed, Is.True);

            // Clean up
            disposable.Dispose();
        }


        [Test]
        public void TestToString() {
            var signal = new Signal<int>(42);

            // ToString should contain the type and value
            var str = signal.ToString();
            Assert.That(str, Does.Contain("42"));
        }

        [Test]
        public void TestDispose() {
            var signal = new Signal<int>(0);
            var observed = false;

            var disposable = signal.Bind(_ => observed = true);
            observed = false;

            // Dispose signal
            disposable.Dispose();

            // After disposal, setting value should not trigger bindings
            signal.Value = 42;
            Assert.That(observed, Is.False);
        }
    }
}