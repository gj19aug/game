﻿using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;

public class Entrypoint : MonoBehaviour
{
    // Inspector
    public PlayerRefs player;
    public new CameraRefs camera;
    public ProjectileRefs projectilePrefab;
    public DebrisRefs debrisPrefab;

    // Cache
    [HideInInspector] public Collider2D[] colliderCache;

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
        colliderCache = new Collider2D[32];
    }

    void Update()
    {
        // NOTE: Input only!

        // Move
        Vector3 throttle = new Vector3();
        throttle.x = GetKeyValue(KeyCode.D) - GetKeyValue(KeyCode.A);
        throttle.y = GetKeyValue(KeyCode.W) - GetKeyValue(KeyCode.S);
        throttle = Vector3.ClampMagnitude(throttle, 1.0f);
        state.playerInput.throttle = throttle;

        // Aim
        Assert.IsTrue(camera.camera.transform.forward == Vector3.forward);
        Vector3 mouseSS = Input.mousePosition; mouseSS.z = -camera.camera.transform.position.z;
        Vector3 mouseWS = camera.camera.ScreenToWorldPoint(mouseSS);
        state.playerInput.aim = (mouseWS - state.playerMove.p).normalized;

        // Shoot
        if (Input.GetKeyDown(KeyCode.Mouse0) | Input.GetKeyDown(KeyCode.Space))
            state.playerInput.events.Add(ShipInputEvent.Shoot);
    }

    void FixedUpdate()
    {
        // NOTE: Simulate!

        ref MoveState mv = ref state.playerMove;
        ref ShipInput ip = ref state.playerInput;
        ref MagnetismSpec mg = ref player.magnetismSpec;

        // Movement
        float dt = Time.fixedDeltaTime;
        MoveSpec ms = player.moveSpec;

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

        // Shooting
        for (int i = 0; i < state.playerInput.events.Count; i++)
        {
            switch (state.playerInput.events[i])
            {
                default: Assert.IsTrue(false); break;

                case ShipInputEvent.Shoot:
                {
                    ProjectileRefs pr = state.projectilePool.Spawn();
                    pr.rigidbody.tag = Tag.Player;
                    pr.rigidbody.position = mv.p + 0.5f*(player.collider.radius + pr.collider.radius)*ip.aim;
                    pr.rigidbody.rotation = Vector2.SignedAngle(Vector2.up, ip.aim);
                    pr.rigidbody.AddForce(pr.spec.impulse * ip.aim, ForceMode2D.Impulse);
                    state.projectiles.Add(new Projectile() { refs = pr, lifetime = pr.spec.lifetime });
                    break;
                }
            }
        }
        state.playerInput.events.Clear();

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
        int count = Physics2D.OverlapCircleNonAlloc(
            state.playerMove.p,
            mg.radius,
            colliderCache,
            mg.affectedLayers);

        for (int i = 0; i < count; i++)
        {
            Rigidbody2D rb = colliderCache[i].attachedRigidbody;
            if (rb.CompareTag(Tag.Player)) continue;

            Vector3 scaledDir = state.playerMove.p - (Vector3) rb.position;
            float dist = scaledDir.magnitude;
            float strength = mg.strength * mg.strengthCurve.Evaluate(dist);
            Vector3 force = scaledDir * (strength / dist);
            rb.AddForce(force, ForceMode2D.Force);
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
        }

        if (mg != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(player.rigidbody.position, mg.radius);
        }
    }
    #endif
}
