using UnityEngine;

[CreateAssetMenu(fileName = "New Camera Spec", menuName = "Camera Spec")]
public class CameraSpec : ScriptableObject
{
    [Range(0.0f, 0.5f)]
    public float lerpFactor = 0.03f;

    [Range(1.0f, 1000.0f)]
    public float baseSize = 7.0f;

    [Range(0.0f, 0.5f)]
    public float sizeScaleRate = 0.05f;
}
