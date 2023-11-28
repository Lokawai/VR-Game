using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using UnityEngine.UI;

public class WebRequestEx3 : MonoBehaviour
{
    public string inputUsername;
    public string inputPassword;
    public UnityEngine.UI.Text messageText;

    string loginURL = "http://localhost:80/dev/php/uLogin.php";

    void Start()
    {
        messageText.text = "Press L to login...";
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.L))
        {
            Debug.Log("CALL login");
            StartCoroutine(LoginToDB(inputUsername, inputPassword));
            Debug.Log("after CALL login");
        }
    }

    IEnumerator LoginToDB(string username, string password)
    {
        Debug.Log("IEnumerator LoginToDB=> login:" + username + " pwd:" + password);
        WWWForm form = new WWWForm();
        form.AddField("usernamePost", username);
        form.AddField("passwordPost", password);

        using (UnityWebRequest www = UnityWebRequest.Post(loginURL, form))
        {
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.Log(www.error);
            }
            else
            {
                Debug.Log("Form post complete!" + www.result);
                messageText.text = www.downloadHandler.text;
                Debug.Log("\n messageText!" + messageText.text);
            }
        }
    }
}
