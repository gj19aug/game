using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.SceneManagement;

public class Entrypoint : MonoBehaviour
{
    // Inspector
    public new CameraRefs camera;
    public ShipRefs playerPrefab;
    public WeaponRefs weaponPrefab;
    public WeaponSpec playerWeaponSpec;
    public WeaponSpec enemyWeaponSpec;
    public ProjectileRefs projectilePrefab;
    public DebrisRefs[] debrisPrefabs;
    public ShipRefs[] enemyPrefabs;

    // Game State
    [HideInInspector] public GameState state;

    public static int GetKeyValue(KeyCode key)
    {
        return Input.GetKey(key) ? 1 : 0;
    }

    void AddWeapon(ref ShipCommon ship, WeaponSpec spec, Vector3 relPos)
    {
        Transform parent = ship.refs.physicsTransform;

        var weapon = new Weapon();
        weapon.refs = state.weaponPool.Spawn();
        weapon.refs.transform.parent = parent;
        weapon.refs.transform.localPosition = relPos;
        weapon.spec = spec;
        // NOTE: Assume x is down
        float angle = Vector2.SignedAngle(relPos, Vector2.down);
        Assert.IsTrue(angle >= -180 && angle <= 180);
        if (angle < 0) weapon.refs.transform.MulScaleX(-1.0f);
        ship.weapons.Add(weapon);
    }

    static void ProcessShipMovement(ref ShipCommon ship)
    {
        ref MoveSpec spec = ref ship.refs.moveSpec;
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

    void ProcessShipEvents(ref ShipCommon ship, List<DebrisRefs> debris)
    {
        float t = Time.fixedTime;
        ref ShipInput input = ref ship.input;

        for (int j = 0; j < ship.weapons.Count; j++)
        {
            Weapon weapon = ship.weapons[j];

            if (input.shoot)
            {
                WeaponRefs refs = weapon.refs;
                WeaponSpec spec = weapon.spec;

                if (t < weapon.nextRefireTime) continue;
                weapon.nextRefireTime = t + spec.refireDelay;

                ProjectileRefs pr = state.projectilePool.Spawn();
                pr.rigidbody.tag = Tag.Player;
                pr.rigidbody.position = refs.fireTransform.position;
                // TODO: Ensure this rotation is correct
                pr.rigidbody.rotation = Vector2.SignedAngle(Vector2.up, input.aim);
                pr.rigidbody.AddForce(spec.impulse * input.aim, ForceMode2D.Impulse);
                state.projectiles.Add(new Projectile() { refs = pr, lifetime = spec.lifetime });
            }

            ship.weapons[j] = weapon;
        }
        input = new ShipInput();
    }

    void Awake()
    {
        state.weaponPool = new Pool<WeaponRefs>();
        state.weaponPool.Initialize(weaponPrefab, 32);
        state.projectilePool = new Pool<ProjectileRefs>();
        state.projectilePool.Initialize(projectilePrefab, 128);
        state.projectiles = new List<Projectile>(128);

        state.debrisPools = new Pool<DebrisRefs>[debrisPrefabs.Length];
        for (int i = 0; i < debrisPrefabs.Length; i++)
        {
            DebrisRefs prefab = debrisPrefabs[i];
            var pool = new Pool<DebrisRefs>();
            pool.Initialize(prefab, 32);
            state.debrisPools[i] = pool;
        }

        state.enemyPools = new Pool<ShipRefs>[enemyPrefabs.Length];
        for (int i = 0; i < enemyPrefabs.Length; i++)
        {
            ShipRefs prefab = enemyPrefabs[i];
            var pool = new Pool<ShipRefs>();
            pool.Initialize(prefab, 16);
            state.enemyPools[i] = pool;
        }

        state.colliderCache = new Collider2D[32];

        // Spawn player
        {
            state.player.common.refs = Instantiate(playerPrefab);
            state.player.common.weapons = new List<Weapon>(2);
            AddWeapon(ref state.player.common, playerWeaponSpec, new Vector3(-0.25f, 0.0f, 0.0f));
            AddWeapon(ref state.player.common, playerWeaponSpec, new Vector3(+0.25f, 0.0f, 0.0f));
        }

        // DEBUG: Spawn a bunch of debris
        for (int i = 0; i < 20; i++)
        {
            Pool<DebrisRefs> pool = RandomEx.Element(state.debrisPools);
            DebrisRefs debris = pool.Spawn();
            debris.rigidbody.position = 10.0f * Random.insideUnitCircle;
            debris.rigidbody.rotation = 360.0f * Random.value;
            debris.rigidbody.AddForce(Random.insideUnitCircle, ForceMode2D.Impulse);
        }

        // TODO: Figure out player-enemy collisions!
        // DEBUG: Spawn an enemy
        {
            Pool<ShipRefs> pool = RandomEx.Element(state.enemyPools);
            var es = new EnemyShip();
            es.common.refs = pool.Spawn();
            es.common.weapons = new List<Weapon>(2);

            es.common.move.p = new Vector2(5, 0);
            AddWeapon(ref es.common, enemyWeaponSpec, new Vector3(-1.0f, 0.0f, 0.0f));
            AddWeapon(ref es.common, enemyWeaponSpec, new Vector3(+1.0f, 0.0f, 0.0f));
            es.target = state.player.common.refs;

            state.enemies.Add(es);
        }
    }

    void Update()
    {
        // NOTE: Input only!

        ref MoveState mv = ref state.player.common.move;
        ref ShipInput ip = ref state.player.common.input;

        // Move
        Vector3 throttle = new Vector3();
        throttle.x = GetKeyValue(KeyCode.D) - GetKeyValue(KeyCode.A);
        throttle.y = GetKeyValue(KeyCode.W) - GetKeyValue(KeyCode.S);
        throttle = Vector3.ClampMagnitude(throttle, 1.0f);
        ip.throttle = throttle;

        // Aim
        Assert.IsTrue(camera.camera.transform.forward == Vector3.forward);
        Vector3 mouseWS = camera.camera.ScreenToWorldPoint(Input.mousePosition); mouseWS.z = 0;
        ip.aim = (mouseWS - mv.p).normalized;

        // Shoot
        ip.shoot = Input.GetKey(KeyCode.Mouse0) | Input.GetKey(KeyCode.Space);

        // HACK: Reset
        if (Input.GetKeyDown(KeyCode.R))
            SceneManager.LoadScene(0, LoadSceneMode.Single);
    }

    void FixedUpdate()
    {
        // NOTE: Simulate!

        float dt = Time.fixedDeltaTime;
        ref PlayerShip player = ref state.player;

        ProcessShipMovement(ref player.common);
        ProcessShipEvents(ref player.common, player.debris);

        for (int i = 0; i < state.enemies.Count; i++)
        {
            EnemyShip enemy = state.enemies[i];

            // (Shitty) AI
            Vector3 relPos = enemy.common.move.p - (Vector3) enemy.target.rigidbody.position;
            Vector3 targetPos = 3.0f * relPos.normalized;
            Vector3 deltaPos = targetPos - relPos;
            enemy.common.input.throttle = Vector3.ClampMagnitude(deltaPos, 1.0f);
            enemy.common.input.shoot = true;

            ProcessShipMovement(ref enemy.common);
            ProcessShipEvents(ref enemy.common, null);
            state.enemies[i] = enemy;
        }

        // Camera
        // TODO: Should the camera have a rigidbody for movement interpolation?
        camera.transform.position = Vector3.Lerp(camera.transform.position, player.common.move.p, camera.spec.lerpFactor);
        camera.camera.orthographicSize = 5.0f + (player.common.refs.physicsTransform.childCount * 0.1f);

        // TODO: Horribly inefficient
        // Projectiles
        for (int i = 0; i < state.projectiles.Count; i++)
        {
            Projectile p = state.projectiles[i];
            p.lifetime -= dt;
            state.projectiles[i] = p;
            if (p.lifetime <= 0)
            {
                state.projectilePool.Despawn(p.refs);
                state.projectiles.RemoveAt(i);
            }
        }

        // Magnetism
        {
            ref MoveState move = ref state.player.common.move;
            ShipRefs refs = player.common.refs;
            MagnetismSpec mag = player.common.refs.magnetismSpec;

            // Attach debris that's touching the player
            int attachCount = refs.rigidbody.GetContacts(state.colliderCache);
            for (int i = 0; i < attachCount; i++)
            {
                ref Collider2D collider = ref state.colliderCache[i];
                var dr = collider.GetComponentInParent<DebrisRefs>();
                if (dr == null) continue;

                Vector3 relPos = (Vector3) dr.rigidbody.position - move.p;
                dr.transform.position = dr.transform.position - (mag.packing * relPos.normalized);
                dr.transform.parent = refs.physicsTransform;
                player.debris.Add(dr);

                // TODO: Having to destroy instead of disable is pretty lame.
                DestroyImmediate(dr.rigidbody);
                dr.rigidbody = null;

                // TODO: Horribly inefficient
                for (int j = 0; j < state.debrisPools.Length; j++)
                {
                    if (state.debrisPools[j].Remove(dr))
                        break;
                }
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
                if (rb.CompareTag(Tag.Player)) continue;

                Vector3 relPos = (Vector3) rb.position - move.p;
                float dist = relPos.magnitude;

                // NOTE: Object is inside the player. Skip it and let physics depenetrate it.
                if (dist < refs.collider.Radius()) continue;

                float strength = mag.strength * mag.strengthCurve.Evaluate(dist / mag.radius);
                Vector3 force = -relPos * (strength / dist);
                rb.AddForce(force, ForceMode2D.Force);
            }
        }
    }

    #if UNITY_EDITOR
    void OnDrawGizmos()
    {
        if (state.player.common.refs == null) return;

        ref ShipCommon ship = ref state.player.common;
        ref ShipRefs refs = ref state.player.common.refs;
        ref ShipInput input = ref state.player.common.input;
        ref MagnetismSpec mag = ref state.player.common.refs.magnetismSpec;

        if (EditorApplication.isPlaying)
        {
            Gizmos.color = Color.green;
            for (int i = 0; i < ship.weapons.Count; i++)
            {
                Weapon weapon = ship.weapons[i];
                Vector3 pos = weapon.refs.fireTransform.position;
                Gizmos.DrawLine(pos, pos + input.aim);
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
