// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Modules.Twin.v1.Models {
    using Microsoft.Azure.IIoT.OpcUa.Registry.Models;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Endpoint with server info
    /// </summary>
    public class ApplicationRegistrationApiModel {

        /// <summary>
        /// Default constructor
        /// </summary>
        public ApplicationRegistrationApiModel() { }

        /// <summary>
        /// Create from service model
        /// </summary>
        /// <param name="model"></param>
        public ApplicationRegistrationApiModel(ApplicationRegistrationModel model) {
            Application = new ApplicationInfoApiModel(model?.Application);
            SecurityAssessment = model.SecurityAssessment;
            if (model?.Endpoints == null) {
                Endpoints = new List<EndpointRegistrationApiModel>();
            }
            else {
                Endpoints = model.Endpoints
                    .Select(e => new EndpointRegistrationApiModel(e))
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
        public List<EndpointRegistrationApiModel> Endpoints { get; set; }

        /// <summary>
        /// Registration security assessment
        /// </summary>
        public SecurityAssessment? SecurityAssessment { get; set; }
    }
}
