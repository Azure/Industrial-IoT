// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Modules.OpcUa.Publisher.v2.Models {
    using Microsoft.Azure.IIoT.OpcUa.Twin.Models;
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Structure field
    /// </summary>
    public class StructureFieldApiModel {

        /// <summary>
        /// Default constructor
        /// </summary>
        public StructureFieldApiModel() { }

        /// <summary>
        /// Create api model from service model
        /// </summary>
        /// <param name="model"></param>
        public StructureFieldApiModel(StructureFieldModel model) {
            if (model == null) {
                throw new ArgumentNullException(nameof(model));
            }
            Description = model.Description == null ? null :
                new LocalizedTextApiModel(model.Description);
            ArrayDimensions = model.ArrayDimensions?.ToList();
            DataTypeId = model.DataTypeId;
            IsOptional = model.IsOptional;
            MaxStringLength = model.MaxStringLength;
            Name = model.Name;
            ValueRank = model.ValueRank;
        }

        /// <summary>
        /// Create service model from api model
        /// </summary>
        public StructureFieldModel ToServiceModel() {
            return new StructureFieldModel {
                ArrayDimensions = ArrayDimensions?.ToList(),
                DataTypeId = DataTypeId,
                Description = Description?.ToServiceModel(),
                IsOptional = IsOptional,
                MaxStringLength = MaxStringLength,
                Name = Name,
                ValueRank = ValueRank
            };
        }

        /// <summary>
        /// Structure name
        /// </summary>
        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }

        /// <summary>
        /// Description
        /// </summary>
        [JsonProperty(PropertyName = "description",
            NullValueHandling = NullValueHandling.Ignore)]
        public LocalizedTextApiModel Description { get; set; }

        /// <summary>
        /// Data type  of the structure field
        /// </summary>
        [JsonProperty(PropertyName = "dataTypeId")]
        public string DataTypeId { get; set; }

        /// <summary>
        /// Value rank of the type
        /// </summary>
        [JsonProperty(PropertyName = "valueRank",
            NullValueHandling = NullValueHandling.Ignore)]
        public int? ValueRank { get; set; }

        /// <summary>
        /// Array dimensions
        /// </summary>
        [JsonProperty(PropertyName = "arrayDimensions",
            NullValueHandling = NullValueHandling.Ignore)]
        public List<uint> ArrayDimensions { get; set; }

        /// <summary>
        /// Max length of a byte or character string
        /// </summary>
        [JsonProperty(PropertyName = "maxStringLength",
            NullValueHandling = NullValueHandling.Ignore)]
        public uint? MaxStringLength { get; set; }

        /// <summary>
        /// If the field is optional
        /// </summary>
        [JsonProperty(PropertyName = "isOptional",
            NullValueHandling = NullValueHandling.Ignore)]
        public bool? IsOptional { get; set; }
    }
}
