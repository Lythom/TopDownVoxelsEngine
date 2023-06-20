using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.Linq;
using Cysharp.Threading.Tasks.Triggers;
using LoneStoneStudio.Tools;
using Shared;
using Shared.Net;
using StarTeam.Tools;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace VoxelsEngine {
    /// <summary>
    ///     The generic version of a ConnectedBehaviour{T} is managed version of ConnectedBehaviour. Use it to have another component explicitly manage it's lifecycle and provide data.
    ///      
    ///     Call .Set(T value) to push new data and subscribe the component. Activation to be handled by the caller.
    ///     Call .UnsubscribeAll() to have the component unsubscribe all of its subscriptions. Deactivation to be handled by the caller.
    ///     Each inherited component must override "OnSet" to handle the data
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class ConnectedBehaviour<T> : ConnectedBehaviour {
        public T? Data { get; private set; }


        protected override void OnEnable() {
            // mute setup, because we use OnSet here which is explicitly managed
        }

        /// <summary>
        ///     Public method to be used to update the ConnectedBehaviour.
        /// </summary>
        /// <param name="data">id of the data or direct reference to the data to display</param>
        public void Set(T data) {
            Data = data;
            _resetSource?.Cancel(false);
            _resetSource = new CancellationTokenSource();
            OnSet(ClientEngine.State, data);
        }

        /// <summary>
        ///     abstract method to override to handle a new value. A updated full GameState is provided to the
        ///     ConnectedBehaviour.
        /// </summary>
        /// <param name="state"></param>
        /// <param name="element"></param>
        protected abstract void OnSet(GameState state, T element);

        // Disable OnSetup, only OnSet will be used so that it provides additional data
        protected override void OnSetup(GameState state) {
        }
    }

    /// <summary>
    ///     Call .Refresh() to trigger a new "OnRefresh".
    ///     Each inherited component must override "OnRefresh" to handle the data
    /// </summary>
    public abstract class ConnectedBehaviour : LoneStoneBehaviour, IDisposable {
        [ContextMenu("Force ConnectedBehaviour Reset")]
        private void ForceRefresh() {
            _resetSource?.Cancel(false);
            _resetSource = new CancellationTokenSource();
            OnSetup(ClientEngine.State);
        }

        protected CancellationToken ResetToken {
            get {
                if (_resetSource == null) _resetSource = new CancellationTokenSource();
                return _resetSource.Token;
            }
        }

        // ReSharper disable once InconsistentNaming
        internal CancellationTokenSource? _resetSource;

        private ClientEngine? _gameManager = null;

        protected ClientEngine ClientEngine {
            get {
                if (_gameManager == null) _gameManager = GetComponentInParent<ClientEngine>(true);
                if (_gameManager == null) _gameManager = FindObjectOfType<ClientEngine>(true);
                return _gameManager;
            }
        }

        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        protected ISocketClient? SocketClient => ClientEngine == null ? null : ClientEngine.SocketClient;

        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        protected SideEffectManager? SideEffectManager => ClientEngine == null ? null : ClientEngine.SideEffectManager;

        private bool _isSetup;

        protected virtual void OnEnable() {
            Setup().Forget();
        }

        private async UniTask Setup() {
            if (!_isSetup) {
                if (ClientEngine == null) {
                    await UniTask.WaitUntil(() => ClientEngine != null, PlayerLoopTiming.Update, gameObject.GetCancellationTokenOnDestroy());
                }

                _isSetup = true;
                _resetSource?.Cancel(false);
                _resetSource = new CancellationTokenSource();
                OnSetup(ClientEngine!.State);
            }
        }

        protected virtual void OnDestroy() {
            Dispose();
        }

        /// <summary>
        /// Call this method to explicitly ask the component to stop self-updating from subscriptions.
        /// By default a component will keep listening subscriptions while disabled, which allow for self reactivation and data being already up to date when re-enabling.
        /// You want to call this method when
        /// - the cost of updating in background is greater than the cost of re-subscribing next time it is enabled,
        /// - you don't need self reactivation.
        /// The component can safely call this method itself in a subscription. Ensure the component or game object is also disabled to prevent a displayed un-updated component.
        /// If unsubscribed, the component will automatically resubscribe and update the next time it is enabled.
        /// </summary>
        public virtual void Dispose() {
            _resetSource?.Cancel(false);
            _isSetup = false;
        }

        /// <summary>
        /// Only called when a game is initialized or reset.
        /// During gameplay, state shape is immutable and only reactive value can change.
        /// </summary>
        /// <param name="state"></param>
        protected abstract void OnSetup(GameState state);

        protected void Subscribe<TSource, TSource2>(IUniTaskAsyncEnumerable<TSource> source, IUniTaskAsyncEnumerable<TSource2> source2, Action<TSource, TSource2> action) {
            Subscribe(
                UniTaskAsyncEnumerable.CombineLatest(source, source2, Fns.Tuplify),
                args => action(args.Item1, args.Item2)
            );
        }

        protected void Subscribe<TSource, TSource2, TSource3>(
            IUniTaskAsyncEnumerable<TSource> source,
            IUniTaskAsyncEnumerable<TSource2> source2,
            IUniTaskAsyncEnumerable<TSource3> source3,
            Action<TSource, TSource2, TSource3> action
        ) {
            Subscribe(
                UniTaskAsyncEnumerable.CombineLatest(source, source2, source3, Fns.Tuplify),
                args => action(args.Item1, args.Item2, args.Item3)
            );
        }

        protected void Subscribe<TSource, TSource2, TSource3, TSource4>(
            IUniTaskAsyncEnumerable<TSource> source,
            IUniTaskAsyncEnumerable<TSource2> source2,
            IUniTaskAsyncEnumerable<TSource3> source3,
            IUniTaskAsyncEnumerable<TSource4> source4,
            Action<TSource, TSource2, TSource3, TSource4> action
        ) {
            Subscribe(
                UniTaskAsyncEnumerable.CombineLatest(source, source2, source3, source4, Fns.Tuplify),
                args => action(args.Item1, args.Item2, args.Item3, args.Item4)
            );
        }

        protected void Subscribe<TSource, TSource2, TSource3, TSource4, TSource5>(
            IUniTaskAsyncEnumerable<TSource> source,
            IUniTaskAsyncEnumerable<TSource2> source2,
            IUniTaskAsyncEnumerable<TSource3> source3,
            IUniTaskAsyncEnumerable<TSource4> source4,
            IUniTaskAsyncEnumerable<TSource5> source5,
            Action<TSource, TSource2, TSource3, TSource4, TSource5> action
        ) {
            Subscribe(
                UniTaskAsyncEnumerable.CombineLatest(source, source2, source3, source4, source5, Fns.Tuplify),
                args => action(args.Item1, args.Item2, args.Item3, args.Item4, args.Item5)
            );
        }

        protected void Subscribe<TSource, TSource2, TSource3, TSource4, TSource5, TSource6>(
            IUniTaskAsyncEnumerable<TSource> source,
            IUniTaskAsyncEnumerable<TSource2> source2,
            IUniTaskAsyncEnumerable<TSource3> source3,
            IUniTaskAsyncEnumerable<TSource4> source4,
            IUniTaskAsyncEnumerable<TSource5> source5,
            IUniTaskAsyncEnumerable<TSource6> source6,
            Action<TSource, TSource2, TSource3, TSource4, TSource5, TSource6> action,
            string? debugSource = null
        ) {
            Subscribe(
                UniTaskAsyncEnumerable.CombineLatest(source, source2, source3, source4, source5, source6, Fns.Tuplify),
                args => action(args.Item1, args.Item2, args.Item3, args.Item4, args.Item5, args.Item6)
            );
        }

        protected void Subscribe<TSource, TSource2, TSource3, TSource4, TSource5, TSource6, TSource7>(
            IUniTaskAsyncEnumerable<TSource> source,
            IUniTaskAsyncEnumerable<TSource2> source2,
            IUniTaskAsyncEnumerable<TSource3> source3,
            IUniTaskAsyncEnumerable<TSource4> source4,
            IUniTaskAsyncEnumerable<TSource5> source5,
            IUniTaskAsyncEnumerable<TSource6> source6,
            IUniTaskAsyncEnumerable<TSource7> source7,
            Action<TSource, TSource2, TSource3, TSource4, TSource5, TSource6,
                TSource7> action,
            string? debugSource = null
        ) {
            Subscribe(
                UniTaskAsyncEnumerable.CombineLatest(source, source2, source3, source4, source5, source6, source7, Fns.Tuplify),
                args => action(args.Item1, args.Item2, args.Item3, args.Item4, args.Item5, args.Item6, args.Item7)
            );
        }

        protected void Subscribe<TSource, TSource2, TSource3, TSource4, TSource5, TSource6, TSource7, TSource8>(
            IUniTaskAsyncEnumerable<TSource> source,
            IUniTaskAsyncEnumerable<TSource2> source2,
            IUniTaskAsyncEnumerable<TSource3> source3,
            IUniTaskAsyncEnumerable<TSource4> source4,
            IUniTaskAsyncEnumerable<TSource5> source5,
            IUniTaskAsyncEnumerable<TSource6> source6,
            IUniTaskAsyncEnumerable<TSource7> source7,
            IUniTaskAsyncEnumerable<TSource8> source8,
            Action<TSource, TSource2, TSource3, TSource4, TSource5, TSource6, TSource7, TSource8> action,
            string? debugSource = null
        ) {
            Subscribe(
                UniTaskAsyncEnumerable.CombineLatest(source, source2, source3, source4, source5, source6, source7, source8, Fns.Tuplify),
                args => action(args.Item1, args.Item2, args.Item3, args.Item4, args.Item5, args.Item6, args.Item7, args.Item8)
            );
        }

        protected void Subscribe<TSource, TSource2, TSource3, TSource4, TSource5, TSource6, TSource7, TSource8, TSource9>(
            IUniTaskAsyncEnumerable<TSource> source,
            IUniTaskAsyncEnumerable<TSource2> source2,
            IUniTaskAsyncEnumerable<TSource3> source3,
            IUniTaskAsyncEnumerable<TSource4> source4,
            IUniTaskAsyncEnumerable<TSource5> source5,
            IUniTaskAsyncEnumerable<TSource6> source6,
            IUniTaskAsyncEnumerable<TSource7> source7,
            IUniTaskAsyncEnumerable<TSource8> source8,
            IUniTaskAsyncEnumerable<TSource9> source9,
            Action<TSource, TSource2, TSource3, TSource4, TSource5, TSource6, TSource7, TSource8, TSource9> action,
            string? debugSource = null
        ) {
            Subscribe(
                UniTaskAsyncEnumerable.CombineLatest(source, source2, source3, source4, source5, source6, source7, source8, source9, Fns.Tuplify),
                args => action(args.Item1, args.Item2, args.Item3, args.Item4, args.Item5, args.Item6, args.Item7, args.Item8, args.Item9)
            );
        }

        /// <summary>
        ///     Subscribe to all source changes while this MonoBehaviour is Instantiated on scene.
        /// </summary>
        /// <param name="source">Reactive source</param>
        /// <param name="action">Things to do when the source change. Ie. update visuals and texts.</param>
        /// <typeparam name="TSource"></typeparam>
        /// <returns></returns>
        protected void Subscribe<TSource>(IUniTaskAsyncEnumerable<TSource> source, Action<TSource> action) {
            source.ForEachAsync(s => {
                try {
                    action(s);
                } catch (Exception e) {
                    Logr.LogException(e, $"Error happened during an update of a {typeof(TSource).Name}. The Subscription as ended.");
                    throw;
                }
            }, ResetToken).Forget();
        }

        protected void SubscribeSideEffect<T>(Action<T> action) {
            if (SideEffectManager == null) return;
            ResetToken.Register(SideEffectManager.For<T>().StartListening(action));
        }

        protected void TriggerSideEffect<T>(T sideEffect) {
            if (SideEffectManager == null) return;
            SideEffectManager.For<T>().Trigger(sideEffect);
        }

        protected void OnClick(UIBehaviour component, Action action, TimeSpan throttle) => OnClick(component, action, throttle, ResetToken);

        protected void OnClick(UIBehaviour component, Action action, Action handleNotInteractive, TimeSpan throttle) =>
            OnClick(component, action, throttle, ResetToken, handleNotInteractive);

        protected async UniTask<INetworkMessage?> SendMessageAsync(INetworkMessage msg, TimeSpan timeout) {
            if (SocketClient == null) {
                Logr.Log("Not connected to a server.");
                return null;
            }

            UniTaskCompletionSource<INetworkMessage> answerReceived = new UniTaskCompletionSource<INetworkMessage>();

            // This is a very naive implementation. If another player send the same GameEvent type it will consider it as the answer.
            // Therefore, there is no need anymore for multiplayer support atm.
            // Fix by implementing https://linear.app/starteam/issue/ENG-1/transctionnalite-des-game-events
            void HandleAnswer(INetworkMessage answer) {
                if (answer.GetType() == msg.GetType()) {
                    answerReceived.TrySetResult(answer);
                }
            }

            SocketClient.OnNetworkMessage += HandleAnswer;
            await SocketClient.Send(msg);
            await UniTask.WhenAny(
                answerReceived.Task,
                UniTask.Delay(timeout, DelayType.Realtime, cancellationToken: ResetToken).SuppressCancellationThrow()
            );
            if (SocketClient != null) SocketClient.OnNetworkMessage -= HandleAnswer;
            return answerReceived.GetStatus(0) == UniTaskStatus.Succeeded ? answerReceived.GetResult(0) : null;
        }

        protected void SendBlindMessageOptimistic(GameEvent msg) {
            if (SocketClient == null) {
                Logr.Log("Not connected to a server.");
                return;
            }

            ClientEngine.HandleEvent(msg);
            SocketClient.Send(msg).Forget();
        }

        /// <summary>
        ///     Subscribe to component Click triggers while this MonoBehaviour is Instantiated on scene and the component is active
        ///     and interactable.
        /// </summary>
        /// <param name="component">component to listen clicks on</param>
        /// <param name="action">action to perform on click</param>
        /// <param name="throttle">Time during which the component will become unresponsive after a click</param>
        /// <param name="token"></param>
        /// <param name="handleNotInteractive"></param>
        internal static void OnClick(UIBehaviour component, Action? action, TimeSpan throttle, CancellationToken token, Action? handleNotInteractive = null) {
            if (Application.isPlaying) {
                Action<PointerEventData> triggerAction = _ => {
                    try {
                        switch (component) {
                            case Button b:
                                if (b.interactable && b.enabled && action != null) {
                                    action();
                                } else {
                                    handleNotInteractive?.Invoke();
                                }

                                break;
                            default:
                                if (component.enabled && action != null) {
                                    action();
                                } else {
                                    handleNotInteractive?.Invoke();
                                }

                                break;
                        }
                    } catch (Exception e) {
                        Logr.LogException(e);
                        throw;
                    }
                };
                var trigger = throttle == TimeSpan.Zero ? triggerAction : CreateThrottled(triggerAction, UniTask.Delay(throttle, cancellationToken: token));
                component.GetAsyncPointerClickTrigger()
                    .Subscribe(trigger, token);
            }
        }

        /// <summary>
        ///     create a throttled Action: when called, the throttled action is executed immediately then ignore subsequent calls
        ///     for a fixed duration
        /// </summary>
        public static Action CreateThrottled(Action action, UniTask delayTask) {
            bool throttling = false;

            async void ThrottledEventHandler() {
                if (throttling) return;
                action();
                throttling = true;
                await delayTask.SuppressCancellationThrow();
                throttling = false;
            }

            return ThrottledEventHandler;
        }

        /// <summary>
        ///     create a throttled Action: when called, the throttled action is executed immediately then ignore subsequent calls
        ///     for a fixed duration
        /// </summary>
        public static Action<TArgs> CreateThrottled<TArgs>(Action<TArgs> action, UniTask delayTask) {
            bool throttling = false;

            async void ThrottledEventHandler(TArgs e) {
                if (throttling) return;
                action(e);
                throttling = true;
                await delayTask.SuppressCancellationThrow();
                throttling = false;
            }

            return ThrottledEventHandler;
        }
    }
}