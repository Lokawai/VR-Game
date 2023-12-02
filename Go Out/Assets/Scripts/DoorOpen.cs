using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class DoorOpen : MonoBehaviour
{

    private GameObject GB;
    [SerializeField]
    private bool ispress = false;
    [SerializeField]
    private Vector3 targetPosition;

    private float speed = 5.0f;

  



    private void Update()
    {
        if (ispress)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, speed * Time.deltaTime);
        }

       
           // transform.position = Vector3.MoveTowards(transform.position, targetPosition, speed * Time.deltaTime);
        
    }

    public void press()
    {
        ispress = !ispress;
    }



}
