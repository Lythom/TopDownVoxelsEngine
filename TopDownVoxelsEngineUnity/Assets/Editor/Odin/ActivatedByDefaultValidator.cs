using Sirenix.OdinInspector.Editor.Validation;
using UnityEngine;

[assembly: RegisterValidator(typeof(ActivatedByDefaultValidator<>))]

// Use of generics here because polymorphism does not work to trigger the validation of an subtype
// It means a validator of Component triggers only on Component,
// whereas a validator of <T> where T : Component triggers on all Type inheriting Component.
public class ActivatedByDefaultValidator<T> : AttributeValidator<ActivatedByDefaultAttribute, T> where T : Object {
    protected override void Validate(ValidationResult result) {
        if (ValueEntry.SmartValue == null)
            return;

        if (ValueEntry.SmartValue is Component c && c.gameObject.activeSelf != Attribute.ShouldBeActive
            || ValueEntry.SmartValue is GameObject o && o.activeSelf != Attribute.ShouldBeActive
           ) {
            result.ResultType = ValidationResultType.Error;
            result.Message = $"GameObject {ValueEntry.SmartValue.name} should be {(Attribute.ShouldBeActive ? "active" : "disabled")} by default";
        }
    }
}