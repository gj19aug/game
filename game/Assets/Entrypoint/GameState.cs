using System.Collections.Generic;
using UnityEngine;

public struct GameState
{
    public MoveState playerMove;
    public ShipInput playerInput;
    public Pool<ProjectileRefs> projectilePool;
    public List<Projectile> projectiles;
}

public struct MoveState
{
    public Vector3 p;
    public Vector3 dp;
}

public enum ShipInputEvent
{
    Null,
    Shoot,
}

// TODO: HAAAATE.
public struct ShipInput
{
    public Vector3 throttle;
    public Vector3 aim;
    public List<ShipInputEvent> events;
}

public struct Projectile
{
    public ProjectileRefs refs;
    public float lifetime;
}
