// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Counter
{
    using Opc.Ua;
    using Opc.Ua.Server;

    /// <summary>
    /// Node manager factory for the deterministic counter server.
    /// </summary>
    public sealed class CounterServer : INodeManagerFactory
    {
        /// <inheritdoc/>
        public ArrayOf<string> NamespacesUris => [Namespaces.Counter];

        /// <summary>
        /// The node manager that was created, if any. Tests use it to
        /// correlate the observed telemetry with what the server actually
        /// produced.
        /// </summary>
        public CounterNodeManager NodeManager { get; private set; }

        /// <summary>
        /// Create factory
        /// </summary>
        /// <param name="options"></param>
        public CounterServer(CounterServerOptions options = null)
        {
            _options = options ?? new CounterServerOptions();
        }

        /// <inheritdoc/>
        public INodeManager Create(IServerInternal server,
            ApplicationConfiguration configuration)
        {
            var nodeManager = new CounterNodeManager(server, configuration, _options);
            NodeManager = nodeManager;
            return nodeManager;
        }

        private readonly CounterServerOptions _options;
    }
}
