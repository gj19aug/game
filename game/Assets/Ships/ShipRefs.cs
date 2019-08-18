using UnityEngine;

public class ShipRefs : MonoBehaviour
{
    public new Rigidbody2D rigidbody;
    public Transform physicsTransform;
    public new PolygonCollider2D collider;
    public SpriteRenderer sprite;
    public MoveSpec moveSpec;
    public MagnetismSpec magnetismSpec;
}
