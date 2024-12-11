// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Stack
{
    using Azure.IIoT.OpcUa.Publisher.Models;

    /// <summary>
    /// Safely access client state for diagnostics
    /// </summary>
    public interface IOpcUaClientDiagnostics
    {
        /// <summary>
        /// Bad publish requests tracked by this client
        /// </summary>
        int BadPublishRequestCount { get; }

        /// <summary>
        /// Good publish requests tracked by this client
        /// </summary>
        int GoodPublishRequestCount { get; }

        /// <summary>
        /// Outstanding requests
        /// </summary>
        int OutstandingRequestCount { get; }

        /// <summary>
        /// Number of subscriptions tracked by client
        /// </summary>
        int SubscriptionCount { get; }

        /// <summary>
        /// Connectivity state
        /// </summary>
        EndpointConnectivityState State { get; }

        /// <summary>
        /// Total connection attempts
        /// </summary>
        int ReconnectCount { get; }

        /// <summary>
        /// Total successful connections
        /// </summary>
        int ConnectCount { get; }

        /// <summary>
        /// Current min publish request count
        /// </summary>
        int MinPublishRequestCount { get; }
    }
}
