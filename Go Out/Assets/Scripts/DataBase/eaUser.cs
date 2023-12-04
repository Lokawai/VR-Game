using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.InputSystem;
using TMPro;
public class eaUser : MonoBehaviour
{

    public string inputUsername;
    public TextMeshProUGUI Te;
    public float inputTime;
    public InputActionAsset TestAsset;
    public InputAction testAction;
    InputActionMap gameplayActionMap;
    string CreateUserURL = "http://localhost:80/dev/php/eaUser.php";
    TimeManager timeManager;
    private void Start()
    {
        timeManager = GameManager.Singleton.GetComponent<TimeManager>();
        var gameplayActionMap = TestAsset.FindActionMap("SpaceAction");
        testAction = gameplayActionMap.FindAction("Space");
        testAction.performed += callCreateUser;
        testAction.Enable();

    }
    // Update is called once per frame
    private void Update()
    {
        inputTime += Time.deltaTime;
        inputUsername = Te.text;
    }
    public void callCreateUser(InputAction.CallbackContext context)
    {
        //StartCoroutine(CreateUser(inputUsername, inputTime));
    }    
    public void ButtonCall()
    {
        StartCoroutine(CreateUser(inputUsername, timeManager.TimeRemain));
    }

    IEnumerator CreateUser(string username, float time)
    {
        WWWForm form = new WWWForm();
        form.AddField("usernamePost", username);
        form.AddField("timePost", time.ToString(".00"));

        using (UnityWebRequest www = UnityWebRequest.Post(CreateUserURL, form))
        {
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.Log(www.error);
            }
            else
            {
                Debug.Log("user post complete!");
            }
        }
    }

}


