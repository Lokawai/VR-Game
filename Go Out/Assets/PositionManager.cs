using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PositionManager : MonoBehaviour
{
    Vector3 initialPos;
    Quaternion initialRotation;
    Vector3 initialScale;
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
        gameObject.SetActive(true);
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
