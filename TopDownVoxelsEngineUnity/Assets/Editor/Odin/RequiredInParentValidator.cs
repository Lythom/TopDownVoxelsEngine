using Sirenix.OdinInspector.Editor.Validation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[assembly: RegisterValidator(typeof(RequiredInParentValidator<>))]

// Use of generics here because polymorphism does not work to trigger the validation of an subtype
// It means a validator of Component triggers only on Component,
// whereas a validator of <T> where T : Component triggers on all Type inheriting Component.
public class RequiredInParentValidator<T> : AttributeValidator<RequiredInParentAttribute, T> where T : Component {
    protected override void Validate(ValidationResult result) {
        var parent = (Component?) result.Setup.ParentInstance;
        // ReSharper disable once Unity.NoNullPropagation
        var parentGameObject = parent?.gameObject;
        // skip check if script is in an asset
        if (parentGameObject == null || PrefabUtility.IsPartOfPrefabAsset(parentGameObject) || PrefabStageUtility.GetCurrentPrefabStage() != null) return;

        var parentOfExpectedType = ValueEntry.SmartValue.GetComponentInParent(Attribute.ExpectedType, true);
        if (parentOfExpectedType == null) {
            result.ResultType = ValidationResultType.Error;
            result.Message = $"There should be a parent of type {Attribute.ExpectedType.Name} in the hierarchy.";
        }
    }
}