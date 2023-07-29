using System;
using UnityEngine;

/// <summary>
///     Triggers a "Required" error if the reference is missing, just like [Required], but only if the parent game object
///     is not in a prefab asset (= is instantiated in a scene)
/// </summary>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public class RequiredInSceneAttribute : PropertyAttribute {
}