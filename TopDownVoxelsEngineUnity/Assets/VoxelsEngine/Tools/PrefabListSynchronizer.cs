using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Pool;
using Object = UnityEngine.Object;

namespace LoneStoneStudio.Tools.Components {
    /// <summary>
    /// Use a pool to instantiate, activate or disable instances of a prefab inside a container.
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    [Serializable, InlineProperty(LabelWidth = 80)]
    public class PrefabListSynchronizer<T> where T : MonoBehaviour, IDisposable {
        [Required, ChildGameObjectsOnly]
        public GameObject Container = null!;

        [Required, AssetsOnly]
        public T Prefab = null!;

        private readonly ObjectPool<T> _pool;
        private readonly List<T> _instances = new();
        private readonly List<T> _toDisable = new();

        public PrefabListSynchronizer() {
            _pool = new(
                () => Object.Instantiate(Prefab, Container.transform),
                null,
                poiui => poiui.Dispose()
            );
        }

        /// <summary>
        /// Synchronize the list of displayed instances of the <see cref="Prefab"/> in the <see cref="Container"/>: send unused instances to pool or fetch new instances as needed.
        /// Declare and configure one instance per item you want to have in the final list in the declareInstances callback, the function will handle the re-sync.
        /// </summary>
        /// <param name="declareInstances">
        ///     <param>Func{T} getInstance Call the function to get a new instance, then initialize it.</param>
        /// </param>
        /// <param name="onDisable"></param>
        /// <param name="onEnable"></param>
        public void DisplayInstances(Action<Func<T>> declareInstances, Action<T>? onDisable = null, Action<T>? onEnable = null) {
            // Clear instances : refill the pool and consider all instances as _toDisable, they will be removed from _toDisable later is reused
            _toDisable.Clear();
            foreach (var instance in _instances) {
                _pool.Release(instance);
                // disable later if not reused to prevent the cost of active false then true again when instance is already active
                _toDisable.Add(instance);
            }

            _instances.Clear();

            declareInstances(DeclareInstance);

            // disable unused instances
            foreach (var poiui in _toDisable) {
                poiui.SmartActive(false);
                onDisable?.Invoke(poiui);
            }

            // activate used instances
            foreach (var poiui in _instances) {
                poiui.SmartActive(true);
                onEnable?.Invoke(poiui);
            }
        }

        private T DeclareInstance() {
            var instance = _pool.Get();
            _instances.Add(instance);
            _toDisable.Remove(instance);
            instance.transform.SetAsLastSibling();
            return instance;
        }
    }
}