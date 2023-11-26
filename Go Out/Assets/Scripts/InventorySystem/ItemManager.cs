using System.Collections;
using System.Collections.Generic;
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
    public static void removeChilds(GameObject parentObject)
    {
        foreach(Transform child in parentObject.transform)
        {
            GameObject.Destroy(child.gameObject);
        }
    }
}