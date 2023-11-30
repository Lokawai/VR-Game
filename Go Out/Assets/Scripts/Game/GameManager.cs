using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine.XR.Interaction.Toolkit.Samples.StarterAssets;
using TMPro;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;

public class GameManager : NetworkBehaviour
{
    public GManager gManager;
    public static GameManager Singleton;
    [SerializeField]
    private GameObject m_XROrigin = default;
    private float originMoveSpeed = default;
    [SerializeField]
    private NetworkManager m_networkManager = default;
    [SerializeField]
    private NetworkSO networkData = default;
    [SerializeField]
    private Vector3 startPosition = default;
    [SerializeField]
    private GameObject player = default;
    DynamicMoveProvider dynamicMove = default;
    public static NetworkObjectManager objectSpawner = default;
    public List<GameObject> currentPlayers = new List<GameObject>();
    [SerializeField]
    private TMP_Text ipAddress = default;
    NetworkVariable<bool> gameStarted = new NetworkVariable<bool>();
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
        gameStarted.Value = false;
        dynamicMove = m_XROrigin.GetComponent<DynamicMoveProvider>();
        originMoveSpeed = dynamicMove.moveSpeed;
        dynamicMove.moveSpeed = 0f;
        dynamicMove.leftControllerTransform.GetComponent<XRRayInteractor>().maxRaycastDistance = 30f;
        dynamicMove.rightControllerTransform.GetComponent<XRRayInteractor>().maxRaycastDistance = 30f;

    }
    private void Awake()
    {
        gManager.StageLevel.Value = 0;
        Singleton = this;
        objectSpawner = GetComponent<NetworkObjectManager>();
    }
    public void StartGame()
    {
        
        dynamicMove.moveSpeed = originMoveSpeed;
        player.transform.position = startPosition;
        m_XROrigin.GetComponent<InventoryBehaviour>().InventoryCanvas.SetActive(true);
        dynamicMove.leftControllerTransform.GetComponent<XRRayInteractor>().maxRaycastDistance = 2f;
        dynamicMove.rightControllerTransform.GetComponent<XRRayInteractor>().maxRaycastDistance = 2f;
        AddStageLevel();
        gameStarted.Value = true;
    }

    public void AddStageLevel()
    {
        gManager.StageLevel.Value++;
        if (gManager.StartStageEvent.Length != 0 && gManager.StartStageEvent.Length <= gManager.StageLevel.Value)
            InvokeUnityEvent(gManager.StartStageEvent[gManager.StageLevel.Value]);
    }
    public static void InvokeUnityEvent(UnityEvent unityEvent)
    {
        //Return if have no value inside UnityEvent
        if (unityEvent.GetPersistentEventCount() == 0) return;
        bool hasPersistentTarget = false;
        //Check if unityEvent have any function inside
        for (int i = 0; i < unityEvent.GetPersistentEventCount(); i++)
        {
            if (unityEvent.GetPersistentTarget(i) != null)
            {
                hasPersistentTarget = true;
            }
        }
        if(hasPersistentTarget)
        {
            unityEvent.Invoke();
        }
    }
    // Update is called once per frame
    void Update()
    {
        NetworkDetection();
        if (gameStarted.Value == false) return;
        if (gManager.LoopingStageEvent.Length != 0 && gManager.LoopingStageEvent.Length <= gManager.StageLevel.Value)
        {
            InvokeUnityEvent(gManager.LoopingStageEvent[gManager.StageLevel.Value]);
        }
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
[System.Serializable]
public struct GManager 
{
    public NetworkVariable<int> StageLevel;
    [Header("Unity Event Functions must be void\nNote: Each Element is a different stage")]
    [SerializeField]
    private UnityEvent[] m_StartStageEvent;
    public UnityEvent[] StartStageEvent { get {return m_StartStageEvent; } }
    [SerializeField]
    private UnityEvent[] m_LoopingStageEvent;
    public UnityEvent[] LoopingStageEvent { get {return m_LoopingStageEvent; } }
}

