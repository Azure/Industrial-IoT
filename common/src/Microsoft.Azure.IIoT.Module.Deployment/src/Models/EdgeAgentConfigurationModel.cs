// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Module.Deployment.Models {
    using Newtonsoft.Json;
    using System.Collections.Generic;

    /// <summary>
    /// Desired iotedge agent configuration
    /// </summary>
    public class EdgeAgentConfigurationModel {

        /// <summary>
        /// Schema version - set to "1.0"
        /// </summary>
        [JsonProperty(PropertyName = "schemaVersion",
            Required = Required.Always)]
        public string SchemaVersion { get; set; } = "1.0";

        /// <summary>
        /// Runtime configuration
        /// </summary>
        [JsonProperty(PropertyName = "runtime",
            Required = Required.Always)]
        public EdgeAgentRuntimeModel Runtime { get; set; }

        /// <summary>
        /// System Modules
        /// </summary>
        [JsonProperty(PropertyName = "systemModules",
            Required = Required.Always)]
        public Dictionary<string, EdgeAgentModuleModel> SystemModules { get; set; } =
            EdgeAgentModuleModelEx.DefaultSystemModules;

        /// <summary>
        /// User Modules
        /// </summary>
        [JsonProperty(PropertyName = "modules",
            NullValueHandling = NullValueHandling.Ignore)]
        public Dictionary<string, EdgeAgentModuleModel> Modules { get; set; }
    }
}
