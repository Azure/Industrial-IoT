using System;
using System.Collections.Generic;

namespace OpcPublisher
{
    using Opc.Ua;
    using OpcPublisher.Crypto;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;
    using static OpcPublisher.OpcSession;

    /// <summary>
    /// Class to manage OPC sessions.
    /// </summary>
    public interface IOpcSession : IDisposable
    {
        /// <summary>
        /// The endpoint to connect to for the session.
        /// </summary>
        string EndpointUrl { get; set; }

        /// <summary>
        /// The encrypted credential for authentication against the OPC UA Server. This is only used, when <see cref="OpcAuthenticationMode"/> is set to "UsernamePassword".
        /// </summary>
        EncryptedNetworkCredential EncryptedAuthCredential { get; set; }

        /// <summary>
        /// The authentication mode to use for authentication against the OPC UA Server.
        /// </summary>
        OpcAuthenticationMode OpcAuthenticationMode { get; set; }

        /// <summary>
        /// The OPC UA stack session object of the session.
        /// </summary>
        IOpcUaSession OpcUaClientSession { get; set; }

        /// <summary>
        /// The state of the session.
        /// </summary>
        SessionState State { get; set; }

        /// <summary>
        /// The subscriptions on this session.
        /// </summary>
        List<IOpcSubscription> OpcSubscriptions { get; }

        /// <summary>
        /// Counts session connection attempts which were unsuccessful.
        /// </summary>
        uint UnsuccessfulConnectionCount { get; set; }

        /// <summary>
        /// Counts missed keep alives.
        /// </summary>
        uint MissedKeepAlives { get; set; }

        /// <summary>
        /// The default publishing interval to use on this session.
        /// </summary>
        int PublishingInterval { get; set; }

        /// <summary>
        /// The OPC UA timeout setting to use for the OPC UA session.
        /// </summary>
        uint SessionTimeout { get; }

        /// <summary>
        /// Flag to control if a secure or unsecure OPC UA transport should be used for the session.
        /// </summary>
        bool UseSecurity { get; set; }

        /// <summary>
        /// Signals to run the connect and monitor task.
        /// </summary>
        AutoResetEvent ConnectAndMonitorSession { get; set; }

        /// <summary>
        /// Number of subscirptoins on this session.
        /// </summary>
        /// <returns></returns>
        int GetNumberOfOpcSubscriptions();

        /// <summary>
        /// Number of configured monitored items on this session.
        /// </summary>
        /// <returns></returns>
        int GetNumberOfOpcMonitoredItemsConfigured();


        /// <summary>
        /// Number of actually monitored items on this sessions.
        /// </summary>
        /// <returns></returns>
        int GetNumberOfOpcMonitoredItemsMonitored();

        /// <summary>
        /// Number of monitored items to be removed from this session.
        /// </summary>
        /// <returns></returns>
        int GetNumberOfOpcMonitoredItemsToRemove();

        /// <summary>
        /// This task is started when a session is configured and is running till session shutdown and ensures:
        /// - disconnected sessions are reconnected.
        /// - monitored nodes are no longer monitored if requested to do so.
        /// - monitoring for a node starts if it is required.
        /// - unused subscriptions (without any nodes to monitor) are removed.
        /// - sessions with out subscriptions are removed.
        /// </summary>
        Task ConnectAndMonitorAsync();

        /// <summary>
        /// Connects the session if it is disconnected.
        /// </summary>
        Task ConnectSessionAsync(CancellationToken ct);

        /// <summary>
        /// Monitoring for a node starts if it is required.
        /// </summary>
        Task MonitorNodesAsync(CancellationToken ct);

        /// <summary>
        /// Checks if there are monitored nodes tagged to stop monitoring.
        /// </summary>
        Task StopMonitoringNodesAsync(CancellationToken ct);

        /// <summary>
        /// Checks if there are subscriptions without any monitored items and remove them.
        /// </summary>
        Task RemoveUnusedSubscriptionsAsync(CancellationToken ct);

        /// <summary>
        /// Checks if there are session without any subscriptions and remove them.
        /// </summary>
        Task RemoveUnusedSessionsAsync(CancellationToken ct);

        /// <summary>
        /// Disconnects a session and removes all subscriptions on it and marks all nodes on those subscriptions
        /// as unmonitored.
        /// </summary>
        Task DisconnectAsync();

        /// <summary>
        /// Returns the namespace index for a namespace URI.
        /// </summary>
        int GetNamespaceIndexUnlocked(string namespaceUri);

        /// <summary>
        /// Adds a node to be monitored. If there is no subscription with the requested publishing interval,
        /// one is created.
        /// </summary>
        Task<HttpStatusCode> AddNodeForMonitoringAsync(NodeId nodeId, ExpandedNodeId expandedNodeId,
            int? opcPublishingInterval, int? opcSamplingInterval, string displayName,
            int? heartbeatInterval, bool? skipFirst, CancellationToken ct);

        /// <summary>
        /// Tags a monitored node to stop monitoring and remove it.
        /// </summary>
        Task<HttpStatusCode> RequestMonitorItemRemovalAsync(NodeId nodeId, ExpandedNodeId expandedNodeId, CancellationToken ct, bool takeLock = true);

        /// <summary>
        /// Checks if the node specified by either the given NodeId or ExpandedNodeId on the given endpoint is published in the session.
        /// </summary>
        bool IsNodePublishedInSession(NodeId nodeId, ExpandedNodeId expandedNodeId);

        /// <summary>
        /// Shutdown the current session if it is connected.
        /// </summary>
        Task ShutdownAsync();

        /// <summary>
        /// Take the session semaphore.
        /// </summary>
        Task<bool> LockSessionAsync();

        /// <summary>
        /// Release the session semaphore.
        /// </summary>
        void ReleaseSession();
        Task Reconnect();
    }
}
