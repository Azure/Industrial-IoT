// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Protocol {

    /// <summary>
    /// Client services configuration
    /// </summary>
    public interface IClientServicesConfig : ITransportQuotaConfig, ISecurityConfig {

        /// <summary>
        /// Application name
        /// </summary>
        string ApplicationName { get; }

        /// <summary>
        /// Application uri
        /// </summary>
        string ApplicationUri { get; }

        /// <summary>
        /// Product uri
        /// </summary>
        string ProductUri { get; }

        /// <summary>
        /// Default session timeout in milliseconds.
        /// </summary>
        uint DefaultSessionTimeout { get; }

        /// <summary>
        /// Keep alive interval in milliseconds.
        /// </summary>
        int KeepAliveInterval { get; }

        /// <summary>
        /// Max keep alive count
        /// </summary>
        uint MaxKeepAliveCount { get; }

        /// <summary>
        /// Minimum subscription lifetime in milliseconds.
        /// </summary>
        int MinSubscriptionLifetime { get; }
    }
}