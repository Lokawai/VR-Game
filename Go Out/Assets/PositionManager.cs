using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class PositionManager : MonoBehaviour
{
    Vector3 initialPos;
    Quaternion initialRotation;
    Vector3 initialScale;
    public NetworkVariable<bool> isCloned = new NetworkVariable<bool>();
    public void Awake()
    {
        isCloned.Value = false;
    }
    // Start is called before the first frame update
    void Start()
    {
        initialPos = gameObject.transform.position;
        initialRotation = gameObject.transform.rotation;
        initialScale = gameObject.transform.localScale;
    }

    public void ResetTransform()
    {
        transform.position = initialPos;
        transform.rotation = initialRotation;
        transform.localScale = initialScale;
        if (!isCloned.Value)
            gameObject.SetActive(true);
        else
            NetworkObjectManager.DestroyObject(gameObject);
    }
    public void SetScale(float Value)
    {
        gameObject.transform.localScale = new Vector3(Value, Value, Value);

    }
    public Vector3 GetInitialPosition()
    {
        return initialPos;
    }
}
