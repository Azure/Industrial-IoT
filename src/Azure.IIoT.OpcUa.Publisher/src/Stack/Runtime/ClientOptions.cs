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
        public TimeSpan? DefaultSessionTimeout { get; set; }

        /// <summary>
        /// Keep alive interval in milliseconds.
        /// </summary>
        public TimeSpan? KeepAliveInterval { get; set; }

        /// <summary>
        /// How long in milliseconds to wait until
        /// reconnecting is attempted.
        /// </summary>
        public TimeSpan? ReconnectRetryDelay { get; set; }

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
