using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DilmerGames.Core.Singletons;
public class VRRigReferences : MonoBehaviour
{
    public static VRRigReferences Singleton;

    [SerializeField] private Transform root;
    public Transform Root { get { return root; } }
    [SerializeField] private Transform head;
    public Transform Head { get { return head; } }
    [SerializeField] private Transform leftHand;
    public Transform LeftHand { get { return leftHand; } }
    [SerializeField] private Transform rightHand;
    public Transform RightHand { get { return rightHand; } }

    private void Awake()
    {
        Singleton = this;
    }
}
