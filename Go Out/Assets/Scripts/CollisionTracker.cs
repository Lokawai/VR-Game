using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CollisionTracker : MonoBehaviour
{
    public bool isHit = false;
    public GameObject hitObject = null;

    private void OnCollisionEnter(Collision collision)
    {
        isHit = true;
        hitObject = collision.gameObject;
    }
    private void OnTriggerEnter(Collider other)
    {
        isHit = true;
        hitObject = other.gameObject;
    }
    public void ResetValue()
    {
        hitObject = null;
        isHit = false;
    }
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
