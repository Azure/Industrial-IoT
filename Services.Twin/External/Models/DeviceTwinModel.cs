// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IoTSolutions.OpcTwin.Services.External.Models {
    using System.Collections.Generic;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// Model of device registry / twin document
    /// </summary>
    public class DeviceTwinModel {

        /// <summary>
        /// Device id
        /// </summary>
        [JsonProperty(PropertyName = "Id")]
        public string Id { get; set; }

        /// <summary>
        /// Module id
        /// </summary>
        [JsonProperty(PropertyName = "ModuleId",
            NullValueHandling = NullValueHandling.Ignore)]
        public string ModuleId { get; set; }

        /// <summary>
        /// Etag for comparison
        /// </summary>
        [JsonProperty(PropertyName = "Etag",
            NullValueHandling = NullValueHandling.Ignore)]
        public string Etag { get; set; }

        /// <summary>
        /// Tags
        /// </summary>
        [JsonProperty(PropertyName = "Tags",
            NullValueHandling = NullValueHandling.Ignore)]
        public Dictionary<string, JToken> Tags { get; set; }

        /// <summary>
        /// Settings
        /// </summary>
        [JsonProperty(PropertyName = "Properties",
            NullValueHandling = NullValueHandling.Ignore)]
        public TwinPropertiesModel Properties { get; set; }
    }
}
