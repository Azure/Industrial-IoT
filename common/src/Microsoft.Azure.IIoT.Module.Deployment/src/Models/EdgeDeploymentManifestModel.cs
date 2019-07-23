// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Module.Deployment.Models {
    using Newtonsoft.Json;
    using System.Collections.Generic;

    /// <summary>
    /// IotEdge deployment manifest
    /// </summary>
    public class EdgeDeploymentManifestModel {

        /// <summary>
        /// Modules to deploy
        /// </summary>
        [JsonProperty(PropertyName = "modules",
            NullValueHandling = NullValueHandling.Ignore)]
        public List<EdgeDeploymentModuleModel> Modules { get; set; }

        /// <summary>
        /// Routes to set
        /// </summary>
        [JsonProperty(PropertyName = "routes",
            NullValueHandling = NullValueHandling.Ignore)]
        public List<EdgeDeploymentRouteModel> Routes { get; set; }
    }
}
