// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IoTSolutions.OpcTwin.WebService.v1.Models {
    using Microsoft.Azure.IoTSolutions.OpcTwin.Services.Models;
    using Newtonsoft.Json;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.ComponentModel.DataAnnotations;
    using System.Linq;

    /// <summary>
    /// Application with list of endpoints
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
            Application = new ApplicationInfoApiModel(model.Application);
            SecurityAssessment = model.SecurityAssessment;
            if (model.Endpoints != null) {
                Endpoints = model.Endpoints
                    .Select(s => new TwinRegistrationApiModel(s))
                    .ToList();
            }
            else {
                Endpoints = new List<TwinRegistrationApiModel>();
            }
        }

        /// <summary>
        /// Convert back to service model
        /// </summary>
        /// <returns></returns>
        public ApplicationRegistrationModel ToServiceModel() {
            return new ApplicationRegistrationModel {
                Application = Application.ToServiceModel(),
                SecurityAssessment = SecurityAssessment,
                Endpoints = Endpoints.Count == 0 ? null :
                    Endpoints.Select(e => e.ToServiceModel()).ToList()
            };
        }

        /// <summary>
        /// Application information
        /// </summary>
        [JsonProperty(PropertyName = "application")]
        [Required]
        public ApplicationInfoApiModel Application { get; set; }

        /// <summary>
        /// List of endpoint twins
        /// </summary>
        [JsonProperty(PropertyName = "endpoints",
            NullValueHandling = NullValueHandling.Ignore)]
        [DefaultValue(null)]
        public List<TwinRegistrationApiModel> Endpoints { get; set; }

        /// <summary>
        /// Application security assessment
        /// </summary>
        [JsonProperty(PropertyName = "securityAssessment",
            NullValueHandling = NullValueHandling.Ignore)]
        [DefaultValue(null)]
        public SecurityAssessment? SecurityAssessment { get; set; }
    }
}
