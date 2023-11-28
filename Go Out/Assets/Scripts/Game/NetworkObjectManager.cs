using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using DilmerGames.Core.Singletons;


public class NetworkObjectManager : NetworkBehaviour
{
    public static NetworkObjectManager Singleton;
    [SerializeField]
    private int maxObjectInstanceCount = 3;
    private NetworkObject obj;
    public GameObject spawnedObj;
    private Vector3 pos;
    private Quaternion quaternion;
    private NetworkObject parentObj;
        public GameObject SpawnObject(GameObject targetObject, Vector3 position, Quaternion quaternion)
        {
        obj = targetObject.GetComponent<NetworkObject>();
        pos = position;
        this.quaternion = quaternion;
            SpawnObjServerRpc();


          return spawnedObj;
        }
    private void Awake()
    {
        Singleton = this;
    }
    private NetworkObject childObj;
    public void SetParent(NetworkObject childToParent, NetworkObject parentObj)
    {
        childObj= childToParent;
        this.parentObj = parentObj;
        SetParentServerRpc();
    }
    public override void OnNetworkObjectParentChanged(NetworkObject parentNetworkObject)
    {
        childObj.TrySetParent(parentNetworkObject);
    }
    [ServerRpc(RequireOwnership = false)]
    public void SpawnObjServerRpc()
    {
        NetworkObject networkObject = Instantiate(obj, pos, quaternion);
        Debug.Log(networkObject.gameObject);
        spawnedObj = networkObject.gameObject;
        networkObject.SpawnAsPlayerObject(OwnerClientId);
    }
    [ServerRpc(RequireOwnership = false)]
    public void SetParentServerRpc()
    {
        childObj.TrySetParent(parentObj);
    }
    NetworkObject targetToDestroy;
    public void DestroyObject(NetworkObject networkObject)
    {
        targetToDestroy = networkObject;
        DestroyObjectServerRpc();
    }
    [ServerRpc(RequireOwnership = false)]
    public void DestroyObjectServerRpc()
    {
        targetToDestroy.Despawn();
    }
    NetworkVariable<Vector3> targetPosition = new NetworkVariable<Vector3>();
    NetworkVariable<Vector3> targetRotation = new NetworkVariable<Vector3>();
    Transform targetTransform;
    public void ChangePosition(Vector3 finalPos, Transform transform)
    {
        targetPosition.Value = finalPos;
        targetTransform = transform;
        ChangePosServerRpc();
    }
    public void ChangeRotation(Vector3 finalRotation, Transform transform)
    {
        targetRotation.Value = finalRotation;
        targetTransform = transform;
        ChangeRotServerRpc();
    }
    [ServerRpc(RequireOwnership = false)]
    private void ChangePosServerRpc()
    {
        targetTransform.position = targetPosition.Value;
    }
    [ServerRpc(RequireOwnership = false)]
    private void ChangeRotServerRpc()
    {
        targetTransform.Rotate(targetRotation.Value, Space.World);
    }
    GameManager targetGameManager;
    public void UpdateManager(GameManager gameManager)
    {
        targetGameManager = gameManager;
    }
    [ServerRpc]
    private void UpdateManagerServerRpc()
    {
        GameManager.Singleton = targetGameManager;
    }
}

