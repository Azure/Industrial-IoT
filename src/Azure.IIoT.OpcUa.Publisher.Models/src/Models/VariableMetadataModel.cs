// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    using System.Collections.Generic;
    using System.Runtime.Serialization;

    /// <summary>
    /// Variable metadata model
    /// </summary>
    [DataContract]
    public sealed record class VariableMetadataModel
    {
        /// <summary>
        /// The data type for the variable.
        /// </summary>
        [DataMember(Name = "dataType", Order = 0,
            EmitDefaultValue = false)]
        public DataTypeMetadataModel? DataType { get; set; }

        /// <summary>
        /// The value rank of the variable.
        /// </summary>
        [DataMember(Name = "valueRank", Order = 1,
            EmitDefaultValue = false)]
        public NodeValueRank? ValueRank { get; set; }

        /// <summary>
        /// Array dimensions of the variable.
        /// </summary>
        [DataMember(Name = "arrayDimensions", Order = 2,
            EmitDefaultValue = false)]
        public IReadOnlyList<uint>? ArrayDimensions { get; set; }
    }
}
