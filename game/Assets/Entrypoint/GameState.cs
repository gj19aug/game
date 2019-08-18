using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct GameState
{
    // Player
    public MoveState playerMove;
    public ShipInput playerInput;
    public List<DebrisRefs> playerDebris;

    // Projectiles
    public Pool<ProjectileRefs> projectilePool;
    public List<Projectile> projectiles;

    // Debris
    public Pool<DebrisRefs>[] debrisPools;

    // Cache
    public Collider2D[] colliderCache;
}

[Serializable]
public struct MoveState
{
    public Vector3 p;
    public Vector3 dp;
}

[Serializable]
public enum ShipInputEvent
{
    Null,
    Shoot,
}

[Serializable]
public struct ShipInput
{
    public Vector3 throttle;
    public Vector3 aim;
    public List<ShipInputEvent> events;
}

[Serializable]
public struct Projectile
{
    public ProjectileRefs refs;
    public float lifetime;
}

public static class Tag
{
    public static string Null = null;
    public static string Player = "Player";
    public static string Enemy = "Enemy";
}
