using UnityEngine;

[CreateAssetMenu(fileName = "New Weapon Spec", menuName = "Weapon Spec")]
public class WeaponSpec : ScriptableObject
{
    [Range(0.0f, 10.0f)]
    public float refireDelay = 0.1f;

    [Range(0.1f, 50.0f)]
    public float impulse = 20.0f;

    [Range(0.1f, 10.0f)]
    public float lifetime = 3.0f;
}
