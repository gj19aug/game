using System;
using System.Collections.Generic;
using UnityEngine;

public enum MetaState
{
    Null,
    StartMenu,
    HowToMenu,
    Gameplay,
    Paused,
    GameLost,
    GameWon,
}

[Serializable]
public struct GameState
{
    // Data
    public float startTime;
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
    public ContactPoint2D[] contactCache;
    public RefList<Impact> impactCache;
    public List<Vector3> lastImpactPositions;
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
    public float health;
}

[Serializable]
public struct PlayerShip
{
    public ShipCommon common;
    public float radius;
    public RefList<AttachedDebris> debris;
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
    public bool cheatHealth;
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
    public Collider2D collider;
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

[Serializable]
public struct AttachedDebris
{
    public Pool<DebrisRefs> pool;
    public DebrisRefs refs;
    public float health;
}

public struct Layer
{
    public readonly int Index;
    public readonly int Mask;

    public Layer(string name)
    {
        Index = LayerMask.NameToLayer(name);
        Mask = 1 << Index;
    }
}

public static class Layers
{
    public static readonly Layer Player = new Layer("Player");
    public static readonly Layer Enemy = new Layer("Enemy");
    public static readonly Layer Debris = new Layer("Debris");
    public static readonly Layer PlayerProjectile = new Layer("Player Projectile");
    public static readonly Layer EnemyProjectile = new Layer("Enemy Projectile");
    public static readonly Layer Environment = new Layer("Environment");
    public static readonly Layer Background = new Layer("Background");
}
