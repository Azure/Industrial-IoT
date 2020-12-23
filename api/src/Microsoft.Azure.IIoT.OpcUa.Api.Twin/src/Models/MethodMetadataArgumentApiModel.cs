// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Twin.Models {
    using Microsoft.Azure.IIoT.OpcUa.Api.Core.Models;
    using Microsoft.Azure.IIoT.Serializers;
    using System.Runtime.Serialization;

    /// <summary>
    /// Method argument metadata model
    /// </summary>
    [DataContract]
    public class MethodMetadataArgumentApiModel {

        /// <summary>
        /// Argument name
        /// </summary>
        [DataMember(Name = "name", Order = 0)]
        public string Name { get; set; }

        /// <summary>
        /// Optional description
        /// </summary>
        [DataMember(Name = "description", Order = 1,
            EmitDefaultValue = false)]
        public string Description { get; set; }

        /// <summary>
        /// Data type node of the argument
        /// </summary>
        [DataMember(Name = "type", Order = 2)]
        public NodeApiModel Type { get; set; }

        /// <summary>
        /// Default value
        /// </summary>
        [DataMember(Name = "defaultValue", Order = 3,
            EmitDefaultValue = false)]
        public VariantValue DefaultValue { get; set; }

        /// <summary>
        /// Optional, scalar if not set
        /// </summary>
        [DataMember(Name = "valueRank", Order = 4,
            EmitDefaultValue = false)]
        public NodeValueRank? ValueRank { get; set; }

        /// <summary>
        /// Optional, array dimension
        /// </summary>
        [DataMember(Name = "arrayDimensions", Order = 5,
            EmitDefaultValue = false)]
        public uint[] ArrayDimensions { get; set; }
    }
}
