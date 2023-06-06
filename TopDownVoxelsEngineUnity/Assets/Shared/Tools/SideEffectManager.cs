using System;
using System.Collections.Generic;

namespace LoneStoneStudio.Tools {
    /**
     * From https://unity3d.com/fr/learn/tutorials/topics/scripting/events-creating-simple-messaging-system
     * Changes : remove the unused Singleton pattern, use static instead
     * : Use Generic To Match SideEffectType instead of string
     * : Add mute ability
     * : remove Unity dependency
     */
    public class SideEffectManager : IDisposable {
        private readonly Dictionary<Type, SideEffectDispatcher> _all = new();

        public void Trigger<T>(T args) {
            For<T>().Trigger(args);
        }

        public SideEffectDispatcher<T> For<T>() {
            if (_all.TryGetValue(typeof(T), out SideEffectDispatcher obs)) {
                return (SideEffectDispatcher<T>) obs;
            }

            var newObs = new SideEffectDispatcher<T>();
            _all.Add(typeof(T), newObs);
            return newObs;
        }

        public void MuteAll() {
            foreach (var sideEffectDispatcher in _all.Values) {
                sideEffectDispatcher.Mute();
            }
        }

        public abstract class SideEffectDispatcher : IDisposable {
            public abstract void Mute();
            public abstract void UnMute();
            public abstract void Dispose();
        }

        public class SideEffectDispatcher<T> : SideEffectDispatcher, IDisposable {
            private readonly List<Action<T>> _listeners = new();

            public Action StartListening(Action<T> listener) {
                _listeners.Add(listener);
                return () => StopListening(listener);
            }

            public void StopListening(Action<T> listener) {
                _listeners.Remove(listener);
            }

            public void Trigger(T args) {
                if (_muted) return;
                foreach (var listener in _listeners) {
                    listener.Invoke(args);
                }
            }

            private bool _muted = false;

            public override void Mute() {
                _muted = true;
            }

            public override void UnMute() {
                _muted = false;
            }

            public void Dispose() {
                _listeners.Clear();
            }
        }

        public void Dispose() {
            foreach (var sideEffectDispatcher in _all.Values) sideEffectDispatcher.Dispose();
        }
    }
}