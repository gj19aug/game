using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct GameState
{
    // Player
    public PlayerShip player;

    // Weapons
    public Pool<WeaponRefs> weaponPool;
    public Pool<ProjectileRefs> projectilePool;
    public List<Projectile> projectiles;

    // Debris
    public Pool<DebrisRefs>[] debrisPools;

    // Enemies
    public Pool<ShipRefs>[] enemyPools;
    public RefList<EnemyShip> enemies;
    public Spawn[] enemySpawns;

    // Caches
    public Collider2D[] colliderCache;
    public RefList<Impact> impactCache;
}

[Serializable]
public class Refs : MonoBehaviour {}

[Serializable]
public struct ShipCommon
{
    public ShipRefs refs;
    public MoveState move;
    public ShipInput input;
    public List<Weapon> weapons;
    public int health;
}

[Serializable]
public struct PlayerShip
{
    public ShipCommon common;
    public List<DebrisRefs> debris;
}

[Serializable]
public struct EnemyShip
{
    public ShipCommon common;
    public AISpec aiSpec;

    // TODO: This is probably bad
    public ShipRefs target;
}

[Serializable]
public struct MoveState
{
    public Vector3 p;
    public Vector3 dp;
    public Vector3 look;
}

[Serializable]
public struct ShipInput
{
    public Vector3 throttle;
    public Vector3 point;
    public Vector3 aim;
    public bool shoot;
}

[Serializable]
public struct Weapon
{
    public WeaponRefs refs;
    public WeaponSpec spec;
    public Vector3 aim;
    public float nextRefireTime;
}

[Serializable]
public struct Projectile
{
    public ProjectileRefs refs;
    public WeaponSpec spec;
    public ShipRefs owner;
    public float lifetime;
}

[Serializable]
public struct Impact
{
    public WeaponSpec spec;
    public ShipRefs owner;
    public ShipRefs victim;
}

[Serializable]
public struct Spawn
{
    public SpawnRefs refs;
    public float nextSpawnTime;
    public List<ShipRefs> ships;
}

public static class Tag
{
    public static string Null = null;
    public static string Player = "Player";
    public static string Enemy = "Enemy";
}
