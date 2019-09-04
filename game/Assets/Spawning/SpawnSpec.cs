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
    public float timeToMaxSpawnRate = 45.0f;
    [Range(0.0f, 10.0f)]
    public float[] timeBetweenSpawns = new float[] { 1.0f };

    public ShipSpawnSpec[] ships;
}
