// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Opc.Ua.Client
{
    using Polly;
    using System;

    /// <summary>
    /// Client options
    /// </summary>
    public record class ClientOptions
    {
        /// <summary>
        /// Reverse connect port to use. The default reverse connect
        /// port is 4840. If the reverse connect port is set to null,
        /// Reverse connect will not be used.
        /// </summary>
        public int? ReverseConnectPort { get; set; }

        /// <summary>
        /// Max number of sessions that can be held through the
        /// connection pool before a session is disposed.
        /// </summary>
        public int MaxPooledSessions { get; set; } = 100;

        /// <summary>
        /// The length of time a session should linger in the session
        /// pool without being used before it is disposed.
        /// </summary>
        public TimeSpan LingerTimeout { get; set; } = TimeSpan.FromMinutes(1);

        /// <summary>
        /// Connect policy to use. The resiliency pipelines will
        /// be used to reconnect the session when the connection
        /// is determined lost. Use rate limiting to limit the
        /// number of reconnects across the entire client.
        /// </summary>
        public ResiliencePipeline? ConnectStrategy { get; init; }

        /// <summary>
        /// Update the application configuration from the certificate
        /// found in the own folder.  This is useful when the application
        /// was configured externally without updating the application
        /// configuration.
        /// </summary>
        public bool UpdateApplicationFromExistingCert { get; internal set; }

        /// <summary>
        /// Host name override to use when accessing the client host
        /// name is not possible or yields wrong results.
        /// </summary>
        public string? HostName { get; init; }
    }
}
