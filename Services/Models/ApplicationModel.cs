// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IoTSolutions.OpcTwin.Services.Models {
    using System.Collections.Generic;

    /// <summary>
    /// Application with optional list of endpoints
    /// </summary>
    public class ApplicationModel {

        /// <summary>
        /// Application information
        /// </summary>
        public ApplicationInfoModel Application { get; set; }

        /// <summary>
        /// List of endpoints for it
        /// </summary>
        public List<EndpointModel> Endpoints { get; set; }
    }
}
