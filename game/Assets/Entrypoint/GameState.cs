using UnityEngine;
using System.Collections.Generic;

public struct GameState
{
    public MoveState playerMove;
}

public struct MoveState
{
    public Vector2 p;
    public Vector2 dp;
    public Vector2 input;
}
