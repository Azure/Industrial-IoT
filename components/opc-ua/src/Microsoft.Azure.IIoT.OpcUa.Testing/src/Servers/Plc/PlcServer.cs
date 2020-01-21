// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Plc {
    using Opc.Ua;
    using Opc.Ua.Server;

    /// <inheritdoc/>
    public class PlcServer : INodeManagerFactory {

        /// <inheritdoc/>
        public INodeManager CreateNodeManager(IServerInternal server,
            ApplicationConfiguration configuration) {
            return new PlcNodeManager(server, configuration);
        }
    }
}
