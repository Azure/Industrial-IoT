// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IoTSolutions.Shared.External.Models {
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using System.Collections.Generic;

    /// <summary>
    /// method metadata query model for webservice api
    /// </summary>
    public class MethodMetadataResponseApiModel {
        /// <summary>
        /// Input argument meta data
        /// </summary>
        [JsonProperty(PropertyName = "inputArgs")]
        public List<MethodArgumentApiModel> InputArguments { get; set; }

        /// <summary>
        /// output argument meta data
        /// </summary>
        [JsonProperty(PropertyName = "outputArgs")]
        public List<MethodArgumentApiModel> OutputArguments { get; set; }

        /// <summary>
        /// Optional error diagnostics
        /// </summary>
        [JsonProperty(PropertyName = "diagnostics",
            NullValueHandling = NullValueHandling.Ignore)]
        public JToken Diagnostics { get; set; }
    }
}
