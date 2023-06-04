using System.Collections;
using System.Collections.Generic;

namespace Shared {
    /// <summary>
    /// A DataStructure that counts the number of identical elements added in.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class CountingList<T> : IEnumerable<T> {
        readonly Dictionary<T, int> _countingList = new();

        public int GetCount(T key) => _countingList.ContainsKey(key) ? _countingList[key] : 0;

        public void Add(T s) {
            if (_countingList.ContainsKey(s)) {
                _countingList[s]++;
            } else {
                _countingList.Add(s, 1);
            }
        }

        public void Clear() {
            _countingList.Clear();
        }

        public IEnumerator<T> GetEnumerator() => _countingList.Keys.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}