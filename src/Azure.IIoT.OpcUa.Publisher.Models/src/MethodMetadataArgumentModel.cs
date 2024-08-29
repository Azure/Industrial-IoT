// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    using Furly.Extensions.Serializers;
    using System.Collections.Generic;
    using System.Runtime.Serialization;

    /// <summary>
    /// Method argument metadata model
    /// </summary>
    [DataContract]
    public sealed record class MethodMetadataArgumentModel
    {
        /// <summary>
        /// Name of the argument
        /// </summary>
        [DataMember(Name = "name", Order = 0)]
        public required string Name { get; set; }

        /// <summary>
        /// Optional description of argument
        /// </summary>
        [DataMember(Name = "description", Order = 1,
            EmitDefaultValue = false)]
        public string? Description { get; set; }

        /// <summary>
        /// Data type node of the argument
        /// </summary>
        [DataMember(Name = "type", Order = 2)]
        public required NodeModel Type { get; set; }

        /// <summary>
        /// Default value for the argument
        /// </summary>
        [DataMember(Name = "defaultValue", Order = 3,
            EmitDefaultValue = false)]
        public VariantValue? DefaultValue { get; set; }

        /// <summary>
        /// Optional, scalar if not set
        /// </summary>
        [DataMember(Name = "valueRank", Order = 4,
            EmitDefaultValue = false)]
        public NodeValueRank? ValueRank { get; set; }

        /// <summary>
        /// Optional Array dimension of argument
        /// </summary>
        [DataMember(Name = "arrayDimensions", Order = 5,
            EmitDefaultValue = false)]
        public IReadOnlyList<uint>? ArrayDimensions { get; set; }

        /// <summary>
        /// Service result in case of error
        /// </summary>
        [DataMember(Name = "errorInfo", Order = 6,
            EmitDefaultValue = false)]
        public ServiceResultModel? ErrorInfo { get; set; }
    }
}
