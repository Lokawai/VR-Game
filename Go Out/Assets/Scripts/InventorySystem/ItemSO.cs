using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ItemStorage", menuName ="ScriptableObjects/ItemSO")]
public class ItemSO : ScriptableObject
{
    public Item[] item;
    [System.Serializable]
    public class Item
    {
        public GameObject itemObject;
        public string itemName;
        public Sprite itemSprite;
    }
}
