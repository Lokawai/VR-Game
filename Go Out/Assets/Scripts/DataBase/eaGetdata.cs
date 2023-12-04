using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class eaGetdata : MonoBehaviour
{

    public string[] items;
    public GameObject textPrefab; // Prefab of the text object for displaying player data
    public Transform textContainer; // Parent transform for the spawned text objects

    private List<PlayerData> rankingData; // List to store ranking data
    void Start()
    {
        // A correct website page.
        StartCoroutine(GetRequest("http://localhost:80/dev/php/eadata.php"));
        // Initialize and populate the rankingData list with sample data
        rankingData = new List<PlayerData>() { };

        // Display the ranking list

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
                    Debug.Log(webRequest.downloadHandler.text);
                    string itemsDataString = webRequest.downloadHandler.text;
                    Debug.Log(pages[page] + ":\n Lab9:Received: (All items) " +
                                      itemsDataString);

                    items = itemsDataString.Split(';');
                    print(GetDataValue(items[0], "Name:"));
                    print(GetDataValue(items[0], "time:") + "s");

                    //expected data return
                    //ID:0|Name:Health Potion|Type:consumables|Cost:50;
                    //ID:1|Name:Health Potion|Type:consumables|Cost:50;ID:2|Name:Health pill|Type:consumables|Cost:30;ID:3|Name:Poison Pill|Type:consumables|Cost:100;
                    break;
            }
        }
        DisplayRankingList();
    }//GetRequest

    private void AddPlayer()
    {
        foreach(string i in items)
        {
            AddPlayerData(GetDataValue(i, "Name:"), GetDataValue(i, "time:"));
        }
        DisplayRankingList();
    }
    private void DisplayRankingList()
    {

        // Clear any existing text objects in the textContainer
        foreach (Transform child in textContainer)
        {
            Destroy(child.gameObject);
        }

        // Iterate through the rankingData list and spawn a text object for each player's information
        for (int i = 0; i < rankingData.Count; i++)
        {
            // Instantiate a new text object from the prefab
            GameObject newText = Instantiate(textPrefab, textContainer);
            Text text = newText.GetComponent<Text>();
            // Set the text value of the spawned object to display the player's information
            newText.GetComponent<Text>().text = $"{i + 1}. {rankingData[i].name}: {rankingData[i].time} s";
            newText.GetComponent<Text>().enabled = true;
            newText.GetComponent<VerticalLayoutGroup>().enabled = true;
        }
    }

    public void AddPlayerData(string name, string time)
    {
        // Create a new PlayerData object with the provided name and time
        PlayerData newPlayer = new PlayerData(name, time);

        // Add the new player to the rankingData list
        rankingData.Add(newPlayer);

        // Sort the rankingData based on times (in descending order)
        rankingData.Sort((a, b) => b.time.CompareTo(a.time));

        // Display the updated ranking list
        DisplayRankingList();
    }

    string GetDataValue(string data, string index)
    {
        string value = data.Substring(data.IndexOf(index) + index.Length);
        if (value.Contains("|"))
            value = value.Remove(value.IndexOf("|"));
        return value;
    }
}

public class PlayerData
{
    public string name;
    public string time;

    public PlayerData(string name, string time)
    {
        this.name = name;
        this.time = time;
    }
}

