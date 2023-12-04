using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using UnityEngine.UI;

public class RankingList : MonoBehaviour
{
    //public GameObject textPrefab; // Prefab of the text object for displaying player data
    //public Transform textContainer; // Parent transform for the spawned text objects

    //private List<PlayerData> rankingData; // List to store ranking data
    //public Text textComponent;
    //private void Start()
    //{
    //    // Initialize and populate the rankingData list with sample data
    //    rankingData = new List<PlayerData>()
    //    {
    //        new PlayerData("Player A", 100),
    //        new PlayerData("Player B", 80),
    //        new PlayerData("Player C", 120),
    //        // Add more players and their scores here...
    //    };

    //    // Sort the rankingData based on scores (in descending order)
    //    rankingData.Sort((a, b) => b.score.CompareTo(a.score));

    //    // Display the ranking list
    //    DisplayRankingList();
    //}

    //private void DisplayRankingList()
    //{
    //    // Clear any existing text objects in the textContainer
    //    foreach (Transform child in textContainer)
    //    {
    //        Destroy(child.gameObject);
    //    }

    //    // Iterate through the rankingData list and spawn a text object for each player's information
    //    for (int i = 0; i < rankingData.Count; i++)
    //    {
    //        // Instantiate a new text object from the prefab
    //        GameObject newText = Instantiate(textPrefab, textContainer);
    //        Text text = newText.GetComponent<Text>();
    //        // Set the text value of the spawned object to display the player's information
    //        newText.GetComponent<Text>().text = $"{i + 1}. {rankingData[i].name}: {rankingData[i].score}";
    //        newText.GetComponent<Text>().enabled = true;
    //        newText.GetComponent<VerticalLayoutGroup>().enabled = true;
    //    }
    //}



    //public void AddPlayerData(string name, int score)
    //{
    //    // Create a new PlayerData object with the provided name and score
    //    PlayerData newPlayer = new PlayerData(name, score);

    //    // Add the new player to the rankingData list
    //    rankingData.Add(newPlayer);

    //    // Sort the rankingData based on scores (in descending order)
    //    rankingData.Sort((a, b) => b.score.CompareTo(a.score));

    //    // Display the updated ranking list
    //    DisplayRankingList();
    //}
}

// Custom data structure to store player data
//public class PlayerData
//{
//    public string name;
//    public int score;

//    public PlayerData(string name, int score)
//    {
//        this.name = name;
//        this.score = score;
//    }
//}
