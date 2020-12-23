// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Opc.Ua.Server {

    /// <summary>
    /// Create node managers
    /// </summary>
    public interface INodeManagerFactory {

        /// <summary>
        /// Create node manager
        /// </summary>
        /// <param name="server"></param>
        /// <param name="configuration"></param>
        /// <returns></returns>
        INodeManager CreateNodeManager(IServerInternal server,
            ApplicationConfiguration configuration);
    }
}
