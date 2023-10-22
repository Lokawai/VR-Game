using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.InputSystem;

public class ActivateTeleportationRay : MonoBehaviour
{

    [SerializeField]
    private GameObject rightHandTeleport;

    [SerializeField]
    private InputActionProperty rightHandActivate;

    // Update is called once per frame
    void Update()
    {
        rightHandTeleport.SetActive(rightHandActivate.action.ReadValue<float>() > 0.1f);
    }
}
