using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;
using Unity.Netcode;


public enum InventoryType
{
    item,
    block
}

public class InventoryBehaviour : MonoBehaviour
{
    [Header("Toggle")]
    [SerializeField] private bool isDropable = false;
    [SerializeField] private bool haveInventoryBag = false;
    [Header("Input Settings")]
    public InputAction dropKey;
    public KeyCode invBagKey = KeyCode.E;
    public InputAction switchItem;
    public bool isItemSelectable = true;
    [Header("Canvas")]
    
    public GameObject InventoryCanvas;
    public GameObject SlotPlaceHolder;
    [Header("Slot UI")]
    public Sprite slotImageNormal;
    public Sprite slotImageSelected;
    [Header("Item Text Display")]
    public TMP_Text DisplayText;
    public float fadeInDuration = 1f; // Duration of the fade in animation
    public float displayDuration = 2f; // Duration to display the text
    public float fadeOutDuration = 1f; // Duration of the fade out animation
    private Coroutine fadeCoroutine;
    [Header("Mouse Input")]
    [SerializeField] private float scrollCoolDown = 0.2f;
    public bool isMouseVisble = false;
    private bool preIsMouseVisible = false;
    private float scrollTimer = 0f; // Timer to track cooldown
    [Header("Main Settings")]
    [SerializeField] private InventoryType inventoryType; 
    public InventorySystem inventory;
    [SerializeField] private int selectedSlot = 0;
    [SerializeField] private GameObject slotObject;
    [SerializeField] private GameObject placeItemLocation;
    [SerializeField] private int[] initialSlotItem;
    NetworkObjectManager networkObjectSpawner;
    [Header("Drop Settings")]
    [SerializeField] private GameObject dropItemPrefab;
    [SerializeField] private float dropForce = 10f;
    [SerializeField] private Vector3 dropOffset;
    [Header("Inventory Bag Settings")]
    [SerializeField] private GameObject invBagPanel;
    [SerializeField] private GameObject invBagContainer;
    [SerializeField] private GameObject interactableSlotObj;
    [Header("Block Inventory Settings")]
    
    private ItemSO itemData;

    [Header("Capture Settings")]
    [SerializeField] private Camera captureCamera;
    [SerializeField] private GameObject captureContainer;
    [SerializeField] private string captureSavePath = "/StructureData/Inventory/Temp";
    private void Awake()
    {
        StartCursorState();
        itemData = ItemManager.ItemData;
        networkObjectSpawner = NetworkObjectManager.Singleton;
    }
    
    private void OnValidate()
    {
        if(isMouseVisble != preIsMouseVisible)
        {
            StartCursorState();
            preIsMouseVisible = isMouseVisble;
        }
    }
    public void StartCursorState()
    {
        StartCoroutine(CursorState());
    }
    public void ToggleCursorState(bool state = default)
    {
        if (state == default)
        {
            isMouseVisble = !isMouseVisble;
        } else
        {
            isMouseVisble = state;
        }
        StartCursorState();
    }
    private IEnumerator CursorState()
    {
        yield return null;

        #region MouseVisible State
        if (isMouseVisble)
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
          
            isItemSelectable = false;
        }
        else
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
          
            isItemSelectable = true;
        }
        yield break;
        #endregion
    }
    // Start is called before the first frame update
    void Start()
    {
        switchItem.Enable();
        dropKey.Enable();
        switchItem.started += SwitchItem_started;
        dropKey.started += DropKey_started;
        #region ShowItem(Debug)
        //Debug.Log("Item List: ");
        //for (int i = 0; i < itemData.item.Length; i++)
        //{
        //    Debug.Log(itemData.item[i].itemObject);
        //}
        #endregion
        if (inventory == null)
        {
            inventory = gameObject.GetComponent<InventorySystem>();
        }
        setup();
        SlotPlaceHolder.GetComponent<RectTransform>().sizeDelta = new Vector2(inventory.slot.Length * slotObject.GetComponent<RectTransform>().rect.width, SlotPlaceHolder.GetComponent<RectTransform>().rect.height);
    }
    public void OnDestroy()
    {
        switchItem.Disable();
        dropKey.Disable();
        switchItem.started -= SwitchItem_started;
        dropKey.started -= DropKey_started;
    }
    private void DropKey_started(InputAction.CallbackContext obj)
    {
        throw new System.NotImplementedException();
    }

    private void SwitchItem_started(InputAction.CallbackContext obj)
    {
        scrollWheelDetection();
    }

    // Update is called once per frame
    void Update()
    {
        if (isItemSelectable)
        {
            scrollTimeCD();
        }
        if(isDropable)
        {
            dropDetection();
        }
        if(haveInventoryBag)
        {
            openBagDetection();
        }
    }
    private void openBagDetection()
    {
        if(Input.GetKeyDown(invBagKey))
        {
            OpenBag();
        }
    }
    private void dropDetection()
    {
        return;
            dropItem();
        
    } 
    private void dropItem()
    {
        GameObject dropBaseObj = Instantiate(dropItemPrefab, gameObject.transform.position + dropOffset, Quaternion.identity);
        GameObject dropItem = Instantiate(itemData.item[inventory.slot[selectedSlot].getId()].itemObject,dropBaseObj.transform.position, Quaternion.identity);
        dropItem.transform.SetParent(dropBaseObj.transform);
        Collider itemCollider = dropItem.GetComponent<Collider>();
        if(itemCollider != null)
        {
            itemCollider.isTrigger = false;
        }
        dropBaseObj.layer = LayerMask.NameToLayer("DroppedItem");
        StartCoroutine(EnableCollisions(dropBaseObj, 0.5f));
        ForceDrop(dropBaseObj);
        resetSlot(selectedSlot);
        BoxCollider boxCollider = dropBaseObj.GetComponent<BoxCollider>();
        if(boxCollider != null)
        {
            boxCollider.size = getModelSize(dropItem.transform.gameObject);
        }
    }
    private IEnumerator EnableCollisions(GameObject target, float delay)
    {
        yield return new WaitForSeconds(delay);
        // Restore the collision detection by setting the layer back to the default layer
        target.layer = LayerMask.NameToLayer("Default");
    }
    private void ForceDrop(GameObject targetObj)
    {
        Rigidbody rb = targetObj.GetComponent<Rigidbody>();
        if (rb != null)
        {
            // Get the player's rotation
            Quaternion playerRotation = transform.rotation;

            // Extract the forward direction from the player's rotation
            Vector3 dropDirection = playerRotation * Vector3.forward;

            // Apply the drop force to the Rigidbody
            rb.AddForce(dropDirection * dropForce, ForceMode.Impulse);
        }
    }
    public void inputNumDetection()
    {
        if (inventory.slot.Length > 1)
        {

           
        }
    }
    private bool isScrollCD = false;
    public void scrollTimeCD()
    {
        if(isScrollCD)
        {
            scrollTimer -= Time.deltaTime;
            if(scrollTimer <= 0)
            {
                isScrollCD = false;
            }
        }
    }
    private void scrollWheelDetection()
    {
        if (inventory.slot.Length <= 1) return;
        if (scrollTimer <= 0f && isScrollCD == false)
        {
            
                // Scroll Upward
                // Perform actions or increase a value
                if (selectedSlot < inventory.GetMaxFrontSlots()- 1)
                {
                    selectedSlot++;
                } else
                {
                    selectedSlot = 0;
                }
                EquipItem(selectedSlot);
                scrollTimer = scrollCoolDown;
            isScrollCD = true;
        }

    }
    public void setup()
    {
        setSlotUI(SlotPlaceHolder, "hotbar");
        setInitialInventory();
        if (invBagPanel != null)
        {
            invBagPanel.SetActive(true);
            setSlotUI(invBagContainer, "bag");
            initalizeInvBag();

            invBagPanel.SetActive(false);
        }
        InventoryCanvas.SetActive(false);
    }
    private void setSlotUI(GameObject slotPH, string type)
    {
        if (type == "hotbar")
        {
            for (int i = 0; i < inventory.slot.Length || i < inventory.GetMaxFrontSlots(); i++)
            {
                setSlotImg(slotPH);
            }
        }
        else if (type == "bag")
        {
            for (int i = 0; i < inventory.slot.Length; i++)
            {
                setSlotImg(slotPH);
            }
        }
    }
    private void setSlotImg(GameObject slotObj)
    {
        GameObject ui = Instantiate(slotObject, slotObj.transform.position, Quaternion.identity);
        ui.transform.SetParent(slotObj.transform);
        
    }
    private void setInitialInventory()
    {
        for (int i = 0; i < initialSlotItem.Length; i++)
        {
            setSlotItem(i, initialSlotItem[i]);
        }
    }
    public void setSlotItem(int id, int item)
    {
        inventory.slot[id].setId(item);
        switch(inventoryType)
        {
            case InventoryType.item:
                if (item != -1 && item < itemData.item.Length)
                {
                    inventory.slot[id].item.SetItem(itemData.item[item].itemObject);
                }
            break;
            case InventoryType.block:
                
            break;
        }
       
        setSlotObjUI(id, SlotPlaceHolder, slotObject);
        if (id == selectedSlot)
        {
            EquipItem(id);
        }
    }
    public void resetSlot(int id)
    {
        inventory.slot[id].item.SetItem(null);
        inventory.slot[id].setId(-1);
        inventory.slot[id].setCount(0);
        inventory.slot[id].setName("");
        Destroy(SlotPlaceHolder.transform.GetChild(id).GetChild(0).gameObject);
        ItemManager.removeChilds(placeItemLocation);
    }
    private Vector3 getModelSize(GameObject targetObject)
    {
        Vector3 size = new Vector3();
        Renderer[] renderers = GetComponentsInChildren<Renderer>();

        // Create an empty bounds to encapsulate all the renderers
        Bounds combinedBounds = new Bounds();

        // Iterate through all the renderers and expand the combined bounds
        foreach (Renderer renderer in renderers)
        {
            // Expand the combined bounds to include the renderer's bounds
            combinedBounds.Encapsulate(renderer.bounds);
            Debug.Log(renderer.bounds);
        }
        Debug.Log("Total: " + combinedBounds);
        // Get the size of the combined bounds
        size = combinedBounds.extents;
        return size;
    }
    private void resetSelectUI()
    {
        foreach (Transform child in SlotPlaceHolder.transform)
        {
            Image childImg = child.GetComponent<Image>();
            childImg.sprite = slotImageNormal;
        }
    }
    [ServerRpc]
    public void EquipItem(int slotId)
    {
        if (placeItemLocation.transform.childCount > 1)
        {
            ItemManager.ServerRemoveChilds(placeItemLocation);
        } else if(placeItemLocation.transform.childCount == 1) //Saftey Issue
        {
            networkObjectSpawner.DestroyObject(placeItemLocation.transform.GetChild(0).GetComponent<NetworkObject>());
        }
        resetSelectUI();
        if (selectedSlot != slotId)
        {
            selectedSlot = slotId;
        }
        if (inventory.slot[slotId].getId() >= 0 && slotId < inventory.slot.Length)
        {
            //Debug.Log(inventory.slot[slotId].getId());
            GameObject targetItem = null;
            int invId = inventory.slot[slotId].getId();
            switch (inventoryType)
            {
                case InventoryType.item:
                    targetItem = networkObjectSpawner.SpawnObject(itemData.item[invId].itemObject, placeItemLocation.transform.position, Quaternion.identity);
                    //targetItem.transform.position += itemData.item[invId].itemObject.transform.position;
                    if(targetItem == null)
                    {
                        Debug.Log("Failed to get the TargetItem" + "Try to get item: " + networkObjectSpawner.spawnedObj);
                        targetItem = networkObjectSpawner.spawnedObj;
                    }
                    //targetItem.transform.rotation = itemData.item[invId].itemObject.transform.rotation;
                    networkObjectSpawner.ChangePosition(targetItem.transform.position + itemData.item[invId].itemObject.transform.position, targetItem.transform);
                    networkObjectSpawner.ChangeRotation(itemData.item[invId].itemObject.transform.rotation, targetItem.transform);
                    break;
                case InventoryType.block:
                   
                 break;
            }
            NetworkObject networkObject = targetItem.GetComponent<NetworkObject>();
            NetworkObject placeItemNetworkObj = placeItemLocation.GetComponent<NetworkObject>();
            
            networkObjectSpawner.SetParent(networkObject, placeItemNetworkObj);
            Collider itemCollider = targetItem.GetComponent<Collider>();
            if (itemCollider != null)
            {
                itemCollider.isTrigger = true;
            }
            
        }
        Image targetImg = SlotPlaceHolder.transform.GetChild(slotId).GetComponent<Image>();
        targetImg.sprite = slotImageSelected;
        StartFadeInText(slotId);

    }


    private void setSlotObjUI(int id, GameObject slotPH, GameObject slotObj)
    {
        if (id < slotPH.transform.childCount)
        {

            GameObject targetUI = slotPH.transform.GetChild(id).gameObject;
            if (targetUI.transform.childCount > 1)
            {
                Destroy(targetUI.transform.GetChild(0).gameObject);
            }
            if (inventoryType == InventoryType.item)
            {
                if (inventory.slot[id].getId() != -1 && inventory.slot[id].getId() < itemData.item.Length)
                {
                    if (targetUI.transform.GetChild(0) == null)
                    {
                        GameObject instantiatedUI = Instantiate(slotObj, Vector3.zero, Quaternion.identity);
                        instantiatedUI.transform.SetParent(targetUI.transform);
                        if (instantiatedUI.GetComponent<Image>() == null)
                        {
                            instantiatedUI.AddComponent<Image>();
                        }
                        instantiatedUI.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
                        Image uiImg = instantiatedUI.GetComponent<Image>();

                        uiImg.sprite = itemData.item[inventory.slot[id].getId()].itemSprite;
                        uiImg.preserveAspect = true;
                    } else
                    {
                        Image uiImg = targetUI.transform.GetChild(0).GetComponent<Image>();
                        uiImg.sprite = itemData.item[inventory.slot[id].getId()].itemSprite;
                    }
                }
            }
            if(inventoryType == InventoryType.block)
            {
               
            }
        }

    }
    private Sprite ConvertMaterialToSprite(Material sourceMaterial)
    {
        // Get the main texture from the material
        Texture2D texture = (Texture2D)sourceMaterial.mainTexture;

        if (texture != null)
        {
            // Create a new sprite using the texture
            Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.one * 0.5f);

            // Use the sprite as desired (e.g., assign it to a SpriteRenderer component)
            return sprite;
        }
        else
        {
            Debug.LogWarning("Texture not found in the material!");
            return null;
        }
    }
    private void initalizeInvBag()
    {
        for (int i = 0; i < inventory.slot.Length; i++)
        {
            setSlotObjUI(i, invBagContainer, interactableSlotObj);
        }
    }
    #region Text Display
    public void StartFadeInText(int itemId, Color color = default(Color))
    {
        int invId = inventory.slot[itemId].getId();
        if (color == default(Color))
        {
            color = Color.white;
        }
        // Stop any ongoing fade coroutine
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
        }

        // Get the item name from the item data using the provided item ID
        string itemName = "";
        if (invId != -1)
        {
            switch (inventoryType)
            {
                case InventoryType.item:
                    itemName = itemData.item[invId].itemName;
                break;

                case InventoryType.block:
                   
                break;
            }
        }

        // Start the fade coroutine
        fadeCoroutine = StartCoroutine(FadeText(itemName, color));
    }
    public void StartFadeInText(string text, Color color = default(Color), float duration = default)
    {
        if(color == default(Color))
        {
            color = Color.white;
        }
        // Stop any ongoing fade coroutine
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
        }

        // Get the item name from the item data using the provided item ID
        // Start the fade coroutine
        fadeCoroutine = StartCoroutine(FadeText(text, color, duration));
    }
    private IEnumerator FadeText(string text, Color color, float duration = default)
    {
        if(duration == default)
        {
            duration = displayDuration;
        }
        // Set the initial text and alpha value
        DisplayText.text = text;
        DisplayText.alpha = 0f;
        DisplayText.color = color;
        // Fade in animation
        float fadeInTimer = 0f;
        while (fadeInTimer < fadeInDuration)
        {
            fadeInTimer += Time.deltaTime;
            float alpha = Mathf.Lerp(0f, 1f, fadeInTimer / fadeInDuration);
            DisplayText.alpha = alpha;
            yield return null;
        }

        // Display the text for a duration
        yield return new WaitForSeconds(duration);

        // Fade out animation
        float fadeOutTimer = 0f;
        while (fadeOutTimer < fadeOutDuration)
        {
            fadeOutTimer += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, fadeOutTimer / fadeOutDuration);
            DisplayText.alpha = alpha;
            yield return null;
        }

        // Reset the text and alpha value
        DisplayText.text = "";
        DisplayText.alpha = 0f;

        // Reset the fade coroutine reference
        fadeCoroutine = null;
    }

    #endregion
    private void OpenBag()
    {
        invBagPanel.SetActive(!invBagPanel.activeInHierarchy);
        Cursor.visible = !Cursor.visible;
        if (Cursor.visible == true)
        {
            Cursor.lockState = CursorLockMode.None;
        } else
        {
            Cursor.lockState = CursorLockMode.Locked;
        }
    }
}
