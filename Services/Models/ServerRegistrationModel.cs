// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IoTSolutions.OpcUaExplorer.Services.Models {

    /// <summary>
    /// Server registration request
    /// </summary>
    public class ServerRegistrationModel {

        /// <summary>
        /// Registered identifier of the server
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Endpoint information of the server to register
        /// </summary>
        public ServerEndpointModel Endpoint { get; set; }
    }
}
