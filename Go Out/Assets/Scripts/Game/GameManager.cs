using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine.XR.Interaction.Toolkit.Samples.StarterAssets;
using TMPro;

public class GameManager : MonoBehaviour
{
    public static GameManager Singleton;
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
    public static NetworkObjectManager objectSpawner;
    public List<GameObject> currentPlayers = new List<GameObject>();
    [SerializeField]
    private TMP_Text ipAddress;
    public void SetIpAddressText(string address)
    {
        ipAddress.text = "IP: " + address;
    }
    public void SetPlayers( )
    {
        currentPlayers.Clear();
        GameObject[] gameObjects = GameObject.FindGameObjectsWithTag("Player");
        foreach(GameObject targetGameObject in gameObjects)
        {
            if(targetGameObject.GetComponent<NetworkPlayer>() != null)
            {
                currentPlayers.Add(targetGameObject);
            }
        }
        NetworkObjectManager.Singleton.UpdateManager(this);
    }
    // Start is called before the first frame update
    public NetworkVariable<int> PlayerID { get {return playerId; }}
    public void AddPlayerId() 
    {
        playerId.Value++;
    }

    private NetworkVariable<int> playerId =  new NetworkVariable<int>();
    void Start()
    {
        dynamicMove = m_XROrigin.GetComponent<DynamicMoveProvider>();
        originMoveSpeed = dynamicMove.moveSpeed;
        dynamicMove.moveSpeed = 0f;
        
    }
    private void Awake()
    {
        Singleton = this;
        objectSpawner = GetComponent<NetworkObjectManager>();
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
