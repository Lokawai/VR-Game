using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

public class Destructable : NetworkBehaviour
{
    private NetworkObjectManager networkObjectManager;
    [SerializeField] private GameObject destroyedVersion;
    [SerializeField] private float MaxDurability = 100f;
    [SerializeField] private bool isInvincible = false;
    [SerializeField] private bool isPushable = false;
    [SerializeField] private float Durability;
    [SerializeField] private AudioClip DestroyedSound;
    [SerializeField] private DestroyState m_DestroyState;
    [Header("DestroyState = CustomAction Only")]
    [SerializeField]
    private UnityEvent customAction;
    private bool isDestroyed = false;
    private void Start()
    {
        Durability = MaxDurability;
        networkObjectManager = NetworkObjectManager.Singleton;
    }
    public void DestroyObject(RaycastHit hit, float force, GameObject hitfx, bool isGun)
    {
        Rigidbody rb = gameObject.GetComponent<Rigidbody>();
        if (rb && isPushable)
        {

            rb.AddForceAtPosition(hit.transform.forward * force * 10f, hit.point);
        }
        if (isGun == true)
        {
            if (hitfx)
            {
                GameObject fx = Instantiate(hitfx, hit.point, Quaternion.identity);
                fx.transform.rotation = Quaternion.LookRotation(hit.normal);
            }
        }
        if (isInvincible) return;
        Durability -= force;

        if (Durability <= 0 && isDestroyed == false)
        {
            if (destroyedVersion != null)
            {
                GameObject shattered = Instantiate(destroyedVersion, transform.position, transform.rotation) as GameObject;

                foreach (Transform child in shattered.transform)
                {
                    MeshCollider meshCollider = child.GetComponent<MeshCollider>();
                    if (meshCollider)
                    {
                        Quaternion rotation = Quaternion.Euler(child.rotation.eulerAngles);
                        meshCollider.sharedMesh = null;
                        meshCollider.sharedMesh = child.GetComponent<MeshFilter>().mesh;
                        meshCollider.transform.rotation = rotation;
                        child.GetComponent<MeshFilter>().mesh.RecalculateNormals();
                    }

                }
            }

            isDestroyed = true;
            PlaySoundAndDestroy();
            if (m_DestroyState == DestroyState.DestroyObject)
            {
                if (IsServer)
                {
                    networkObjectManager.DestroyObject(gameObject.GetComponent<NetworkObject>());
                }
                else
                    Destroy(gameObject);
            } else if(m_DestroyState == DestroyState.DisableObject)
            {
                isDestroyed = false;
                Durability = MaxDurability;
                gameObject.SetActive(false);
            }
            if(m_DestroyState == DestroyState.CustomAction)
            {
                customAction.Invoke();
                isDestroyed = false;
                Durability = MaxDurability;
                
            }
        }

    }
    public void InvokeAction()
    {
        customAction.Invoke();
    }
    public void takeDamage(float damage)
    {
        Durability -= damage;
        if (Durability <= 0 && isDestroyed == false)
        {
            if (destroyedVersion != null)
            {
                GameObject shattered = Instantiate(destroyedVersion, transform.position, transform.rotation) as GameObject;

                foreach (Transform child in shattered.transform)
                {
                    MeshCollider meshCollider = child.GetComponent<MeshCollider>();
                    if (meshCollider)
                    {
                        Quaternion rotation = Quaternion.Euler(child.rotation.eulerAngles);
                        meshCollider.sharedMesh = null;
                        meshCollider.sharedMesh = child.GetComponent<MeshFilter>().mesh;
                        meshCollider.transform.rotation = rotation;
                        child.GetComponent<MeshFilter>().mesh.RecalculateNormals();
                    }
                    Rigidbody rb = child.GetComponent<Rigidbody>();
                    if (rb)
                    {
                        rb.AddForce(child.forward * damage * 10, ForceMode.Impulse);
                    }
                }
            }
            isDestroyed = true;
            PlaySoundAndDestroy();
            Destroy(gameObject);
        }
    }
    public void PlaySoundAndDestroy()
    {
        if (DestroyedSound != null)
        {
            // Create a new empty game object at the position of the original object
            GameObject soundObject = new GameObject("SoundObject");
            soundObject.transform.position = transform.position;

            // Add an AudioSource component to the new game object
            AudioSource audioSource = soundObject.AddComponent<AudioSource>();
            if (audioSource)
            {
                // Set the audio clip and play it
                audioSource.clip = DestroyedSound;
                audioSource.Play();
                Destroy(soundObject, DestroyedSound.length);
            }
            else
            {
                Destroy(soundObject);
            }
        }
      
    }
}
public enum DestroyState 
{
    DestroyObject,
    DisableObject,
    CustomAction
}
