using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
public class FireBullet : MonoBehaviour
{
    [SerializeField]
    private GameObject Bullet;

    [SerializeField]
    private Transform spawnPoint;

    [SerializeField]
    private float fireSpeed = 10;



    // Start is called before the first frame update
    void Start()
    {
        XRGrabInteractable grabbable =GetComponent<XRGrabInteractable>();
        grabbable.activated.AddListener(Firebullet);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void Firebullet(ActivateEventArgs arg)
    {
        GameObject spwanedBullet = Instantiate(Bullet);
        spwanedBullet.transform.position= spawnPoint.position;
        spwanedBullet.GetComponent<Rigidbody>().AddForce(spawnPoint.forward * fireSpeed, ForceMode.Impulse);
        Destroy(spwanedBullet, 5f);
    }
}
