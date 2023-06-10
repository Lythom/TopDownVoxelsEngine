using System;
using UnityEngine;

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Class)]
public class ActivatedByDefaultAttribute : PropertyAttribute {
    public bool ShouldBeActive;

    public ActivatedByDefaultAttribute(bool shouldBeActive) {
        ShouldBeActive = shouldBeActive;
    }
}