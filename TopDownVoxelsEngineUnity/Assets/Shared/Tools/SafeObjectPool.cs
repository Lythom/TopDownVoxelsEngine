using System;
using System.Collections.Concurrent;

namespace Shared {
    public class SafeObjectPool<T> where T : new() {
        private readonly ConcurrentBag<T> _objects;
        private readonly Func<T> _objectGenerator;

        public SafeObjectPool(Func<T>? objectGenerator = null) {
            if (objectGenerator == null) {
                objectGenerator = () => new T();
            }

            _objects = new ConcurrentBag<T>();
            _objectGenerator = objectGenerator;
        }

        public T Get() {
            return _objects.TryTake(out T item) ? item : _objectGenerator();
        }

        public void Return(T item) {
            _objects.Add(item);
        }
    }
}