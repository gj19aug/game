using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Profiling;
using UnityEngine.SceneManagement;

public class Entrypoint : MonoBehaviour
{
    // Inspector
    public new CameraRefs camera;
    public ShipSpec playerSpec;
    public SpawnRefs[] enemySpawns;
    public DebrisRefs[] debrisPrefabs;

    public GameObject startMenu;
    public GameObject startMenuTitle;
    public GameObject startMenuStart;
    public GameObject startMenuTryAgain;
    public GameObject startMenuGameLost;
    public GameObject pauseMenu;

    // Game State
    private GameState state;

    // ---------------------------------------------------------------------------------------------
    // Meta State

    public static MetaState metaState { get; private set; } = MetaState.StartMenu;
    private static Entrypoint instance;

    public static void SetMetaState(MetaState newState)
    {
        if (newState == metaState) return;
        metaState = newState;

        switch (metaState)
        {
            case MetaState.StartMenu:
            {
                Time.timeScale = 1f;
                SceneManager.LoadScene(0, LoadSceneMode.Single);
                instance.pauseMenu.SetActive(false);
                instance = null;
                gameOverRunning = false;
                break;
            }

            case MetaState.HowToMenu:
            {
                break;
            }

            case MetaState.Gameplay:
            {
                Time.timeScale = 1f;
                instance.pauseMenu.SetActive(false);
                break;
            }

            case MetaState.Paused:
            {
                Time.timeScale = 0f;
                instance.pauseMenu.SetActive(true);
                break;
            }

            case MetaState.GameLost:
            {
                instance.startMenu.SetActive(true);
                instance.startMenuTitle.SetActive(false);
                instance.startMenuStart.SetActive(false);
                instance.startMenuTryAgain.SetActive(true);
                instance.startMenuGameLost.SetActive(true);
                break;
            }

            case MetaState.GameWon:
            {
                break;
            }
        }
    }

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
        // TODO: Propagate to weapon spec
        int count = isPlayer ? 1 : 16;
        InitializePools(spec.shipPrefab, count);
        InitializePools(spec.explosionPrefab, count);
        InitializePools(spec.weaponSpec);
        InitializePools(spec.magnetismSpec);
        //InitializePools(spec.aiSpec);
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
        Assert.AreApproximatelyEqual(Mathf.Abs(spec.weaponPrefab.transform.localScale.x), 1.0f);

        ref Weapon weapon = ref ship.weapons.Add();
        weapon.refs = Spawn(spec.weaponPrefab);
        weapon.refs.transform.parent = parent;
        weapon.refs.transform.localPosition = relPos;
        weapon.refs.transform.localRotation = Quaternion.identity;
        weapon.refs.transform.localScale = Vector3.one;
        weapon.spec = spec;
        // NOTE: Assume x is down
        float angle = Vector2.SignedAngle(relPos, Vector2.down);
        Assert.IsTrue(angle >= -180 && angle <= 180);
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
        ShipSpec spec = ship.spec;
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

    void ProcessShipWeapons(ref ShipCommon ship)
    {
        float t = Time.fixedTime;
        ref ShipInput input = ref ship.input;
        bool isPlayer = ship.refs == state.player.common.refs;

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
                int layer = isPlayer ? Layers.PlayerProjectile.Index : Layers.EnemyProjectile.Index;
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
    }

    void ProcessShipImpact(ref ShipCommon ship, ref Impact impact)
    {
        ShipRefs player = state.player.common.refs;
        bool isPlayer = ship.refs == player;
        if (isPlayer)
        {
            Profiler.BeginSample("Player Impact");
            Assert.IsTrue(impact.owner != ship.refs);

            // Players can't hit themselves
            if (impact.owner == ship.refs) return;

            int count = Physics2D.OverlapCircleNonAlloc(
                impact.position,
                impact.spec.damageRadius,
                state.colliderCache,
                Layers.Player.Mask);

            for (int i = 0; i < count; i++)
            {
                Collider2D collider = state.colliderCache[i];

                DebrisRefs dr = collider.GetComponentInParent<DebrisRefs>();
                if (dr != null)
                {
                    for (int j = state.player.debris.Count - 1; j >= 0; j--)
                    {
                        ref AttachedDebris debris = ref state.player.debris[j];
                        if (debris.refs != dr) continue;

                        // Camera shake
                        Vector3 camPos = camera.transform.localPosition;
                        camera.transform.localPosition = camPos + Random.insideUnitSphere * 0.15f;

                        float dist = ((Vector3) collider.ClosestPoint(impact.position) - impact.position).magnitude;
                        float damage = Mathf.Lerp(0, impact.spec.damage, 1.0f - (dist / impact.spec.damageRadius));
                        Assert.IsTrue(dist <= impact.spec.damageRadius + Mathf.Epsilon);

                        debris.health -= damage;
                        if (debris.health <= 0.0f)
                        {
                            DetachDebris(ref debris, ref ship, damage);
                            state.player.common.health--;
                            state.player.debris.RemoveAt(j);
                            RecalculatePlayerRadius();
                        }
                    }
                }
                else if (collider == state.player.common.refs.collider)
                {
                    // NOTE: Has to be a *direct* impact
                    if (impact.collider == ship.refs.collider)
                        StartCoroutine(GameOver(impact.spec.damage));
                }
            }
            Profiler.EndSample();
        }
        else
        {
            Profiler.BeginSample("Enemy Impact");
            ship.health -= impact.spec.damage;
            if (ship.health <= 0.0f)
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
                    float impulse = spec.debrisImpulse * Random.Range(0.5f, 1.5f);
                    Vector3 impulseDir = Random.insideUnitCircle.normalized;
                    Vector3 position = e.common.move.p + spec.debrisRange * impulseDir;
                    SpawnDebris(position, impulse, impulseDir);
                }
            }
            Profiler.EndSample();
        }
    }

    ref EnemyShip SpawnEnemy(ShipSpec spec, Vector3 position)
    {
        ref EnemyShip e = ref state.enemies.Add();
        e.common.spec = spec;
        e.common.refs = Spawn(spec.shipPrefab);
        e.common.weapons = new RefList<Weapon>(2);
        e.common.health = e.common.spec.initialHealth;
        e.common.move.p = position;
        e.common.refs.rigidbody.position = position;

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

    void SpawnDebris(Vector3 position, float impulse, Vector3 impulseDir)
    {
        DebrisRefs debris = Spawn(RandomEx.Element(debrisPrefabs));
        debris.rigidbody.position = position;
        debris.rigidbody.rotation = 360.0f * Random.value;
        debris.rigidbody.AddForce(impulse * impulseDir, ForceMode2D.Impulse);
        debris.rigidbody.AddTorque(0.05f * impulse, ForceMode2D.Impulse);
    }

    void AttachDebris(ref AttachedDebris debris, ref ShipCommon ship, Vector3 relPos)
    {
        DebrisRefs dr = debris.refs;

        // TODO: Having to destroy instead of disable is pretty lame.
        dr.rigidbody.gameObject.layer = Layers.Player.Index;
        dr.collider.gameObject.layer = Layers.Player.Index;
        DestroyImmediate(dr.rigidbody);
        dr.rigidbody = null;

        dr.physicsTransform.position = ship.move.p + relPos;
        dr.transform.parent = ship.refs.physicsTransform;

        debris.health = 1;
        debris.pool = FindPool(dr);
        debris.pool.Remove(dr);
    }

    void DetachDebris(ref AttachedDebris debris, ref ShipCommon ship, float damage)
    {
        DebrisRefs dr = debris.refs;

        // TODO: Having to destroy instead of disable is pretty lame.
        dr.rigidbody = dr.physicsTransform.gameObject.AddComponent<Rigidbody2D>();
        dr.rigidbody.gravityScale = 0.0f;
        dr.rigidbody.gameObject.layer = Layers.Background.Index;
        dr.collider.gameObject.layer = Layers.Background.Index;

        float impulse = Random.Range(0.5f, 1.5f) * 3.0f * damage;
        Vector3 impulseDir = dr.physicsTransform.position - ship.move.p;
        dr.rigidbody.AddForce(impulse * impulseDir, ForceMode2D.Impulse);
        dr.rigidbody.AddTorque(0.1f * impulse, ForceMode2D.Impulse);

        debris.pool.Add(dr);
        dr.transform.parent = debris.pool.root;
    }

    void RecalculatePlayerRadius()
    {
        ref PlayerShip player = ref state.player;
        ref MoveState move = ref state.player.common.move;
        ShipRefs refs = state.player.common.refs;

        CircleCollider2D collider = refs.collider as CircleCollider2D;
        player.radius = collider.radius;

        #if false
        float radius = 0;
        float maxDistSq = 0;
        for (int i = 0; i < player.debris.Count; i++)
        {
            CircleCollider2D debrisCollider = player.debris[i].refs.collider as CircleCollider2D;
            float distSq = ((Vector3) debrisCollider.offset + debrisCollider.transform.position - move.p).sqrMagnitude;
            if (distSq > 0.0f && distSq >= maxDistSq)
            {
                maxDistSq = distSq;
                radius = Mathf.Max(radius, debrisCollider.radius);
            }
        }
        player.radius = Mathf.Max(Mathf.Sqrt(maxDistSq) + radius);
        #else
        player.radius += Mathf.Sqrt(0.05f * player.debris.Count);
        #endif
    }

    // ---------------------------------------------------------------------------------------------
    // Game Flow

    // HACK: Terrible.
    private static bool gameOverRunning;

    IEnumerator GameOver(float damage)
    {
        if (gameOverRunning) yield break;
        gameOverRunning = true;

        float startTime = Time.unscaledTime;

        // Big Camera shake
        Vector3 camPos = camera.transform.localPosition;
        camera.transform.localPosition = camPos + Random.insideUnitSphere * 2.00f;

        // Eject all ship debris
        for (int i = 0; i < state.player.debris.Count; i++)
            DetachDebris(ref state.player.debris[i], ref state.player.common, damage);
        state.player.common.health = 0.0f;
        state.player.debris.Clear();
        RecalculatePlayerRadius();

        // TODO: Spawn big explosion

        float duration = 1.0f;
        float nextTime = startTime + duration;
        while (Time.unscaledTime < nextTime)
        {
            float normalizedTime = Mathf.Clamp01((nextTime - Time.unscaledTime) / duration);
            Time.timeScale = normalizedTime;
            yield return new WaitForSecondsRealtime(0.0f);
        }
        Time.timeScale = 0.0f;

        SetMetaState(MetaState.GameLost);
    }

    // ---------------------------------------------------------------------------------------------
    // Unity Events

    void Awake()
    {
        instance = this;

        float t = Time.fixedTime;

        state.pools = new Dictionary<Refs, object>(32);
        InitializePools(playerSpec, true);
        for (int i = 0; i < enemySpawns.Length; i++)
            InitializePools(enemySpawns[i].spec);
        for (int i = 0; i < debrisPrefabs.Length; i++)
            InitializePools(debrisPrefabs[i], 256);

        state.colliderCache = new Collider2D[32];
        state.contactCache = new ContactPoint2D[32];
        state.impactCache = new RefList<Impact>(32);
        state.lastImpactPositions = new List<Vector3>(8);

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
            spawn.nextSpawnTime = t;
            spawn.ships = new List<ShipRefs>(refs.spec.maxCount);
        }

        // Spawn player
        {
            state.player.common.spec = playerSpec;
            state.player.common.refs = Instantiate(playerSpec.shipPrefab);
            state.player.common.weapons = new RefList<Weapon>(2);
            state.player.debris = new RefList<AttachedDebris>(128);
            RecalculatePlayerRadius();
            AddWeapon(ref state.player.common, playerSpec.weaponSpec, new Vector3(-0.25f, 0.0f, 0.0f));
            AddWeapon(ref state.player.common, playerSpec.weaponSpec, new Vector3(+0.25f, 0.0f, 0.0f));
        }

        // Spawn debris to initialize player health
        ref ShipCommon player = ref state.player.common;
        player.health = playerSpec.initialHealth;
        for (int i = 0; i < player.health; i++)
        {
            Vector3 relPos = Random.insideUnitCircle;
            relPos = 4.0f * relPos + 1.0f * relPos.normalized + player.move.p;
            float impulse = 1.0f * Random.Range(0.5f, 1.5f);
            SpawnDebris(player.move.p + relPos, impulse, -relPos);
        }

        // TODO: Figure out player-enemy collisions!
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

        // Cheats
        input.cheatHealth = Input.GetKey(KeyCode.F1);

        if (Application.isEditor)
        {
            // Dev Reset
            if (Input.GetKeyDown(KeyCode.R))
                SetMetaState(MetaState.StartMenu);
        }
    }

    void FixedUpdate()
    {
        // BUG: Camera size is wrong on restart
        // HACK: This is bad and I feel bad.
        if (metaState == MetaState.StartMenu)
        {
            Time.timeScale = 0.0f;
            return;
        }

        // NOTE: Simulate!

        float t = Time.fixedTime;
        float dt = Time.fixedDeltaTime;

        ShipRefs playerRefs = state.player.common.refs;
        ref PlayerShip player = ref state.player;
        ref MoveState playerMove = ref state.player.common.move;

        // Find Projectile Impacts
        Profiler.BeginSample("Find Projectile Impacts");
        for (int i = state.projectiles.Count - 1; i >= 0 ; i--)
        {
            Projectile p = state.projectiles[i];
            int count = p.refs.rigidbody.GetContacts(state.contactCache);
            if (count == 0) continue;

            // Despawn
            Despawn(p.spec.projectilePrefab, p.refs);
            state.projectiles.RemoveAt(i);

            // TODO: I wonder if this indirection is a bad idea?
            // Register Impact
            for (int j = 0; j < count; j++)
            {
                // NOTE: 'other' is the projectile, regular is the victim
                ref ContactPoint2D contact = ref state.contactCache[j];
                if (contact.rigidbody == null) continue;

                ShipRefs victim = contact.rigidbody.GetComponentInParent<ShipRefs>();
                if (victim != null)
                {
                    ref Impact impact = ref state.impactCache.Add();
                    impact.position = contact.point;
                    impact.collider = contact.collider;
                    impact.spec = p.spec;
                    impact.owner = p.owner;
                    impact.victim = victim;

                    if (state.lastImpactPositions.Count == state.lastImpactPositions.Capacity)
                        state.lastImpactPositions.RemoveAt(0);
                    state.lastImpactPositions.Add(impact.position);
                    break;
                }
            }
        }
        Profiler.EndSample();

        // Check to see if ship explosion needs to be despawned
        Profiler.BeginSample("Ship Explosion");
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
        Profiler.EndSample();

        // Check to see if hit effect needs to be despawned
        Profiler.BeginSample("Projectile Impact Despawn");
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
        Profiler.EndSample();

        // Process Impacts
        Profiler.BeginSample("Process Projectile Impacts");
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
            }
        }
        state.impactCache.Clear();
        Profiler.EndSample();

        Profiler.BeginSample("Ship Update");
        ProcessShipMovement(ref player.common);
        ProcessShipWeapons(ref player.common);

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

            Vector3 desiredP = targetP - ((5.0f + player.radius) * toTarget.normalized);
            Vector3 toDesired = desiredP - move.p;

            float distToDesired = toDesired.magnitude;
            float throttle = Mathf.Lerp(0, 1, (distToDesired - 0.5f) / 1.0f);

            input.throttle = (throttle / distToDesired) * toDesired;
            input.shoot = Vector2.Angle(move.look, input.aim) < 22;

            ProcessShipMovement(ref enemy.common);
            ProcessShipWeapons(ref enemy.common);
        }
        Profiler.EndSample();

        // Camera
        Profiler.BeginSample("Camera Update");
        {
            CameraSpec spec = camera.spec;

            // TODO: Should the camera have a rigidbody for movement interpolation?
            camera.transform.position = Vector3.Lerp(camera.transform.position, playerMove.p, spec.lerpFactor);
            camera.camera.orthographicSize = spec.baseSize + Mathf.Sqrt(spec.sizeScaleRate * playerRefs.physicsTransform.childCount);
        }
        Profiler.EndSample();

        // TODO: Horribly inefficient
        // Projectiles
        Profiler.BeginSample("Projectile Depawn");
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
        Profiler.EndSample();

        // Magnetism
        Profiler.BeginSample("Magnetism Update");
        {
            MagnetismSpec mag = player.common.spec.magnetismSpec;

            // Attach debris that's touching the player
            Profiler.BeginSample("Magnetism Attach");
            var filter = new ContactFilter2D();
            //filter.useLayerMask = true;
            filter.SetLayerMask(mag.affectedLayers);
            int attachCount = playerRefs.rigidbody.GetContacts(filter, state.colliderCache);

            for (int i = 0; i < attachCount; i++)
            {
                Collider2D collider = state.colliderCache[i];

                var dr = collider.GetComponentInParent<DebrisRefs>();
                if (dr == null) continue;

                // TODO: Could probably use Bounds
                CircleCollider2D playerCollider = playerRefs.collider as CircleCollider2D;
                CircleCollider2D debrisCollider = collider as CircleCollider2D;
                Assert.IsNotNull(playerCollider);
                Assert.IsNotNull(debrisCollider);

                Vector3 relPos = (Vector3) dr.rigidbody.position - playerMove.p;
                float distCur = relPos.magnitude;
                float distTar = Mathf.Max(distCur - mag.packing, playerCollider.radius + debrisCollider.radius);
                relPos *= distTar / distCur;

                player.common.health++;
                ref AttachedDebris debris = ref player.debris.Add();
                debris.refs = dr;
                AttachDebris(ref debris, ref player.common, relPos);
                RecalculatePlayerRadius();
            }
            Profiler.EndSample();

            // Apply force to nearby objects
            Profiler.BeginSample("Magnetism Pull");
            int nearCount = Physics2D.OverlapCircleNonAlloc(
                playerMove.p,
                mag.radius + player.radius,
                state.colliderCache,
                mag.affectedLayers);

            for (int i = 0; i < nearCount; i++)
            {
                Rigidbody2D rb = state.colliderCache[i].attachedRigidbody;
                Vector3 relPos = (Vector3) rb.position - playerMove.p;
                float dist = relPos.magnitude;

                // TODO: Depenetration?
                float strength = mag.strength * mag.strengthCurve.Evaluate(dist / mag.radius);
                Vector3 force = -relPos * (strength / dist);
                rb.AddForce(force, ForceMode2D.Force);
            }
            Profiler.EndSample();
        }
        Profiler.EndSample();

        // Spawning
        Profiler.BeginSample("Enemy Spawning");
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
                        float timeStep = spec.timeToMaxSpawnRate / (spec.timeBetweenSpawns.Length - 1);
                        int spawnRateIndex = Mathf.FloorToInt(t / timeStep);
                        spawnRateIndex = Mathf.Min(spawnRateIndex, spec.timeBetweenSpawns.Length - 1);
                        spawn.nextSpawnTime += spec.timeBetweenSpawns[spawnRateIndex];
                        Vector3 pos = spawn.refs.transform.position + (Vector3) (5.0f * Random.insideUnitCircle);
                        // HACK: Shitty weighted random
                        ShipSpec enemySpec = null;
                        {
                            float totalProbability = 0.0f;
                            for (int j = 0; j < spec.ships.Length; j++)
                                totalProbability += spec.ships[j].probability;
                            float random = Random.Range(0.0f, totalProbability);

                            totalProbability = 0.0f;
                            for (int j = 0; j < spec.ships.Length; j++)
                            {
                                totalProbability += spec.ships[j].probability;
                                if (random <= totalProbability)
                                {
                                    enemySpec = spec.ships[j].spec;
                                    break;
                                }
                            }
                        }
                        ref EnemyShip e = ref SpawnEnemy(enemySpec, pos);
                        spawn.ships.Add(e.common.refs);
                    }
                }
            }
        }
        Profiler.EndSample();

        // Cheats
        Profiler.BeginSample("Cheats");
        {
            ref ShipInput input = ref player.common.input;
            if (input.cheatHealth)
            {
                for (int j = 0; j < 10; j++)
                {
                    Vector3 relPos = Random.insideUnitCircle;
                    relPos = 4.0f * relPos + (player.radius + 1.0f) * relPos.normalized;
                    float impulse = 1.0f * Random.Range(0.5f, 1.5f);
                    SpawnDebris(playerMove.p + relPos, impulse, -relPos);
                }
            }
        }
        Profiler.EndSample();
    }

    #if UNITY_EDITOR
    void OnDrawGizmos()
    {
        if (state.player.common.refs == null) return;

        ref PlayerShip player = ref state.player;
        ref ShipCommon ship = ref state.player.common;
        ShipRefs refs = state.player.common.refs;
        MagnetismSpec mag = state.player.common.spec.magnetismSpec;

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

            Gizmos.DrawWireSphere(ship.move.p, player.radius);
        }

        if (mag != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(refs.rigidbody.position, mag.radius);
        }

        if (state.lastImpactPositions != null)
        {
            for (int i = 0; i < state.lastImpactPositions.Count; i++)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(state.lastImpactPositions[i], 0.2f);
            }
        }
    }
    #endif
}
