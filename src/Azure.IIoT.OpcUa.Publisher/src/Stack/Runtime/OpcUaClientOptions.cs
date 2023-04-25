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
        /// How long to keep clients around after a service call.
        /// </summary>
        public TimeSpan? LingerTimeout { get; set; }

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
