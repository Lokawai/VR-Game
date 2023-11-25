using UnityEngine;
using Unity.Netcode;

/// <summary>
/// This can be added to the same GameObject the NetworkManager component is assigned to in order to prevent
/// multiple NetworkManager instances from being instantiated if the same scene is loaded.
/// </summary>
public class NetworkManagerMonitor : MonoBehaviour
{
    // Start is called before the first frame update
    private void Start()
    {
        var networkManagerInstances = FindObjectsOfType<NetworkManager>();
        foreach (var instance in networkManagerInstances)
        {
            if (instance.IsListening)
            {
                if (gameObject != instance.gameObject)
                {
                    var networkManager = GetComponent<NetworkManager>();
                    Destroy(gameObject);
                }
            }
        }
    }
}
