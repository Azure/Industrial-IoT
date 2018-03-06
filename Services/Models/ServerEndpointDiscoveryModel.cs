// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IoTSolutions.OpcTwin.Services.Models {
    using System;

    /// <summary>
    /// Endpoint with server info when discovered
    /// </summary>
    public class ServerEndpointDiscoveryModel {

        /// <summary>
        /// Discovered Server endpoint
        /// </summary>
        public ServerEndpointModel ServerEndpoint { get; set; }

        /// <summary>
        /// Timestamp of the discovery sweep
        /// </summary>
        public DateTime TimeStamp { get; set; }

        /// <summary>
        /// Index in batch with timestamp
        /// </summary>
        public int Index { get; set; }
    }
}
