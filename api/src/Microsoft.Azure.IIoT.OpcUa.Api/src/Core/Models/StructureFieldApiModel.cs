// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Core.Models {
    using System.Runtime.Serialization;
    using System.Collections.Generic;

    /// <summary>
    /// Structure field
    /// </summary>
    [DataContract]
    public class StructureFieldApiModel {

        /// <summary>
        /// Structure name
        /// </summary>
        [DataMember(Name = "name", Order = 0)]
        public string Name { get; set; }

        /// <summary>
        /// Description
        /// </summary>
        [DataMember(Name = "description", Order = 1,
            EmitDefaultValue = false)]
        public LocalizedTextApiModel Description { get; set; }

        /// <summary>
        /// Data type  of the structure field
        /// </summary>
        [DataMember(Name = "dataTypeId", Order = 2)]
        public string DataTypeId { get; set; }

        /// <summary>
        /// Value rank of the type
        /// </summary>
        [DataMember(Name = "valueRank", Order = 3,
            EmitDefaultValue = false)]
        public NodeValueRank? ValueRank { get; set; }

        /// <summary>
        /// Array dimensions
        /// </summary>
        [DataMember(Name = "arrayDimensions", Order = 4,
            EmitDefaultValue = false)]
        public List<uint> ArrayDimensions { get; set; }

        /// <summary>
        /// Max length of a byte or character string
        /// </summary>
        [DataMember(Name = "maxStringLength", Order = 5,
            EmitDefaultValue = false)]
        public uint? MaxStringLength { get; set; }

        /// <summary>
        /// If the field is optional
        /// </summary>
        [DataMember(Name = "isOptional", Order = 6,
            EmitDefaultValue = false)]
        public bool? IsOptional { get; set; }
    }
}
