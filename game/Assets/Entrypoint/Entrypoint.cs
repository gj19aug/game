using UnityEngine;

public class Entrypoint : MonoBehaviour
{
    public PlayerRefs player;
    public new CameraRefs camera;

    public GameState state;

    public static int GetKeyValue(KeyCode key)
    {
        return Input.GetKey(key) ? 1 : 0;
    }

    void Update()
    {
        Vector2 input;
        input.x = GetKeyValue(KeyCode.D) - GetKeyValue(KeyCode.A);
        input.y = GetKeyValue(KeyCode.W) - GetKeyValue(KeyCode.S);
        input = Vector2.ClampMagnitude(input, 1.0f);
        state.playerMove.input = input;
    }

    void FixedUpdate()
    {
        float dt = Time.fixedDeltaTime;
        MoveSpec ms = player.moveSpec;
        ref MoveState mv = ref state.playerMove;

        float ddpMul = ms.acceleration;
        float dpDragMul = ms.velocityMultiplierForDrag;
        float drag = ms.dragCurve.Evaluate(dpDragMul * mv.dp.magnitude) * ms.drag;

        Vector2 ddp = ddpMul * mv.input;
        ddp += drag * -mv.dp;
        mv.p += 0.5f * ddp * dt * dt + mv.dp * dt;
        mv.dp += ddp * dt;

        player.transform.position = mv.p;
    }
}
