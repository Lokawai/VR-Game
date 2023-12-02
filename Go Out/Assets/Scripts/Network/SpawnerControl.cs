using DilmerGames.Core.Singletons;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;

public class SpawnerControl : NetworkSingleton<SpawnerControl>
{
    private NetworkObjectManager networkObjectManager;
    [SerializeField]
    private GameObject objectPrefab;
    [SerializeField]
    private Transform spawnPlaceHolder;
    [SerializeField]
    private int maxObjectInstanceCount = 3;
    private GameManager gameManager;
    private void Start()
    {
        networkObjectManager = NetworkObjectManager.Singleton;
        gameManager = GameManager.Singleton;
    }
    [SerializeField]
    private UnityEvent unityEvent;
    public void SpawnObjects()
    {
        if (!IsServer) return;

        for (int i = 0; i < maxObjectInstanceCount; i++)
        {
            GameObject go = networkObjectManager.SpawnObject(objectPrefab, spawnPlaceHolder.position, Quaternion.identity);
            if (go.GetComponent<GunController>() != null)
            {
                go.GetComponent<XRGrabInteractable>().selectEntered.AddListener(SetStateFalse);
                go.GetComponent<XRGrabInteractable>().selectExited.AddListener(SetStateTrue);
            }
            go.GetComponent<Rigidbody>().isKinematic = false;  //if checked, need to turn off
           

        }
    }
    private void SetStateFalse(SelectEnterEventArgs args)
    {
        gameManager.SetEnableTextFalse();
    }
    private void SetStateTrue(SelectExitEventArgs args)
    {
        gameManager.SetEnableTextTrue();
    }
}
