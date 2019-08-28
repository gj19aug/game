using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.SceneManagement;

public class Entrypoint : MonoBehaviour
{
    // Inspector
    public new CameraRefs camera;
    public ShipSpec playerSpec;
    public SpawnRefs[] enemySpawns;
    public DebrisRefs[] debrisPrefabs;

    // Game State
    private GameState state;

    // ---------------------------------------------------------------------------------------------
    // Object Pooling

    void InitializePools<T>(T prefab, int initialCount) where T : Refs
    {
        if (state.pools.ContainsKey(prefab)) return;

        // TODO: remove the double cast (C# supports covariant/contravariant generics, right?)
        var pool = new Pool<T>();
        state.pools[prefab] = pool;
        pool.Initialize(prefab, initialCount);
    }

    void InitializePools(SpawnSpec spec)
    {
        for (int i = 0; i < spec.ships.Length; i++)
            InitializePools(spec.ships[i].spec, false);
    }

    void InitializePools(ShipSpec spec, bool isPlayer)
    {
        InitializePools(spec.shipPrefab, 16);
        InitializePools(spec.explosionPrefab, 16);
        InitializePools(spec.weaponSpec);
        InitializePools(spec.magnetismSpec);
        InitializePools(spec.aiSpec);
    }

    void InitializePools(WeaponSpec spec)
    {
        InitializePools(spec.weaponPrefab, 2 * 32);
        InitializePools(spec.projectilePrefab, 256);
        InitializePools(spec.impactPrefab, 64);
    }

    void InitializePools(MagnetismSpec spec) {}
    void InitializePools(AISpec spec) {}

    Pool<T> GetPool<T>(T prefab) where T : Refs
    {
        Assert.IsNotNull(prefab);
        return (Pool<T>) state.pools[prefab];
    }

    Pool<T> FindPool<T>(T instance) where T : Refs
    {
        // HACK: Horribly inefficient
        Assert.IsNotNull(instance);
        foreach (var kvp in state.pools)
        {
            if (kvp.Value is Pool<T>)
            {
                var pool = (Pool<T>) kvp.Value;
                if (pool.Contains(instance))
                    return pool;
            }
        }
        Assert.IsTrue(false);
        return null;
    }

    T Spawn<T>(T prefab) where T : Refs
    {
        Assert.IsNotNull(prefab);
        if (!state.pools.ContainsKey(prefab))
            Debug.LogFormat("Spawn Pool missing for '{0}'", prefab.name);

        var pool = (Pool<T>) state.pools[prefab];
        return (T) pool.Spawn();
    }

    void Despawn<T>(T prefab, T instance) where T : Refs
    {
        Assert.IsNotNull(prefab);
        Assert.IsNotNull(instance);
        var pool = (Pool<T>) state.pools[prefab];
        pool.Despawn(instance);
    }

    void Despawn<T>(T instance) where T : Refs
    {
        Assert.IsNotNull(instance);
        var pool = FindPool(instance);
        pool.Despawn(instance);
    }

    // ---------------------------------------------------------------------------------------------
    // Everything Else

    public static int GetKeyValue(KeyCode key)
    {
        return Input.GetKey(key) ? 1 : 0;
    }

    void AddWeapon(ref ShipCommon ship, WeaponSpec spec, Vector3 relPos)
    {
        Transform parent = ship.refs.physicsTransform;

        ref Weapon weapon = ref ship.weapons.Add();
        weapon.refs = Spawn(spec.weaponPrefab);
        weapon.refs.transform.parent = parent;
        weapon.refs.transform.localPosition = relPos;
        weapon.refs.transform.localRotation = Quaternion.identity;
        weapon.spec = spec;
        // NOTE: Assume x is down
        float angle = Vector2.SignedAngle(relPos, Vector2.down);
        Assert.IsTrue(angle >= -180 && angle <= 180);
        Assert.AreApproximatelyEqual(Mathf.Abs(weapon.refs.transform.localScale.x), 1.0f);
        weapon.refs.transform.SetScaleX(Mathf.Sign(angle));
    }

    void RemoveWeapons(ref ShipCommon ship)
    {
        for (int i = 0; i < ship.weapons.Count; i++)
        {
            ref Weapon w = ref ship.weapons[i];
            Despawn(w.spec.weaponPrefab, w.refs);

            Pool<WeaponRefs> pool = GetPool(w.spec.weaponPrefab);
            w.refs.transform.parent = pool.root;
        }
        ship.weapons.Clear();
    }

    static void ProcessShipMovement(ref ShipCommon ship)
    {
        ref ShipSpec spec = ref ship.spec;
        ref MoveState move = ref ship.move;
        ref ShipInput input = ref ship.input;
        Rigidbody2D rigidbody = ship.refs.rigidbody;

        float dt = Time.fixedDeltaTime;
        float ddpMul = spec.acceleration;
        float dpDragMul = spec.velocityMultiplierForDrag;
        float drag = spec.dragCurve.Evaluate(dpDragMul * move.dp.magnitude) * spec.drag;

        Vector3 ddp = ddpMul * input.throttle;
        ddp += drag * -move.dp;
        move.p += 0.5f * ddp * dt * dt + move.dp * dt;
        move.dp += ddp * dt;

        move.look = Vector3.Slerp(move.look, input.aim, spec.turnSpeed);

        // TODO: This tanks performance. Why? (related: full kinematic events on player & enemies)
        //refs.rigidbody.MovePosition(move.p);

        rigidbody.position = move.p;
        rigidbody.rotation = Vector2.SignedAngle(Vector2.up, move.look);
    }

    Vector3 CalculateWeaponDirection(Vector3 shipDir, Vector3 weaponDir)
    {
        // HACK: This is just stupid
        Quaternion a = Quaternion.FromToRotation(Vector3.up, shipDir);
        Quaternion b = Quaternion.FromToRotation(shipDir, weaponDir);
        Vector3 direction = a * b * Vector3.up;
        return direction;
    }

    void ProcessShipWeapons(ref ShipCommon ship, bool isPlayer)
    {
        float t = Time.fixedTime;
        ref ShipInput input = ref ship.input;

        for (int j = 0; j < ship.weapons.Count; j++)
        {
            ref Weapon weapon = ref ship.weapons[j];
            WeaponRefs refs = weapon.refs;
            WeaponSpec spec = weapon.spec;

            Vector3 desiredAim = input.point - refs.fireTransform.position;
            #if false
            float dot = Vector3.Dot(ship.move.dp, desiredAim);
            desiredAim = desiredAim + Mathf.Max(0.0f, dot) * ship.move.dp;
            #endif
            weapon.aim = Vector3.Slerp(weapon.aim, desiredAim, spec.turnSpeed);

            if (input.shoot && t >= weapon.nextRefireTime)
            {
                weapon.nextRefireTime = t + spec.refireDelay;

                Vector3 aim = CalculateWeaponDirection(ship.move.look, weapon.aim);

                ProjectileRefs pr = Spawn(spec.projectilePrefab);
                int layer = isPlayer ? Layers.PlayerProjectile : Layers.EnemyProjectile;
                pr.rigidbody.gameObject.layer = layer;
                pr.collider.gameObject.layer = layer;

                pr.rigidbody.position = refs.fireTransform.position;
                pr.rigidbody.rotation = Vector2.SignedAngle(Vector2.up, aim);
                pr.rigidbody.AddForce(spec.impulse * aim, ForceMode2D.Impulse);

                var p = new Projectile();
                p.refs = pr;
                p.spec = spec;
                p.owner = ship.refs;
                p.lifetime = spec.lifetime;
                state.projectiles.Add(p);
            }
        }

        // HACK: Unity input is a pile of radioactive garbage
        ShipInput prevInput = input;
        input = new ShipInput();
        input.throttle = prevInput.throttle;
        input.point = prevInput.point;
        input.aim = prevInput.aim;
    }

    bool IsPlayerShip(ref ShipCommon ship)
    {
        ref ShipCommon player = ref state.player.common;
        return EqualityComparer<ShipCommon>.Default.Equals(player, ship);
    }

    void ProcessShipImpact(ref ShipCommon ship, ref Impact impact)
    {
        if (IsPlayerShip(ref ship))
        {
            // TODO: Implement
        }
        else
        {
            ship.health -= impact.spec.damage;
            if (ship.health <= 0)
            {
                ref EnemyShip e = ref FindEnemy(ship.refs);
                ShipSpec spec = e.common.spec;

                // Explosion VFX
                ref ExplosionEffect effect = ref state.explosionEffects.Add();
                effect.refs = Spawn(spec.explosionPrefab);
                effect.refs.transform.position = impact.position;
                effect.refs.transform.rotation = Quaternion.identity;
                effect.lifetime = 0.9f;

                DespawnEnemy(ref e);
                float lerp = spec.debrisDistribution.Evaluate(Random.Range(0.0f, 1.0f));
                float floatCount = Mathf.Lerp(spec.debrisDropped.min, spec.debrisDropped.max, lerp);
                int count = Mathf.RoundToInt(floatCount);
                for (int i = 0; i < count; i++)
                {
                    Vector3 dir = Random.insideUnitCircle;
                    DebrisRefs debris = Spawn(RandomEx.Element(debrisPrefabs));
                    debris.rigidbody.position = e.common.move.p + (1.0f + spec.debrisRange) * dir;
                    debris.rigidbody.rotation = 360.0f * Random.value;
                    debris.rigidbody.AddForce(spec.debrisImpulse * Random.Range(0.5f, 1.5f) * dir, ForceMode2D.Impulse);
                }
            }
        }
    }

    ref EnemyShip SpawnEnemy(ShipSpec spec, Vector3 position)
    {
        ref EnemyShip e = ref state.enemies.Add();
        e.common.spec = spec;
        e.common.refs = Spawn(spec.shipPrefab);
        e.common.weapons = new RefList<Weapon>(2);
        e.common.health = e.common.spec.maxHealth;
        e.common.move.p = position;

        AddWeapon(ref e.common, spec.weaponSpec, new Vector3(-1.0f, 0.0f, 0.0f));
        AddWeapon(ref e.common, spec.weaponSpec, new Vector3(+1.0f, 0.0f, 0.0f));

        e.target = state.player.common.refs;
        return ref e;
    }

    void DespawnEnemy(ref EnemyShip enemy)
    {
        EnemyShip e = enemy;
        for (int i = 0; i < e.common.weapons.Count; i++)
            RemoveWeapons(ref e.common);
        Despawn(e.common.spec.shipPrefab, e.common.refs);
        state.enemies.Remove(ref e);

        // NOTE: Enemies may not necessarily come from spawners
        for (int i = 0; i < state.enemySpawns.Length; i++)
        {
            ref Spawn spawn = ref state.enemySpawns[i];
            spawn.ships.Remove(e.common.refs);
        }
    }

    bool ShipExists(ShipRefs refs)
    {
        for (int i = 0; i < state.enemies.Count; i++)
        {
            ref EnemyShip e = ref state.enemies[i];
            if (e.common.refs == refs)
                return true;
        }

        if (state.player.common.refs == refs)
            return true;

        return false;
    }

    ref ShipCommon FindShip(ShipRefs refs)
    {
        for (int i = 0; i < state.enemies.Count; i++)
        {
            ref EnemyShip e = ref state.enemies[i];
            if (e.common.refs == refs)
                return ref e.common;
        }

        if (state.player.common.refs == refs)
            return ref state.player.common;

        Assert.IsTrue(false);
        return ref state.enemies[0].common;
    }

    ref EnemyShip FindEnemy(ShipRefs refs)
    {
        for (int i = 0; i < state.enemies.Count; i++)
        {
            ref EnemyShip e = ref state.enemies[i];
            if (e.common.refs == refs)
                return ref e;
        }

        Assert.IsTrue(false);
        return ref state.enemies[0];
    }

    // ---------------------------------------------------------------------------------------------
    // Unity Events

    void Awake()
    {
        float t = Time.fixedTime;

        state.pools = new Dictionary<Refs, object>(32);
        InitializePools(playerSpec, true);
        for (int i = 0; i < enemySpawns.Length; i++)
            InitializePools(enemySpawns[i].spec);
        for (int i = 0; i < debrisPrefabs.Length; i++)
            InitializePools(debrisPrefabs[i], 256);

        state.colliderCache = new Collider2D[32];
        state.impactCache = new RefList<Impact>(32);

        state.projectiles = new List<Projectile>(128);
        state.impactEffects = new RefList<ImpactEffect>(128);
        state.explosionEffects = new RefList<ExplosionEffect>(128);
        state.enemies = new RefList<EnemyShip>(64);

        state.enemySpawns = new Spawn[enemySpawns.Length];
        for (int i = 0; i < enemySpawns.Length; i++)
        {
            SpawnRefs refs = enemySpawns[i];
            ref Spawn spawn = ref state.enemySpawns[i];
            spawn.refs = refs;
            spawn.nextSpawnTime = t + refs.spec.timeBetweenSpawns;
            spawn.ships = new List<ShipRefs>(refs.spec.maxCount);
        }

        // Spawn player
        {
            state.player.common.spec = playerSpec;
            state.player.common.refs = Instantiate(playerSpec.shipPrefab);
            state.player.common.weapons = new RefList<Weapon>(2);
            state.player.debris = new List<DebrisRefs>(128);
            AddWeapon(ref state.player.common, playerSpec.weaponSpec, new Vector3(-0.25f, 0.0f, 0.0f));
            AddWeapon(ref state.player.common, playerSpec.weaponSpec, new Vector3(+0.25f, 0.0f, 0.0f));
        }

        // DEBUG: Spawn a bunch of debris
        #if false
        for (int i = 0; i < 20; i++)
        {
            DebrisRefs debris = SpawnFromPoolSet(state.debrisPools);
            debris.rigidbody.position = 10.0f * Random.insideUnitCircle;
            debris.rigidbody.rotation = 360.0f * Random.value;
            debris.rigidbody.AddForce(Random.insideUnitCircle, ForceMode2D.Impulse);
        }
        #endif

        // TODO: Figure out player-enemy collisions!
        // DEBUG: Spawn an enemy
        //SpawnEnemy(new Vector3(5, 0, 0));
    }

    void Update()
    {
        // NOTE: Input only!

        ref MoveState move = ref state.player.common.move;
        ref ShipInput input = ref state.player.common.input;

        // Move
        Vector3 throttle = new Vector3();
        throttle.x = GetKeyValue(KeyCode.D) - GetKeyValue(KeyCode.A);
        throttle.y = GetKeyValue(KeyCode.W) - GetKeyValue(KeyCode.S);
        throttle = Vector3.ClampMagnitude(throttle, 1.0f);
        input.throttle = throttle;

        // Aim
        Assert.IsTrue(camera.camera.transform.forward == Vector3.forward);
        input.point = camera.camera.ScreenToWorldPoint(Input.mousePosition); input.point.z = 0;
        input.aim = (input.point - move.p).normalized;

        // Shoot
        input.shoot = Input.GetKey(KeyCode.Mouse0) | Input.GetKey(KeyCode.Space);

        // HACK: Reset
        if (Input.GetKeyDown(KeyCode.R))
            SceneManager.LoadScene(0, LoadSceneMode.Single);
    }

    void FixedUpdate()
    {
        // NOTE: Simulate!

        float t = Time.fixedTime;
        float dt = Time.fixedDeltaTime;

        // Projectile Impacts
        for (int i = state.projectiles.Count - 1; i >= 0 ; i--)
        {
            Projectile p = state.projectiles[i];
            int count = p.refs.rigidbody.GetContacts(state.colliderCache);
            if (count == 0) continue;

            // Despawn
            Despawn(p.spec.projectilePrefab, p.refs);
            state.projectiles.RemoveAt(i);

            // TODO: I wonder if this indirection is a bad idea?
            // Register Impact
            for (int j = 0; j < count; j++)
            {
                ShipRefs victim = state.colliderCache[j].GetComponentInParent<ShipRefs>();
                if (victim != null)
                {
                    ref Impact impact = ref state.impactCache.Add();
                    impact.position = p.refs.rigidbody.transform.position;
                    impact.spec = p.spec;
                    impact.owner = p.owner;
                    impact.victim = victim;
                    break;
                }
            }
        }

        // Check to see if ship explosion needs to be despawned
        for (int i = state.explosionEffects.Count - 1; i >= 0; i--)
        {
            ref ExplosionEffect effect = ref state.explosionEffects[i];

            effect.lifetime -= dt;
            if (effect.lifetime <= 0.0f)
            {
                Despawn(effect.refs);
                state.explosionEffects.RemoveAt(i);
            }
        }

        // Check to see if hit effect needs to be despawned
        for (int i = state.impactEffects.Count - 1; i >= 0; i--)
        {
            ref ImpactEffect effect = ref state.impactEffects[i];

            effect.lifetime -= dt;
            if (effect.lifetime <= 0.0f)
            {
                Despawn(effect.refs);
                state.impactEffects.RemoveAt(i);
            }
        }

        // Process Impacts
        for (int i = 0; i < state.impactCache.Count; i++)
        {
            ref Impact impact = ref state.impactCache[i];
            if (impact.victim)
            {
                // HACK: Ugh.
                if (ShipExists(impact.victim))
                {
                    ref ShipCommon s = ref FindShip(impact.victim);
                    ProcessShipImpact(ref s, ref impact);
                }

                // VFX
                ref ImpactEffect effect = ref state.impactEffects.Add();
                effect.refs = Spawn(impact.spec.impactPrefab);
                effect.refs.transform.position = impact.position;
                effect.refs.transform.rotation = Random.rotation;
                effect.lifetime = 0.7f;

                // Cam shake test
                Vector3 camPos = camera.transform.localPosition;
                camera.transform.localPosition = camPos + Random.insideUnitSphere * 0.15f;
            }
        }
        state.impactCache.Clear();

        ref PlayerShip player = ref state.player;
        ProcessShipMovement(ref player.common);
        ProcessShipWeapons(ref player.common, true);

        for (int i = 0; i < state.enemies.Count; i++)
        {
            ref EnemyShip enemy = ref state.enemies[i];
            ref MoveState move = ref enemy.common.move;
            ref ShipInput input = ref enemy.common.input;

            // (Shitty) AI
            Vector3 targetP = enemy.target.rigidbody.position;
            Vector3 toTarget = targetP - move.p;
            input.point = targetP;
            input.aim = toTarget.normalized;

            Vector3 desiredP = targetP - (5.0f * toTarget.normalized);
            Vector3 toDesired = desiredP - move.p;

            float distToDesired = toDesired.magnitude;
            float throttle = Mathf.Lerp(0, 1, (distToDesired - 0.75f) / 3.0f);

            input.throttle = (throttle / distToDesired) * toDesired;
            input.shoot = Vector2.Angle(move.look, input.aim) < 22;

            ProcessShipMovement(ref enemy.common);
            ProcessShipWeapons(ref enemy.common, false);
        }

        // Camera
        // TODO: Should the camera have a rigidbody for movement interpolation?
        camera.transform.position = Vector3.Lerp(camera.transform.position, player.common.move.p, camera.spec.lerpFactor);
        camera.camera.orthographicSize = 7.0f + (player.common.refs.physicsTransform.childCount * 0.01f);

        // TODO: Horribly inefficient
        // Projectiles
        for (int i = 0; i < state.projectiles.Count; i++)
        {
            Projectile p = state.projectiles[i];
            p.lifetime -= dt;
            state.projectiles[i] = p;
            if (p.lifetime <= 0)
            {
                Despawn(p.spec.projectilePrefab, p.refs);
                state.projectiles.RemoveAt(i);
            }
        }

        // Magnetism
        {
            ref MoveState move = ref state.player.common.move;
            ShipRefs refs = player.common.refs;
            MagnetismSpec mag = player.common.spec.magnetismSpec;

            // Attach debris that's touching the player
            int attachCount = refs.rigidbody.GetContacts(state.colliderCache);
            for (int i = 0; i < attachCount; i++)
            {
                Collider2D collider = state.colliderCache[i];
                var dr = collider.GetComponentInParent<DebrisRefs>();
                if (dr == null) continue;

                Vector3 relPos = (Vector3) dr.rigidbody.position - move.p;
                dr.transform.position = dr.transform.position - (mag.packing * relPos.normalized);
                dr.transform.parent = refs.physicsTransform;
                player.debris.Add(dr);

                // TODO: Having to destroy instead of disable is pretty lame.
                dr.rigidbody.gameObject.layer = Layers.Player;
                dr.collider.gameObject.layer = Layers.Player;
                DestroyImmediate(dr.rigidbody);
                dr.rigidbody = null;

                Pool<DebrisRefs> pool = FindPool(dr);
                pool.Remove(dr);
            }

            // Apply force to nearby objects
            int nearCount = Physics2D.OverlapCircleNonAlloc(
                move.p,
                mag.radius,
                state.colliderCache,
                mag.affectedLayers);

            for (int i = 0; i < nearCount; i++)
            {
                Rigidbody2D rb = state.colliderCache[i].attachedRigidbody;
                Vector3 relPos = (Vector3) rb.position - move.p;

                float dist = relPos.magnitude;

                // NOTE: Object is inside the player. Skip it and let physics depenetrate it.
                if (dist < refs.collider.Radius()) continue;

                float strength = mag.strength * mag.strengthCurve.Evaluate(dist / mag.radius);
                Vector3 force = -relPos * (strength / dist);
                rb.AddForce(force, ForceMode2D.Force);
            }
        }

        // Spawning
        {
            for (int i = 0; i < state.enemySpawns.Length; i++)
            {
                ref Spawn spawn = ref state.enemySpawns[i];
                SpawnSpec spec = spawn.refs.spec;

                if (spawn.ships.Count < spec.maxCount)
                {
                    if (t >= spawn.nextSpawnTime)
                    {
                        // BUG: Floating point issues for large t
                        spawn.nextSpawnTime += spec.timeBetweenSpawns;
                        Vector3 pos = spawn.refs.transform.position + (Vector3) (5.0f * Random.insideUnitCircle);
                        // TODO: Weighted random
                        ShipSpawnSpec enemySpawn = RandomEx.Element(spec.ships);
                        ref EnemyShip e = ref SpawnEnemy(enemySpawn.spec, pos);
                        spawn.ships.Add(e.common.refs);
                    }
                }
            }
        }
    }

    #if UNITY_EDITOR
    void OnDrawGizmos()
    {
        if (state.player.common.refs == null) return;

        ref ShipCommon ship = ref state.player.common;
        ref ShipRefs refs = ref state.player.common.refs;
        ref MagnetismSpec mag = ref state.player.common.spec.magnetismSpec;

        if (EditorApplication.isPlaying)
        {
            Gizmos.color = Color.green;
            for (int i = 0; i < ship.weapons.Count; i++)
            {
                ref Weapon weapon = ref ship.weapons[i];
                Vector3 pos = weapon.refs.fireTransform.position;
                Vector3 aim = CalculateWeaponDirection(ship.move.look, weapon.aim);
                Gizmos.DrawLine(pos, pos + aim);
            }
        }

        if (mag != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(refs.rigidbody.position, mag.radius);
        }
    }
    #endif
}
