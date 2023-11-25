using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Unity.Netcode;
using Unity.Netcode.TestHelpers.Runtime;


namespace TestProject.RuntimeTests
{
    /// <summary>
    /// Tests Various Features of The NetworkAnimator
    /// !! NOTE !!
    /// This test depends upon the following assets:
    /// Assets\Tests\Animation\Resources\AnimatorObject.prefab
    /// Assets\Tests\Manual\NetworkAnimatorTests\CubeAnimatorController.controller (referenced in AnimatorObject)
    /// Possibly we could build this at runtime, but for now it uses the same animator controller as the manual
    /// test does.
    /// </summary>
    public class NetworkAnimatorTests : NetcodeIntegrationTest
    {
        private const string k_AnimatorObjectName = "AnimatorObject";
        private const string k_OwnerAnimatorObjectName = "OwnerAnimatorObject";

        protected override int NumberOfClients => 1;
        private GameObject m_AnimationTestPrefab => m_AnimatorObjectPrefab ? m_AnimatorObjectPrefab as GameObject : null;
        private GameObject m_AnimationOwnerTestPrefab => m_OwnerAnimatorObjectPrefab ? m_OwnerAnimatorObjectPrefab as GameObject : null;

        private AnimatorTestHelper.ParameterValues m_ParameterValues;
        private Object m_AnimatorObjectPrefab;
        private Object m_OwnerAnimatorObjectPrefab;

        protected override void OnOneTimeSetup()
        {
            m_AnimatorObjectPrefab = Resources.Load(k_AnimatorObjectName);
            Assert.NotNull(m_AnimatorObjectPrefab, $"Failed to load resource {k_AnimatorObjectName}");
            m_OwnerAnimatorObjectPrefab = Resources.Load(k_OwnerAnimatorObjectName);
            Assert.NotNull(m_OwnerAnimatorObjectPrefab, $"Failed to load resource {k_OwnerAnimatorObjectName}");
            base.OnOneTimeSetup();
        }

        protected override IEnumerator OnSetup()
        {
            AnimatorTestHelper.Initialize();
            CheckStateEnterCount.ResetTest();
            TriggerTest.ResetTest();
            StateSyncTest.ResetTest();
            yield return base.OnSetup();
        }

        protected override IEnumerator OnTearDown()
        {
            m_EnableVerboseDebug = false;
            yield return base.OnTearDown();
        }

        protected override void OnServerAndClientsCreated()
        {
            // Server authority prefab
            var networkObjectServer = (m_AnimatorObjectPrefab as GameObject).GetComponent<NetworkObject>();
            networkObjectServer.NetworkManagerOwner = m_ServerNetworkManager;
            networkObjectServer.name = "ServerAuthority";
            NetcodeIntegrationTestHelpers.MakeNetworkObjectTestPrefab(networkObjectServer);
            var networkAnimatorServerAuthPrefab = new NetworkPrefab() { Prefab = networkObjectServer.gameObject };
            m_ServerNetworkManager.NetworkConfig.NetworkPrefabs.Add(networkAnimatorServerAuthPrefab);

            // Owner authority prefab
            var networkObjectOwner = (m_OwnerAnimatorObjectPrefab as GameObject).GetComponent<NetworkObject>();
            networkObjectOwner.NetworkManagerOwner = m_ServerNetworkManager;
            networkObjectOwner.name = "OwnerAuthority";
            NetcodeIntegrationTestHelpers.MakeNetworkObjectTestPrefab(networkObjectOwner);
            var networkAnimatorOwnerAuthPrefab = new NetworkPrefab() { Prefab = networkObjectOwner.gameObject };
            m_ServerNetworkManager.NetworkConfig.NetworkPrefabs.Add(networkAnimatorOwnerAuthPrefab);

            foreach (var clientNetworkManager in m_ClientNetworkManagers)
            {
                clientNetworkManager.NetworkConfig.NetworkPrefabs.Add(networkAnimatorServerAuthPrefab);
                clientNetworkManager.NetworkConfig.NetworkPrefabs.Add(networkAnimatorOwnerAuthPrefab);
            }

            base.OnServerAndClientsCreated();
        }

        private bool ParameterValuesMatch(OwnerShipMode ownerShipMode, AuthoritativeMode authoritativeMode, bool debugInfo = false)
        {
            var serverParameters = AnimatorTestHelper.ServerSideInstance.GetParameterValues();
            if (!serverParameters.ValuesMatch(m_ParameterValues, debugInfo))
            {
                return false;
            }
            foreach (var animatorTestHelper in AnimatorTestHelper.ClientSideInstances)
            {
                var clientParameters = animatorTestHelper.Value.GetParameterValues();
                if (!clientParameters.ValuesMatch(m_ParameterValues, debugInfo))
                {
                    return false;
                }
            }

            return true;
        }

        public enum OwnerShipMode
        {
            ServerOwner,
            ClientOwner
        }

        public enum AuthoritativeMode
        {
            ServerAuth,
            OwnerAuth
        }

        private GameObject SpawnPrefab(bool isClientOwner, AuthoritativeMode authoritativeMode)
        {
            var networkManager = isClientOwner ? m_ClientNetworkManagers[0] : m_ServerNetworkManager;
            if (authoritativeMode == AuthoritativeMode.ServerAuth)
            {
                Assert.NotNull(m_AnimatorObjectPrefab);
                return SpawnObject(m_AnimatorObjectPrefab as GameObject, networkManager);
            }
            else
            {
                Assert.NotNull(m_OwnerAnimatorObjectPrefab);
                return SpawnObject(m_OwnerAnimatorObjectPrefab as GameObject, networkManager);
            }
        }

        private string GetNetworkAnimatorName(AuthoritativeMode authoritativeMode)
        {
            if (authoritativeMode == AuthoritativeMode.ServerAuth)
            {
                return m_AnimationTestPrefab.name;
            }
            return m_AnimationOwnerTestPrefab.name;
        }

        /// <summary>
        /// Verifies that parameters are synchronized with currently connected clients
        /// when no transition or layer change has occurred.
        /// </summary>
        /// <param name="authoritativeMode">Server or Owner authoritative</param>
        [UnityTest]
        public IEnumerator ParameterUpdateTests([Values] OwnerShipMode ownerShipMode, [Values] AuthoritativeMode authoritativeMode)
        {
            VerboseDebug($" ++++++++++++++++++ Parameter Test [{ownerShipMode}] Starting ++++++++++++++++++ ");

            // Spawn our test animator object
            var objectInstance = SpawnPrefab(ownerShipMode == OwnerShipMode.ClientOwner, authoritativeMode);

            // Wait for it to spawn server-side
            yield return WaitForConditionOrTimeOut(() => AnimatorTestHelper.ServerSideInstance != null);
            AssertOnTimeout($"Timed out waiting for the server-side instance of {GetNetworkAnimatorName(authoritativeMode)} to be spawned!");

            // Wait for it to spawn client-side
            yield return WaitForConditionOrTimeOut(() => AnimatorTestHelper.ClientSideInstances.ContainsKey(m_ClientNetworkManagers[0].LocalClientId));
            AssertOnTimeout($"Timed out waiting for the client-side instance of {GetNetworkAnimatorName(authoritativeMode)} to be spawned!");

            // Create new parameter values
            m_ParameterValues = new AnimatorTestHelper.ParameterValues() { FloatValue = 1.0f, IntValue = 5, BoolValue = true };

            if (authoritativeMode == AuthoritativeMode.OwnerAuth)
            {
                var objectToUpdate = ownerShipMode == OwnerShipMode.ClientOwner ? AnimatorTestHelper.ClientSideInstances[m_ClientNetworkManagers[0].LocalClientId] : AnimatorTestHelper.ServerSideInstance;
                // Set the new parameter values via the owner
                objectToUpdate.UpdateParameters(m_ParameterValues);
            }
            else
            {
                // Set the new parameter values via the server
                AnimatorTestHelper.ServerSideInstance.UpdateParameters(m_ParameterValues);
            }

            // Wait for the client side to update to the new parameter values
            yield return WaitForConditionOrTimeOut(() => ParameterValuesMatch(ownerShipMode, authoritativeMode, m_EnableVerboseDebug));
            AssertOnTimeout($"Timed out waiting for the client-side parameters to match {m_ParameterValues}!");
            VerboseDebug($" ------------------ Parameter Test [{ownerShipMode}] Stopping ------------------ ");
        }


        private bool AllTriggersDetected(OwnerShipMode ownerShipMode)
        {
            var serverParameters = AnimatorTestHelper.ServerSideInstance.GetParameterValues();
            if (ownerShipMode == OwnerShipMode.ClientOwner)
            {
                if (!TriggerTest.ClientsThatTriggered.Contains(m_ServerNetworkManager.LocalClientId))
                {
                    return false;
                }
            }

            foreach (var animatorTestHelper in AnimatorTestHelper.ClientSideInstances)
            {
                if (ownerShipMode == OwnerShipMode.ClientOwner && animatorTestHelper.Value.OwnerClientId == m_ClientNetworkManagers[0].LocalClientId)
                {
                    continue;
                }
                if (!TriggerTest.ClientsThatTriggered.Contains(animatorTestHelper.Value.NetworkManager.LocalClientId))
                {
                    return false;
                }
            }
            return true;
        }

        private bool WaitForClientsToInitialize()
        {
            foreach (var networkManager in m_ClientNetworkManagers)
            {
                var clientId = networkManager.LocalClientId;
                if (!AnimatorTestHelper.ClientSideInstances.ContainsKey(clientId))
                {
                    return false;
                }
                if (!AnimatorTestHelper.ClientSideInstances[clientId].GetComponent<Animator>().isInitialized)
                {
                    return false;
                }
                VerboseDebug($"{networkManager.name} initialized and spawned {AnimatorTestHelper.ClientSideInstances[clientId]}.");
            }
            return true;
        }

        /// <summary>
        /// Verifies that triggers are synchronized with currently connected clients
        /// </summary>
        /// <param name="authoritativeMode">Server or Owner authoritative</param>
        [UnityTest]
        public IEnumerator TriggerUpdateTests([Values] OwnerShipMode ownerShipMode, [Values] AuthoritativeMode authoritativeMode)
        {
            CheckStateEnterCount.ResetTest();
            VerboseDebug($" ++++++++++++++++++ Trigger Test [{TriggerTest.Iteration}][{ownerShipMode}] Starting ++++++++++++++++++ ");
            TriggerTest.IsVerboseDebug = m_EnableVerboseDebug;
            AnimatorTestHelper.IsTriggerTest = m_EnableVerboseDebug;

            // Spawn our test animator object
            var objectInstance = SpawnPrefab(ownerShipMode == OwnerShipMode.ClientOwner, authoritativeMode);

            // Wait for it to spawn server-side
            yield return WaitForConditionOrTimeOut(() => AnimatorTestHelper.ServerSideInstance != null);
            AssertOnTimeout($"Timed out waiting for the server-side instance of {GetNetworkAnimatorName(authoritativeMode)} to be spawned!");

            // Wait for it to spawn client-side
            yield return WaitForConditionOrTimeOut(WaitForClientsToInitialize);
            AssertOnTimeout($"Timed out waiting for the client-side instance of {GetNetworkAnimatorName(authoritativeMode)} to be spawned!");
            var animatorTestHelper = ownerShipMode == OwnerShipMode.ClientOwner ? AnimatorTestHelper.ClientSideInstances[m_ClientNetworkManagers[0].LocalClientId] : AnimatorTestHelper.ServerSideInstance;
            if (authoritativeMode == AuthoritativeMode.ServerAuth)
            {
                animatorTestHelper = AnimatorTestHelper.ServerSideInstance;
            }

            if (m_EnableVerboseDebug)
            {
                var retryTrigger = true;
                var timeOutHelper = new TimeoutHelper(1.0f);
                var count = 0;
                while (retryTrigger)
                {
                    VerboseDebug($"Current Trigger State: {animatorTestHelper.GetCurrentTriggerState()}");
                    VerboseDebug($"Setting Trigger");
                    animatorTestHelper.SetTrigger();
                    VerboseDebug($"New Trigger State: {animatorTestHelper.GetCurrentTriggerState()}");
                    // Wait for all triggers to fire
                    yield return WaitForConditionOrTimeOut(() => AllTriggersDetected(ownerShipMode), timeOutHelper);
                    retryTrigger = timeOutHelper.TimedOut;
                    if (retryTrigger)
                    {
                        count++;
                        Debug.LogWarning($"[{ownerShipMode}][{count}] Resending trigger!");
                    }
                }
            }
            else
            {
                animatorTestHelper.SetTrigger();
                // Wait for all triggers to fire
                yield return WaitForConditionOrTimeOut(() => AllTriggersDetected(ownerShipMode));
                AssertOnTimeout($"Timed out waiting for all triggers to match!");
            }

            yield return s_DefaultWaitForTick;

            var clientIdList = new List<ulong>();
            foreach (var client in m_ClientNetworkManagers)
            {
                clientIdList.Add(client.LocalClientId);
            }

            // Verify we only entered each state once
            yield return WaitForConditionOrTimeOut(() => CheckStateEnterCount.AllStatesEnteredMatch(clientIdList));
            AssertOnTimeout($"Timed out waiting for all states entered to match!");

            AnimatorTestHelper.IsTriggerTest = false;
            VerboseDebug($" ------------------ Trigger Test [{TriggerTest.Iteration}][{ownerShipMode}] Stopping ------------------ ");
        }

        protected override void OnNewClientCreated(NetworkManager networkManager)
        {
            var networkPrefab = new NetworkPrefab() { Prefab = m_AnimationTestPrefab };
            networkManager.NetworkConfig.NetworkPrefabs.Add(networkPrefab);
            networkPrefab = new NetworkPrefab() { Prefab = m_AnimationOwnerTestPrefab };
            networkManager.NetworkConfig.NetworkPrefabs.Add(networkPrefab);
        }

        /// <summary>
        /// Verifies that late joining clients are synchronized to an
        /// animator's trigger state.
        /// </summary>
        /// <param name="authoritativeMode">Server or Owner authoritative</param>
        [UnityTest]
        public IEnumerator LateJoinTriggerSynchronizationTest([Values] OwnerShipMode ownerShipMode, [Values] AuthoritativeMode authoritativeMode)
        {
            VerboseDebug($" ++++++++++++++++++ Late Join Trigger Test [{TriggerTest.Iteration}][{ownerShipMode}] Starting ++++++++++++++++++ ");
            TriggerTest.IsVerboseDebug = m_EnableVerboseDebug;
            AnimatorTestHelper.IsTriggerTest = m_EnableVerboseDebug;
            bool isClientOwner = ownerShipMode == OwnerShipMode.ClientOwner;

            // Spawn our test animator object
            var objectInstance = SpawnPrefab(ownerShipMode == OwnerShipMode.ClientOwner, authoritativeMode);

            // Wait for it to spawn server-side
            yield return WaitForConditionOrTimeOut(() => AnimatorTestHelper.ServerSideInstance != null);
            AssertOnTimeout($"Timed out waiting for the server-side instance of {GetNetworkAnimatorName(authoritativeMode)} to be spawned!");

            // Wait for it to spawn client-side
            yield return WaitForConditionOrTimeOut(WaitForClientsToInitialize);
            AssertOnTimeout($"Timed out waiting for the client-side instance of {GetNetworkAnimatorName(authoritativeMode)} to be spawned!");

            // Set the trigger based on the type of test
            if (authoritativeMode == AuthoritativeMode.OwnerAuth)
            {
                var objectToUpdate = ownerShipMode == OwnerShipMode.ClientOwner ? AnimatorTestHelper.ClientSideInstances[m_ClientNetworkManagers[0].LocalClientId] : AnimatorTestHelper.ServerSideInstance;
                // Set the animation trigger via the owner
                objectToUpdate.SetTrigger();
            }
            else
            {
                // Set the animation trigger via the server
                AnimatorTestHelper.ServerSideInstance.SetTrigger();
            }

            // Wait for all triggers to fire
            yield return WaitForConditionOrTimeOut(() => AllTriggersDetected(ownerShipMode));
            AssertOnTimeout($"Timed out waiting for all triggers to match!");

            // Create new parameter values
            m_ParameterValues = new AnimatorTestHelper.ParameterValues() { FloatValue = 1.0f, IntValue = 5, BoolValue = true };

            if (authoritativeMode == AuthoritativeMode.OwnerAuth)
            {
                var objectToUpdate = ownerShipMode == OwnerShipMode.ClientOwner ? AnimatorTestHelper.ClientSideInstances[m_ClientNetworkManagers[0].LocalClientId] : AnimatorTestHelper.ServerSideInstance;
                // Set the new parameter values
                objectToUpdate.UpdateParameters(m_ParameterValues);
            }
            else
            {
                // Set the new parameter values
                AnimatorTestHelper.ServerSideInstance.UpdateParameters(m_ParameterValues);
            }

            // Wait for the client side to update to the new parameter values
            yield return WaitForConditionOrTimeOut(() => ParameterValuesMatch(ownerShipMode, authoritativeMode, m_EnableVerboseDebug));
            AssertOnTimeout($"Timed out waiting for the client-side parameters to match {m_ParameterValues.ValuesToString()}!");

            yield return CreateAndStartNewClient();

            Assert.IsTrue(m_ClientNetworkManagers.Length == 2, $"Newly created and connected client was not added to {nameof(m_ClientNetworkManagers)}!");

            // Wait for it to spawn client-side
            yield return WaitForConditionOrTimeOut(WaitForClientsToInitialize);
            AssertOnTimeout($"Timed out waiting for the late joining client-side instance of {GetNetworkAnimatorName(authoritativeMode)} to be spawned!");

            // Make sure the AnimatorTestHelper client side instances (plus host) is the same as the TotalClients
            Assert.True((AnimatorTestHelper.ClientSideInstances.Count + 1) == TotalClients);

            // Now check that the late joining client and all other clients are synchronized to the trigger
            yield return WaitForConditionOrTimeOut(() => AllTriggersDetected(ownerShipMode));

            var message = string.Empty;
            if (s_GlobalTimeoutHelper.TimedOut)
            {
                message = "\n Clients that triggered:";
                foreach (var id in TriggerTest.ClientsThatTriggered)
                {
                    message += $" ({id})";
                }
            }
            AssertOnTimeout($"Timed out waiting for the late joining client's triggers to match!{message}", s_GlobalTimeoutHelper);
            // Now check that the late joining client and all other clients are synchronized to the updated parameter values
            yield return WaitForConditionOrTimeOut(() => ParameterValuesMatch(ownerShipMode, authoritativeMode, m_EnableVerboseDebug));
            AssertOnTimeout($"Timed out waiting for the client-side parameters to match {m_ParameterValues.ValuesToString()}!");

            var newlyJoinedClient = m_ClientNetworkManagers[1];
            yield return StopOneClient(newlyJoinedClient);
            VerboseDebug($" ------------------ Late Join Trigger Test [{TriggerTest.Iteration}][{ownerShipMode}] Stopping ------------------ ");
        }

        /// <summary>
        /// Verifies that late joining clients are synchronized to all of the
        /// states of an animator.
        /// </summary>
        /// <param name="authoritativeMode">Server or Owner authoritative</param>
        [UnityTest]
        public IEnumerator LateJoinSynchronizationTest([Values] OwnerShipMode ownerShipMode, [Values] AuthoritativeMode authoritativeMode)
        {
            VerboseDebug($" ++++++++++++++++++ Late Join Synchronization Test [{TriggerTest.Iteration}][{ownerShipMode}] Starting ++++++++++++++++++ ");

            StateSyncTest.IsVerboseDebug = m_EnableVerboseDebug;
            TriggerTest.IsVerboseDebug = m_EnableVerboseDebug;
            AnimatorTestHelper.IsTriggerTest = m_EnableVerboseDebug;
            bool isClientOwner = ownerShipMode == OwnerShipMode.ClientOwner;

            // Spawn our test animator object
            var objectInstance = SpawnPrefab(ownerShipMode == OwnerShipMode.ClientOwner, authoritativeMode);

            // Wait for it to spawn server-side
            yield return WaitForConditionOrTimeOut(() => AnimatorTestHelper.ServerSideInstance != null);
            AssertOnTimeout($"Timed out waiting for the server-side instance of {GetNetworkAnimatorName(authoritativeMode)} to be spawned!");

            // Wait for it to spawn client-side
            yield return WaitForConditionOrTimeOut(WaitForClientsToInitialize);
            AssertOnTimeout($"Timed out waiting for the client-side instance of {GetNetworkAnimatorName(authoritativeMode)} to be spawned!");

            // Set the late join parameter based on the type of test
            if (authoritativeMode == AuthoritativeMode.OwnerAuth)
            {
                var objectToUpdate = ownerShipMode == OwnerShipMode.ClientOwner ? AnimatorTestHelper.ClientSideInstances[m_ClientNetworkManagers[0].LocalClientId] : AnimatorTestHelper.ServerSideInstance;
                // Set the late join parameter via the owner
                objectToUpdate.SetLateJoinParam(true);
            }
            else
            {
                // Set the late join parameter to kick off the late join synchronization state
                // (it rotates to 180 degrees and then stops animating until the value is reset)
                AnimatorTestHelper.ServerSideInstance.SetLateJoinParam(true);
            }

            var firstClientAnimatorTestHelper = AnimatorTestHelper.ClientSideInstances[m_ClientNetworkManagers[0].LocalClientId];

            // Wait for the 1st client to rotate to the 180.0f degree point
            yield return WaitForConditionOrTimeOut(() => Mathf.Approximately(firstClientAnimatorTestHelper.transform.rotation.eulerAngles.y, 180.0f));
            AssertOnTimeout($"Timed out waiting for client-side cube to reach 180.0f!");

            m_ServerNetworkManager.OnClientConnectedCallback += Server_OnClientConnectedCallback;
            // Create and join a new client (late joining client)
            yield return CreateAndStartNewClient();

            Assert.IsTrue(m_ClientNetworkManagers.Length == 2, $"Newly created and connected client was not added to {nameof(m_ClientNetworkManagers)}!");

            // Wait for the client to have spawned and the spawned prefab to be instantiated
            yield return WaitForConditionOrTimeOut(WaitForClientsToInitialize);
            AssertOnTimeout($"Timed out waiting for the late joining client-side instance of {GetNetworkAnimatorName(authoritativeMode)} to be spawned!");

            // Make sure the AnimatorTestHelper client side instances (plus host) is the same as the TotalClients
            Assert.True((AnimatorTestHelper.ClientSideInstances.Count + 1) == TotalClients);

            var lateJoinObjectInstance = AnimatorTestHelper.ClientSideInstances[m_ClientNetworkManagers[1].LocalClientId];
            yield return WaitForConditionOrTimeOut(() => Mathf.Approximately(lateJoinObjectInstance.transform.rotation.eulerAngles.y, 180.0f));
            AssertOnTimeout($"[Late Join] Timed out waiting for cube to reach 180.0f!");

            // Validate the fix by making sure the late joining client was synchronized to the server's Animator's states
            yield return WaitForConditionOrTimeOut(LateJoinClientSynchronized);
            AssertOnTimeout("[Late Join] Timed out waiting for newly joined client to have expected state synchronized!");

            var newlyJoinedClient = m_ClientNetworkManagers[1];
            yield return StopOneClient(newlyJoinedClient);
            VerboseDebug($" ------------------ Late Join Synchronization Test [{TriggerTest.Iteration}][{ownerShipMode}] Stopping ------------------ ");
        }

        /// <summary>
        /// Update Server Side Animator Layer's AnimationStateInfo when late joining
        /// client connects to get the values being sent to the late joining client
        /// during NetworkAnimator synchronization.
        /// </summary>
        private void Server_OnClientConnectedCallback(ulong obj)
        {
            m_ServerNetworkManager.OnClientConnectedCallback -= Server_OnClientConnectedCallback;
            var serverAnimator = AnimatorTestHelper.ServerSideInstance.GetAnimator();

            // Only update the 3rd layer since this is where we want to assure all values are synchronized to the
            // same values upon the client connecting.
            var index = 2;
            Assert.True(StateSyncTest.StatesEntered.ContainsKey(m_ServerNetworkManager.LocalClientId), $"Server does not have an entry for layer {index}!");
            var animationStateInfo = serverAnimator.GetCurrentAnimatorStateInfo(index);
            StateSyncTest.StatesEntered[m_ServerNetworkManager.LocalClientId][index] = animationStateInfo;
            VerboseDebug($"[{index}][STATE-REFRESH][{m_ServerNetworkManager.name}] updated state normalized time ({animationStateInfo.normalizedTime}) to compare with late joined client.");
        }

        /// <summary>
        /// Used by: LateJoinSynchronizationTest
        /// Wait condition method that compares the states of the late joined client
        /// and the server.
        /// </summary>
        private bool LateJoinClientSynchronized()
        {
            if (!StateSyncTest.StatesEntered.ContainsKey(m_ClientNetworkManagers[1].LocalClientId))
            {
                VerboseDebug($"Late join client has not had any states synchronized yet!");
                return false;
            }

            var serverStates = StateSyncTest.StatesEntered[m_ServerNetworkManager.LocalClientId];
            var clientStates = StateSyncTest.StatesEntered[m_ClientNetworkManagers[1].LocalClientId];

            if (serverStates.Count() != clientStates.Count())
            {
                VerboseDebug($"[Count][Server] {serverStates.Count} | [Client-{m_ClientNetworkManagers[1].LocalClientId}]{clientStates.Count}");
                return false;
            }

            // We only check the last layer for this test as the other layers will have their normalized time slightly out of sync
            var index = 2;
            var serverAnimState = serverStates[index];
            if (clientStates[index].shortNameHash != serverAnimState.shortNameHash)
            {
                VerboseDebug($"[Hash Fail] Server({serverAnimState.shortNameHash}) | Client({clientStates[index].shortNameHash}) ");
                return false;
            }

            var clientNormalizedTime = clientStates[index].normalizedTime;
            var serverNormalizedTime = serverAnimState.normalizedTime;
            if (!Mathf.Approximately(clientNormalizedTime, serverNormalizedTime))
            {
                VerboseDebug($"[NormalizedTime Fail][{index}][{serverStates.Count}:{clientStates.Count}] Server({serverNormalizedTime}) | Client-{m_ClientNetworkManagers[1].LocalClientId}({clientNormalizedTime})");
                return false;
            }
            VerboseDebug($"[NormalizedTime][{index}][{serverStates.Count}:{clientStates.Count}] Server({serverNormalizedTime}) | Client-{m_ClientNetworkManagers[1].LocalClientId}({clientNormalizedTime})");

            return true;
        }

        private bool m_ClientDisconnected;
        /// <summary>
        /// This validates that NetworkAnimator properly removes its subscription to the
        /// OnClientConnectedCallback when it is despawned and destroyed during the
        /// shutdown sequence on both the server and the client.
        /// </summary>
        [UnityTest]
        public IEnumerator ShutdownWhileSpawnedAndStartBackUpTest()
        {
            VerboseDebug($" ++++++++++++++++++ Disconnect-Reconnect Server Test Starting ++++++++++++++++++ ");
            // Spawn our test animator object
            var objectInstance = SpawnPrefab(false, AuthoritativeMode.ServerAuth);
            var networkObjectInstance = objectInstance.GetComponent<NetworkObject>();
            var serverAnimatorTestHelper = objectInstance.GetComponent<AnimatorTestHelper>();
            m_ServerNetworkManager.OnClientDisconnectCallback += ServerNetworkManager_OnClientDisconnectCallback;
            // Wait for it to spawn server-side
            yield return WaitForConditionOrTimeOut(() => AnimatorTestHelper.ServerSideInstance != null);
            AssertOnTimeout($"Timed out waiting for the server-side instance of {GetNetworkAnimatorName(AuthoritativeMode.ServerAuth)} to be spawned!");

            // Wait for it to spawn client-side
            yield return WaitForConditionOrTimeOut(WaitForClientsToInitialize);
            AssertOnTimeout($"Timed out waiting for the client-side instance of {GetNetworkAnimatorName(AuthoritativeMode.ServerAuth)} to be spawned!");

            var clientAnimatorTestHelper = s_GlobalNetworkObjects[m_ClientNetworkManagers[0].LocalClientId].Values.Where((c) => c.GetComponent<AnimatorTestHelper>() != null).First().GetComponent<AnimatorTestHelper>();
            Assert.IsNotNull(clientAnimatorTestHelper, $"Could not find the client side {nameof(AnimatorTestHelper)}!");
            VerboseDebug($" ++++++++++++++++++ Disconnect-Reconnect Shutting Down Client and Server ++++++++++++++++++ ");
            clientAnimatorTestHelper.OnCheckIsServerIsClient += Client_OnCheckIsServerIsClient;

            // Now shutdown the client-side to verify this fix.
            // The client-side spawned NetworkObject should get despawned
            // and invoke the Client_OnCheckIsServerIsClient action.
            m_ClientNetworkManagers[0].Shutdown(true);

            // Wait for the server to receive the client disconnection notification
            yield return WaitForConditionOrTimeOut(() => m_ClientDisconnected);
            AssertOnTimeout($"Timed out waiting for the client to disconnect!");

            Assert.IsTrue(m_ClientTestHelperDespawned, $"Client-Side {nameof(AnimatorTestHelper)} did not have a valid IsClient setting!");

            serverAnimatorTestHelper.OnCheckIsServerIsClient += Server_OnCheckIsServerIsClient;
            m_ServerNetworkManager.OnClientDisconnectCallback -= ServerNetworkManager_OnClientDisconnectCallback;

            // Now shutdown the server-side to verify this fix.
            // The server-side spawned NetworkObject should get despawned
            // and invoke the Server_OnCheckIsServerIsClient action.
            m_ServerNetworkManager.Shutdown();

            yield return s_DefaultWaitForTick;

            yield return WaitForConditionOrTimeOut(() => !m_ServerNetworkManager.ShutdownInProgress);

            Assert.IsTrue(m_ServerTestHelperDespawned, $"Server-Side {nameof(AnimatorTestHelper)} did not have a valid IsServer setting!");
            AssertOnTimeout($"Timed out waiting for the server to shutdown!");

            VerboseDebug($" ++++++++++++++++++ Disconnect-Reconnect Restarting Server and Client ++++++++++++++++++ ");
            // Since the dynamically generated PlayerPrefab is destroyed when the server shuts down,
            // we need to create a new one and assign it to NetworkPrefab index 0
            m_PlayerPrefab = new GameObject("Player");
            NetworkObject networkObject = m_PlayerPrefab.AddComponent<NetworkObject>();
            NetcodeIntegrationTestHelpers.MakeNetworkObjectTestPrefab(networkObject);
            m_ServerNetworkManager.NetworkConfig.NetworkPrefabs[0].Prefab = m_PlayerPrefab;
            m_ClientNetworkManagers[0].NetworkConfig.NetworkPrefabs[0].Prefab = m_PlayerPrefab;
            OnCreatePlayerPrefab();

            // Now, restart the server and the client
            m_ServerNetworkManager.StartHost();
            m_ClientNetworkManagers[0].StartClient();

            // Wait for the server and client to start and connect
            yield return WaitForClientsConnectedOrTimeOut();

            VerboseDebug($" ++++++++++++++++++ Disconnect-Reconnect Server Test Stopping ++++++++++++++++++ ");
        }

        private bool m_ServerTestHelperDespawned;
        /// <summary>
        /// Server-Side
        /// This callback will be invoked as the spawned prefab is destroyed during shutdown
        /// </summary>
        private void Server_OnCheckIsServerIsClient(bool isServer, bool isClient)
        {
            // Validates this is still set when the NetworkObject is despawned during shutdown
            Assert.IsTrue(isServer);
            m_ServerTestHelperDespawned = true;
        }

        private bool m_ClientTestHelperDespawned;
        /// <summary>
        /// Client-Side
        /// This callback will be invoked as the spawned prefab is destroyed during shutdown
        /// </summary>
        private void Client_OnCheckIsServerIsClient(bool isServer, bool isClient)
        {
            // Validates this is still set when the NetworkObject is despawned during shutdown
            Assert.IsTrue(isClient);
            m_ClientTestHelperDespawned = true;
        }

        /// <summary>
        /// Verifies the client has disconnected
        /// </summary>
        private void ServerNetworkManager_OnClientDisconnectCallback(ulong obj)
        {
            m_ClientDisconnected = true;
        }
    }
}
