// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Modules.OpcUa.Twin.v1.Models {
    using Microsoft.Azure.IIoT.OpcUa.Registry.Models;
    using Newtonsoft.Json;
    using System;
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
            if (model == null) {
                throw new ArgumentNullException(nameof(model));
            }
            Application = model.Application == null ? null :
                new ApplicationInfoApiModel(model.Application);
            SecurityAssessment = model.SecurityAssessment;
            Endpoints = model.Endpoints?
                .Select(e => e == null ? null : new EndpointRegistrationApiModel(e))
                .ToList();
        }

        /// <summary>
        /// Convert back to service model
        /// </summary>
        /// <returns></returns>
        public ApplicationRegistrationModel ToServiceModel() {
            return new ApplicationRegistrationModel {
                Application = Application?.ToServiceModel(),
                SecurityAssessment = SecurityAssessment,
                Endpoints = (Endpoints?.Count ?? 0) == 0 ? null :
                    Endpoints.Select(e => e?.ToServiceModel()).ToList()
            };
        }

        /// <summary>
        /// Application information
        /// </summary>
        [JsonProperty(PropertyName = "Application")]
        public ApplicationInfoApiModel Application { get; set; }

        /// <summary>
        /// List of endpoint twins
        /// </summary>
        [JsonProperty(PropertyName = "Endpoints",
            NullValueHandling = NullValueHandling.Ignore)]
        public List<EndpointRegistrationApiModel> Endpoints { get; set; }

        /// <summary>
        /// Application security assessment
        /// </summary>
        [JsonProperty(PropertyName = "SecurityAssessment",
            NullValueHandling = NullValueHandling.Ignore)]
        public SecurityAssessment? SecurityAssessment { get; set; }
    }
}
