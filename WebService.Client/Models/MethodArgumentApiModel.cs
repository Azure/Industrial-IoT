// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IoTSolutions.OpcTwin.WebService.Client.Models {
    using Newtonsoft.Json;

    /// <summary>
    /// method arg model for webservice api
    /// </summary>
    public class MethodArgumentApiModel {

        /// <summary>
        /// Initial value or value to use
        /// </summary>
        [JsonProperty(PropertyName = "value")]
        public string Value { get; set; }

        /// <summary>
        /// Data type of the value
        /// </summary>
        [JsonProperty(PropertyName = "datatype")]
        public string TypeId { get; set; }

        /// <summary>
        /// Optional, scalar if not set
        /// </summary>
        [JsonProperty(PropertyName = "valueRank",
            NullValueHandling = NullValueHandling.Ignore)]
        public int? ValueRank { get; set; }

        /// <summary>
        /// Optional, array dimension
        /// </summary>
        [JsonProperty(PropertyName = "arrayDimensions",
            NullValueHandling = NullValueHandling.Ignore)]
        public uint[] ArrayDimensions { get; set; }

        /// <summary>
        /// Optional, argument name
        /// </summary>
        [JsonProperty(PropertyName = "name",
            NullValueHandling = NullValueHandling.Ignore)]
        public string Name { get; set; }

        /// <summary>
        /// Optional, type name
        /// </summary>
        [JsonProperty(PropertyName = "typeName",
            NullValueHandling = NullValueHandling.Ignore)]
        public string TypeName { get; set; }

        /// <summary>
        /// Optional, description
        /// </summary>
        [JsonProperty(PropertyName = "description",
            NullValueHandling = NullValueHandling.Ignore)]
        public string Description { get; set; }
    }
}
