using UnityEngine;

public class ShipRefs : Refs
{
    public new Rigidbody2D rigidbody;
    public Transform physicsTransform;
    public new PolygonCollider2D collider;
    public SpriteRenderer sprite;
    public ShipSpec shipSpec;
    public MagnetismSpec magnetismSpec;
}
