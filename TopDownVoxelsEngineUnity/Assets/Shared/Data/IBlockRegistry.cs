using System.Collections.Generic;

namespace Shared {
    public interface IRegistry<T> {
        Dictionary<string, T> Get();
        T? Get(string path);
    }

}