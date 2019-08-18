using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;

public class Entrypoint : MonoBehaviour
{
    // Inspector
    public PlayerRefs player;
    public new CameraRefs camera;
    public ProjectileRefs projectilePrefab;
    public DebrisRefs[] debrisPrefabs;

    // Game State
    [HideInInspector] public GameState state;

    public static int GetKeyValue(KeyCode key)
    {
        return Input.GetKey(key) ? 1 : 0;
    }

    void Awake()
    {
        state.playerInput.events = new List<ShipInputEvent>(8);
        state.projectilePool = new Pool<ProjectileRefs>();
        state.projectilePool.Initialize("Projectiles", projectilePrefab, 64);
        state.projectiles = new List<Projectile>(64);
        state.debrisPools = new Pool<DebrisRefs>[debrisPrefabs.Length];
        for (int i = 0; i < debrisPrefabs.Length; i++)
        {
            var pool = new Pool<DebrisRefs>();
            pool.Initialize("Debris", debrisPrefabs[i], 32);
            state.debrisPools[i] = pool;
        }

        state.colliderCache = new Collider2D[32];

        // DEBUG: Spawn a bunch of debris
        for (int i = 0; i < 20; i++)
        {
            Pool<DebrisRefs> pool = RandomEx.Element(state.debrisPools);
            DebrisRefs pr = pool.Spawn();
            pr.rigidbody.position = 10.0f * Random.insideUnitCircle;
            #if false
            pr.rigidbody.rotation = Vector2.SignedAngle(Vector2.up, ip.aim);
            pr.rigidbody.AddForce(pr.spec.impulse * ip.aim, ForceMode2D.Impulse);
            state.projectiles.Add(new Projectile() { refs = pr, lifetime = pr.spec.lifetime });
            #endif
        }
    }

    void Update()
    {
        // NOTE: Input only!

        ref MoveState mv = ref state.playerMove;
        ref ShipInput ip = ref state.playerInput;

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

    void FixedUpdate()
    {
        // NOTE: Simulate!

        ref MoveState mv = ref state.playerMove;
        ref ShipInput ip = ref state.playerInput;
        ref MagnetismSpec mg = ref player.magnetismSpec;
        ref List<DebrisRefs> pd = ref state.playerDebris;
        ref MoveSpec ms = ref player.moveSpec;

        // Movement
        float dt = Time.fixedDeltaTime;
        float ddpMul = ms.acceleration;
        float dpDragMul = ms.velocityMultiplierForDrag;
        float drag = ms.dragCurve.Evaluate(dpDragMul * mv.dp.magnitude) * ms.drag;

        Vector3 ddp = ddpMul * ip.throttle;
        ddp += drag * -mv.dp;
        mv.p += 0.5f * ddp * dt * dt + mv.dp * dt;
        mv.dp += ddp * dt;

        player.rigidbody.position = mv.p;

        // Camera
        camera.transform.position = Vector3.Lerp(camera.transform.position, mv.p, camera.spec.lerpFactor);
        camera.camera.orthographicSize = 5.0f + (player.physicsTransform.childCount * 0.1f);

        // Shooting
        for (int i = 0; i < ip.events.Count; i++)
        {
            switch (ip.events[i])
            {
                default: Assert.IsTrue(false); break;

                case ShipInputEvent.Shoot:
                {
                    // DEBUG: Remove this once we shoot from turrets directly
                    // TODO: Horribly inefficient
                    float spawnRadius = player.collider.radius;
                    for (int j = 0; j < pd.Count; j++)
                    {
                        Bounds debrisBounds = pd[j].collider.bounds;
                        Vector3 relPos = debrisBounds.center - mv.p;
                        float dot = Vector3.Dot(relPos.normalized, ip.aim);
                        float dist = relPos.magnitude + debrisBounds.extents.magnitude;
                        spawnRadius = Mathf.Max(spawnRadius, dot*dist);
                    }

                    ProjectileRefs pr = state.projectilePool.Spawn();
                    pr.rigidbody.tag = Tag.Player;
                    pr.rigidbody.position = mv.p + (spawnRadius + pr.collider.radius) * ip.aim;
                    pr.rigidbody.rotation = Vector2.SignedAngle(Vector2.up, ip.aim);
                    pr.rigidbody.AddForce(pr.spec.impulse * ip.aim, ForceMode2D.Impulse);
                    state.projectiles.Add(new Projectile() { refs = pr, lifetime = pr.spec.lifetime });

                    state.lastProjectileSpawn = pr.rigidbody.position;
                    break;
                }
            }
        }
        ip.events.Clear();

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
        {
            int count = player.rigidbody.GetContacts(state.colliderCache);
            for (int i = 0; i < count; i++)
            {
                ref Collider2D collider = ref state.colliderCache[i];
                var refs = collider.GetComponentInParent<DebrisRefs>();
                if (refs == null) continue;

                Vector3 relPos = (Vector3) refs.rigidbody.position - mv.p;
                refs.transform.position = refs.transform.position - (mg.packing * relPos.normalized);
                refs.transform.parent = player.physicsTransform;
                pd.Add(refs);

                // TODO: Having to destroy instead of disable is pretty lame.
                DestroyImmediate(refs.rigidbody);
                refs.rigidbody = null;

                // TODO: Horribly inefficient
                for (int j = 0; j < state.debrisPools.Length; j++)
                {
                    if (state.debrisPools[j].Remove(refs))
                        break;
                }
            }
        }


        // Apply force to nearby objects
        {
            int count = Physics2D.OverlapCircleNonAlloc(
                mv.p,
                mg.radius,
                state.colliderCache,
                mg.affectedLayers);

            for (int i = 0; i < count; i++)
            {
                Rigidbody2D rb = state.colliderCache[i].attachedRigidbody;
                if (rb.CompareTag(Tag.Player)) continue;

                Vector3 relPos = (Vector3) rb.position - mv.p;
                float dist = relPos.magnitude;

                // NOTE: Object is inside the player. Skip it and let physics depenetrate it.
                if (dist < player.collider.radius) continue;

                float strength = mg.strength * mg.strengthCurve.Evaluate(dist / mg.radius);
                Vector3 force = -relPos * (strength / dist);
                rb.AddForce(force, ForceMode2D.Force);
            }
        }
    }

    #if UNITY_EDITOR
    void OnDrawGizmos()
    {
        ref MoveState mv = ref state.playerMove;
        ref ShipInput ip = ref state.playerInput;
        ref MagnetismSpec mg = ref player.magnetismSpec;

        if (EditorApplication.isPlaying)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(mv.p, mv.p + ip.aim);
            Gizmos.DrawSphere(state.lastProjectileSpawn, 0.1f);
        }

        if (mg != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(player.rigidbody.position, mg.radius);
        }
    }
    #endif
}
