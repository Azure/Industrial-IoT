// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Services.OpcUa.Twin.v2.Models {
    using Microsoft.Azure.IIoT.OpcUa.Twin.Models;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using System;

    /// <summary>
    /// Method argument metadata model
    /// </summary>
    public class MethodMetadataArgumentApiModel {

        /// <summary>
        /// Default constructor
        /// </summary>
        public MethodMetadataArgumentApiModel() {}

        /// <summary>
        /// Create from service model
        /// </summary>
        /// <param name="model"></param>
        public MethodMetadataArgumentApiModel(MethodMetadataArgumentModel model) {
            if (model == null) {
                throw new ArgumentNullException(nameof(model));
            }
            DefaultValue = model.DefaultValue;
            Type = model.Type == null ? null :
                new NodeApiModel(model.Type);
            ValueRank = model.ValueRank;
            Name = model.Name;
            Description = model.Description;
            ArrayDimensions = model.ArrayDimensions;
        }

        /// <summary>
        /// Convert back to service model
        /// </summary>
        /// <returns></returns>
        public MethodMetadataArgumentModel ToServiceModel() {
            return new MethodMetadataArgumentModel {
                DefaultValue = DefaultValue,
                Type = Type?.ToServiceModel(),
                ValueRank = ValueRank,
                ArrayDimensions = ArrayDimensions,
                Description = Description,
                Name = Name
            };
        }

        /// <summary>
        /// Argument name
        /// </summary>
        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }

        /// <summary>
        /// Optional description
        /// </summary>
        [JsonProperty(PropertyName = "description",
            NullValueHandling = NullValueHandling.Ignore)]
        public string Description { get; set; }

        /// <summary>
        /// Data type node of the argument
        /// </summary>
        [JsonProperty(PropertyName = "type")]
        public NodeApiModel Type { get; set; }

        /// <summary>
        /// Default value
        /// </summary>
        [JsonProperty(PropertyName = "defaultValue",
            NullValueHandling = NullValueHandling.Ignore)]
        public JToken DefaultValue { get; set; }

        /// <summary>
        /// Optional, scalar if not set
        /// </summary>
        [JsonProperty(PropertyName = "valueRank",
            NullValueHandling = NullValueHandling.Ignore)]
        public NodeValueRank? ValueRank { get; set; }

        /// <summary>
        /// Optional, array dimension
        /// </summary>
        [JsonProperty(PropertyName = "arrayDimensions",
            NullValueHandling = NullValueHandling.Ignore)]
        public uint[] ArrayDimensions { get; set; }
    }
}
