// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Module.Deployment.Models {
    using Newtonsoft.Json;

    /// <summary>
    /// IotEdge deployment route
    /// </summary>
    public class EdgeDeploymentRouteModel {

        /// <summary>
        /// Name of the deployment
        /// </summary>
        [JsonProperty(PropertyName = "name",
            Required = Required.Always)]
        public string Name { get; set; }

        /// <summary>
        /// From where to route
        /// </summary>
        [JsonProperty(PropertyName = "from",
            NullValueHandling = NullValueHandling.Ignore)]
        public string From { get; set; }

        /// <summary>
        /// To where to route
        /// </summary>
        [JsonProperty(PropertyName = "to",
            NullValueHandling = NullValueHandling.Ignore)]
        public string To { get; set; }

        /// <summary>
        /// Optional condition
        /// </summary>
        [JsonProperty(PropertyName = "condition",
            NullValueHandling = NullValueHandling.Ignore)]
        public string Condition { get; set; }
    }
}
