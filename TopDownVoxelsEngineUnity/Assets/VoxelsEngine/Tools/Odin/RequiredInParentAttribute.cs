using System;
using UnityEngine;

/// <summary>
///     Triggers a "Required" error if no parent with the specified type exists and the
///     is not in a prefab asset (= is instantiated in a scene)
/// </summary>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Class)]
public class RequiredInParentAttribute : PropertyAttribute {
    public Type ExpectedType;

    public RequiredInParentAttribute(Type expectedType) {
        ExpectedType = expectedType;
    }
}