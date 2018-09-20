// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Models {
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using System.Collections.Generic;

    /// <summary>
    /// Method metadata query model
    /// </summary>
    public class MethodMetadataResponseApiModel {

        /// <summary>
        /// Id of object that the method is a component of
        /// </summary>
        [JsonProperty(PropertyName = "objectId",
            NullValueHandling = NullValueHandling.Ignore)]
        public string ObjectId { get; set; }

        /// <summary>
        /// Input argument meta data
        /// </summary>
        [JsonProperty(PropertyName = "inputArguments",
            NullValueHandling = NullValueHandling.Ignore)]
        public List<MethodMetadataArgumentApiModel> InputArguments { get; set; }

        /// <summary>
        /// output argument meta data
        /// </summary>
        [JsonProperty(PropertyName = "outputArguments",
            NullValueHandling = NullValueHandling.Ignore)]
        public List<MethodMetadataArgumentApiModel> OutputArguments { get; set; }

        /// <summary>
        /// Optional error diagnostics
        /// </summary>
        [JsonProperty(PropertyName = "diagnostics",
            NullValueHandling = NullValueHandling.Ignore)]
        public JToken Diagnostics { get; set; }
    }
}
