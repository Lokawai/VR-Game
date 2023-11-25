using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

namespace TestProject.RuntimeTests
{
    public class CheckStateEnterCount : StateMachineBehaviour
    {
        public static Dictionary<ulong, Dictionary<int, List<AnimatorStateInfo>>> OnStateEnterCounter = new Dictionary<ulong, Dictionary<int, List<AnimatorStateInfo>>>();
        public static bool IsIntegrationTest;
        public static bool IsManualTestEnabled = true;

        public static void ResetTest(bool isIntegrationTest = true)
        {
            IsIntegrationTest = isIntegrationTest;
            IsManualTestEnabled = !isIntegrationTest;
            OnStateEnterCounter.Clear();
        }

        public static bool AllStatesEnteredMatch(List<ulong> clientIdsToCheck)
        {
            if (clientIdsToCheck.Contains(NetworkManager.ServerClientId))
            {
                clientIdsToCheck.Remove(NetworkManager.ServerClientId);
            }

            if (!OnStateEnterCounter.ContainsKey(NetworkManager.ServerClientId))
            {
                Debug.Log($"Server has not entered into any states! OnStateEntered Entry Count ({OnStateEnterCounter.Count})");
                return false;
            }

            var serverStates = OnStateEnterCounter[NetworkManager.ServerClientId];

            foreach (var layerEntries in serverStates)
            {
                var layerIndex = layerEntries.Key;
                var layerStates = layerEntries.Value;
                if (layerStates.Count > 1)
                {
                    Debug.Log($"Server layer ({layerIndex}) state was entered ({layerStates.Count}) times!");
                    return false;
                }

                foreach (var clientId in clientIdsToCheck)
                {
                    if (!OnStateEnterCounter.ContainsKey(clientId))
                    {
                        Debug.Log($"Client-{clientId} never entered into any state for layer index ({layerIndex})!");
                        return false;
                    }
                    var clientStates = OnStateEnterCounter[clientId];
                    if (!clientStates.ContainsKey(layerIndex))
                    {
                        Debug.Log($"Client-{clientId} never layer ({layerIndex}) state!");
                        return false;
                    }
                    var clientLayerStateEntries = clientStates[layerIndex];
                    if (clientLayerStateEntries.Count > 1)
                    {
                        Debug.Log($"Client-{clientId} layer ({layerIndex}) state was entered ({layerStates.Count}) times!");
                        return false;
                    }
                    // We should have only entered into the state once on the server
                    // and all connected clients
                    var serverAnimStateInfo = layerStates[0];
                    var clientAnimStateInfo = clientLayerStateEntries[0];
                    // We just need to make sure we are looking at the same state
                    if (clientAnimStateInfo.fullPathHash != serverAnimStateInfo.fullPathHash)
                    {
                        Debug.Log($"Client-{clientId} full path hash ({clientAnimStateInfo.fullPathHash}) for layer ({layerIndex}) was not the same as the Server full path hash ({serverAnimStateInfo.fullPathHash})!");
                        return false;
                    }
                }
            }
            return true;
        }

        public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            if (IsIntegrationTest)
            {
                var networkObject = animator.GetComponent<NetworkObject>();
                var localClientId = networkObject.NetworkManager.IsServer ? NetworkManager.ServerClientId : networkObject.NetworkManager.LocalClientId;
                if (!OnStateEnterCounter.ContainsKey(localClientId))
                {
                    OnStateEnterCounter.Add(localClientId, new Dictionary<int, List<AnimatorStateInfo>>());
                }
                if (!OnStateEnterCounter[localClientId].ContainsKey(layerIndex))
                {
                    OnStateEnterCounter[localClientId].Add(layerIndex, new List<AnimatorStateInfo>());
                }
                OnStateEnterCounter[localClientId][layerIndex].Add(stateInfo);
            }
            else if (IsManualTestEnabled)
            {
                Debug.Log($"[{layerIndex}][{stateInfo.shortNameHash}][{stateInfo.normalizedTime}][{animator.IsInTransition(layerIndex)}]");
            }
        }
    }
}
