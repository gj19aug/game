using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AsteroidBirth : MonoBehaviour
{
    public Rigidbody2D rb;
    // Start is called before the first frame update
    void Start()
    {
        rb.AddTorque(Random.Range(300.0f, 500.0f), ForceMode2D.Force); 
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
