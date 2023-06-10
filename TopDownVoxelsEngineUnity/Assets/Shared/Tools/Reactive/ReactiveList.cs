using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using MessagePack;
using ObservableCollections;
using Sirenix.OdinInspector;

namespace LoneStoneStudio.Tools {
    /// <summary>
    ///  This is pull-based encapsulation of List to achieve a reactive subscription API rather than event based.
    ///  Warning! There is no guarantee that you get ALL intermediate change events if using a debounce (only the last one),
    ///  Also debouncing to the end of frame is the default behaviour of StarTeam Subscription of ConnectedComponents.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    [Serializable, MessagePackObject]
    public class ReactiveList<T> : IAsyncReactiveProperty<ReactiveListChangeEvent<T>>, IList<T>, IReadOnlyList<T> {
#if UNITY_2020_3_OR_NEWER
        [UnityEngine.SerializeField]
#endif
        [ShowInInspector]
        private List<T> _list;

        private AsyncReactiveProperty<ReactiveListChangeEvent<T>> _asyncReactiveProperty;

        [IgnoreMember]
        public object SyncRoot { get; } = new();

        [Key(0)]
        public ReactiveListChangeEvent<T> Value {
            get => _asyncReactiveProperty.Value;
            set {
                lock (SyncRoot) {
                    _list.Clear();
                    _list.AddRange(value.List);
                    OnCollectionChanged(NotifyCollectionChangedEventArgs<T>.Reset());
                }
            }
        }

        public void ForceNotifyChange() {
            OnCollectionChanged(NotifyCollectionChangedEventArgs<T>.Reset());
        }

        public ReactiveList() {
            _list = new List<T>();
            _asyncReactiveProperty = new AsyncReactiveProperty<ReactiveListChangeEvent<T>>(new() {
                List = _list,
                Event = new NotifyReactiveListChangedEventArgs<T>(NotifyCollectionChangedEventArgs<T>.Reset())
            });
        }

        public ReactiveList(IEnumerable<T>? initialValue) {
            _list = initialValue != null ? new List<T>(initialValue) : new List<T>();
            _asyncReactiveProperty = new AsyncReactiveProperty<ReactiveListChangeEvent<T>>(new() {
                List = _list,
                Event = new NotifyReactiveListChangedEventArgs<T>(NotifyCollectionChangedEventArgs<T>.Reset())
            });
        }

        private void OnCollectionChanged(in NotifyCollectionChangedEventArgs<T> e) {
            _asyncReactiveProperty.Value = new ReactiveListChangeEvent<T> {
                List = _list,
                Event = new NotifyReactiveListChangedEventArgs<T>(e)
            };
        }

        public IUniTaskAsyncEnumerable<ReactiveListChangeEvent<T>> WithoutCurrent() {
            return _asyncReactiveProperty.WithoutCurrent();
        }

        public UniTask<ReactiveListChangeEvent<T>> WaitAsync(CancellationToken cancellationToken = new CancellationToken()) {
            return _asyncReactiveProperty.WaitAsync(cancellationToken);
        }

        public IUniTaskAsyncEnumerator<ReactiveListChangeEvent<T>> GetAsyncEnumerator(CancellationToken cancellationToken = new CancellationToken()) {
            return _asyncReactiveProperty.GetAsyncEnumerator(cancellationToken);
        }

        public virtual void Dispose() {
            _asyncReactiveProperty.Dispose();
        }

        public override string ToString() {
            return _asyncReactiveProperty.ToString();
        }

        [IgnoreMember]
        public int Count {
            get {
                lock (SyncRoot) {
                    return _list.Count;
                }
            }
        }

        [IgnoreMember]
        public bool IsReadOnly => false;

        public T this[int index] {
            get {
                lock (SyncRoot) {
                    return _list[index];
                }
            }
            set {
                lock (SyncRoot) {
                    var oldValue = _list[index];
                    _list[index] = value;
                    OnCollectionChanged(NotifyCollectionChangedEventArgs<T>.Replace(value, oldValue, index));
                }
            }
        }

        public void Add(T item) {
            lock (SyncRoot) {
                var index = _list.Count;
                _list.Add(item);
                OnCollectionChanged(NotifyCollectionChangedEventArgs<T>.Add(item, index));
            }
        }

        public void AddRange(IEnumerable<T> items) {
            lock (SyncRoot) {
                var index = _list.Count;
                var collection = items.ToArray();
                _list.AddRange(collection);
                OnCollectionChanged(NotifyCollectionChangedEventArgs<T>.Add(collection.AsSpan(), index));
            }
        }

        public void AddRange(T[] items) {
            lock (SyncRoot) {
                var index = _list.Count;
                _list.AddRange(items);
                OnCollectionChanged(NotifyCollectionChangedEventArgs<T>.Add(items, index));
            }
        }

        public void AddRange(ReadOnlySpan<T> items) {
            lock (SyncRoot) {
                var index = _list.Count;
                foreach (var item in items) {
                    _list.Add(item);
                }

                OnCollectionChanged(NotifyCollectionChangedEventArgs<T>.Add(items, index));
            }
        }

        public void Clear() {
            lock (SyncRoot) {
                _list.Clear();
                OnCollectionChanged(NotifyCollectionChangedEventArgs<T>.Reset());
            }
        }

        public bool Contains(T item) {
            lock (SyncRoot) {
                return _list.Contains(item);
            }
        }

        public void CopyTo(T[] array, int arrayIndex) {
            lock (SyncRoot) {
                _list.CopyTo(array, arrayIndex);
            }
        }

        public IEnumerator<T> GetEnumerator() {
            lock (SyncRoot) {
                foreach (var item in _list) {
                    yield return item;
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator() {
            return GetEnumerator();
        }

        public void ForEach(Action<T> action) {
            lock (SyncRoot) {
                foreach (var item in _list) {
                    action(item);
                }
            }
        }

        public int IndexOf(T item) {
            lock (SyncRoot) {
                return _list.IndexOf(item);
            }
        }

        public void Insert(int index, T item) {
            lock (SyncRoot) {
                _list.Insert(index, item);
                OnCollectionChanged(NotifyCollectionChangedEventArgs<T>.Add(item, index));
            }
        }

        public void InsertRange(int index, T[] items) {
            lock (SyncRoot) {
                _list.InsertRange(index, items);
                OnCollectionChanged(NotifyCollectionChangedEventArgs<T>.Add(items, index));
            }
        }

        public void InsertRange(int index, IEnumerable<T> items) {
            lock (SyncRoot) {
                var arr = items.ToArray();
                _list.InsertRange(index, arr);
                OnCollectionChanged(NotifyCollectionChangedEventArgs<T>.Add(arr.AsSpan(), index));
            }
        }

        public void InsertRange(int index, ReadOnlySpan<T> items) {
            lock (SyncRoot) {
                var arr = items.ToArray();
                _list.InsertRange(index, arr);
                OnCollectionChanged(NotifyCollectionChangedEventArgs<T>.Add(arr.AsSpan(), index));
            }
        }

        public bool Remove(T item) {
            lock (SyncRoot) {
                var index = _list.IndexOf(item);

                if (index >= 0) {
                    _list.RemoveAt(index);
                    OnCollectionChanged(NotifyCollectionChangedEventArgs<T>.Remove(item, index));
                    return true;
                }

                return false;
            }
        }

        public void RemoveAt(int index) {
            lock (SyncRoot) {
                var item = _list[index];
                _list.RemoveAt(index);
                OnCollectionChanged(NotifyCollectionChangedEventArgs<T>.Remove(item, index));
            }
        }

        public void RemoveRange(int index, int count) {
            lock (SyncRoot) {
                var range = _list.GetRange(index, count).ToArray();
                _list.RemoveRange(index, count);
                OnCollectionChanged(NotifyCollectionChangedEventArgs<T>.Remove(range.AsSpan(), index));
            }
        }

        public void Move(int oldIndex, int newIndex) {
            lock (SyncRoot) {
                var removedItem = _list[oldIndex];
                _list.RemoveAt(oldIndex);
                _list.Insert(newIndex, removedItem);
                OnCollectionChanged(NotifyCollectionChangedEventArgs<T>.Move(removedItem, newIndex, oldIndex));
            }
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

        public bool Pop(out T? result) {
            lock (SyncRoot) {
                result = default;
                if (_list.Count == 0) return false;
                result = _list[0];
                _list.RemoveAt(0);
                OnCollectionChanged(NotifyCollectionChangedEventArgs<T>.Remove(result, 0));
                return true;
            }
        }

        public void TransferTo(ReactiveList<T> destination) {
            destination.AddRange(this);
            Clear();
        }
    }

    [MessagePackObject]
    public struct ReactiveListChangeEvent<T> {
        [Key(0)]
        public List<T> List;

        [IgnoreMember]
        public NotifyReactiveListChangedEventArgs<T> Event;
    }

    public readonly struct NotifyReactiveListChangedEventArgs<T> {
        public readonly NotifyCollectionChangedAction Action;
        public readonly bool IsSingleItem;
        public readonly T NewItem;
        public readonly T OldItem;
        public readonly IReadOnlyList<T> NewItems;
        public readonly IReadOnlyList<T> OldItems;
        public readonly int NewStartingIndex;
        public readonly int OldStartingIndex;

        public NotifyReactiveListChangedEventArgs(NotifyCollectionChangedEventArgs<T> from) {
            Action = from.Action;
            IsSingleItem = from.IsSingleItem;
            NewItem = from.NewItem;
            OldItem = from.OldItem;
            NewItems = from.NewItems.ToArray();
            OldItems = from.OldItems.ToArray();
            NewStartingIndex = from.NewStartingIndex;
            OldStartingIndex = from.OldStartingIndex;
        }
    }
}