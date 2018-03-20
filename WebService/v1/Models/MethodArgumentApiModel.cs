// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IoTSolutions.OpcTwin.WebService.v1.Models {
    using Microsoft.Azure.IoTSolutions.OpcTwin.Services.Models;
    using Newtonsoft.Json;

    /// <summary>
    /// method arg model for webservice api
    /// </summary>
    public class MethodArgumentApiModel {

        /// <summary>
        /// Default constructor
        /// </summary>
        public MethodArgumentApiModel() {}

        /// <summary>
        /// Create from service model
        /// </summary>
        /// <param name="model"></param>
        public MethodArgumentApiModel(MethodArgumentModel model) {
            Value = model.Value;
            TypeId = model.TypeId;
            ValueRank = model.ValueRank;
            Name = model.Name;
            TypeName = model.TypeName;
            Description = model.Description;
            ArrayDimensions = model.ArrayDimensions;
        }

        /// <summary>
        /// Convert back to service model
        /// </summary>
        /// <returns></returns>
        public MethodArgumentModel ToServiceModel() {
            return new MethodArgumentModel {
                Value = Value,
                TypeId = TypeId,
                ValueRank = ValueRank,
                ArrayDimensions = ArrayDimensions,
                Description = Description,
                Name = Name,
                TypeName = TypeName
            };
        }

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
