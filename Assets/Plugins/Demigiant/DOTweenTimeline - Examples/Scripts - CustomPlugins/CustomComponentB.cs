using UnityEngine;

/// <summary>
/// At runtime, this component simply displays the value of its public properties, updating them every frame
/// </summary>
[RequireComponent(typeof(TextMesh))]
public class CustomComponentB : MonoBehaviour
{
    public Vector3 sampleVector3 = new Vector3(2, 5, -9);
    public Color sampleColor = Color.magenta;

    TextMesh _textMesh;

    void Start()
    {
        _textMesh = this.GetComponent<TextMesh>();
    }

    void Update()
    {
        _textMesh.text = string.Format(
            "CustomComponentB values:\nsampleVector3: {0}\nsampleColor: {1}",
            sampleVector3, sampleColor
        );
    }
}