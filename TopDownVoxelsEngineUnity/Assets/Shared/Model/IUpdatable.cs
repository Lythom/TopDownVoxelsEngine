namespace Shared {
    public interface IUpdatable<in T> {
        void UpdateValue(T nextState);
    }
}