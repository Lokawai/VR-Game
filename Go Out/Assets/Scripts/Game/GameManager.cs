using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine.XR.Interaction.Toolkit.Samples.StarterAssets;

public class GameManager : MonoBehaviour
{
    [SerializeField]
    private GameObject m_XROrigin;
    private float originMoveSpeed;
    [SerializeField]
    private NetworkManager m_networkManager;
    [SerializeField]
    private NetworkSO networkData;
    [SerializeField]
    private Vector3 startPosition;
    [SerializeField]
    private GameObject player;
    DynamicMoveProvider dynamicMove;
    // Start is called before the first frame update
    void Start()
    {
        dynamicMove = m_XROrigin.GetComponent<DynamicMoveProvider>();
        originMoveSpeed = dynamicMove.moveSpeed;
        dynamicMove.moveSpeed = 0f;
    }
    public void StartGame()
    {
        
        dynamicMove.moveSpeed = originMoveSpeed;
        player.transform.position = startPosition;
        m_XROrigin.GetComponent<InventoryBehaviour>().InventoryCanvas.SetActive(true);
    }
    // Update is called once per frame
    void Update()
    {
        NetworkDetection();
    }

    public void NetworkDetection()
    {
        if(networkData != null && networkData.startNetwork == true)
        {
            StartGame();
            ConnectServer();
            networkData.startNetwork = false;
        }
    }
    private void ConnectServer()
    {
        if(networkData.startClient)
        {
            NetworkManager.Singleton.StartClient();
            networkData.startClient = false;
        }
        if(networkData.startHost)
        {
            NetworkManager.Singleton.StartHost();
            networkData.startHost = false;
        }
    }
}
