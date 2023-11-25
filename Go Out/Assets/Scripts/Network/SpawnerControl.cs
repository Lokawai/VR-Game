using DilmerGames.Core.Singletons;
using Unity.Netcode;
using UnityEngine;

public class SpawnerControl : NetworkSingleton<SpawnerControl>
{
    [SerializeField]
    private GameObject objectPrefab;

    [SerializeField]
    private int maxObjectInstanceCount = 3;

    public void SpawnObjects()
    {
        if (!IsServer) return;

        for (int i = 0; i < maxObjectInstanceCount; i++)
        {
            GameObject go = Instantiate(objectPrefab,
                new Vector3(Random.Range(-10, 10), 10.0f, Random.Range(-10, 10)), Quaternion.identity);

            go.GetComponent<Rigidbody>().isKinematic = false;  //if checked, need to turn off
            go.GetComponent<NetworkObject>().Spawn();  //sync object across network

        }
    }
}
