using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
public class NetworkPlayer : NetworkBehaviour
{
    [SerializeField]
    private Transform root;
    [SerializeField]
    private Transform head;
    [SerializeField]
    private Transform leftHand;
    [SerializeField]
    private Transform rightHand;
    [SerializeField]
    private Renderer[] meshToDisable;
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (IsOwner)
        {
            foreach (var item in meshToDisable)
            {
                item.enabled = false;
            }
        }
    }


    // Update is called once per frame
    void Update()
    {
        
        if (IsOwner)
        {
            root.position = VRRigReferences.Singleton.Root.position;
            root.rotation = VRRigReferences.Singleton.Root.rotation;

            head.position = VRRigReferences.Singleton.Head.position;
            head.rotation = VRRigReferences.Singleton.Head.rotation;

            leftHand.position = VRRigReferences.Singleton.LeftHand.position;
            leftHand.rotation = VRRigReferences.Singleton.LeftHand.rotation;

            rightHand.position = VRRigReferences.Singleton.RightHand.position;
            rightHand.rotation = VRRigReferences.Singleton.RightHand.rotation;
        }
    }
}
