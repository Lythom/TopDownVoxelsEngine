using System;
using UnityEngine;

namespace LoneStoneStudio.Tools {
    /// <summary>
    /// Attribute that require implementation of the provided interface.
    /// </summary>
    public class RequireInterfaceAttribute : PropertyAttribute {
        // Interface type.
        public Type RequiredType { get; }

        /// <summary>
        /// Requiring implementation of the <see cref="T:RequireInterfaceAttribute"/> interface.
        /// </summary>
        /// <param name="type">Interface type.</param>
        public RequireInterfaceAttribute(Type type) {
            RequiredType = type;
        }
    }
}