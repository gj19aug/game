using UnityEngine;

[CreateAssetMenu(fileName = "New Magnetism Spec", menuName = "Magnetism Spec")]
public class MagnetismSpec : ScriptableObject
{
    // TODO: This is actually going to change over time. Can't just be a constant.
    [Range(0, 5)]
    public float radius = 5;

    [Range(0, 500)]
    public float strength = 10;

    public AnimationCurve strengthCurve = new AnimationCurve(
        new Keyframe(0, 1, -2, -2),
        new Keyframe(1, 0, 0, 0)
    );

    [Range(-1, 1)]
    public float packing = 0.1f;

    // NOTE: Can't provide a default because Unity is garbage
    public LayerMask affectedLayers;
}
