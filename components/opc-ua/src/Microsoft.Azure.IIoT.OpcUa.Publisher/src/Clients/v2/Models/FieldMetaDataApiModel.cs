// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Publisher.Clients.v2.Models {
    using Microsoft.Azure.IIoT.OpcUa.Publisher.Models;
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Describes the field metadata
    /// </summary>
    public class FieldMetaDataApiModel {

        /// <summary>
        /// Default constructor
        /// </summary>
        public FieldMetaDataApiModel() { }

        /// <summary>
        /// Create api model from service model
        /// </summary>
        /// <param name="model"></param>
        public FieldMetaDataApiModel(FieldMetaDataModel model) {
            if (model == null) {
                throw new ArgumentNullException(nameof(model));
            }
            Description = model.Description == null ? null :
                new LocalizedTextApiModel(model.Description);
            ArrayDimensions = model.ArrayDimensions?.ToList();
            BuiltInType = model.BuiltInType;
            DataSetFieldId = model.DataSetFieldId;
            DataTypeId = model.DataTypeId;
            FieldFlags = model.FieldFlags;
            MaxStringLength = model.MaxStringLength;
            Name = model.Name;
            Properties = model.Properties?
                .ToDictionary(k => k.Key, v => v.Value);
            ValueRank = model.ValueRank;
        }

        /// <summary>
        /// Create service model from api model
        /// </summary>
        public FieldMetaDataModel ToServiceModel() {
            return new FieldMetaDataModel {
                Description = Description?.ToServiceModel(),
                ArrayDimensions = ArrayDimensions?.ToList(),
                BuiltInType = BuiltInType,
                DataSetFieldId = DataSetFieldId,
                DataTypeId = DataTypeId,
                FieldFlags = FieldFlags,
                MaxStringLength = MaxStringLength,
                Name = Name,
                Properties = Properties?
                    .ToDictionary(k => k.Key, v => v.Value),
                ValueRank = ValueRank
            };
        }

        /// <summary>
        /// Name of the field
        /// </summary>
        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }

        /// <summary>
        /// Description for the field
        /// </summary>
        [JsonProperty(PropertyName = "description",
            NullValueHandling = NullValueHandling.Ignore)]
        public LocalizedTextApiModel Description { get; set; }

        /// <summary>
        /// Field Flags.
        /// </summary>
        [JsonProperty(PropertyName = "fieldFlags",
            NullValueHandling = NullValueHandling.Ignore)]
        public ushort? FieldFlags { get; set; }

        /// <summary>
        /// Built in type
        /// </summary>
        [JsonProperty(PropertyName = "builtInType",
            NullValueHandling = NullValueHandling.Ignore)]
        public string BuiltInType { get; set; }

        /// <summary>
        /// The Datatype Id
        /// </summary>
        [JsonProperty(PropertyName = "dataTypeId")]
        public string DataTypeId { get; set; }

        /// <summary>
        /// ValueRank.
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
        /// Max String Length constraint.
        /// </summary>
        [JsonProperty(PropertyName = "maxStringLength",
            NullValueHandling = NullValueHandling.Ignore)]
        public uint? MaxStringLength { get; set; }

        /// <summary>
        /// The unique guid of the field in the dataset.
        /// </summary>
        [JsonProperty(PropertyName = "dataSetFieldId",
            NullValueHandling = NullValueHandling.Ignore)]
        public Guid? DataSetFieldId { get; set; }

        /// <summary>
        /// Additional properties
        /// </summary>
        [JsonProperty(PropertyName = "properties",
            NullValueHandling = NullValueHandling.Ignore)]
        public Dictionary<string, string> Properties { get; set; }
    }
}
