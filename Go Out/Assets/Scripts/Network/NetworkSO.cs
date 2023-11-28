using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName ="Network/NetworkData")]
public class NetworkSO : ScriptableObject
{
    public bool startNetwork = false;
    public bool startHost = false;
    public bool startClient = false;
    public void StartNetworkState()
    {
        startNetwork = true;
    }
    public void StartHost()
    {
        startHost = true;
        StartNetworkState();
    }
    public void StartClient()
    {
        startClient = true;
        StartNetworkState();
    }
}
