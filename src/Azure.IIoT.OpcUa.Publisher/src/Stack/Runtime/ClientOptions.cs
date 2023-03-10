// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Stack
{
    /// <summary>
    /// Opc ua client options
    /// </summary>
    public sealed class ClientOptions
    {
        /// <summary>
        /// Application name
        /// </summary>
        public string ApplicationName { get; set; }

        /// <summary>
        /// Application uri
        /// </summary>
        public string ApplicationUri { get; set; }

        /// <summary>
        /// Product uri
        /// </summary>
        public string ProductUri { get; set; }

        /// <summary>
        /// Default session timeout in milliseconds.
        /// </summary>
        public uint DefaultSessionTimeout { get; set; }

        /// <summary>
        /// Keep alive interval in milliseconds.
        /// </summary>
        public int KeepAliveInterval { get; set; }

        /// <summary>
        /// Max keep alive count
        /// </summary>
        public uint MaxKeepAliveCount { get; set; }

        /// <summary>
        /// Minimum subscription lifetime in milliseconds.
        /// </summary>
        public int MinSubscriptionLifetime { get; set; }

        /// <summary>
        /// Transport quota
        /// </summary>
        public TransportOptions Quotas { get; } = new TransportOptions();

        /// <summary>
        /// Security configuration
        /// </summary>
        public SecurityOptions Security { get; } = new SecurityOptions();
    }
}
