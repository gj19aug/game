using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;

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
        // Movement
        Vector2 input;
        input.x = GetKeyValue(KeyCode.D) - GetKeyValue(KeyCode.A);
        input.y = GetKeyValue(KeyCode.W) - GetKeyValue(KeyCode.S);
        input = Vector2.ClampMagnitude(input, 1.0f);
        state.playerMove.input = input;

        // Aim direction
        Assert.IsTrue(camera.camera.transform.forward == Vector3.forward);
        Vector3 mouseSS = Input.mousePosition; mouseSS.z = -camera.camera.transform.position.z;
        Vector3 mouseWS = camera.camera.ScreenToWorldPoint(mouseSS);
        state.playerAim = mouseWS - state.playerMove.p;
    }

    void FixedUpdate()
    {
        float dt = Time.fixedDeltaTime;
        MoveSpec ms = player.moveSpec;
        ref MoveState mv = ref state.playerMove;

        float ddpMul = ms.acceleration;
        float dpDragMul = ms.velocityMultiplierForDrag;
        float drag = ms.dragCurve.Evaluate(dpDragMul * mv.dp.magnitude) * ms.drag;

        Vector3 ddp = ddpMul * mv.input;
        ddp += drag * -mv.dp;
        mv.p += 0.5f * ddp * dt * dt + mv.dp * dt;
        mv.dp += ddp * dt;

        player.transform.position = mv.p;
    }

    #if UNITY_EDITOR
    void OnDrawGizmos()
    {
        if (EditorApplication.isPlaying)
        {
            Vector3 from = state.playerMove.p;
            Vector3 to = from + state.playerAim;
            Gizmos.color = Color.green;
            Gizmos.DrawLine(from, to);
        }
    }
    #endif
}
