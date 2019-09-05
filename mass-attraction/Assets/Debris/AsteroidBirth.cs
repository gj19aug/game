using UnityEngine;

public class AsteroidBirth : MonoBehaviour
{
    public Rigidbody2D rb;

    void Start()
    {
        rb.AddTorque(Random.Range(300.0f, 500.0f), ForceMode2D.Force);
    }
}
