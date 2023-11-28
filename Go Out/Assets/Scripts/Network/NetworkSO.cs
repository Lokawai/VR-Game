using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

[CreateAssetMenu(fileName ="Network/NetworkData")]
public class NetworkSO : ScriptableObject
{
    public bool startNetwork = false;
    public bool startHost = false;
    public bool startClient = false;
    public string ipAddress = "0.0.0.0";
    public void StartNetworkState()
    {
        startNetwork = true;
    }
    public void StartHost()
    {
        startHost = true;
        
        GameManager.Singleton.SetIpAddressText(ipAddress);
        StartNetworkState();
        
    }
    public void StartClient()
    {
        startClient = true;
        
        StartNetworkState();

    }
    public void SetAddress()
    {
        UnityTransport transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        transport.ConnectionData.Address = ipAddress;
    }
    public void JoinAddress(string address)
    {
        ipAddress = address;
        UnityTransport transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        transport.ConnectionData.Address = ipAddress;
    }
    public void GetLocalIPAddress()
    {
        var host = Dns.GetHostEntry(Dns.GetHostName());

        foreach (var ip in host.AddressList)
        {
            if (ip.AddressFamily == AddressFamily.InterNetwork)
            {
                //Logger.Instance.LogInfo(ip.ToString());
                ipAddress = ip.ToString();
                return;
            }
        }
        throw new System.Exception("No network adapters with an IPV4 address in the system");
    }
}
