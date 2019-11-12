// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Module.Deployment.Models {
    using Newtonsoft.Json;

    /// <summary>
    /// Each runtime configuration is part of desired module agent
    /// configuration
    /// </summary>
    public class EdgeAgentRuntimeModel {

        /// <summary>
        /// Runtime type (default to docker)
        /// </summary>
        [JsonProperty(PropertyName = "type",
            Required = Required.Always)]
        public string Type { get; set; } = "docker";

        /// <summary>
        /// Module settings
        /// </summary>
        [JsonProperty(PropertyName = "settings",
            NullValueHandling = NullValueHandling.Ignore)]
        public EdgeAgentRuntimeSettingsModel Settings { get; set; }
    }
}
