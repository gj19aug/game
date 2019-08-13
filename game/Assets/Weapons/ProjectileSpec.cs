using UnityEngine;

[CreateAssetMenu(fileName = "New Projectile Spec", menuName = "Projectile Spec")]
public class ProjectileSpec : ScriptableObject
{
    [Range(0.1f, 50.0f)]
    public float impulse = 20.0f;

    [Range(0.1f, 10.0f)]
    public float lifetime = 3.0f;
}
