// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Core.Models {
    /// <summary>
    /// Application type
    /// </summary>
    public enum ApplicationType {

        /// <summary>
        /// Application is server
        /// </summary>
        Server,

        /// <summary>
        /// Application is client
        /// </summary>
        Client,

        /// <summary>
        /// Application is client and server
        /// </summary>
        ClientAndServer,

        /// <summary>
        /// Application is discovery server
        /// </summary>
        DiscoveryServer
    }
}

