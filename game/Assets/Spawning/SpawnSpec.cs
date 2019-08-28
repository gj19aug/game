using System;
using UnityEngine;

[Serializable]
public struct ShipSpawnSpec
{
    [Range(0.0f, 1.0f)]
    public float probability;

    public ShipSpec spec;
}

[CreateAssetMenu(fileName = "New Spawn Spec", menuName = "Spawn Spec")]
public class SpawnSpec : ScriptableObject
{
    public int maxCount = 5;
    public float timeBetweenSpawns = 0.4f;
    public ShipSpawnSpec[] ships;
}
