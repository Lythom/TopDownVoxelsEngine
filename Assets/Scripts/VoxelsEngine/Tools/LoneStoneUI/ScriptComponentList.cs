using System.Collections.Generic;
using JetBrains.Annotations;
using Sirenix.OdinInspector;

namespace LoneStoneStudio.Tools {
    /// <summary>
    ///     Data holder component to populate lists easily and safely
    /// </summary>
    /// <typeparam name="T">Type of elements to hold</typeparam>
    public class ScriptComponentList<T> : LoneStoneBehaviour {
        public int ExpectedCount = 6;

        [Required, ChildGameObjectsOnly, ValidateInput("ProvideExpectedCountElements", "@ExpectedCount + \" elements must be provided\"")]
        public List<T> Elements = null!;

        [UsedImplicitly]
        bool ProvideExpectedCountElements(List<T>? value) => value != null && value.Count == ExpectedCount;

        [Button]
        void UpdateWithChildren() {
            Elements.AddRange(GetComponentsInChildren<T>());
        }
    }
}