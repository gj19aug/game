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
        Vector2 throttle;
        throttle.x = GetKeyValue(KeyCode.D) - GetKeyValue(KeyCode.A);
        throttle.y = GetKeyValue(KeyCode.W) - GetKeyValue(KeyCode.S);
        state.playerMove.throttle = throttle;
    }

    void FixedUpdate()
    {
        // TODO: Move spec
        const float acceleration = 30.0f;
        const float deceleration = 4.0f;
        const float maxVelocity = 8.0f;
        float dt = Time.fixedDeltaTime;

        // Accelarate
        if (state.playerMove.throttle != Vector2.zero)
        {
            state.playerMove.acceleration = acceleration * state.playerMove.throttle;
            state.playerMove.velocity += state.playerMove.acceleration * dt;
            state.playerMove.velocity = Vector2.ClampMagnitude(state.playerMove.velocity, maxVelocity);
            state.playerMove.position += state.playerMove.velocity * dt;
            player.transform.position = state.playerMove.position;
        }
        // Decelerate to a stop
        else
        {
            state.playerMove.acceleration = deceleration * -state.playerMove.velocity;
            state.playerMove.velocity += state.playerMove.acceleration * dt;
            state.playerMove.velocity = Vector2.ClampMagnitude(state.playerMove.velocity, maxVelocity);
            state.playerMove.position += state.playerMove.velocity * dt;
            player.transform.position = state.playerMove.position;
        }
    }
}
