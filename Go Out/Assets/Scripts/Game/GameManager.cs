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
    [SerializeField] private NetworkVariable<GameState> gameState = new NetworkVariable<GameState>(); 
    [SerializeField]
    private GameObject m_XROrigin = default;
    private float originMoveSpeed = default;
    [SerializeField]
    private NetworkManager m_networkManager = default;
    [SerializeField]
    private GameObject m_UIObject;
    [SerializeField]
    private NetworkSO networkData = default;
    [SerializeField]
    private Vector3 startPosition = default;
    [SerializeField]
    private float delayTeleportTime = 1f;
    [SerializeField]
    private GameObject player = default;
    DynamicMoveProvider dynamicMove = default;
    public static NetworkObjectManager objectSpawner = default;
    public List<GameObject> currentPlayers = new List<GameObject>();
    [SerializeField]
    private TMP_Text ipAddress = default;
    [SerializeField] private TMP_Text actionBar;
    NetworkVariable<bool> gameStarted = new NetworkVariable<bool>();
    private NetworkVariable<bool> isEnded = new NetworkVariable<bool>();
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
    public void SetEnableTextFalse()
    {
        actionBar.enabled = false;
    }
    public void SetEnableTextTrue()
    {
        actionBar.enabled = true;
    }
    private NetworkVariable<int> playerId =  new NetworkVariable<int>();
    private float initialLeftRaycastDis = 0f;
    private float initialRightRaycastDis = 0f;
    void Start()
    {
        isEnded.Value = false;
        gameStarted.Value = false;
        dynamicMove = m_XROrigin.GetComponent<DynamicMoveProvider>();
        originMoveSpeed = dynamicMove.moveSpeed;
        dynamicMove.moveSpeed = 0f;
        dynamicMove.leftControllerTransform.GetComponent<XRRayInteractor>().maxRaycastDistance = 30f;
        dynamicMove.rightControllerTransform.GetComponent<XRRayInteractor>().maxRaycastDistance = 30f;
        initialLeftRaycastDis = dynamicMove.leftControllerTransform.GetComponent<XRRayInteractor>().maxRaycastDistance;
        initialRightRaycastDis = dynamicMove.rightControllerTransform.GetComponent<XRRayInteractor>().maxRaycastDistance;

        gameState.Value = GameState.Default;
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
        StartCoroutine(DelayTeleport(startPosition));
        //player.transform.position = startPosition;
        m_XROrigin.GetComponent<InventoryBehaviour>().InventoryCanvas.SetActive(true);
        dynamicMove.leftControllerTransform.GetComponent<XRRayInteractor>().maxRaycastDistance = 2f;
        dynamicMove.rightControllerTransform.GetComponent<XRRayInteractor>().maxRaycastDistance = 2f;
        AddStageLevel();
        gameStarted.Value = true;
        Animator animator = m_UIObject.GetComponent<Animator>();
        animator.Play("FadeIn");

    }
    public IEnumerator DelayTeleport(Vector3 pos)
    {

        yield return new WaitForSeconds(delayTeleportTime);
        player.transform.position = pos;

    }

    public void AddStageLevel()
    {
        gManager.StageLevel.Value++;
        if (gManager.StartStageEvent.Length > -1)
            InvokeUnityEvent(gManager.StartStageEvent[gManager.StageLevel.Value]);

    }
    public IEnumerator EndGame()
    {
        Animator animator = m_UIObject.GetComponent<Animator>();
        animator.Play("GameEnd");
        TimeManager timeUnit = GetComponent<TimeManager>();
        timeUnit.Reset();
        yield return new WaitForSeconds(0.5f);

        timeUnit.ActiveReturnTimer();
        gameStarted.Value = false;
        yield return new WaitForSeconds(timeUnit.ReturnTime);
        dynamicMove.moveSpeed = 0f;
        dynamicMove.leftControllerTransform.GetComponent<XRRayInteractor>().maxRaycastDistance = initialLeftRaycastDis;
        dynamicMove.rightControllerTransform.GetComponent<XRRayInteractor>().maxRaycastDistance = initialRightRaycastDis;

        animator.Play("FadeIn");
        StartCoroutine(DelayTeleport(player.GetComponent<PositionManager>().GetInitialPosition()));
        gManager.StageLevel.Value = 0;
        PositionManager[] targetGameObjects = GameObject.FindObjectsOfType<PositionManager>();
        foreach(PositionManager positionManager in targetGameObjects)
        {
            DoorOpen doorOpen = positionManager.GetComponent<DoorOpen>();
            if (doorOpen != null)
            {
                doorOpen.Reset();
            }
            if(!positionManager.CompareTag("Player"))
            positionManager.ResetTransform();
        }
        PressurePad[] targetPGameObjects = GameObject.FindObjectsOfType<PressurePad>();
        foreach (PressurePad pressure in targetPGameObjects)
        {
            pressure.SetPressedState(false);
        }
        gameState.Value = GameState.Default;

        NetworkManager.Singleton.Shutdown();
    }
    public void LoseGame()
    {
        TimeManager timeManager = GetComponent<TimeManager>();
        timeManager.m_ReturnDisplayHeader = "You ran out of time!";
        if (gameState.Value == GameState.Default)
        {
            gameState.Value = GameState.Lose;
            isEnded.Value = true;
        }

    }
    public void WinGame()
    {
        TimeManager timeManager = GetComponent<TimeManager>();
        timeManager.m_ReturnDisplayHeader = "You escaped and won the game!";
        if (gameState.Value == GameState.Default)
        {
            gameState.Value = GameState.Win;
            isEnded.Value = true;
        }
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
        if(isEnded.Value)
        {
            switch(gameState.Value)
            {
                case GameState.Lose:
                    StartCoroutine(EndGame());
                    break;
                case GameState.Win:
                    StartCoroutine(EndGame());
                    break;

            }
            isEnded.Value = false;
        }
    }

    public void NetworkDetection()
    {
        if(networkData != null && networkData.startNetwork == true)
        {
            ConnectServer();
            StartGame();
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

public enum GameState
{
    Default,
    Win,
    Lose
}
