using UnityEngine;

public class PlayerRefs : MonoBehaviour
{
    public new Rigidbody2D rigidbody;
    public Transform physicsTransform;
    public new CircleCollider2D collider;
    public SpriteRenderer sprite;
    public MoveSpec moveSpec;
    public MagnetismSpec magnetismSpec;
}
