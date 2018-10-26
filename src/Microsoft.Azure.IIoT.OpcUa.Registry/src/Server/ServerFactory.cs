// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Registry.Server {
    using Microsoft.Azure.IIoT.OpcUa.Protocol;
    using Microsoft.Azure.IIoT.Diagnostics;
    using Opc.Ua;
    using System.Collections.Generic;

    /// <summary>
    /// Registry server factory
    /// </summary>
    public class ServerFactory : IServerFactory {

        /// <summary>
        /// Whether to log status
        /// </summary>
        public bool LogStatus { get; set; }

        /// <summary>
        /// Server factory
        /// </summary>
        /// <param name="logger"></param>
        public ServerFactory(ILogger logger) {
            _logger = logger;
        }

        /// <inheritdoc/>
        public ApplicationConfiguration CreateServer(IEnumerable<int> ports,
            out ServerBase server) {
            server = new DiscoveryServer(LogStatus, _logger);
            return DiscoveryServer.CreateServerConfiguration(ports);
        }

        private readonly ILogger _logger;
    }
}
