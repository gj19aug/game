using UnityEngine;
using System.Collections.Generic;

public struct GameState
{
    public MoveState playerMove;
}

public struct MoveState
{
    public Vector2 position;
    public Vector2 velocity;
    public Vector2 acceleration;
    public Vector2 throttle;
}
