// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Stack
{
    using Opc.Ua;
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Create servers
    /// </summary>
    public interface IServerFactory
    {
        /// <summary>
        /// Create server and server configuration for hosting.
        /// </summary>
        /// <param name="ports"></param>
        /// <param name="pkiRootPath"></param>
        /// <param name="server"></param>
        /// <param name="listenHostName"></param>
        /// <param name="alternativeAddresses"></param>
        /// <param name="path"></param>
        /// <param name="certStoreType"></param>
        /// <param name="configure"></param>
        /// <returns></returns>
        ApplicationConfiguration CreateServer(IEnumerable<int> ports,
            string pkiRootPath, out ServerBase server, string listenHostName = null,
            IEnumerable<string> alternativeAddresses = null, string path = null,
            string certStoreType = null, Action<ServerConfiguration> configure = null);
    }
}
