// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Publisher.Models {
    using Microsoft.Azure.IIoT.OpcUa.Api.Core.Models;
    using System.Runtime.Serialization;
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Describes the field metadata
    /// </summary>
    [DataContract]
    public class FieldMetaDataApiModel {

        /// <summary>
        /// Name of the field
        /// </summary>
        [DataMember(Name = "name", Order = 0)]
        public string Name { get; set; }

        /// <summary>
        /// Description for the field
        /// </summary>
        [DataMember(Name = "description", Order = 1,
            EmitDefaultValue = false)]
        public LocalizedTextApiModel Description { get; set; }

        /// <summary>
        /// Field Flags.
        /// </summary>
        [DataMember(Name = "fieldFlags", Order = 2,
            EmitDefaultValue = false)]
        public ushort? FieldFlags { get; set; }

        /// <summary>
        /// Built in type
        /// </summary>
        [DataMember(Name = "builtInType", Order = 3,
            EmitDefaultValue = false)]
        public string BuiltInType { get; set; }

        /// <summary>
        /// The Datatype Id
        /// </summary>
        [DataMember(Name = "dataTypeId", Order = 4)]
        public string DataTypeId { get; set; }

        /// <summary>
        /// ValueRank.
        /// </summary>
        [DataMember(Name = "valueRank", Order = 5,
            EmitDefaultValue = false)]
        public int? ValueRank { get; set; }

        /// <summary>
        /// Array dimensions
        /// </summary>
        [DataMember(Name = "arrayDimensions", Order = 6,
            EmitDefaultValue = false)]
        public List<uint> ArrayDimensions { get; set; }

        /// <summary>
        /// Max String Length constraint.
        /// </summary>
        [DataMember(Name = "maxStringLength", Order = 7,
            EmitDefaultValue = false)]
        public uint? MaxStringLength { get; set; }

        /// <summary>
        /// The unique guid of the field in the dataset.
        /// </summary>
        [DataMember(Name = "dataSetFieldId", Order = 8,
            EmitDefaultValue = false)]
        public Guid? DataSetFieldId { get; set; }

        /// <summary>
        /// Additional properties
        /// </summary>
        [DataMember(Name = "properties", Order = 9,
            EmitDefaultValue = false)]
        public Dictionary<string, string> Properties { get; set; }
    }
}
