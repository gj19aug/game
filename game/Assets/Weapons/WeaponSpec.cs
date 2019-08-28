using UnityEngine;

[CreateAssetMenu(fileName = "New Weapon Spec", menuName = "Weapon Spec")]
public class WeaponSpec : ScriptableObject
{
    public WeaponRefs weaponPrefab;
    public ProjectileRefs projectilePrefab;
    public ImpactRefs impactPrefab;

    [Range(0.0f, 10.0f)]
    public float refireDelay = 0.1f;

    [Range(0.1f, 50.0f)]
    public float impulse = 20.0f;

    [Range(0.1f, 10.0f)]
    public float lifetime = 3.0f;

    [Tooltip("How fast the weapon is capable of turning. Units are incomprehensible.")]
    [Range(0, 1)]
    public float turnSpeed = 0.1f;

    [Range(1, 100)]
    public int damage = 1;
}
