using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DilmerGames.Core.Singletons;
using Unity.Netcode;  //to reference NetworkManager

public class PlayersManager : NetworkSingleton<PlayersManager>
{
    private NetworkVariable<int> playersInGame = new NetworkVariable<int>();

    public int PlayersInGame
    {
        get
        {
            return playersInGame.Value;
        }
    }

    void Start()
    {

        NetworkManager.Singleton.OnServerStarted += () =>
        {
            if (IsServer)
            {
                Logger.Instance.LogInfo($"Player 1 just connected...");
                playersInGame.Value++;
            }
        };
        NetworkManager.Singleton.OnClientConnectedCallback += (id) =>
        {
            if (IsServer)
            {
                Logger.Instance.LogInfo($"Player {id+1} just connected...");
                playersInGame.Value++;
            }
        };

        NetworkManager.Singleton.OnClientDisconnectCallback += (id) =>
        {
            if (IsServer)
            {
                Logger.Instance.LogInfo($"Player {id+1} just disconnected...");
                playersInGame.Value--;
            }
        };
    }
}


