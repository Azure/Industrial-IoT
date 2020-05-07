// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Protocol {
    using Opc.Ua;
    using System.Collections.Generic;

    /// <summary>
    /// Create servers
    /// </summary>
    public interface IServerFactory {

        /// <summary>
        /// Create server and server configuration for hosting.
        /// </summary>
        /// <returns></returns>
        ApplicationConfiguration CreateServer(IEnumerable<int> ports,
            string pkiRootPath, out ServerBase server);
    }
}
