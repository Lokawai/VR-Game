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
                go.GetComponent<XRGrabInteractable>().selectEntered.AddListener(SetStateTrue);
                go.GetComponent<XRGrabInteractable>().selectExited.AddListener(SetStateFalse);
                PositionManager positionManager = go.GetComponent<PositionManager>();
                positionManager.isCloned.Value = true;
            }
            go.GetComponent<Rigidbody>().isKinematic = false;  //if checked, need to turn off
           

        }
    }

    private void SetStateFalse(SelectExitEventArgs args)
    {
        gameManager.SetEnableTextFalse();
    }
    private void SetStateTrue(SelectEnterEventArgs args)
    {
        gameManager.SetEnableTextTrue();
    }
}
