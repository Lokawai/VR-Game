using UnityEngine;
using UnityEngine.Networking;
using System.Collections;

// UnityWebRequest.Get example

// Access a website and use UnityWebRequest.Get to download a page.
// Also try to download a non-existing page. Display the error.

public class WebRequestEx1 : MonoBehaviour
{
    public string[] items;

    void Start()
    {
        // A correct website page.
        StartCoroutine(GetRequest("http://localhost:80/dev/php/itemdata.php"));

    }

    IEnumerator GetRequest(string uri)
    {
        using (UnityWebRequest webRequest = UnityWebRequest.Get(uri))
        {
            // Request and wait for the desired page.
            yield return webRequest.SendWebRequest();

            string[] pages = uri.Split('/');
            int page = pages.Length - 1;

            switch (webRequest.result)
            {
                case UnityWebRequest.Result.ConnectionError:
                case UnityWebRequest.Result.DataProcessingError:
                    Debug.LogError(pages[page] + ": Error: " + webRequest.error);
                    break;
                case UnityWebRequest.Result.ProtocolError:
                    Debug.LogError(pages[page] + ": HTTP Error: " + webRequest.error);
                    break;
                case UnityWebRequest.Result.Success:
                    //Debug.Log(webRequest.downloadHandler.text);
                    string itemsDataString = webRequest.downloadHandler.text;
                    //Debug.Log(pages[page] + ":\n Lab9:Received: (All items) " + itemsDataString);

                    items = itemsDataString.Split(';');
                    print(GetDataValue(items[0], "Name:"));

                    //expected data return
                    //ID:0|Name:Health Potion|Type:consumables|Cost:50;
                    //ID:1|Name:Health Potion|Type:consumables|Cost:50;ID:2|Name:Health pill|Type:consumables|Cost:30;ID:3|Name:Poison Pill|Type:consumables|Cost:100;
                    break;
            }
        }
    }//GetRequest

    string GetDataValue(string data, string index)
    {
        string value = data.Substring(data.IndexOf(index) + index.Length);
        if (value.Contains("|"))
            value = value.Remove(value.IndexOf("|"));
        return value;
    }
}
