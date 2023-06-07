using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using MessagePack;

// ReSharper disable InvalidXmlDocComment

namespace LoneStoneStudio.Tools {
    /// <summary>
    ///  This is an enhanced encapsulation of AsyncReactiveProperty to achieve :
    /// - Customized render in editor. See also <seealso cref="ReactiveOdinDrawer">ReactiveOdinDrawer</seealso>.<br/>
    /// <br/>
    /// How to address lists ?<br/>
    /// => Use <see cref="ReactiveList{T}"/> to have a reactive view of a list for any change, if you have a dynamic list (that might change over time) to display.<br/>
    /// => Use <see cref="ObservableCollections.ObservableList{T}"/> if you have a dynamic list (that might change over time) to display and don't need Subscription API for the list.<br/>
    /// => Use a List of <see cref="Reactive{T}"/> if you have a static list of mutable elements to watch.<br/>
    /// You can hardly have both, because a dynamic list change would break elements listeners on list redraw.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    [Serializable, MessagePackObject]
    public class Reactive<T> : IAsyncReactiveProperty<T>, IDisposable
#if UNITY_2020_3_OR_NEWER
        , UnityEngine.ISerializationCallbackReceiver
#endif
    {
        private AsyncReactiveProperty<T> _asyncReactiveProperty;

#if UNITY_2020_3_OR_NEWER
        [UnityEngine.SerializeField]
#endif
        private T _v;

        public Reactive(T initialValue) {
            _asyncReactiveProperty = new AsyncReactiveProperty<T>(initialValue);
            _v = initialValue;
        }

        [Key(0)]
        public T Value {
            get => _asyncReactiveProperty.Value;
            set => SetValue(value);
        }

        public static implicit operator T(Reactive<T> value) {
            return value.Value;
        }

        public IUniTaskAsyncEnumerable<T> WithoutCurrent() {
            return _asyncReactiveProperty.WithoutCurrent();
        }

        public UniTask<T> WaitAsync(CancellationToken cancellationToken = new CancellationToken()) {
            return _asyncReactiveProperty.WaitAsync(cancellationToken);
        }

        public IUniTaskAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = new CancellationToken()) {
            return _asyncReactiveProperty.GetAsyncEnumerator(cancellationToken);
        }

        public void Dispose() {
            _asyncReactiveProperty.Dispose();
        }

        private bool SetValue(T nextValue) {
            if (EqualityComparer<T>.Default.Equals(nextValue, _asyncReactiveProperty.Value)) {
                _v = _asyncReactiveProperty.Value;
                return false;
            }

            _asyncReactiveProperty.Value = nextValue;
            _v = _asyncReactiveProperty.Value;
            return true;
        }

        public override string ToString() {
            return _asyncReactiveProperty.ToString();
        }

        public void OnBeforeSerialize() {
        }

        public void OnAfterDeserialize() {
            Value = _v;
        }
    }
}