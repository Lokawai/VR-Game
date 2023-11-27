using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public static class ItemManager
{
    private static ItemSO itemData;

    public static ItemSO ItemData
    {
        get
        {
            if (itemData == null)
            {
                itemData = Resources.Load<ItemSO>("ItemData/Data");

                if (itemData == null)
                {
                    Debug.LogError("ItemData not found!");
                }
            }

            return itemData;
        }
    }
    public static void ServerRemoveChilds(GameObject parentObject)
    {
        NetworkObjectManager networkObjectSpawner = NetworkObjectManager.Singleton;
        foreach(Transform child in parentObject.transform)
        {
            networkObjectSpawner.DestroyObject(child.gameObject.GetComponent<NetworkObject>());
        }
    }
    public static void removeChilds(GameObject parentObject)
    {
        foreach (Transform child in parentObject.transform)
        {
            GameObject.Destroy(child.gameObject);
        }
    }
}