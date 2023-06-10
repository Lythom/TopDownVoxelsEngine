using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using MessagePack;
using ObservableCollections;
using Sirenix.OdinInspector;

namespace LoneStoneStudio.Tools {
    /// <summary>
    ///  This is pull-based encapsulation of ObservableDictionary to achieve a reactive subscription API rather than event based.
    ///  Warning! There is no guarantee that you get ALL intermediate change events if using a debounce (only the last one),
    ///  Also debouncing to the end of frame is the default behaviour of StarTeam Subscription of ConnectedComponents.
    /// </summary>
    /// <typeparam name="TKey"></typeparam>
    /// <typeparam name="TValue"></typeparam>
    [Serializable, MessagePackObject]
    public class ReactiveDictionary<TKey, TValue> : IAsyncReactiveProperty<ReactiveDictionaryChangeEvent<TKey, TValue>>, IDictionary<TKey, TValue>
#if UNITY_2020_3_OR_NEWER
        , UnityEngine.ISerializationCallbackReceiver
#endif
        where TKey : notnull {
#if UNITY_2020_3_OR_NEWER
        [UnityEngine.SerializeField]
#endif
        private List<TKey> _serializedKey = new();
#if UNITY_2020_3_OR_NEWER
        [UnityEngine.SerializeField]
#endif
        private List<TValue> _serializedValues = new();

        [ShowInInspector, IgnoreMember]
        private Dictionary<TKey, TValue> _dictionary;

        [IgnoreMember]
        public object SyncRoot { get; } = new object();

        private AsyncReactiveProperty<ReactiveDictionaryChangeEvent<TKey, TValue>> _asyncReactiveProperty;

        [Key(0)]
        public ReactiveDictionaryChangeEvent<TKey, TValue> Value {
            get => _asyncReactiveProperty.Value;
            set {
                lock (SyncRoot) {
                    _dictionary.Clear();
                    foreach (var (key, val) in value.Dictionary) {
                        _dictionary.Add(key, val);
                    }

                    OnCollectionChanged(NotifyCollectionChangedEventArgs<KeyValuePair<TKey, TValue>>.Reset());
                }
            }
        }

        public ReactiveDictionary() {
            _dictionary = new Dictionary<TKey, TValue>();
            _asyncReactiveProperty = new AsyncReactiveProperty<ReactiveDictionaryChangeEvent<TKey, TValue>>(new ReactiveDictionaryChangeEvent<TKey, TValue> {
                Dictionary = _dictionary,
                Event = new NotifyReactiveDictionaryChangedEventArgs<TKey, TValue>(NotifyCollectionChangedEventArgs<KeyValuePair<TKey, TValue>>.Reset())
            });
        }

        public ReactiveDictionary(IDictionary<TKey, TValue>? initialValue) {
            _dictionary = initialValue != null ? new Dictionary<TKey, TValue>(initialValue) : new Dictionary<TKey, TValue>();
            _asyncReactiveProperty = new AsyncReactiveProperty<ReactiveDictionaryChangeEvent<TKey, TValue>>(new ReactiveDictionaryChangeEvent<TKey, TValue> {
                Dictionary = _dictionary,
                Event = new NotifyReactiveDictionaryChangedEventArgs<TKey, TValue>(NotifyCollectionChangedEventArgs<KeyValuePair<TKey, TValue>>.Reset())
            });
        }

        private void OnCollectionChanged(in NotifyCollectionChangedEventArgs<KeyValuePair<TKey, TValue>> e) {
            _asyncReactiveProperty.Value = new ReactiveDictionaryChangeEvent<TKey, TValue> {
                Dictionary = _dictionary,
                Event = new NotifyReactiveDictionaryChangedEventArgs<TKey, TValue>(e)
            };
        }

        public IUniTaskAsyncEnumerable<ReactiveDictionaryChangeEvent<TKey, TValue>> WithoutCurrent() {
            return _asyncReactiveProperty.WithoutCurrent();
        }

        public UniTask<ReactiveDictionaryChangeEvent<TKey, TValue>> WaitAsync(CancellationToken cancellationToken = new()) {
            return _asyncReactiveProperty.WaitAsync(cancellationToken);
        }

        public IUniTaskAsyncEnumerator<ReactiveDictionaryChangeEvent<TKey, TValue>> GetAsyncEnumerator(CancellationToken cancellationToken = new()) {
            return _asyncReactiveProperty.GetAsyncEnumerator(cancellationToken);
        }

        public virtual void Dispose() {
            _asyncReactiveProperty.Dispose();
        }

        public override string ToString() => _asyncReactiveProperty.ToString();

        public void OnBeforeSerialize() {
            _serializedKey = Keys.ToList();
            _serializedValues = Values.ToList();
        }

        public void OnAfterDeserialize() {
            lock (SyncRoot) {
                _dictionary.Clear();
                foreach (var (k, v) in _serializedKey.Zip(_serializedValues, (key, value) => (key, value))) {
                    _dictionary.Add(k, v);
                }
            }
        }

        private Dictionary<TKey, TValue> _toRemoveDictionary = new();

        public void SynchronizeToTarget(IDictionary<TKey, TValue> target) {
            _toRemoveDictionary.Clear();

            foreach (var removalCandidate in this) {
                if (!target.Contains(removalCandidate)) _toRemoveDictionary.Add(removalCandidate.Key, removalCandidate.Value);
            }

            // Add elements we should have
            lock (SyncRoot) {
                foreach (var toHave in target) {
                    if (!_dictionary.ContainsKey(toHave.Key)) Add(toHave);
                }
            }

            // remove from removalDictionary
            foreach (var toRemove in _toRemoveDictionary) Remove(toRemove);
        }


        public TValue this[TKey key] {
            get {
                lock (SyncRoot) {
                    return _dictionary[key];
                }
            }
            set {
                lock (SyncRoot) {
                    if (_dictionary.TryGetValue(key, out var oldValue)) {
                        _dictionary[key] = value;
                        OnCollectionChanged(NotifyCollectionChangedEventArgs<KeyValuePair<TKey, TValue>>.Replace(
                            new KeyValuePair<TKey, TValue>(key, value),
                            new KeyValuePair<TKey, TValue>(key, oldValue),
                            -1));
                    } else {
                        Add(key, value);
                    }
                }
            }
        }

        [IgnoreMember]
        public ICollection<TKey> Keys {
            get {
                lock (SyncRoot) {
                    return _dictionary.Keys;
                }
            }
        }

        [IgnoreMember]
        public ICollection<TValue> Values {
            get {
                lock (SyncRoot) {
                    return _dictionary.Values;
                }
            }
        }

        [IgnoreMember]
        public int Count {
            get {
                lock (SyncRoot) {
                    return _dictionary.Count;
                }
            }
        }

        [IgnoreMember]
        public bool IsReadOnly => false;

        public void Add(TKey key, TValue value) {
            lock (SyncRoot) {
                _dictionary.Add(key, value);
                OnCollectionChanged(NotifyCollectionChangedEventArgs<KeyValuePair<TKey, TValue>>.Add(new KeyValuePair<TKey, TValue>(key, value), -1));
            }
        }

        public void Add(KeyValuePair<TKey, TValue> item) {
            Add(item.Key, item.Value);
        }

        public void Clear() {
            lock (SyncRoot) {
                _dictionary.Clear();
                OnCollectionChanged(NotifyCollectionChangedEventArgs<KeyValuePair<TKey, TValue>>.Reset());
            }
        }

        public bool Contains(KeyValuePair<TKey, TValue> item) {
            lock (SyncRoot) {
                return ((ICollection<KeyValuePair<TKey, TValue>>) _dictionary).Contains(item);
            }
        }

        public bool ContainsKey(TKey key) {
            lock (SyncRoot) {
                return ((IDictionary<TKey, TValue>) _dictionary).ContainsKey(key);
            }
        }

        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex) {
            lock (SyncRoot) {
                ((ICollection<KeyValuePair<TKey, TValue>>) _dictionary).CopyTo(array, arrayIndex);
            }
        }

        public bool Remove(TKey key) {
            lock (SyncRoot) {
                if (_dictionary.Remove(key, out var value)) {
                    OnCollectionChanged(NotifyCollectionChangedEventArgs<KeyValuePair<TKey, TValue>>.Remove(new KeyValuePair<TKey, TValue>(key, value), -1));
                    return true;
                }

                return false;
            }
        }

        public bool Remove(KeyValuePair<TKey, TValue> item) {
            lock (SyncRoot) {
                if (_dictionary.TryGetValue(item.Key, out var value)) {
                    if (EqualityComparer<TValue>.Default.Equals(value, item.Value)) {
                        if (_dictionary.Remove(item.Key, out var value2)) {
                            OnCollectionChanged(
                                NotifyCollectionChangedEventArgs<KeyValuePair<TKey, TValue>>.Remove(new KeyValuePair<TKey, TValue>(item.Key, value2), -1));
                            return true;
                        }
                    }
                }

                return false;
            }
        }
#pragma warning disable CS8767
        public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value) {
#pragma warning restore CS8767
            lock (SyncRoot) {
                return _dictionary.TryGetValue(key, out value);
            }
        }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() {
            lock (SyncRoot) {
                foreach (var item in _dictionary) {
                    yield return item;
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator() {
            return GetEnumerator();
        }
    }

    [MessagePackObject]
    public struct ReactiveDictionaryChangeEvent<TKey, TValue> where TKey : notnull {
        [Key(0)]
        public Dictionary<TKey, TValue> Dictionary;

        [IgnoreMember]
        public NotifyReactiveDictionaryChangedEventArgs<TKey, TValue> Event;
    }

    public readonly struct NotifyReactiveDictionaryChangedEventArgs<TKey, TValue> {
        public readonly NotifyCollectionChangedAction Action;
        public readonly bool IsSingleItem;
        public readonly KeyValuePair<TKey, TValue> NewItem;
        public readonly KeyValuePair<TKey, TValue> OldItem;
        public readonly KeyValuePair<TKey, TValue>[] NewItems;
        public readonly KeyValuePair<TKey, TValue>[] OldItems;
        public readonly int NewStartingIndex;
        public readonly int OldStartingIndex;

        public NotifyReactiveDictionaryChangedEventArgs(NotifyCollectionChangedEventArgs<KeyValuePair<TKey, TValue>> from) {
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