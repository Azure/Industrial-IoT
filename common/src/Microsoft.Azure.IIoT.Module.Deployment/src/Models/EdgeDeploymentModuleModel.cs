// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Module.Deployment.Models {
    using Docker.DotNet.Models;
    using Newtonsoft.Json;
    using System.Collections.Generic;

    /// <summary>
    /// IotEdge deployment module
    /// </summary>
    public class EdgeDeploymentModuleModel {

        /// <summary>
        /// Name of the deployment
        /// </summary>
        [JsonProperty(PropertyName = "name",
            Required = Required.Always)]
        public string Name { get; set; }

        /// <summary>
        /// Name of the image
        /// </summary>
        [JsonProperty(PropertyName = "image",
            Required = Required.Always)]
        public string ImageName { get; set; }

        /// <summary>
        /// Version (optional)
        /// </summary>
        [JsonProperty(PropertyName = "version",
            NullValueHandling = NullValueHandling.Ignore)]
        public string Version { get; set; }

        /// <summary>
        /// Container create options
        /// </summary>
        [JsonProperty(PropertyName = "createOptions",
            NullValueHandling = NullValueHandling.Ignore)]
        public CreateContainerParameters CreateOptions { get; set; }

        /// <summary>
        /// Module restart policy
        /// </summary>
        [JsonProperty(PropertyName = "restartPolicy",
            NullValueHandling = NullValueHandling.Ignore)]
        public ModuleRestartPolicy? RestartPolicy { get; set; }

        /// <summary>
        /// Desired state should be stopped - default to running
        /// </summary>
        [JsonProperty(PropertyName = "stopped",
            NullValueHandling = NullValueHandling.Ignore)]
        public bool? Stopped { get; set; }

        /// <summary>
        /// Desired module settings
        /// </summary>
        [JsonProperty(PropertyName = "properties",
            NullValueHandling = NullValueHandling.Ignore)]
        public Dictionary<string, dynamic> Properties { get; set; }
    }
}
