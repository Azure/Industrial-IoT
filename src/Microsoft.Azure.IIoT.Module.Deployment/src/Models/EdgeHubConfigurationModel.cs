// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Module.Deployment.Models {
    using Newtonsoft.Json;
    using System.Collections.Generic;

    /// <summary>
    /// Desired iotedge hub configuration
    /// </summary>
    public class EdgeHubConfigurationModel {

        /// <summary>
        /// Schema version - set to "1.0"
        /// </summary>
        [JsonProperty(PropertyName = "schemaVersion",
            Required = Required.Always)]
        public string SchemaVersion { get; set; } = "1.0";

        /// <summary>
        /// Routes configuration
        /// </summary>
        [JsonProperty(PropertyName = "routes",
            Required = Required.Always)]
        public Dictionary<string, string> Routes { get; set; }

        /// <summary>
        /// Store and forward configuration
        /// </summary>
        [JsonProperty(PropertyName = "storeAndForwardConfiguration",
            NullValueHandling = NullValueHandling.Ignore)]
        public EdgeHubStoreAndForwardModel StorageConfig { get; set; }
    }
}
