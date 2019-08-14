using UnityEngine;

[CreateAssetMenu(fileName = "New Camera Spec", menuName = "Camera Spec")]
public class CameraSpec : ScriptableObject
{
    [Range(0.0f, 0.5f)]
    public float lerpFactor = 0.09f;
}
