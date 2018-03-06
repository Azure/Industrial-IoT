// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IoTSolutions.OpcTwin.Services.Models {
    using System.Collections.Generic;

    /// <summary>
    /// Server with list of endpoints
    /// </summary>
    public class ServerModel {

        /// <summary>
        /// Server information
        /// </summary>
        public ServerInfoModel Server { get; set; }

        /// <summary>
        /// List of endpoints
        /// </summary>
        public List<EndpointModel> Endpoints { get; set; }
    }
}
