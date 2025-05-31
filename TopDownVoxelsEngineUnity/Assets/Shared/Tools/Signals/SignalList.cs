using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MessagePack;
using TinkState;
using UnityEngine;

namespace Shared.Signals {

    [Serializable, MessagePackObject]
    public class SignalList<T> : ObservableList<T>
#if UNITY_2020_3_OR_NEWER
        , ISerializationCallbackReceiver
#endif
        where T : notnull {
        private ObservableList<T> _list;

#if UNITY_2020_3_OR_NEWER
        [SerializeField]
#endif
        private List<T>? _serialized = null;

        [SerializationConstructor]
        public SignalList() {
            _list = Observable.List<T>();
        }

        private List<T> _toRemoveList = new();

        public void SynchronizeToTarget(IList<T> target) {
            _toRemoveList.Clear();

            foreach (var removalCandidate in this) {
                if (!target.Contains(removalCandidate)) _toRemoveList.Add(removalCandidate);
            }

            // Add elements we should have
            foreach (var toHave in target) {
                if (!Contains(toHave)) Add(toHave);
            }

            // remove from removalList
            foreach (var toRemove in _toRemoveList) Remove(toRemove);
        }

        public Observable<IReadOnlyList<T>> Observe() {
            return _list.Observe();
        }

        public IEnumerator<T> GetEnumerator() {
            return _list.GetEnumerator();
        }

        public override string ToString() {
            return _list.ToString();
        }

        IEnumerator IEnumerable.GetEnumerator() {
            return GetEnumerator();
        }

        public void OnBeforeSerialize() {
            _serialized = _list.ToList();
        }

        public void OnAfterDeserialize() {
            _list.Clear();
            if (_serialized is null || _serialized.Count == 0) return;
            foreach (var t in _serialized) _list.Add(t);
        }

        public void Add(T item) {
            _list.Add(item);
        }

        public void Clear() {
            _list.Clear();
        }

        public bool Contains(T item) {
            return _list.Contains(item);
        }

        public void CopyTo(T[] array, int arrayIndex) {
            _list.CopyTo(array, arrayIndex);
        }

        public bool Remove(T item) {
            return _list.Remove(item);
        }

        public int Count => _list.Count;
        public bool IsReadOnly => _list.IsReadOnly;

        public int IndexOf(T item) {
            return _list.IndexOf(item);
        }

        public void Insert(int index, T item) {
            _list.Insert(index, item);
        }

        public void RemoveAt(int index) {
            _list.RemoveAt(index);
        }

        public T this[int index] {
            get => _list[index];
            set => _list[index] = value;
        }
    }
}