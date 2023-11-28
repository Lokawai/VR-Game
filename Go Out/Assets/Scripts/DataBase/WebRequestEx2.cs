using UnityEngine;
using UnityEngine.Networking;
using System.Collections;

public class WebRequestEx2 : MonoBehaviour
{
    public string inputUsername;
    public string inputPassword;
    public string inputEmail;
    //public UnityEngine.UI.Text tinputUsername;
    //public UnityEngine.UI.Text tinputPassword;
    //public UnityEngine.UI.Text tinputEmail;

    string CreateUserURL = "http://localhost:80/dev/php/uInsertUser.php";

    // Update is called once per frame
    void Update()
    {
        //inputUsername = tinputUsername.text;
        //inputPassword = tinputPassword.text;
        //inputEmail = tinputEmail.text;
        if (Input.GetKeyDown(KeyCode.Space))
            StartCoroutine(CreateUser(inputUsername, inputPassword, inputEmail));
    }

    IEnumerator CreateUser(string username, string password, string email)
    {
        WWWForm form = new WWWForm();
        form.AddField("usernamePost", username);
        form.AddField("passwordPost", password);
        form.AddField("emailPost", email);

        using (UnityWebRequest www = UnityWebRequest.Post(CreateUserURL, form))
        {
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.Log(www.error);
            }
            else
            {
                Debug.Log("Form post complete!");
            }
        }
    }

}
