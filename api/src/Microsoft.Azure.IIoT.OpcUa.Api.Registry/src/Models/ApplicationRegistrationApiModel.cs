// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Registry.Models {
    using Newtonsoft.Json;
    using System.Collections.Generic;

    /// <summary>
    /// Application with optional list of endpoints
    /// </summary>
    public class ApplicationRegistrationApiModel {

        /// <summary>
        /// Server information
        /// </summary>
        [JsonProperty(PropertyName = "application")]
        public ApplicationInfoApiModel Application { get; set; }

        /// <summary>
        /// List of endpoints for the application
        /// </summary>
        [JsonProperty(PropertyName = "endpoints",
            NullValueHandling = NullValueHandling.Ignore)]
        public List<EndpointRegistrationApiModel> Endpoints { get; set; }

        /// <summary>
        /// Application security assessment
        /// </summary>
        [JsonProperty(PropertyName = "securityAssessment",
            NullValueHandling = NullValueHandling.Ignore)]
        public SecurityAssessment? SecurityAssessment { get; set; }
    }
}
