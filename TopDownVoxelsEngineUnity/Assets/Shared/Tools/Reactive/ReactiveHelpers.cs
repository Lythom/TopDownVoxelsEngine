using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.Linq;
using Shared;
using StarTeam.Tools;

#if !UNITY_EDITOR && !UNITY_2018_3_OR_NEWER
using YieldAwaitable = Cysharp.Threading.Tasks.UniTask.YieldAwaitable;
#endif

namespace LoneStoneStudio.Tools {
    public static class ReactiveHelpers {
        /// <summary>
        /// Update <paramref name="target"/> value from a reactive pointer <paramref name="source"/> used to reference a reactive source using <paramref name="compoundGetter"/> which expose the final value we want.
        /// <example>mySelector.BindCompoundValue(local.SelectedCharacter, s => game.Characters[s].Coordinates, _selectorsSubscription.Token)</example>
        /// In this example, mySelector will react when either SelectedCharacter or game.Characters[SelectedCharacter].Coordinates is changed.<br/>
        /// If <paramref name="compoundGetter" /> returns null, then the subscription will pause until it return non-null and the target will reset to it's provided <paramref name="defaultValue" />.<br/>
        /// If provided, parameter <paramref name="updateTargetHandler" /> MUST modify the target.Value or the selector won't update.
        /// </summary>
        /// <param name="target">Reactive to update</param>
        /// <param name="source">Reactive that indicate a selector data to observe</param>
        /// <param name="compoundGetter">Callback to select the Reactive to observe changes from</param>
        /// <param name="updateTargetHandler">Specify the implementation of the target value update. Ie. <code>(target, nextValue, cancelToken) => target.Value = nextValue</code>
        /// A cancellationToken is provided in case the update logic should be asynchronous (like with a Debounce).</param>
        /// <param name="defaultValue">Value to reset to if the compoundSourceGetter returns null.</param>
        /// <param name="cancellationToken">Subscription is automatically cancelled with this token</param>
        /// <typeparam name="TS">Source type</typeparam>
        /// <typeparam name="TS2">Compound source type</typeparam>
        /// <typeparam name="TT">Target type</typeparam>
        /// <returns></returns>
        public static void BindCompoundValue<TS, TS2, TT>(
            this IAsyncReactiveProperty<TT> target,
            IUniTaskAsyncEnumerable<TS> source,
            Func<TS, IUniTaskAsyncEnumerable<TS2>?> compoundGetter,
            Action<IAsyncReactiveProperty<TT>, TS2, CancellationToken> updateTargetHandler,
            TT defaultValue,
            CancellationToken cancellationToken
        ) {
            CancellationTokenSource? cancelSource = null;
            CancellationTokenRegistration? registration = null;
            source.ForEachAsync(s => {
                cancelSource?.Cancel();
                cancelSource = null;
                registration?.Dispose();
                registration = null;
                try {
                    var chained = compoundGetter(s);
                    if (chained == null) {
                        target.Value = defaultValue;
                        return;
                    }

                    cancelSource = new CancellationTokenSource();
                    registration = cancellationToken.Register(() => cancelSource.Cancel()); // cascade cancellation of source cancellationToken
                    chained.ForEachAsync(v => updateTargetHandler(target, v, cancelSource.Token), cancelSource.Token).Forget();
                } catch (Exception e) {
                    Console.Error.WriteLine("Error in binding. Cancelling subscription to prevent further errors.\n" + e);
                    throw;
                }
            }, cancellationToken);
        }

        public static void BindCompoundValue<TS, TT>(
            this IAsyncReactiveProperty<TT?> target,
            IAsyncReactiveProperty<TS> source,
            Func<TS, IAsyncReactiveProperty<TT?>?> compoundSourceGetter,
            CancellationToken cancellationToken
        ) {
            var defaultValue = target.Value;
            BindCompoundValue(target, source, compoundSourceGetter, (t, v, _) => t.Value = v, defaultValue, cancellationToken);
        }

        /// <summary>
        /// Update reactive target value anytime one of the sources reactive is updated.
        /// Next value is calculated using the calculateValue callback. The callback is provided the last value updated in the list.
        /// </summary>
        /// <param name="target">Reactive to update</param>
        /// <param name="sources">Reactive to observe changes from</param>
        /// <param name="calculateValue">Callback to calculate the next value that target should take</param>
        /// <param name="cancellationToken">Subscription is automatically cancelled with this token</param>
        /// <typeparam name="TT">Type of the target</typeparam>
        /// <typeparam name="TS">Type of the sources</typeparam>
        public static void BindLatest<TT, TS>(
            this IAsyncReactiveProperty<TT> target,
            IEnumerable<IAsyncReactiveProperty<TS>> sources,
            Func<TS, TT> calculateValue,
            CancellationToken cancellationToken
        ) {
            IAsyncReactiveProperty<TS>? last = null;
            foreach (var source in sources) {
                source.WithoutCurrent().ForEachAsync(s => {
                    try {
                        target.Value = calculateValue(s);
                    } catch (Exception e) {
                        Console.Error.WriteLine("Error in binding. Cancelling subscription to prevent further errors.\n" + e);
                        throw;
                    }
                }, cancellationToken);
                last = source;
            }

            // trigger initial update only once
            if (last != null) target.Value = calculateValue(last.Value);
        }

        /// <summary>
        /// Update reactive target value anytime the source reactive is updated. No debounce.
        /// Next value is calculated using the calculateValue callback.
        /// </summary>
        /// <param name="target">Reactive to update</param>
        /// <param name="source">Reactive to observe changes from</param>
        /// <param name="calculateValue">Callback to calculate the next value that target should take</param>
        /// <param name="cancellationToken">Subscription is automatically cancelled with this token</param>
        /// <typeparam name="TT">Type of the target</typeparam>
        /// <typeparam name="TS">Type of the source</typeparam>
        public static void Bind<TT, TS>(
            this IAsyncReactiveProperty<TT> target,
            IUniTaskAsyncEnumerable<TS> source,
            Func<TS, TT> calculateValue,
            CancellationToken cancellationToken
        ) {
            source.ForEachAsync(s => {
                try {
                    target.Value = calculateValue(s);
                } catch (Exception e) {
                    Console.Error.WriteLine("Error in binding. Cancelling subscription to prevent further errors.\n" + e);
                    throw;
                }
            }, cancellationToken);
        }

        public static void Bind<TS, TS2, TT>(
            this Reactive<TT> target,
            IUniTaskAsyncEnumerable<TS> source,
            IUniTaskAsyncEnumerable<TS2> source2,
            Func<TS, TS2, TT> calculateValue,
            CancellationToken cancellationToken
        ) {
            target.Bind(
                UniTaskAsyncEnumerable.CombineLatest(source, source2, Fns.Tuplify),
                args => calculateValue(args.Item1, args.Item2),
                cancellationToken
            );
        }

        public static void Bind<TS, TS2, TS3, TT>(
            this Reactive<TT> target,
            IUniTaskAsyncEnumerable<TS> source,
            IUniTaskAsyncEnumerable<TS2> source2,
            IUniTaskAsyncEnumerable<TS3> source3,
            Func<TS, TS2, TS3, TT> calculateValue,
            CancellationToken cancellationToken
        ) {
            target.Bind(
                UniTaskAsyncEnumerable.CombineLatest(source, source2, source3, Fns.Tuplify),
                args => calculateValue(args.Item1, args.Item2, args.Item3),
                cancellationToken
            );
        }

        public static void Bind<TS, TS2, TS3, TS4, TT>(
            this Reactive<TT> target,
            IUniTaskAsyncEnumerable<TS> source,
            IUniTaskAsyncEnumerable<TS2> source2,
            IUniTaskAsyncEnumerable<TS3> source3,
            IUniTaskAsyncEnumerable<TS4> source4,
            Func<TS, TS2, TS3, TS4, TT> calculateValue,
            CancellationToken cancellationToken
        ) {
            target.Bind(
                UniTaskAsyncEnumerable.CombineLatest(source, source2, source3, source4, Fns.Tuplify),
                args => calculateValue(args.Item1, args.Item2, args.Item3, args.Item4),
                cancellationToken
            );
        }


        /// <summary>
        /// Create a reactive selector from a reactive source.
        /// Debounced so that it is recalculated once per frame max.
        /// Will be calculated before Subscribes.
        /// </summary>
        public static Reactive<TTarget> CreateSelector<TSource, TTarget>(
            IUniTaskAsyncEnumerable<TSource> source,
            Func<TSource, TTarget> valueGetter,
            TTarget initialValue,
            CancellationToken cancellationToken
        ) {
            var selector = new Reactive<TTarget>(initialValue);
            selector.Bind(source, valueGetter, cancellationToken);
            return selector;
        }

        public static Reactive<TTarget> CreateSelector<TSource, TSource2, TTarget>(
            IUniTaskAsyncEnumerable<TSource> source,
            IUniTaskAsyncEnumerable<TSource2> source2,
            Func<TSource, TSource2, TTarget> valueGetter,
            TTarget initialValue,
            CancellationToken cancellationToken
        ) {
            return CreateSelector(
                UniTaskAsyncEnumerable.CombineLatest(source, source2, Fns.Tuplify),
                args => valueGetter(args.Item1, args.Item2),
                initialValue,
                cancellationToken
            );
        }

        public static Reactive<TTarget> CreateSelector<TSource, TSource2, TSource3, TTarget>(
            IUniTaskAsyncEnumerable<TSource> source,
            IUniTaskAsyncEnumerable<TSource2> source2,
            IUniTaskAsyncEnumerable<TSource3> source3,
            Func<TSource, TSource2, TSource3, TTarget> valueGetter,
            TTarget initialValue,
            CancellationToken cancellationToken
        ) {
            return CreateSelector(
                UniTaskAsyncEnumerable.CombineLatest(source, source2, source3, Fns.Tuplify),
                args => valueGetter(args.Item1, args.Item2, args.Item3),
                initialValue,
                cancellationToken
            );
        }

        public static Reactive<TTarget> CreateSelector<TSource, TSource2, TSource3, TSource4, TTarget>(
            IUniTaskAsyncEnumerable<TSource> source,
            IUniTaskAsyncEnumerable<TSource2> source2,
            IUniTaskAsyncEnumerable<TSource3> source3,
            IUniTaskAsyncEnumerable<TSource4> source4,
            Func<TSource, TSource2, TSource3, TSource4, TTarget> valueGetter,
            TTarget initialValue,
            CancellationToken cancellationToken
        ) {
            return CreateSelector(
                UniTaskAsyncEnumerable.CombineLatest(source, source2, source3, source4, Fns.Tuplify),
                args => valueGetter(args.Item1, args.Item2, args.Item3, args.Item4),
                initialValue,
                cancellationToken
            );
        }

        public static Reactive<TTarget> CreateSelector<TSource, TSource2, TSource3, TSource4, TSource5, TTarget>(
            IUniTaskAsyncEnumerable<TSource> source,
            IUniTaskAsyncEnumerable<TSource2> source2,
            IUniTaskAsyncEnumerable<TSource3> source3,
            IUniTaskAsyncEnumerable<TSource4> source4,
            IUniTaskAsyncEnumerable<TSource5> source5,
            Func<TSource, TSource2, TSource3, TSource4, TSource5,
                TTarget> valueGetter,
            TTarget initialValue,
            CancellationToken cancellationToken
        ) {
            return CreateSelector(
                UniTaskAsyncEnumerable.CombineLatest(source, source2, source3, source4, source5, Fns.Tuplify),
                args => valueGetter(args.Item1, args.Item2, args.Item3, args.Item4, args.Item5),
                initialValue,
                cancellationToken
            );
        }

        public static Reactive<TTarget> CreateSelector<TSource, TSource2, TSource3, TSource4, TSource5, TSource6, TTarget>(
            IUniTaskAsyncEnumerable<TSource> source,
            IUniTaskAsyncEnumerable<TSource2> source2,
            IUniTaskAsyncEnumerable<TSource3> source3,
            IUniTaskAsyncEnumerable<TSource4> source4,
            IUniTaskAsyncEnumerable<TSource5> source5,
            IUniTaskAsyncEnumerable<TSource6> source6,
            Func<TSource, TSource2, TSource3, TSource4, TSource5, TSource6,
                TTarget> valueGetter,
            TTarget initialValue,
            CancellationToken cancellationToken
        ) {
            return CreateSelector(
                UniTaskAsyncEnumerable.CombineLatest(source, source2, source3, source4, source5, source6, Fns.Tuplify),
                args => valueGetter(args.Item1, args.Item2, args.Item3, args.Item4, args.Item5, args.Item6),
                initialValue,
                cancellationToken
            );
        }

        public static Reactive<TTarget> CreateSelector<TSource, TSource2, TSource3, TSource4, TSource5, TSource6, TSource7, TTarget>(
            IUniTaskAsyncEnumerable<TSource> source,
            IUniTaskAsyncEnumerable<TSource2> source2,
            IUniTaskAsyncEnumerable<TSource3> source3,
            IUniTaskAsyncEnumerable<TSource4> source4,
            IUniTaskAsyncEnumerable<TSource5> source5,
            IUniTaskAsyncEnumerable<TSource6> source6,
            IUniTaskAsyncEnumerable<TSource7> source7,
            Func<TSource, TSource2, TSource3, TSource4, TSource5, TSource6, TSource7,
                TTarget> valueGetter,
            TTarget initialValue,
            CancellationToken cancellationToken
        ) {
            return CreateSelector(
                UniTaskAsyncEnumerable.CombineLatest(source, source2, source3, source4, source5, source6, source7, Fns.Tuplify),
                args => valueGetter(args.Item1, args.Item2, args.Item3, args.Item4, args.Item5, args.Item6, args.Item7),
                initialValue,
                cancellationToken
            );
        }

        public static Reactive<TTarget> CreateSelector<TSource, TSource2, TSource3, TSource4, TSource5, TSource6, TSource7, TSource8, TTarget>(
            IUniTaskAsyncEnumerable<TSource> source,
            IUniTaskAsyncEnumerable<TSource2> source2,
            IUniTaskAsyncEnumerable<TSource3> source3,
            IUniTaskAsyncEnumerable<TSource4> source4,
            IUniTaskAsyncEnumerable<TSource5> source5,
            IUniTaskAsyncEnumerable<TSource6> source6,
            IUniTaskAsyncEnumerable<TSource7> source7,
            IUniTaskAsyncEnumerable<TSource8> source8,
            Func<TSource, TSource2, TSource3, TSource4, TSource5, TSource6, TSource7, TSource8,
                TTarget> valueGetter,
            TTarget initialValue,
            CancellationToken cancellationToken
        ) {
            return CreateSelector(
                UniTaskAsyncEnumerable.CombineLatest(source, source2, source3, source4, source5, source6, source7, source8, Fns.Tuplify),
                args => valueGetter(args.Item1, args.Item2, args.Item3, args.Item4, args.Item5, args.Item6, args.Item7, args.Item8),
                initialValue,
                cancellationToken
            );
        }

        public static Reactive<TTarget> CreateSelector<TSource, TSource2, TSource3, TSource4, TSource5, TSource6, TSource7, TSource8, TSource9, TTarget>(
            IUniTaskAsyncEnumerable<TSource> source,
            IUniTaskAsyncEnumerable<TSource2> source2,
            IUniTaskAsyncEnumerable<TSource3> source3,
            IUniTaskAsyncEnumerable<TSource4> source4,
            IUniTaskAsyncEnumerable<TSource5> source5,
            IUniTaskAsyncEnumerable<TSource6> source6,
            IUniTaskAsyncEnumerable<TSource7> source7,
            IUniTaskAsyncEnumerable<TSource8> source8,
            IUniTaskAsyncEnumerable<TSource9> source9,
            Func<TSource, TSource2, TSource3, TSource4, TSource5, TSource6, TSource7, TSource8, TSource9,
                TTarget> valueGetter,
            TTarget initialValue,
            CancellationToken cancellationToken
        ) {
            return CreateSelector(
                UniTaskAsyncEnumerable.CombineLatest(source, source2, source3, source4, source5, source6, source7, source8, source9, Fns.Tuplify),
                args => valueGetter(args.Item1, args.Item2, args.Item3, args.Item4, args.Item5, args.Item6, args.Item7, args.Item8, args.Item9),
                initialValue,
                cancellationToken
            );
        }

        /// <summary>
        /// Update a list of IUpdatable using targetState as final state reference, optimising existing memory reuse.
        /// It will reuse existing items using <see cref="IUpdatable{T}.UpdateValue"/>.
        /// <param name="copyItem"/> is used to create new items if the current list is not long enough.
        /// If current list is too long for target, it is truncated.
        /// </summary>
        public static void UpdateList<T>(IList<T> current, IList<T>? targetState, Func<T, T> copyItem) where T : IUpdatable<T> {
            if (targetState == null) {
                current.Clear();
                return;
            }

            var diff = current.Count - targetState.Count;
            for (int i = 0; i < diff; i++) {
                current.RemoveAt(current.Count - 1);
            }

            for (int i = 0; i < targetState.Count; i++) {
                if (current.Count <= i) {
                    current.Add(copyItem(targetState[i]));
                } else {
                    if (current[i] == null) {
                        current[i] = copyItem(targetState[i]);
                    } else {
                        current[i].UpdateValue(targetState[i]);
                    }
                }
            }
        }

        /// <summary>
        /// Update a list of items using targetState as final state reference.
        /// It will reuse existing items if equal.
        /// <param name="copyItem"/> is used to create new items.
        /// If current list is too long for target, it is truncated.
        /// </summary>
        public static void UpdateListOfEquatables<T>(IList<T> currentToBeUpdated, IList<T>? targetState, Func<T, T> copyItem) where T : IEquatable<T> {
            if (targetState == null) {
                currentToBeUpdated.Clear();
                return;
            }

            var diff = currentToBeUpdated.Count - targetState.Count;
            for (int i = 0; i < diff; i++) {
                currentToBeUpdated.RemoveAt(currentToBeUpdated.Count - 1);
            }

            for (int i = 0; i < targetState.Count; i++) {
                if (currentToBeUpdated.Count <= i) {
                    currentToBeUpdated.Add(copyItem(targetState[i]));
                } else {
                    if (currentToBeUpdated[i] is not IEquatable<T> curr || !curr.Equals(targetState[i])) {
                        currentToBeUpdated[i] = copyItem(targetState[i]);
                    }
                }
            }
        }

        /// <summary>
        /// Update a Dictionary of IUpdatable using targetState as final state reference, optimising existing memory reuse.
        /// It will reuse existing items using <see cref="IUpdatable{T}.UpdateValue"/>.
        /// <param name="copyItem"/> is used to create new items if there is no existing entry.
        /// </summary>
        public static void UpdateDictionary<TId, T>(IDictionary<TId, T> current, IDictionary<TId, T>? targetValue, Func<T, T> copyItem) where T : IUpdatable<T> {
            UpdateDictionary(current, targetValue, copyItem, (curr, target) => curr.UpdateValue(target));
        }

        /// <summary>
        /// Update a Dictionary of generic values using targetState as final state reference, optimising existing memory reuse.
        /// It will reuse existing items using the provided <param name="updateValue"/>.
        /// <param name="createItem"/> is used to create new items if there is no existing entry to reuse.
        /// </summary>
        public static void UpdateDictionary<TId, T>(IDictionary<TId, T> current, IDictionary<TId, T>? targetValue, Func<T, T> createItem, Action<T, T>? updateValue) {
            if (targetValue == null) {
                current.Clear();
                return;
            }

            // remove old
            List<TId> removeList = new List<TId>();
            foreach (var (key, _) in current) {
                if (!targetValue.ContainsKey(key)) {
                    removeList.Add(key);
                }
            }

            foreach (var item in removeList) {
                current.Remove(item);
            }

            // add or update
            foreach (var (key, nextItem) in targetValue) {
                if (updateValue != null && current.TryGetValue(key, out var value)) {
                    updateValue(value, nextItem);
                } else {
                    current[key] = createItem(nextItem);
                }
            }
        }

        /// <summary>
        /// Update a Dictionary of generic values using targetState as final state reference, optimising existing memory reuse.
        /// It will reuse existing items using the Reactive value setter.
        /// A new reactive is create to add new items if there is no existing entry to reuse.
        /// </summary>
        public static void UpdateDictionary<TId, T>(Dictionary<TId, Reactive<T>> current, Dictionary<TId, Reactive<T>>? targetValue) {
            if (targetValue == null) {
                current.Clear();
                return;
            }

            // remove old
            List<TId> removeList = new List<TId>();
            foreach (var (key, _) in current) {
                if (!targetValue.ContainsKey(key)) {
                    removeList.Add(key);
                }
            }

            foreach (var item in removeList) {
                current.Remove(item);
            }

            // add or update
            foreach (var (key, nextItem) in targetValue) {
                if (current.ContainsKey(key)) {
                    current[key].Value = nextItem.Value;
                } else {
                    current[key] = new Reactive<T>(nextItem);
                }
            }
        }
    }
}