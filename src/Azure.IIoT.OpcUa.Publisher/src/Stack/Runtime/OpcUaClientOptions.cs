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
        /// Default session timeout.
        /// </summary>
        public TimeSpan? DefaultSessionTimeout { get; set; }

        /// <summary>
        /// Keep alive interval.
        /// </summary>
        public TimeSpan? KeepAliveInterval { get; set; }

        /// <summary>
        /// How long to wait until connected or until
        /// reconnecting is attempted.
        /// </summary>
        public TimeSpan? CreateSessionTimeout { get; set; }

        /// <summary>
        /// How long to at least wait until reconnecting.
        /// </summary>
        public TimeSpan? MinReconnectDelay { get; set; }

        /// <summary>
        /// How long to at most wait until reconnecting.
        /// </summary>
        public TimeSpan? MaxReconnectDelay { get; set; }

        /// <summary>
        /// How long to keep clients around after a service call.
        /// </summary>
        public TimeSpan? LingerTimeout { get; set; }

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
        public TimeSpan? SubscriptionManagementInterval { get; set; }

        /// <summary>
        /// At what interval should bad monitored items be retried.
        /// These are items that have been rejected by the server
        /// during subscription update or never successfully
        /// published.
        /// </summary>
        public TimeSpan? BadMonitoredItemRetryDelay { get; set; }

        /// <summary>
        /// At what interval should invalid monitored items be
        /// retried. These are items that are potentially
        /// misconfigured.
        /// </summary>
        public TimeSpan? InvalidMonitoredItemRetryDelay { get; set; }

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
    }
}
