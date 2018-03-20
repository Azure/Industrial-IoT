// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IoTSolutions.OpcTwin.EdgeService.v1.Models {
    using Microsoft.Azure.IoTSolutions.OpcTwin.Services.Models;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Endpoint with server info
    /// </summary>
    public class ApplicationApiModel {

        /// <summary>
        /// Default constructor
        /// </summary>
        public ApplicationApiModel() { }

        /// <summary>
        /// Create from service model
        /// </summary>
        /// <param name="model"></param>
        public ApplicationApiModel(ApplicationModel model) {
            Application = new ApplicationInfoApiModel(model?.Application);
            if (model?.Endpoints == null) {
                Endpoints = new List<EndpointApiModel>();
            }
            else {
                Endpoints = model.Endpoints
                    .Select(e => new EndpointApiModel(e))
                    .ToList();
            }
        }

        /// <summary>
        /// Server of the endpoint
        /// </summary>
        public ApplicationInfoApiModel Application { get; set; }

        /// <summary>
        /// Endoint validated
        /// </summary>
        public List<EndpointApiModel> Endpoints { get; set; }
    }
}
