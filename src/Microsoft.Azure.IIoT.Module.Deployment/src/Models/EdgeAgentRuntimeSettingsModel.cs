// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Module.Deployment.Models {
    using Newtonsoft.Json;
    using System.Collections.Generic;

    /// <summary>
    /// IotEdge Agent runtime settings
    /// </summary>
    public class EdgeAgentRuntimeSettingsModel {

        /// <summary>
        /// Min docker version, e.g. "v1.25"
        /// </summary>
        [JsonProperty(PropertyName = "minDockerVersion",
            Required = Required.Always)]
        public string MinDockerVersion { get; set; } = "v1.25";

        /// <summary>
        /// Logging options
        /// </summary>
        [JsonProperty(PropertyName = "loggingOptions",
            NullValueHandling = NullValueHandling.Ignore)]
        public string LoggingOptions { get; set; }

        /// <summary>
        /// Registered registries if any
        /// </summary>
        [JsonProperty(PropertyName = "registryCredentials",
            NullValueHandling = NullValueHandling.Ignore)]
        public Dictionary<string, RegistryCredentialsModel> Registries { get; set; }
    }
}
