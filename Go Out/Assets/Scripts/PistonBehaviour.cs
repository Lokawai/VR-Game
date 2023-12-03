using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PistonBehaviour : MonoBehaviour
{
    [Header("Visual & Sound Settings")]
    [SerializeField] private GameObject visualFX;
    [SerializeField] private AudioClip activeSound;
    [SerializeField] private AudioClip hitSound;
    [Header("Piston Settings")]
    [SerializeField] private float hitForce = 10f;
    [SerializeField] private float maxExtendRange = 10f;
    [SerializeField] private float stayTime = 0f;
    [SerializeField] private float extendSpeed = 1f;
    [SerializeField] private bool ignoreCollision = false;
    [SerializeField] private LayerMask collisionLayer;
    [Header("Debug Purpose")]
    [SerializeField] private bool activePiston = false;
    public PistonState currentState { get; private set; }
    [SerializeField]
    private Transform extenderObject;
    [SerializeField]
    private Transform targetObject;

    private float initialScaleY;
    private float initialTargetY;
    private Vector3 initialExtenderPosition;
    private float currentExtension = 0f;
    AudioSource audioSource;
    private void Start()
    {
        initialScaleY = extenderObject.localScale.y;
        initialTargetY = targetObject.localPosition.y;
        initialExtenderPosition = extenderObject.localPosition;
        currentExtension = 0f;
        audioSource = GetComponent<AudioSource>();
    }
    private void Awake()
    {

    }
    private void Update()
    {
        if(activePiston)
        {
            currentState = PistonState.extend;
            activePiston = false;
        }
        float currentScaleY = extenderObject.localScale.y;
        float scaleDelta = currentScaleY - initialScaleY;

        Vector3 extenderPosition = initialExtenderPosition;
        extenderPosition.y += currentScaleY - initialExtenderPosition.y -1f ; // Move the extender halfway to maintain the pivot in the middle
        extenderObject.localPosition = extenderPosition;

        Vector3 targetPosition = targetObject.localPosition;
        targetPosition.y = initialTargetY + scaleDelta + currentScaleY - initialExtenderPosition.y - 1f;
        targetObject.localPosition = targetPosition;
        if(currentState == PistonState.extend)
        {
            Extend();
            if (!ignoreCollision)
            {
                RaycastDetection();
            }
        } else if(currentState == PistonState.shrunk)
        {
            Shrunk();
        }

    }
    private void RaycastDetection()
    {
        RaycastHit hit;
        if (Physics.Raycast(targetObject.position, targetObject.up, out hit, 0.1f))
        {
            if ((collisionLayer.value & (1 << hit.transform.gameObject.layer)) > 0)
            {
                if (hitSound != null && audioSource != null)
                {
                    audioSource.clip = hitSound;
                    audioSource.PlayOneShot(hitSound);
                }

                Destructable destructable = hit.transform.GetComponent<Destructable>();
                // A collision occurred within the raycast range
                if (destructable != null)
                {
                    destructable.DestroyObject(hit, hitForce, visualFX, true);
                }
                else
                {
                    if (visualFX)
                    {
                        GameObject fx = Instantiate(visualFX, hit.point, Quaternion.identity);
                        fx.transform.rotation = Quaternion.LookRotation(hit.normal);
                    }
                }
                if (currentState == PistonState.extend)
                    StartCoroutine(PistonStay());
            }
        }
    }
    public void SetPistonState(PistonState pistonState)
    {

        currentState = pistonState;   

    }
    public IEnumerator PistonStay()
    {
        currentState = PistonState.stay;
        yield return new WaitForSeconds(stayTime);
        currentState = PistonState.shrunk;
    }
    public void SetIntPistonState(int value)
    {
        switch(value)
        {
            case 0:
                currentState = PistonState.none;
                break;
            case 1:
                currentState = PistonState.extend;
                if (activeSound != null && audioSource != null)
                {
                    audioSource.clip = activeSound;
                    audioSource.PlayOneShot(activeSound);
                }
                break;
            case 2:
                currentState = PistonState.shrunk;
                break;
            case 3:
                currentState = PistonState.stay;
                break;
        }
    }
    private void Extend()
    {
        if (currentExtension < maxExtendRange)
        {
            currentExtension += extendSpeed * Time.deltaTime;
            currentExtension = Mathf.Min(currentExtension, maxExtendRange);

            float scaleDelta = currentExtension - initialScaleY;

            Vector3 extenderPosition = extenderObject.localPosition;

            Vector3 extenderScale = extenderObject.localScale;
            extenderScale.y = currentExtension;
            extenderObject.localScale = extenderScale;

          
        }
        else if(currentExtension >= maxExtendRange)
        {
            StartCoroutine(PistonStay());
            
        }
    }
    private void Shrunk()
    {
        if (currentExtension > initialScaleY)
        {
            currentExtension -= extendSpeed * Time.deltaTime;
            currentExtension = Mathf.Max(currentExtension, initialScaleY);

            float scaleDelta = currentExtension - extenderObject.localScale.y;

            Vector3 extenderScale = extenderObject.localScale;
            extenderScale.y = currentExtension;
            extenderObject.localScale = extenderScale;

           
        } else
        {
            if(currentState == PistonState.shrunk)
            {
                currentState = PistonState.none;
            }
        }
    }
}
public enum PistonState
{
    none,
    extend,
    shrunk,
    stay
}