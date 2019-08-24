using UnityEngine;

[CreateAssetMenu(fileName = "New Spawn Spec", menuName = "Spawn Spec")]
public class SpawnSpec : ScriptableObject
{
    public int maxCount = 5;
    public float timeBetweenSpawns = 0.4f;
}
