using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class PressurePad : MonoBehaviour
{
    [Header("Note: Require the tag of Object as \"Target\" in order to activate")]
    [SerializeField] private UnityEvent m_enterAction;
    [SerializeField] private bool includePlayer = false;
    [SerializeField] private float m_DistanceToActivateFromPlayer = 0.5f;
    private bool isPressed = false;
    public void SetPressedState(bool state)
    {
        isPressed = state;
    }
    private void OnTriggerStay(Collider other)
    {
        if(other.tag == "Target")
        {
            
            float distance = Vector3.Distance(transform.position, other.transform.position);
            if(distance < 0.05f)
            {
                Rigidbody targetBody = other.GetComponent<Rigidbody>();
                if(targetBody != null)
                {
                    targetBody.isKinematic = true;
                }
            }
            if(!isPressed)
            {
                m_enterAction.Invoke();
                isPressed = true;
            }
        }
    }
    private void Update()
    {
        if (includePlayer)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            float distance = Vector3.Distance(player.transform.position, gameObject.transform.position);
            if (distance <= m_DistanceToActivateFromPlayer)
            {
                if(!isPressed)
                {
                    m_enterAction.Invoke();
                    isPressed = true;
                }
            }
        }
    }
}
