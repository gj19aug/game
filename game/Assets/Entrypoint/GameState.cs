using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct GameState
{
    // Data
    public PlayerShip player;
    public List<Projectile> projectiles;
    public RefList<ImpactEffect> impactEffects;
    public RefList<ExplosionEffect> explosionEffects;
    public RefList<EnemyShip> enemies;
    public Spawn[] enemySpawns;

    // Caches
    // HACK: 'object' here is a Pool<T> where T : Refs. Can't put the actual type because C# won't
    // let you cast between Pool<T> and Pool<Refs> unless going through a covariant interface *sigh*
    public Dictionary<Refs, object> pools;
    public Collider2D[] colliderCache;
    public RefList<Impact> impactCache;
}

[Serializable]
public class Refs : MonoBehaviour {}

[Serializable]
public struct ShipCommon
{
    public ShipSpec spec;
    public ShipRefs refs;
    public MoveState move;
    public ShipInput input;
    public RefList<Weapon> weapons;
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
    public Vector3 position;
}

[Serializable]
public struct ImpactEffect
{
    public ImpactRefs refs;
    public float lifetime;
}

[Serializable]
public struct ExplosionEffect
{
    public ExplosionRefs refs;
    public float lifetime;
}

[Serializable]
public struct Spawn
{
    public SpawnRefs refs;
    public float nextSpawnTime;
    public List<ShipRefs> ships;
}

public static class Layers
{
    public static int Player = LayerMask.NameToLayer("Player");
    public static int Enemy = LayerMask.NameToLayer("Enemy");
    public static int Debris = LayerMask.NameToLayer("Debris");
    public static int PlayerProjectile = LayerMask.NameToLayer("Player Projectile");
    public static int EnemyProjectile = LayerMask.NameToLayer("Enemy Projectile");
}
