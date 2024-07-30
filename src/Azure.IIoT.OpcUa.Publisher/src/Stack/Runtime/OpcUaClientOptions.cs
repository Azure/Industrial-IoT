// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Stack
{
    using System;

    /// <summary>
    /// Opc ua client options
    /// </summary>
    public sealed class OpcUaClientOptions
    {
        /// <summary>
        /// Application name
        /// </summary>
        public string? ApplicationName { get; set; }

        /// <summary>
        /// Application uri
        /// </summary>
        public string? ApplicationUri { get; set; }

        /// <summary>
        /// Product uri
        /// </summary>
        public string? ProductUri { get; set; }

        /// <summary>
        /// Default session timeout. This is the timoout used to
        /// establish a session with the server.
        /// </summary>
        public TimeSpan? DefaultSessionTimeoutDuration { get; set; }

        /// <summary>
        /// Default service call timeout duration. If the service
        /// call timeout is not specified in the request header and
        /// the this value is used.
        /// </summary>
        public TimeSpan? DefaultServiceCallTimeoutDuration { get; set; }

        /// <summary>
        /// Default connect timeout duration. If the connect timeout
        /// is not specified in the request header this value is used.
        /// If not specified the default service call timeout is used.
        /// </summary>
        public TimeSpan? DefaultConnectTimeoutDuration { get; set; }

        /// <summary>
        /// Keep alive interval. The client will send keep alives
        /// to the server at this interval and expect a response
        /// or initiate a session recovery / reconnect sequence.
        /// </summary>
        public TimeSpan? KeepAliveIntervalDuration { get; set; }

        /// <summary>
        /// How long to wait until connected or until
        /// reconnecting is attempted.
        /// </summary>
        public TimeSpan? CreateSessionTimeoutDuration { get; set; }

        /// <summary>
        /// Reverse connect port to use other than the
        /// default port 4840.
        /// </summary>
        public int? ReverseConnectPort { get; set; }

        /// <summary>
        /// Disable complex type preloading. The type system
        /// will still be lazily loaded when requested e.g.,
        /// during subscription creation.
        /// </summary>
        public bool? DisableComplexTypePreloading { get; set; }

        /// <summary>
        /// How long to at least wait until reconnecting.
        /// </summary>
        public TimeSpan? MinReconnectDelayDuration { get; set; }

        /// <summary>
        /// How long to at most wait until reconnecting.
        /// </summary>
        public TimeSpan? MaxReconnectDelayDuration { get; set; }

        /// <summary>
        /// How long to keep clients around after a service call.
        /// </summary>
        public TimeSpan? LingerTimeoutDuration { get; set; }

        /// <summary>
        /// How long to wait until retrying on errors related
        /// to creating and modifying the subscription.
        /// </summary>
        public TimeSpan? SubscriptionErrorRetryDelay { get; set; }

        /// <summary>
        /// The watchdog period to kick off regular management
        /// of the subscription and reapply any state on failed
        /// nodes.
        /// </summary>
        public TimeSpan? SubscriptionManagementIntervalDuration { get; set; }

        /// <summary>
        /// At what interval should bad monitored items be retried.
        /// These are items that have been rejected by the server
        /// during subscription update or never successfully
        /// published.
        /// </summary>
        public TimeSpan? BadMonitoredItemRetryDelayDuration { get; set; }

        /// <summary>
        /// At what interval should invalid monitored items be
        /// retried. These are items that are potentially
        /// misconfigured.
        /// </summary>
        public TimeSpan? InvalidMonitoredItemRetryDelayDuration { get; set; }

        /// <summary>
        /// Transport quota
        /// </summary>
        public TransportOptions Quotas { get; } = new TransportOptions();

        /// <summary>
        /// Security configuration
        /// </summary>
        public SecurityOptions Security { get; } = new SecurityOptions();

        /// <summary>
        /// Enable traces in the stack beyond errors
        /// </summary>
        public bool? EnableOpcUaStackLogging { get; set; }

        /// <summary>
        /// Folder to write keysets to for later decryption
        /// of wireshark traces.
        /// </summary>
        public string? OpcUaKeySetLogFolderName { get; set; }

        /// <summary>
        /// Minimum number of publish requests to queue
        /// at all times. Default is 2.
        /// </summary>
        public int? MinPublishRequests { get; set; }

        /// <summary>
        /// The publish requests per subscription factor in
        /// percent, e.g., 120% means 1.2 requests per
        /// subscription. Use this to control network latency
        /// </summary>
        public int? PublishRequestsPerSubscriptionPercent { get; set; }

        /// <summary>
        /// Max number of publish requests to queue
        /// at all times. Default is 15.
        /// </summary>
        public int? MaxPublishRequests { get; set; }

        /// <summary>
        /// Limit max nodes to read in a batch operation
        /// </summary>
        public int? MaxNodesPerReadOverride { get; set; }

        /// <summary>
        /// Limit max nodes to browse in a batch operation
        /// </summary>
        public int? MaxNodesPerBrowseOverride { get; set; }

        /// <summary>
        /// Manage the connectivity of the session and state
        /// actively when publishing errors occur that are
        /// related to session connectivity.
        /// </summary>
        public bool? ActivePublishErrorHandling { get; set; }

        /// <summary>
        /// Dump diagnostics period
        /// </summary>
        public TimeSpan? DiagnosticsInterval { get; set; }
    }
}
