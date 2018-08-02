// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Module.Deployment.Models {
    using Newtonsoft.Json;
    using System.Collections.Generic;

    /// <summary>
    /// Each module configuration is part of desired module agent
    /// configuration
    /// </summary>
    public class EdgeAgentModuleModel {

        /// <summary>
        /// Module version
        /// </summary>
        [JsonProperty(PropertyName = "version",
            NullValueHandling = NullValueHandling.Ignore)]
        public string Version { get; set; }

        /// <summary>
        /// Module type (default to docker)
        /// </summary>
        [JsonProperty(PropertyName = "type",
            Required = Required.Always)]
        public string Type { get; set; } = "docker";

        /// <summary>
        /// Desired status
        /// </summary>
        [JsonProperty(PropertyName = "status",
            NullValueHandling = NullValueHandling.Ignore)]
        public ModuleDesiredStatus? DesiredStatus { get; set; }

        /// <summary>
        /// Restart policy
        /// </summary>
        [JsonProperty(PropertyName = "restartPolicy",
            NullValueHandling = NullValueHandling.Ignore)]
        public ModuleRestartPolicy? RestartPolicy { get; set; }

        /// <summary>
        /// Environment settings
        /// </summary>
        [JsonProperty(PropertyName = "env",
            NullValueHandling = NullValueHandling.Ignore)]
        public IDictionary<string, EnvironmentVariableModel> Environment { get; set; }

        /// <summary>
        /// Module settings
        /// </summary>
        [JsonProperty(PropertyName = "settings")]
        public EdgeAgentModuleSettingsModel Settings { get; set; }
    }
}
