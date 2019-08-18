using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct GameState
{
    // Player
    public PlayerShip player;

    // Projectiles
    public Pool<ProjectileRefs> projectilePool;
    public List<Projectile> projectiles;

    // Debris
    public Pool<DebrisRefs>[] debrisPools;

    // Enemies
    public Pool<ShipRefs>[] enemyPools;
    public List<EnemyShip> enemies;

    // Cache
    public Collider2D[] colliderCache;

    // Debug
    public Vector3 lastProjectileSpawn;
}

[Serializable]
public struct PlayerShip
{
    public ShipRefs refs;
    public MoveState move;
    public ShipInput input;
    public List<DebrisRefs> debris;
}

[Serializable]
public struct EnemyShip
{
    public ShipRefs refs;
    public MoveState move;
    public ShipInput input;
    public AISpec aiSpec;

    // TODO: This is probably bad
    public ShipRefs target;
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
