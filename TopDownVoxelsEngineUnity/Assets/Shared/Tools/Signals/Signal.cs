using System;
using System.Collections.Generic;
using MessagePack;
using TinkState;

namespace Shared.Signals {

    [Serializable, MessagePackObject]
    public class Signal<T> : State<T>
#if UNITY_2020_3_OR_NEWER
        , UnityEngine.ISerializationCallbackReceiver
#endif
    {
        private State<T> _state;

#if UNITY_2020_3_OR_NEWER
        [UnityEngine.SerializeField]
#endif
        private T _v;

        public Signal(T initialValue) {
            _state = Observable.State(initialValue); // Use State as the underlying interface / implementation
            _v = initialValue; // use _v to serialize the value
        }

        [Key(0)]
        public T Value {
            get => _state.Value;
            set => SetValue(value);
        }

        private bool SetValue(T nextValue) {
            _state.Value = nextValue;
            _v = _state.Value;
            return true;
        }

        public override string ToString() {
            return _state.Value?.ToString() ?? "null";
        }

        public IDisposable Bind(Action<T> callback, IEqualityComparer<T>? comparer = null, Scheduler? scheduler = null) {
            return _state.Bind(callback, comparer!, scheduler!);
        }

        public Observable<TOut> Map<TOut>(Func<T, TOut> transform, IEqualityComparer<TOut>? comparer = null) {
            return _state.Map(transform, comparer!);
        }


        public void OnBeforeSerialize() {
        }

        public void OnAfterDeserialize() {
            Value = _v; // On unity deserialize hook, initialize the State object with the serialized value
        }
    }
}