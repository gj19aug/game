using UnityEngine;

[CreateAssetMenu(fileName = "New Move Spec", menuName = "Move Spec")]
public class MoveSpec : ScriptableObject
{
    //[Header("The effective movement formula is x1 = x0 + 0.5 (drag * dragCurve(vMul * v) * aMul * input) * t^2 + vt")]

    [Tooltip("Determines how fast the character will accelerate from a stop. Also increases top speed.")]
    [Range(0, 100)]
    public float acceleration = 30.0f;

    [Tooltip("Determines how fast the character will decelerate and stop. Also decreases top speed.")]
    [Range(0, 10)]
    public float drag = 4.0f;

    [Tooltip("Determines how much of the drag set above is applied based on the characters current speed. Current speed is the x input. The y output is multiplied against the drag value above.")]
    public AnimationCurve dragCurve = AnimationCurve.Linear(0.0f, 0.0f, 1.0f, 1.0f);

    [Tooltip("Multiplier for the characters current speed before feeding it into the drag curve above. This is a cheap hack that can be used to ensure the drag curve is active across the entire velocity range.")]
    [Range(0, 1)]
    public float velocityMultiplierForDrag = 0.2f;
}
