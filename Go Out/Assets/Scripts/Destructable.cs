using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class Destructable : MonoBehaviour
{
    [SerializeField] private GameObject destroyedVersion;
    [SerializeField] private float MaxDurability = 100f;

    [SerializeField] private float Durability;
    [SerializeField] private AudioClip DestroyedSound;
    private bool isDestroyed = false;
    private void Start()
    {
        Durability = MaxDurability;
    }
    public void DestroyObject(RaycastHit hit, float force, GameObject hitfx, bool isGun)
    {
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
                    Rigidbody rb = child.GetComponent<Rigidbody>();
                    if (rb)
                    {
                        rb.AddForce(child.forward * force * 10, ForceMode.Impulse);
                    }
                }
            }

            isDestroyed = true;
            PlaySoundAndDestroy();
            Destroy(gameObject);
        }
        else
        {
            if (isGun == true)
            {
                if (hitfx)
                {
                    GameObject fx = Instantiate(hitfx, hit.point, Quaternion.identity);
                    fx.transform.rotation = Quaternion.LookRotation(hit.normal);
                }
            }
        }
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