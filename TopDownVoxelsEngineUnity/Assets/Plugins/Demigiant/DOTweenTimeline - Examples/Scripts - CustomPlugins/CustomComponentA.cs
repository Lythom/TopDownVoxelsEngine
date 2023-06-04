using UnityEngine;
#nullable disable

/// <summary>
/// At runtime, this component simply displays the value of its public properties, updating them every frame
/// </summary>
[RequireComponent(typeof(TextMesh))]
public class CustomComponentA : MonoBehaviour
{
    public float sampleFloat = 10.5f;
    public Vector2 sampleVector2 = new Vector2(-40, 26);
    public string sampleString = "Sample text hello woobleedeewoobleedeedoo";

    TextMesh _textMesh;

    void Start()
    {
        _textMesh = this.GetComponent<TextMesh>();
    }

    void Update()
    {
        _textMesh.text = string.Format(
            "CustomComponentA values:\nsampleFloat: {0}\nsampleVector2: {1}\nsampleString: {2}",
            sampleFloat, sampleVector2, sampleString
        );
    }
}