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
                Endpoints = new List<TwinRegistrationApiModel>();
            }
            else {
                Endpoints = model.Endpoints
                    .Select(e => new TwinRegistrationApiModel(e))
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
        public List<TwinRegistrationApiModel> Endpoints { get; set; }

        /// <summary>
        /// Registration security assessment
        /// </summary>
        public SecurityAssessment? SecurityAssessment { get; set; }
    }
}
