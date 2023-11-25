using System;
using System.Collections.Generic;
using UnityEngine;
using NetcodeNetworkEvent = Unity.Netcode.NetworkEvent;
using TransportNetworkEvent = Unity.Networking.Transport.NetworkEvent;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Collections;
using Unity.Networking.Transport;
using Unity.Networking.Transport.Relay;
using Unity.Networking.Transport.Utilities;

namespace Unity.Netcode.Transports.UTP
{
    /// <summary>
    /// Provides an interface that overrides the ability to create your own drivers and pipelines
    /// </summary>
    public interface INetworkStreamDriverConstructor
    {
        /// <summary>
        /// Creates the internal NetworkDriver
        /// </summary>
        /// <param name="transport">The owner transport</param>
        /// <param name="driver">The driver</param>
        /// <param name="unreliableFragmentedPipeline">The UnreliableFragmented NetworkPipeline</param>
        /// <param name="unreliableSequencedFragmentedPipeline">The UnreliableSequencedFragmented NetworkPipeline</param>
        /// <param name="reliableSequencedPipeline">The ReliableSequenced NetworkPipeline</param>
        void CreateDriver(
            UnityTransport transport,
            out NetworkDriver driver,
            out NetworkPipeline unreliableFragmentedPipeline,
            out NetworkPipeline unreliableSequencedFragmentedPipeline,
            out NetworkPipeline reliableSequencedPipeline);
    }

    /// <summary>
    /// Helper utility class to convert <see cref="Networking.Transport"/> error codes to human readable error messages.
    /// </summary>
    public static class ErrorUtilities
    {
        private const string k_NetworkSuccess = "Success";
        private const string k_NetworkIdMismatch = "NetworkId is invalid, likely caused by stale connection {0}.";
        private const string k_NetworkVersionMismatch = "NetworkVersion is invalid, likely caused by stale connection {0}.";
        private const string k_NetworkStateMismatch = "Sending data while connecting on connection {0} is not allowed.";
        private const string k_NetworkPacketOverflow = "Unable to allocate packet due to buffer overflow.";
        private const string k_NetworkSendQueueFull = "Currently unable to queue packet as there is too many in-flight packets. This could be because the send queue size ('Max Send Queue Size') is too small.";
        private const string k_NetworkHeaderInvalid = "Invalid Unity Transport Protocol header.";
        private const string k_NetworkDriverParallelForErr = "The parallel network driver needs to process a single unique connection per job, processing a single connection multiple times in a parallel for is not supported.";
        private const string k_NetworkSendHandleInvalid = "Invalid NetworkInterface Send Handle. Likely caused by pipeline send data corruption.";
        private const string k_NetworkArgumentMismatch = "Invalid NetworkEndpoint Arguments.";

        /// <summary>
        /// Convert error code to human readable error message.
        /// </summary>
        /// <param name="error">Status code of the error</param>
        /// <param name="connectionId">Subject connection ID of the error</param>
        /// <returns>Human readable error message.</returns>
        public static string ErrorToString(Networking.Transport.Error.StatusCode error, ulong connectionId)
        {
            switch (error)
            {
                case Networking.Transport.Error.StatusCode.Success:
                    return k_NetworkSuccess;
                case Networking.Transport.Error.StatusCode.NetworkIdMismatch:
                    return string.Format(k_NetworkIdMismatch, connectionId);
                case Networking.Transport.Error.StatusCode.NetworkVersionMismatch:
                    return string.Format(k_NetworkVersionMismatch, connectionId);
                case Networking.Transport.Error.StatusCode.NetworkStateMismatch:
                    return string.Format(k_NetworkStateMismatch, connectionId);
                case Networking.Transport.Error.StatusCode.NetworkPacketOverflow:
                    return k_NetworkPacketOverflow;
                case Networking.Transport.Error.StatusCode.NetworkSendQueueFull:
                    return k_NetworkSendQueueFull;
                case Networking.Transport.Error.StatusCode.NetworkHeaderInvalid:
                    return k_NetworkHeaderInvalid;
                case Networking.Transport.Error.StatusCode.NetworkDriverParallelForErr:
                    return k_NetworkDriverParallelForErr;
                case Networking.Transport.Error.StatusCode.NetworkSendHandleInvalid:
                    return k_NetworkSendHandleInvalid;
                case Networking.Transport.Error.StatusCode.NetworkArgumentMismatch:
                    return k_NetworkArgumentMismatch;
            }

            return $"Unknown ErrorCode {Enum.GetName(typeof(Networking.Transport.Error.StatusCode), error)}";
        }
    }

    /// <summary>
    /// The Netcode for GameObjects NetworkTransport for UnityTransport.
    /// Note: This is highly recommended to use over UNet.
    /// </summary>
    public partial class UnityTransport : NetworkTransport, INetworkStreamDriverConstructor
    {
        /// <summary>
        /// Enum type stating the type of protocol
        /// </summary>
        public enum ProtocolType
        {
            /// <summary>
            /// Unity Transport Protocol
            /// </summary>
            UnityTransport,
            /// <summary>
            /// Unity Transport Protocol over Relay
            /// </summary>
            RelayUnityTransport,
        }

        private enum State
        {
            Disconnected,
            Listening,
            Connected,
        }

        /// <summary>
        /// The default maximum (receive) packet queue size
        /// </summary>
        public const int InitialMaxPacketQueueSize = 128;

        /// <summary>
        /// The default maximum payload size
        /// </summary>
        public const int InitialMaxPayloadSize = 6 * 1024;

        /// <summary>
        /// The default maximum send queue size
        /// </summary>
        public const int InitialMaxSendQueueSize = 16 * InitialMaxPayloadSize;

        private static ConnectionAddressData s_DefaultConnectionAddressData = new ConnectionAddressData { Address = "127.0.0.1", Port = 7777, ServerListenAddress = string.Empty };

#pragma warning disable IDE1006 // Naming Styles

        /// <summary>
        /// The global <see cref="INetworkStreamDriverConstructor"/> implementation
        /// </summary>
        public static INetworkStreamDriverConstructor s_DriverConstructor;
#pragma warning restore IDE1006 // Naming Styles

        /// <summary>
        /// Returns either the global <see cref="INetworkStreamDriverConstructor"/> implementation or the current <see cref="UnityTransport"/> instance
        /// </summary>
        public INetworkStreamDriverConstructor DriverConstructor => s_DriverConstructor ?? this;

        [Tooltip("Which protocol should be selected (Relay/Non-Relay).")]
        [SerializeField]
        private ProtocolType m_ProtocolType;

        [Tooltip("The maximum amount of packets that can be in the internal send/receive queues. Basically this is how many packets can be sent/received in a single update/frame.")]
        [SerializeField]
        private int m_MaxPacketQueueSize = InitialMaxPacketQueueSize;

        /// <summary>The maximum amount of packets that can be in the internal send/receive queues.</summary>
        /// <remarks>Basically this is how many packets can be sent/received in a single update/frame.</remarks>
        public int MaxPacketQueueSize
        {
            get => m_MaxPacketQueueSize;
            set => m_MaxPacketQueueSize = value;
        }

        [Tooltip("The maximum size of an unreliable payload that can be handled by the transport.")]
        [SerializeField]
        private int m_MaxPayloadSize = InitialMaxPayloadSize;

        /// <summary>The maximum size of an unreliable payload that can be handled by the transport.</summary>
        public int MaxPayloadSize
        {
            get => m_MaxPayloadSize;
            set => m_MaxPayloadSize = value;
        }

        [Tooltip("The maximum size in bytes of the transport send queue. The send queue accumulates messages for batching and stores messages when other internal send queues are full. If you routinely observe an error about too many in-flight packets, try increasing this.")]
        [SerializeField]
        private int m_MaxSendQueueSize = InitialMaxSendQueueSize;

        /// <summary>The maximum size in bytes of the transport send queue.</summary>
        /// <remarks>
        /// The send queue accumulates messages for batching and stores messages when other internal
        /// send queues are full. If you routinely observe an error about too many in-flight packets,
        /// try increasing this.
        /// </remarks>
        public int MaxSendQueueSize
        {
            get => m_MaxSendQueueSize;
            set => m_MaxSendQueueSize = value;
        }

        [Tooltip("Timeout in milliseconds after which a heartbeat is sent if there is no activity.")]
        [SerializeField]
        private int m_HeartbeatTimeoutMS = NetworkParameterConstants.HeartbeatTimeoutMS;

        /// <summary>Timeout in milliseconds after which a heartbeat is sent if there is no activity.</summary>
        public int HeartbeatTimeoutMS
        {
            get => m_HeartbeatTimeoutMS;
            set => m_HeartbeatTimeoutMS = value;
        }

        [Tooltip("Timeout in milliseconds indicating how long we will wait until we send a new connection attempt.")]
        [SerializeField]
        private int m_ConnectTimeoutMS = NetworkParameterConstants.ConnectTimeoutMS;

        /// <summary>
        /// Timeout in milliseconds indicating how long we will wait until we send a new connection attempt.
        /// </summary>
        public int ConnectTimeoutMS
        {
            get => m_ConnectTimeoutMS;
            set => m_ConnectTimeoutMS = value;
        }

        [Tooltip("The maximum amount of connection attempts we will try before disconnecting.")]
        [SerializeField]
        private int m_MaxConnectAttempts = NetworkParameterConstants.MaxConnectAttempts;

        /// <summary>The maximum amount of connection attempts we will try before disconnecting.</summary>
        public int MaxConnectAttempts
        {
            get => m_MaxConnectAttempts;
            set => m_MaxConnectAttempts = value;
        }

        [Tooltip("Inactivity timeout after which a connection will be disconnected. The connection needs to receive data from the connected endpoint within this timeout. Note that with heartbeats enabled, simply not sending any data will not be enough to trigger this timeout (since heartbeats count as connection events).")]
        [SerializeField]
        private int m_DisconnectTimeoutMS = NetworkParameterConstants.DisconnectTimeoutMS;

        /// <summary>Inactivity timeout after which a connection will be disconnected.</summary>
        /// <remarks>
        /// The connection needs to receive data from the connected endpoint within this timeout.
        /// Note that with heartbeats enabled, simply not sending any data will not be enough to
        /// trigger this timeout (since heartbeats count as connection events).
        /// </remarks>
        public int DisconnectTimeoutMS
        {
            get => m_DisconnectTimeoutMS;
            set => m_DisconnectTimeoutMS = value;
        }

        /// <summary>
        /// Structure to store the address to connect to
        /// </summary>
        [Serializable]
        public struct ConnectionAddressData
        {
            /// <summary>
            /// IP address of the server (address to which clients will connect to).
            /// </summary>
            [Tooltip("IP address of the server (address to which clients will connect to).")]
            [SerializeField]
            public string Address;

            /// <summary>
            /// UDP port of the server.
            /// </summary>
            [Tooltip("UDP port of the server.")]
            [SerializeField]
            public ushort Port;

            /// <summary>
            /// IP address the server will listen on. If not provided, will use 'Address'.
            /// </summary>
            [Tooltip("IP address the server will listen on. If not provided, will use 'Address'.")]
            [SerializeField]
            public string ServerListenAddress;

            private static NetworkEndPoint ParseNetworkEndpoint(string ip, ushort port)
            {
                if (!NetworkEndPoint.TryParse(ip, port, out var endpoint))
                {
                    Debug.LogError($"Invalid network endpoint: {ip}:{port}.");
                    return default;
                }

                return endpoint;
            }

            /// <summary>
            /// Endpoint (IP address and port) clients will connect to.
            /// </summary>
            public NetworkEndPoint ServerEndPoint => ParseNetworkEndpoint(Address, Port);

            /// <summary>
            /// Endpoint (IP address and port) server will listen/bind on.
            /// </summary>
            public NetworkEndPoint ListenEndPoint => ParseNetworkEndpoint((ServerListenAddress == string.Empty) ? Address : ServerListenAddress, Port);
        }

        /// <summary>
        /// The connection (address) data for this <see cref="UnityTransport"/> instance.
        /// This is where you can change IP Address, Port, or server's listen address.
        /// <see cref="ConnectionAddressData"/>
        /// </summary>
        public ConnectionAddressData ConnectionData = s_DefaultConnectionAddressData;

        /// <summary>
        /// Parameters for the Network Simulator
        /// </summary>
        [Serializable]
        public struct SimulatorParameters
        {
            /// <summary>
            /// Delay to add to every send and received packet (in milliseconds). Only applies in the editor and in development builds. The value is ignored in production builds.
            /// </summary>
            [Tooltip("Delay to add to every send and received packet (in milliseconds). Only applies in the editor and in development builds. The value is ignored in production builds.")]
            [SerializeField]
            public int PacketDelayMS;

            /// <summary>
            /// Jitter (random variation) to add/substract to the packet delay (in milliseconds). Only applies in the editor and in development builds. The value is ignored in production builds.
            /// </summary>
            [Tooltip("Jitter (random variation) to add/substract to the packet delay (in milliseconds). Only applies in the editor and in development builds. The value is ignored in production builds.")]
            [SerializeField]
            public int PacketJitterMS;

            /// <summary>
            /// Percentage of sent and received packets to drop. Only applies in the editor and in the editor and in developments builds.
            /// </summary>
            [Tooltip("Percentage of sent and received packets to drop. Only applies in the editor and in the editor and in developments builds.")]
            [SerializeField]
            public int PacketDropRate;
        }

        /// <summary>
        /// Can be used to simulate poor network conditions such as:
        /// - packet delay/latency
        /// - packet jitter (variances in latency, see: https://en.wikipedia.org/wiki/Jitter)
        /// - packet drop rate (packet loss)
        /// </summary>
        public SimulatorParameters DebugSimulator = new SimulatorParameters
        {
            PacketDelayMS = 0,
            PacketJitterMS = 0,
            PacketDropRate = 0
        };

        private struct PacketLossCache
        {
            public int PacketsReceived;
            public int PacketsDropped;
            public float PacketLoss;
        };

        private PacketLossCache m_PacketLossCache = new PacketLossCache();

        private State m_State = State.Disconnected;
        private NetworkDriver m_Driver;
        private NetworkSettings m_NetworkSettings;
        private ulong m_ServerClientId;

        private NetworkPipeline m_UnreliableFragmentedPipeline;
        private NetworkPipeline m_UnreliableSequencedFragmentedPipeline;
        private NetworkPipeline m_ReliableSequencedPipeline;

        /// <summary>
        /// The client id used to represent the server.
        /// </summary>
        public override ulong ServerClientId => m_ServerClientId;

        /// <summary>
        /// The current ProtocolType used by the transport
        /// </summary>
        public ProtocolType Protocol => m_ProtocolType;

        private RelayServerData m_RelayServerData;

        internal NetworkManager NetworkManager;

        /// <summary>
        /// SendQueue dictionary is used to batch events instead of sending them immediately.
        /// </summary>
        private readonly Dictionary<SendTarget, BatchedSendQueue> m_SendQueue = new Dictionary<SendTarget, BatchedSendQueue>();

        // Since reliable messages may be spread out over multiple transport payloads, it's possible
        // to receive only parts of a message in an update. We thus keep the reliable receive queues
        // around to avoid losing partial messages.
        private readonly Dictionary<ulong, BatchedReceiveQueue> m_ReliableReceiveQueues = new Dictionary<ulong, BatchedReceiveQueue>();

        private void InitDriver()
        {
            DriverConstructor.CreateDriver(
                this,
                out m_Driver,
                out m_UnreliableFragmentedPipeline,
                out m_UnreliableSequencedFragmentedPipeline,
                out m_ReliableSequencedPipeline);
        }

        private void DisposeInternals()
        {
            if (m_Driver.IsCreated)
            {
                m_Driver.Dispose();
            }

            m_NetworkSettings.Dispose();

            foreach (var queue in m_SendQueue.Values)
            {
                queue.Dispose();
            }

            m_SendQueue.Clear();
        }

        private NetworkPipeline SelectSendPipeline(NetworkDelivery delivery)
        {
            switch (delivery)
            {
                case NetworkDelivery.Unreliable:
                    return m_UnreliableFragmentedPipeline;

                case NetworkDelivery.UnreliableSequenced:
                    return m_UnreliableSequencedFragmentedPipeline;

                case NetworkDelivery.Reliable:
                case NetworkDelivery.ReliableSequenced:
                case NetworkDelivery.ReliableFragmentedSequenced:
                    return m_ReliableSequencedPipeline;

                default:
                    Debug.LogError($"Unknown {nameof(NetworkDelivery)} value: {delivery}");
                    return NetworkPipeline.Null;
            }
        }

        private bool ClientBindAndConnect()
        {
            var serverEndpoint = default(NetworkEndPoint);

            if (m_ProtocolType == ProtocolType.RelayUnityTransport)
            {
                //This comparison is currently slow since RelayServerData does not implement a custom comparison operator that doesn't use
                //reflection, but this does not live in the context of a performance-critical loop, it runs once at initial connection time.
                if (m_RelayServerData.Equals(default(RelayServerData)))
                {
                    Debug.LogError("You must call SetRelayServerData() at least once before calling StartRelayServer.");
                    return false;
                }

                m_NetworkSettings.WithRelayParameters(ref m_RelayServerData, m_HeartbeatTimeoutMS);
            }
            else
            {
                serverEndpoint = ConnectionData.ServerEndPoint;
            }

            InitDriver();

            int result = m_Driver.Bind(NetworkEndPoint.AnyIpv4);
            if (result != 0)
            {
                Debug.LogError("Client failed to bind");
                return false;
            }

            var serverConnection = m_Driver.Connect(serverEndpoint);
            m_ServerClientId = ParseClientId(serverConnection);

            return true;
        }

        private bool ServerBindAndListen(NetworkEndPoint endPoint)
        {
            InitDriver();

            int result = m_Driver.Bind(endPoint);
            if (result != 0)
            {
                Debug.LogError("Server failed to bind");
                return false;
            }

            result = m_Driver.Listen();
            if (result != 0)
            {
                Debug.LogError("Server failed to listen");
                return false;
            }

            m_State = State.Listening;
            return true;
        }

        private static RelayAllocationId ConvertFromAllocationIdBytes(byte[] allocationIdBytes)
        {
            unsafe
            {
                fixed (byte* ptr = allocationIdBytes)
                {
                    return RelayAllocationId.FromBytePointer(ptr, allocationIdBytes.Length);
                }
            }
        }

        private static RelayHMACKey ConvertFromHMAC(byte[] hmac)
        {
            unsafe
            {
                fixed (byte* ptr = hmac)
                {
                    return RelayHMACKey.FromBytePointer(ptr, RelayHMACKey.k_Length);
                }
            }
        }

        private static RelayConnectionData ConvertConnectionData(byte[] connectionData)
        {
            unsafe
            {
                fixed (byte* ptr = connectionData)
                {
                    return RelayConnectionData.FromBytePointer(ptr, RelayConnectionData.k_Length);
                }
            }
        }

        internal void SetMaxPayloadSize(int maxPayloadSize)
        {
            m_MaxPayloadSize = maxPayloadSize;
        }

        private void SetProtocol(ProtocolType inProtocol)
        {
            m_ProtocolType = inProtocol;
        }

        /// <summary>Set the relay server data for the server.</summary>
        /// <param name="ipv4Address">IP address of the relay server.</param>
        /// <param name="port">UDP port of the relay server.</param>
        /// <param name="allocationIdBytes">Allocation ID as a byte array.</param>
        /// <param name="keyBytes">Allocation key as a byte array.</param>
        /// <param name="connectionDataBytes">Connection data as a byte array.</param>
        /// <param name="hostConnectionDataBytes">The HostConnectionData as a byte array.</param>
        /// <param name="isSecure">Whether the connection is secure (uses DTLS).</param>
        public void SetRelayServerData(string ipv4Address, ushort port, byte[] allocationIdBytes, byte[] keyBytes, byte[] connectionDataBytes, byte[] hostConnectionDataBytes = null, bool isSecure = false)
        {
            RelayConnectionData hostConnectionData;

            if (!NetworkEndPoint.TryParse(ipv4Address, port, out var serverEndpoint))
            {
                Debug.LogError($"Invalid address {ipv4Address}:{port}");

                // We set this to default to cause other checks to fail to state you need to call this
                // function again.
                m_RelayServerData = default;
                return;
            }

            var allocationId = ConvertFromAllocationIdBytes(allocationIdBytes);
            var key = ConvertFromHMAC(keyBytes);
            var connectionData = ConvertConnectionData(connectionDataBytes);

            if (hostConnectionDataBytes != null)
            {
                hostConnectionData = ConvertConnectionData(hostConnectionDataBytes);
            }
            else
            {
                hostConnectionData = connectionData;
            }

            m_RelayServerData = new RelayServerData(ref serverEndpoint, 0, ref allocationId, ref connectionData, ref hostConnectionData, ref key, isSecure);
            m_RelayServerData.ComputeNewNonce();

            SetProtocol(ProtocolType.RelayUnityTransport);
        }

        /// <summary>Set the relay server data for the host.</summary>
        /// <param name="ipAddress">IP address of the relay server.</param>
        /// <param name="port">UDP port of the relay server.</param>
        /// <param name="allocationId">Allocation ID as a byte array.</param>
        /// <param name="key">Allocation key as a byte array.</param>
        /// <param name="connectionData">Connection data as a byte array.</param>
        /// <param name="isSecure">Whether the connection is secure (uses DTLS).</param>
        public void SetHostRelayData(string ipAddress, ushort port, byte[] allocationId, byte[] key, byte[] connectionData, bool isSecure = false)
        {
            SetRelayServerData(ipAddress, port, allocationId, key, connectionData, null, isSecure);
        }

        /// <summary>Set the relay server data for the host.</summary>
        /// <param name="ipAddress">IP address of the relay server.</param>
        /// <param name="port">UDP port of the relay server.</param>
        /// <param name="allocationId">Allocation ID as a byte array.</param>
        /// <param name="key">Allocation key as a byte array.</param>
        /// <param name="connectionData">Connection data as a byte array.</param>
        /// <param name="hostConnectionData">Host's connection data as a byte array.</param>
        /// <param name="isSecure">Whether the connection is secure (uses DTLS).</param>
        public void SetClientRelayData(string ipAddress, ushort port, byte[] allocationId, byte[] key, byte[] connectionData, byte[] hostConnectionData, bool isSecure = false)
        {
            SetRelayServerData(ipAddress, port, allocationId, key, connectionData, hostConnectionData, isSecure);
        }

        /// <summary>
        /// Sets IP and Port information. This will be ignored if using the Unity Relay and you should call <see cref="SetRelayServerData"/>
        /// </summary>
        /// <param name="ipv4Address">The remote IP address</param>
        /// <param name="port">The remote port</param>
        /// <param name="listenAddress">The local listen address</param>
        public void SetConnectionData(string ipv4Address, ushort port, string listenAddress = null)
        {
            ConnectionData = new ConnectionAddressData
            {
                Address = ipv4Address,
                Port = port,
                ServerListenAddress = listenAddress ?? string.Empty
            };

            SetProtocol(ProtocolType.UnityTransport);
        }

        /// <summary>
        /// Sets IP and Port information. This will be ignored if using the Unity Relay and you should call <see cref="SetRelayServerData"/>
        /// </summary>
        /// <param name="endPoint">The remote end point</param>
        /// <param name="listenEndPoint">The local listen endpoint</param>
        public void SetConnectionData(NetworkEndPoint endPoint, NetworkEndPoint listenEndPoint = default)
        {
            string serverAddress = endPoint.Address.Split(':')[0];

            string listenAddress = string.Empty;
            if (listenEndPoint != default)
            {
                listenAddress = listenEndPoint.Address.Split(':')[0];
                if (endPoint.Port != listenEndPoint.Port)
                {
                    Debug.LogError($"Port mismatch between server and listen endpoints ({endPoint.Port} vs {listenEndPoint.Port}).");
                }
            }

            SetConnectionData(serverAddress, endPoint.Port, listenAddress);
        }

        /// <summary>Set the parameters for the debug simulator.</summary>
        /// <param name="packetDelay">Packet delay in milliseconds.</param>
        /// <param name="packetJitter">Packet jitter in milliseconds.</param>
        /// <param name="dropRate">Packet drop percentage.</param>
        public void SetDebugSimulatorParameters(int packetDelay, int packetJitter, int dropRate)
        {
            if (m_Driver.IsCreated)
            {
                Debug.LogError("SetDebugSimulatorParameters() must be called before StartClient() or StartServer().");
                return;
            }

            DebugSimulator = new SimulatorParameters
            {
                PacketDelayMS = packetDelay,
                PacketJitterMS = packetJitter,
                PacketDropRate = dropRate
            };
        }

        private bool StartRelayServer()
        {
            //This comparison is currently slow since RelayServerData does not implement a custom comparison operator that doesn't use
            //reflection, but this does not live in the context of a performance-critical loop, it runs once at initial connection time.
            if (m_RelayServerData.Equals(default(RelayServerData)))
            {
                Debug.LogError("You must call SetRelayServerData() at least once before calling StartRelayServer.");
                return false;
            }
            else
            {
                m_NetworkSettings.WithRelayParameters(ref m_RelayServerData, m_HeartbeatTimeoutMS);
                return ServerBindAndListen(NetworkEndPoint.AnyIpv4);
            }
        }

        // Send as many batched messages from the queue as possible.
        private void SendBatchedMessages(SendTarget sendTarget, BatchedSendQueue queue)
        {
            var clientId = sendTarget.ClientId;
            var connection = ParseClientId(clientId);
            var pipeline = sendTarget.NetworkPipeline;

            while (!queue.IsEmpty)
            {
                var result = m_Driver.BeginSend(pipeline, connection, out var writer);
                if (result != (int)Networking.Transport.Error.StatusCode.Success)
                {
                    Debug.LogError("Error sending the message: " +
                        ErrorUtilities.ErrorToString((Networking.Transport.Error.StatusCode)result, clientId));
                    return;
                }

                // We don't attempt to send entire payloads over the reliable pipeline. Instead we
                // fragment it manually. This is safe and easy to do since the reliable pipeline
                // basically implements a stream, so as long as we separate the different messages
                // in the stream (the send queue does that automatically) we are sure they'll be
                // reassembled properly at the other end. This allows us to lift the limit of ~44KB
                // on reliable payloads (because of the reliable window size).
                var written = pipeline == m_ReliableSequencedPipeline ? queue.FillWriterWithBytes(ref writer) : queue.FillWriterWithMessages(ref writer);

                result = m_Driver.EndSend(writer);
                if (result == written)
                {
                    // Batched message was sent successfully. Remove it from the queue.
                    queue.Consume(written);
                }
                else
                {
                    // Some error occured. If it's just the UTP queue being full, then don't log
                    // anything since that's okay (the unsent message(s) are still in the queue
                    // and we'll retry sending the later). Otherwise log the error and remove the
                    // message from the queue (we don't want to resend it again since we'll likely
                    // just get the same error again).
                    if (result != (int)Networking.Transport.Error.StatusCode.NetworkSendQueueFull)
                    {
                        Debug.LogError("Error sending the message: " + ErrorUtilities.ErrorToString((Networking.Transport.Error.StatusCode)result, clientId));
                        queue.Consume(written);
                    }

                    return;
                }
            }
        }

        private bool AcceptConnection()
        {
            var connection = m_Driver.Accept();

            if (connection == default)
            {
                return false;
            }

            InvokeOnTransportEvent(NetcodeNetworkEvent.Connect,
                ParseClientId(connection),
                default,
                Time.realtimeSinceStartup);

            return true;

        }

        private void ReceiveMessages(ulong clientId, NetworkPipeline pipeline, DataStreamReader dataReader)
        {
            BatchedReceiveQueue queue;
            if (pipeline == m_ReliableSequencedPipeline)
            {
                if (m_ReliableReceiveQueues.TryGetValue(clientId, out queue))
                {
                    queue.PushReader(dataReader);
                }
                else
                {
                    queue = new BatchedReceiveQueue(dataReader);
                    m_ReliableReceiveQueues[clientId] = queue;
                }
            }
            else
            {
                queue = new BatchedReceiveQueue(dataReader);
            }

            while (!queue.IsEmpty)
            {
                var message = queue.PopMessage();
                if (message == default)
                {
                    // Only happens if there's only a partial message in the queue (rare).
                    break;
                }

                InvokeOnTransportEvent(NetcodeNetworkEvent.Data, clientId, message, Time.realtimeSinceStartup);
            }
        }

        private bool ProcessEvent()
        {
            var eventType = m_Driver.PopEvent(out var networkConnection, out var reader, out var pipeline);
            var clientId = ParseClientId(networkConnection);

            switch (eventType)
            {
                case TransportNetworkEvent.Type.Connect:
                    {
                        InvokeOnTransportEvent(NetcodeNetworkEvent.Connect,
                            clientId,
                            default,
                            Time.realtimeSinceStartup);

                        m_State = State.Connected;
                        return true;
                    }
                case TransportNetworkEvent.Type.Disconnect:
                    {
                        // Handle cases where we're a client receiving a Disconnect event. The
                        // meaning of the event depends on our current state. If we were connected
                        // then it means we got disconnected. If we were disconnected means that our
                        // connection attempt has failed.
                        if (m_State == State.Connected)
                        {
                            m_State = State.Disconnected;
                            m_ServerClientId = default;
                        }
                        else if (m_State == State.Disconnected)
                        {
                            Debug.LogError("Failed to connect to server.");
                            m_ServerClientId = default;
                        }

                        m_ReliableReceiveQueues.Remove(clientId);
                        ClearSendQueuesForClientId(clientId);

                        InvokeOnTransportEvent(NetcodeNetworkEvent.Disconnect,
                            clientId,
                            default,
                            Time.realtimeSinceStartup);

                        return true;
                    }
                case TransportNetworkEvent.Type.Data:
                    {
                        ReceiveMessages(clientId, pipeline, reader);
                        return true;
                    }
            }

            return false;
        }

        private void Update()
        {
            if (m_Driver.IsCreated)
            {
                foreach (var kvp in m_SendQueue)
                {
                    SendBatchedMessages(kvp.Key, kvp.Value);
                }

                m_Driver.ScheduleUpdate().Complete();

                if (m_ProtocolType == ProtocolType.RelayUnityTransport && m_Driver.GetRelayConnectionStatus() == RelayConnectionStatus.AllocationInvalid)
                {
                    Debug.LogError("Transport failure! Relay allocation needs to be recreated, and NetworkManager restarted. " +
                        "Use NetworkManager.OnTransportFailure to be notified of such events programmatically.");

                    InvokeOnTransportEvent(NetcodeNetworkEvent.TransportFailure, 0, default, Time.realtimeSinceStartup);
                    return;
                }

                while (AcceptConnection() && m_Driver.IsCreated)
                {
                    ;
                }

                while (ProcessEvent() && m_Driver.IsCreated)
                {
                    ;
                }

#if MULTIPLAYER_TOOLS_1_0_0_PRE_7
                if (NetworkManager)
                {
                    ExtractNetworkMetrics();
                }
#endif
            }
        }

        private void OnDestroy()
        {
            DisposeInternals();
        }

#if MULTIPLAYER_TOOLS_1_0_0_PRE_7
        private void ExtractNetworkMetrics()
        {
            if (NetworkManager.IsServer)
            {
                var ngoConnectionIds = NetworkManager.ConnectedClients.Keys;
                foreach (var ngoConnectionId in ngoConnectionIds)
                {
                    if (ngoConnectionId == 0 && NetworkManager.IsHost)
                    {
                        continue;
                    }
                    var transportClientId = NetworkManager.ClientIdToTransportId(ngoConnectionId);
                    ExtractNetworkMetricsForClient(transportClientId);
                }
            }
            else
            {
                if (m_ServerClientId != 0)
                {
                    ExtractNetworkMetricsForClient(m_ServerClientId);
                }
            }
        }

        private void ExtractNetworkMetricsForClient(ulong transportClientId)
        {
            var networkConnection =  ParseClientId(transportClientId);
            ExtractNetworkMetricsFromPipeline(m_UnreliableFragmentedPipeline, networkConnection);
            ExtractNetworkMetricsFromPipeline(m_UnreliableSequencedFragmentedPipeline, networkConnection);
            ExtractNetworkMetricsFromPipeline(m_ReliableSequencedPipeline, networkConnection);

            var rttValue = NetworkManager.IsServer ? 0 : ExtractRtt(networkConnection);
            NetworkMetrics.UpdateRttToServer(rttValue);

            var packetLoss = NetworkManager.IsServer ? 0 : ExtractPacketLoss(networkConnection);
            NetworkMetrics.UpdatePacketLoss(packetLoss);
        }

        private void ExtractNetworkMetricsFromPipeline(NetworkPipeline pipeline, NetworkConnection networkConnection)
        {
            //Don't need to dispose of the buffers, they are filled with data pointers.
            m_Driver.GetPipelineBuffers(pipeline,
                NetworkPipelineStageCollection.GetStageId(typeof(NetworkMetricsPipelineStage)),
                networkConnection,
                out _,
                out _,
                out var sharedBuffer);

            unsafe
            {
                var networkMetricsContext = (NetworkMetricsContext*)sharedBuffer.GetUnsafePtr();

                NetworkMetrics.TrackPacketSent(networkMetricsContext->PacketSentCount);
                NetworkMetrics.TrackPacketReceived(networkMetricsContext->PacketReceivedCount);

                networkMetricsContext->PacketSentCount = 0;
                networkMetricsContext->PacketReceivedCount = 0;
            }
        }
#endif

        private int ExtractRtt(NetworkConnection networkConnection)
        {
            if (m_Driver.GetConnectionState(networkConnection) != NetworkConnection.State.Connected)
            {
                return 0;
            }

            m_Driver.GetPipelineBuffers(m_ReliableSequencedPipeline,
                NetworkPipelineStageCollection.GetStageId(typeof(ReliableSequencedPipelineStage)),
                networkConnection,
                out _,
                out _,
                out var sharedBuffer);

            unsafe
            {
                var sharedContext = (ReliableUtility.SharedContext*)sharedBuffer.GetUnsafePtr();

                return sharedContext->RttInfo.LastRtt;
            }
        }

        private float ExtractPacketLoss(NetworkConnection networkConnection)
        {
            if (m_Driver.GetConnectionState(networkConnection) != NetworkConnection.State.Connected)
            {
                return 0f;
            }

            m_Driver.GetPipelineBuffers(m_ReliableSequencedPipeline,
                NetworkPipelineStageCollection.GetStageId(typeof(ReliableSequencedPipelineStage)),
                networkConnection,
                out _,
                out _,
                out var sharedBuffer);

            unsafe
            {
                var sharedContext = (ReliableUtility.SharedContext*)sharedBuffer.GetUnsafePtr();

                var packetReceivedDelta = (float)(sharedContext->stats.PacketsReceived - m_PacketLossCache.PacketsReceived);
                var packetDroppedDelta = (float)(sharedContext->stats.PacketsDropped - m_PacketLossCache.PacketsDropped);

                // There can be multiple update happening in a single frame where no packets have transitioned
                // In those situation we want to return the last packet loss value instead of 0 to avoid invalid swings
                if (packetDroppedDelta == 0 && packetReceivedDelta == 0)
                {
                    return m_PacketLossCache.PacketLoss;
                }

                m_PacketLossCache.PacketsReceived = sharedContext->stats.PacketsReceived;
                m_PacketLossCache.PacketsDropped = sharedContext->stats.PacketsDropped;

                m_PacketLossCache.PacketLoss = packetReceivedDelta > 0 ? packetDroppedDelta / packetReceivedDelta : 0;

                return m_PacketLossCache.PacketLoss;
            }
        }

        private static unsafe ulong ParseClientId(NetworkConnection utpConnectionId)
        {
            return *(ulong*)&utpConnectionId;
        }

        private static unsafe NetworkConnection ParseClientId(ulong netcodeConnectionId)
        {
            return *(NetworkConnection*)&netcodeConnectionId;
        }

        private void ClearSendQueuesForClientId(ulong clientId)
        {
            // NativeList and manual foreach avoids any allocations.
            using var keys = new NativeList<SendTarget>(16, Allocator.Temp);
            foreach (var key in m_SendQueue.Keys)
            {
                if (key.ClientId == clientId)
                {
                    keys.Add(key);
                }
            }

            foreach (var target in keys)
            {
                m_SendQueue[target].Dispose();
                m_SendQueue.Remove(target);
            }
        }

        private void FlushSendQueuesForClientId(ulong clientId)
        {
            foreach (var kvp in m_SendQueue)
            {
                if (kvp.Key.ClientId == clientId)
                {
                    SendBatchedMessages(kvp.Key, kvp.Value);
                }
            }
        }

        /// <summary>
        /// Disconnects the local client from the remote
        /// </summary>
        public override void DisconnectLocalClient()
        {
            if (m_State == State.Connected)
            {
                FlushSendQueuesForClientId(m_ServerClientId);

                if (m_Driver.Disconnect(ParseClientId(m_ServerClientId)) == 0)
                {
                    m_State = State.Disconnected;

                    m_ReliableReceiveQueues.Remove(m_ServerClientId);
                    ClearSendQueuesForClientId(m_ServerClientId);

                    // If we successfully disconnect we dispatch a local disconnect message
                    // this how uNET and other transports worked and so this is just keeping with the old behavior
                    // should be also noted on the client this will call shutdown on the NetworkManager and the Transport
                    InvokeOnTransportEvent(NetcodeNetworkEvent.Disconnect,
                        m_ServerClientId,
                        default,
                        Time.realtimeSinceStartup);
                }
            }
        }

        /// <summary>
        /// Disconnects a remote client from the server
        /// </summary>
        /// <param name="clientId">The client to disconnect</param>
        public override void DisconnectRemoteClient(ulong clientId)
        {
            Debug.Assert(m_State == State.Listening, "DisconnectRemoteClient should be called on a listening server");

            if (m_State == State.Listening)
            {
                FlushSendQueuesForClientId(clientId);

                m_ReliableReceiveQueues.Remove(clientId);
                ClearSendQueuesForClientId(clientId);

                var connection = ParseClientId(clientId);
                if (m_Driver.GetConnectionState(connection) != NetworkConnection.State.Disconnected)
                {
                    m_Driver.Disconnect(connection);
                }
            }
        }

        /// <summary>
        /// Gets the current RTT for a specific client
        /// </summary>
        /// <param name="clientId">The client RTT to get</param>
        /// <returns>The RTT</returns>
        public override ulong GetCurrentRtt(ulong clientId)
        {
            // We don't know if this is getting called from inside NGO (which presumably knows to
            // use the transport client ID) or from a user (which will be using the NGO client ID).
            // So we just try both cases (ExtractRtt returns 0 for invalid connections).

            if (NetworkManager != null)
            {
                var transportId = NetworkManager.ClientIdToTransportId(clientId);

                var rtt = ExtractRtt(ParseClientId(transportId));
                if (rtt > 0)
                {
                    return (ulong)rtt;
                }
            }

            return (ulong)ExtractRtt(ParseClientId(clientId));
        }

        /// <summary>
        /// Initializes the transport
        /// </summary>
        /// <param name="networkManager">The NetworkManager that initialized and owns the transport</param>
        public override void Initialize(NetworkManager networkManager = null)
        {
            Debug.Assert(sizeof(ulong) == UnsafeUtility.SizeOf<NetworkConnection>(), "Netcode connection id size does not match UTP connection id size");

            NetworkManager = networkManager;

            m_NetworkSettings = new NetworkSettings(Allocator.Persistent);

#if !UNITY_WEBGL
            // If the user sends a message of exactly m_MaxPayloadSize in length, we need to
            // account for the overhead of its length when we store it in the send queue.
            var fragmentationCapacity = m_MaxPayloadSize + BatchedSendQueue.PerMessageOverhead;

            m_NetworkSettings
                .WithFragmentationStageParameters(payloadCapacity: fragmentationCapacity)
                .WithBaselibNetworkInterfaceParameters(
                    receiveQueueCapacity: m_MaxPacketQueueSize,
                    sendQueueCapacity: m_MaxPacketQueueSize);
#endif
        }

        /// <summary>
        /// Polls for incoming events, with an extra output parameter to report the precise time the event was received.
        /// </summary>
        /// <param name="clientId">The clientId this event is for</param>
        /// <param name="payload">The incoming data payload</param>
        /// <param name="receiveTime">The time the event was received, as reported by Time.realtimeSinceStartup.</param>
        /// <returns>Returns the event type</returns>
        public override NetcodeNetworkEvent PollEvent(out ulong clientId, out ArraySegment<byte> payload, out float receiveTime)
        {
            clientId = default;
            payload = default;
            receiveTime = default;
            return NetcodeNetworkEvent.Nothing;
        }

        /// <summary>
        /// Send a payload to the specified clientId, data and networkDelivery.
        /// </summary>
        /// <param name="clientId">The clientId to send to</param>
        /// <param name="payload">The data to send</param>
        /// <param name="networkDelivery">The delivery type (QoS) to send data with</param>
        public override void Send(ulong clientId, ArraySegment<byte> payload, NetworkDelivery networkDelivery)
        {
            var pipeline = SelectSendPipeline(networkDelivery);

            if (pipeline != m_ReliableSequencedPipeline && payload.Count > m_MaxPayloadSize)
            {
                Debug.LogError($"Unreliable payload of size {payload.Count} larger than configured 'Max Payload Size' ({m_MaxPayloadSize}).");
                return;
            }

            var sendTarget = new SendTarget(clientId, pipeline);
            if (!m_SendQueue.TryGetValue(sendTarget, out var queue))
            {
                queue = new BatchedSendQueue(Math.Max(m_MaxSendQueueSize, m_MaxPayloadSize));
                m_SendQueue.Add(sendTarget, queue);
            }

            if (!queue.PushMessage(payload))
            {
                if (pipeline == m_ReliableSequencedPipeline)
                {
                    // If the message is sent reliably, then we're over capacity and we can't
                    // provide any reliability guarantees anymore. Disconnect the client since at
                    // this point they're bound to become desynchronized.

                    var ngoClientId = NetworkManager?.TransportIdToClientId(clientId) ?? clientId;
                    Debug.LogError($"Couldn't add payload of size {payload.Count} to reliable send queue. " +
                        $"Closing connection {ngoClientId} as reliability guarantees can't be maintained. " +
                        $"Perhaps 'Max Send Queue Size' ({m_MaxSendQueueSize}) is too small for workload.");

                    if (clientId == m_ServerClientId)
                    {
                        DisconnectLocalClient();
                    }
                    else
                    {
                        DisconnectRemoteClient(clientId);

                        // DisconnectRemoteClient doesn't notify SDK of disconnection.
                        InvokeOnTransportEvent(NetcodeNetworkEvent.Disconnect,
                            clientId,
                            default(ArraySegment<byte>),
                            Time.realtimeSinceStartup);
                    }
                }
                else
                {
                    // If the message is sent unreliably, we can always just flush everything out
                    // to make space in the send queue. This is an expensive operation, but a user
                    // would need to send A LOT of unreliable traffic in one update to get here.

                    m_Driver.ScheduleFlushSend(default).Complete();
                    SendBatchedMessages(sendTarget, queue);

                    // Don't check for failure. If it still doesn't work, there's nothing we can do
                    // at this point and the message is lost (it was sent unreliable anyway).
                    queue.PushMessage(payload);
                }
            }
        }

        /// <summary>
        /// Connects client to the server
        /// Note:
        /// When this method returns false it could mean:
        /// - You are trying to start a client that is already started
        /// - It failed during the initial port binding when attempting to begin to connect
        /// </summary>
        /// <returns>true if the client was started and false if it failed to start the client</returns>
        public override bool StartClient()
        {
            if (m_Driver.IsCreated)
            {
                return false;
            }

            var succeeded = ClientBindAndConnect();
            if (!succeeded)
            {
                Shutdown();
            }
            return succeeded;
        }

        /// <summary>
        /// Starts to listening for incoming clients
        /// Note:
        /// When this method returns false it could mean:
        /// - You are trying to start a client that is already started
        /// - It failed during the initial port binding when attempting to begin to connect
        /// </summary>
        /// <returns>true if the server was started and false if it failed to start the server</returns>
        public override bool StartServer()
        {
            if (m_Driver.IsCreated)
            {
                return false;
            }

            bool succeeded;
            switch (m_ProtocolType)
            {
                case ProtocolType.UnityTransport:
                    succeeded = ServerBindAndListen(ConnectionData.ListenEndPoint);
                    if (!succeeded)
                    {
                        Shutdown();
                    }
                    return succeeded;
                case ProtocolType.RelayUnityTransport:
                    succeeded = StartRelayServer();
                    if (!succeeded)
                    {
                        Shutdown();
                    }
                    return succeeded;
                default:
                    return false;
            }
        }

        /// <summary>
        /// Shuts down the transport
        /// </summary>
        public override void Shutdown()
        {
            if (!m_Driver.IsCreated)
            {
                return;
            }

            // Flush all send queues to the network. NGO can be configured to flush its message
            // queue on shutdown. But this only calls the Send() method, which doesn't actually
            // get anything to the network.
            foreach (var kvp in m_SendQueue)
            {
                SendBatchedMessages(kvp.Key, kvp.Value);
            }

            // The above flush only puts the message in UTP internal buffers, need an update to
            // actually get the messages on the wire. (Normally a flush send would be sufficient,
            // but there might be disconnect messages and those require an update call.)
            m_Driver.ScheduleUpdate().Complete();

            DisposeInternals();

            m_ReliableReceiveQueues.Clear();

            // We must reset this to zero because UTP actually re-uses clientIds if there is a clean disconnect
            m_ServerClientId = 0;
        }

        private void ConfigureSimulator()
        {
            m_NetworkSettings.WithSimulatorStageParameters(
                maxPacketCount: 300, // TODO Is there any way to compute a better value?
                maxPacketSize: NetworkParameterConstants.MTU,
                packetDelayMs: DebugSimulator.PacketDelayMS,
                packetJitterMs: DebugSimulator.PacketJitterMS,
                packetDropPercentage: DebugSimulator.PacketDropRate
            );
        }

        /// <summary>
        /// Creates the internal NetworkDriver
        /// </summary>
        /// <param name="transport">The owner transport</param>
        /// <param name="driver">The driver</param>
        /// <param name="unreliableFragmentedPipeline">The UnreliableFragmented NetworkPipeline</param>
        /// <param name="unreliableSequencedFragmentedPipeline">The UnreliableSequencedFragmented NetworkPipeline</param>
        /// <param name="reliableSequencedPipeline">The ReliableSequenced NetworkPipeline</param>
        public void CreateDriver(UnityTransport transport, out NetworkDriver driver,
            out NetworkPipeline unreliableFragmentedPipeline,
            out NetworkPipeline unreliableSequencedFragmentedPipeline,
            out NetworkPipeline reliableSequencedPipeline)
        {
#if MULTIPLAYER_TOOLS_1_0_0_PRE_7
            NetworkPipelineStageCollection.RegisterPipelineStage(new NetworkMetricsPipelineStage());
#endif

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            ConfigureSimulator();
#endif

            m_NetworkSettings.WithNetworkConfigParameters(
                maxConnectAttempts: transport.m_MaxConnectAttempts,
                connectTimeoutMS: transport.m_ConnectTimeoutMS,
                disconnectTimeoutMS: transport.m_DisconnectTimeoutMS,
                heartbeatTimeoutMS: transport.m_HeartbeatTimeoutMS);

            driver = NetworkDriver.Create(m_NetworkSettings);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (DebugSimulator.PacketDelayMS > 0 || DebugSimulator.PacketDropRate > 0)
            {
                unreliableFragmentedPipeline = driver.CreatePipeline(
                    typeof(FragmentationPipelineStage),
                    typeof(SimulatorPipelineStage),
                    typeof(SimulatorPipelineStageInSend)
#if MULTIPLAYER_TOOLS_1_0_0_PRE_7
                    , typeof(NetworkMetricsPipelineStage)
#endif
                );
                unreliableSequencedFragmentedPipeline = driver.CreatePipeline(
                    typeof(FragmentationPipelineStage),
                    typeof(UnreliableSequencedPipelineStage),
                    typeof(SimulatorPipelineStage),
                    typeof(SimulatorPipelineStageInSend)
#if MULTIPLAYER_TOOLS_1_0_0_PRE_7
                    ,typeof(NetworkMetricsPipelineStage)
#endif
                );
                reliableSequencedPipeline = driver.CreatePipeline(
                    typeof(ReliableSequencedPipelineStage),
                    typeof(SimulatorPipelineStage),
                    typeof(SimulatorPipelineStageInSend)
#if MULTIPLAYER_TOOLS_1_0_0_PRE_7
                    ,typeof(NetworkMetricsPipelineStage)
#endif
                );
            }
            else
#endif
            {
                unreliableFragmentedPipeline = driver.CreatePipeline(
                    typeof(FragmentationPipelineStage)
#if MULTIPLAYER_TOOLS_1_0_0_PRE_7
                    ,typeof(NetworkMetricsPipelineStage)
#endif
                );
                unreliableSequencedFragmentedPipeline = driver.CreatePipeline(
                    typeof(FragmentationPipelineStage),
                    typeof(UnreliableSequencedPipelineStage)
#if MULTIPLAYER_TOOLS_1_0_0_PRE_7
                    ,typeof(NetworkMetricsPipelineStage)
#endif
                );
                reliableSequencedPipeline = driver.CreatePipeline(
                    typeof(ReliableSequencedPipelineStage)
#if MULTIPLAYER_TOOLS_1_0_0_PRE_7
                    ,typeof(NetworkMetricsPipelineStage)
#endif
                );
            }
        }

        // -------------- Utility Types -------------------------------------------------------------------------------


        /// <summary>
        /// Cached information about reliability mode with a certain client
        /// </summary>
        private struct SendTarget : IEquatable<SendTarget>
        {
            public readonly ulong ClientId;
            public readonly NetworkPipeline NetworkPipeline;

            public SendTarget(ulong clientId, NetworkPipeline networkPipeline)
            {
                ClientId = clientId;
                NetworkPipeline = networkPipeline;
            }

            public bool Equals(SendTarget other)
            {
                return ClientId == other.ClientId && NetworkPipeline.Equals(other.NetworkPipeline);
            }

            public override bool Equals(object obj)
            {
                return obj is SendTarget other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return (ClientId.GetHashCode() * 397) ^ NetworkPipeline.GetHashCode();
                }
            }
        }
    }
}
