// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Module.Deployment.Models {
    using Newtonsoft.Json;

    /// <summary>
    /// IotEdge Agent module settings
    /// </summary>
    public class EdgeAgentModuleSettingsModel {

        /// <summary>
        /// Image name
        /// </summary>
        [JsonProperty(PropertyName = "image",
            Required = Required.Always)]
        public string Image { get; set; }

        /// <summary>
        /// Create options
        /// </summary>
        [JsonProperty(PropertyName = "createOptions",
            NullValueHandling = NullValueHandling.Ignore)]
        public string CreateOptions { get; set; } = "";
    }
}
