using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;

public class Entrypoint : MonoBehaviour
{
    // Inspector
    public new CameraRefs camera;
    public ShipRefs playerPrefab;
    public ProjectileRefs projectilePrefab;
    public DebrisRefs[] debrisPrefabs;
    public ShipRefs[] enemyPrefabs;

    // Game State
    [HideInInspector] public GameState state;

    public static int GetKeyValue(KeyCode key)
    {
        return Input.GetKey(key) ? 1 : 0;
    }

    void Awake()
    {
        state.player.refs = Instantiate(playerPrefab);
        state.player.input.events = new List<ShipInputEvent>(8);
        state.projectilePool = new Pool<ProjectileRefs>();
        state.projectilePool.Initialize(projectilePrefab, 64);
        state.projectiles = new List<Projectile>(64);

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

        // DEBUG: Spawn a bunch of debris
        for (int i = 0; i < 20; i++)
        {
            Pool<DebrisRefs> pool = RandomEx.Element(state.debrisPools);
            DebrisRefs debris = pool.Spawn();
            debris.rigidbody.position = 10.0f * Random.insideUnitCircle;
            debris.rigidbody.rotation = 360.0f * Random.value;
        }

        // TODO: Figure out player-enemy collisions!
        // DEBUG: Spawn an enemy
        {
            Pool<ShipRefs> pool = RandomEx.Element(state.enemyPools);
            var es = new EnemyShip();
            es.refs = pool.Spawn();
            es.move.p = new Vector2(5, 0);
            es.input.events = new List<ShipInputEvent>(8);
            es.target = state.player.refs;
            state.enemies.Add(es);
        }
    }

    void Update()
    {
        // NOTE: Input only!

        ref MoveState mv = ref state.player.move;
        ref ShipInput ip = ref state.player.input;

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
        if (Input.GetKeyDown(KeyCode.Mouse0) | Input.GetKeyDown(KeyCode.Space))
            ip.events.Add(ShipInputEvent.Shoot);
    }

    static void ProcessShipMovement(ShipRefs refs, ref MoveState move, ref ShipInput input)
    {
        ref MoveSpec spec = ref refs.moveSpec;

        float dt = Time.fixedDeltaTime;
        float ddpMul = spec.acceleration;
        float dpDragMul = spec.velocityMultiplierForDrag;
        float drag = spec.dragCurve.Evaluate(dpDragMul * move.dp.magnitude) * spec.drag;

        Vector3 ddp = ddpMul * input.throttle;
        ddp += drag * -move.dp;
        move.p += 0.5f * ddp * dt * dt + move.dp * dt;
        move.dp += ddp * dt;

        refs.rigidbody.MovePosition(move.p);
    }

    void ProcessShipEvents(ShipRefs refs, ref MoveState move, ref ShipInput input, List<DebrisRefs> debris)
    {
        for (int i = 0; i < input.events.Count; i++)
        {
            switch (input.events[i])
            {
                default: Assert.IsTrue(false); break;

                case ShipInputEvent.Shoot:
                {
                    // DEBUG: Remove this once we shoot from turrets directly
                    // TODO: Horribly inefficient
                    float spawnRadius = refs.collider.Radius();
                    if (debrisPrefabs != null)
                    {
                        for (int j = 0; j < debris.Count; j++)
                        {
                            Bounds debrisBounds = debris[j].collider.bounds;
                            Vector3 relPos = debrisBounds.center - move.p;
                            float dot = Vector3.Dot(relPos.normalized, input.aim);
                            float dist = relPos.magnitude + debrisBounds.extents.magnitude;
                            spawnRadius = Mathf.Max(spawnRadius, dot*dist);
                        }
                    }

                    ProjectileRefs pr = state.projectilePool.Spawn();
                    pr.rigidbody.tag = Tag.Player;
                    pr.rigidbody.position = move.p + (spawnRadius + pr.collider.radius) * input.aim;
                    pr.rigidbody.rotation = Vector2.SignedAngle(Vector2.up, input.aim);
                    pr.rigidbody.AddForce(pr.spec.impulse * input.aim, ForceMode2D.Impulse);
                    state.projectiles.Add(new Projectile() { refs = pr, lifetime = pr.spec.lifetime });

                    state.lastProjectileSpawn = (Vector3) pr.rigidbody.position - move.p;
                    break;
                }
            }
        }
        input.events.Clear();
    }

    void FixedUpdate()
    {
        // NOTE: Simulate!

        float dt = Time.fixedDeltaTime;
        ref PlayerShip player = ref state.player;

        ProcessShipMovement(player.refs, ref player.move, ref player.input);
        ProcessShipEvents(player.refs, ref player.move, ref player.input, player.debris);

        for (int i = 0; i < state.enemies.Count; i++)
        {
            EnemyShip enemy = state.enemies[i];
            ProcessShipMovement(enemy.refs, ref enemy.move, ref enemy.input);
            ProcessShipEvents(enemy.refs, ref enemy.move, ref enemy.input, null);
            state.enemies[i] = enemy;
        }

        // Camera
        // TODO: Should the camera have a rigidbody for movement interpolation?
        camera.transform.position = Vector3.Lerp(camera.transform.position, player.move.p, camera.spec.lerpFactor);
        camera.camera.orthographicSize = 5.0f + (player.refs.physicsTransform.childCount * 0.1f);

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
        // Attach debris that's touching the player
        MagnetismSpec mag = player.refs.magnetismSpec;
        {
            int count = player.refs.rigidbody.GetContacts(state.colliderCache);
            for (int i = 0; i < count; i++)
            {
                ref Collider2D collider = ref state.colliderCache[i];
                var dr = collider.GetComponentInParent<DebrisRefs>();
                if (dr == null) continue;

                Vector3 relPos = (Vector3) dr.rigidbody.position - player.move.p;
                dr.transform.position = dr.transform.position - (mag.packing * relPos.normalized);
                dr.transform.parent = player.refs.physicsTransform;
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
        }

        // Apply force to nearby objects
        {
            int count = Physics2D.OverlapCircleNonAlloc(
                player.move.p,
                mag.radius,
                state.colliderCache,
                mag.affectedLayers);

            for (int i = 0; i < count; i++)
            {
                Rigidbody2D rb = state.colliderCache[i].attachedRigidbody;
                if (rb.CompareTag(Tag.Player)) continue;

                Vector3 relPos = (Vector3) rb.position - player.move.p;
                float dist = relPos.magnitude;

                // NOTE: Object is inside the player. Skip it and let physics depenetrate it.
                if (dist < player.refs.collider.Radius()) continue;

                float strength = mag.strength * mag.strengthCurve.Evaluate(dist / mag.radius);
                Vector3 force = -relPos * (strength / dist);
                rb.AddForce(force, ForceMode2D.Force);
            }
        }
    }

    #if UNITY_EDITOR
    void OnDrawGizmos()
    {
        if (state.player.refs == null) return;

        ref ShipRefs pRefs = ref state.player.refs;
        ref MoveState mv = ref state.player.move;
        ref ShipInput ip = ref state.player.input;
        ref MagnetismSpec mg = ref state.player.refs.magnetismSpec;

        if (EditorApplication.isPlaying)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(mv.p, mv.p + ip.aim);
            Gizmos.DrawSphere(mv.p + state.lastProjectileSpawn, 0.1f);
        }

        if (mg != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(pRefs.rigidbody.position, mg.radius);
        }
    }
    #endif
}
