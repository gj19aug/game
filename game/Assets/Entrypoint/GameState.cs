using UnityEngine;

public struct GameState
{
    public MoveState playerMove;
    public Vector3 playerAim;
}

public struct MoveState
{
    public Vector3 p;
    public Vector3 dp;
    public Vector3 input;
}
