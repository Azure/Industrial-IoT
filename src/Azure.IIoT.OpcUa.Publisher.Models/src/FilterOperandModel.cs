// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    using Furly.Extensions.Serializers;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.Runtime.Serialization;

    /// <summary>
    /// Filter operand
    /// </summary>
    [DataContract]
    public sealed record class FilterOperandModel
    {
        /// <summary>
        /// Element reference in the outer list if
        /// operand is an element operand
        /// </summary>
        [DataMember(Name = "index", Order = 0,
            EmitDefaultValue = false)]
        public uint? Index { get; set; }

        /// <summary>
        /// Variant value if operand is a literal
        /// </summary>
        [DataMember(Name = "value", Order = 1,
            EmitDefaultValue = false)]
        [SkipValidation]
        public VariantValue? Value { get; set; }

        /// <summary>
        /// Type definition node id if operand is
        /// simple or full attribute operand.
        /// </summary>
        [DataMember(Name = "nodeId", Order = 2,
            EmitDefaultValue = false)]
        public string? NodeId { get; set; }

        /// <summary>
        /// Browse path of attribute operand
        /// </summary>
        [DataMember(Name = "browsePath", Order = 3,
            EmitDefaultValue = false)]
        public IReadOnlyList<string>? BrowsePath { get; set; }

        /// <summary>
        /// Attribute id
        /// </summary>
        [DataMember(Name = "attributeId", Order = 4,
            EmitDefaultValue = false)]
        public NodeAttribute? AttributeId { get; set; }

        /// <summary>
        /// Index range of attribute operand
        /// </summary>
        [DataMember(Name = "indexRange", Order = 5,
            EmitDefaultValue = false)]
        public string? IndexRange { get; set; }

        /// <summary>
        /// Optional alias to refer to it makeing it a
        /// full blown attribute operand
        /// </summary>
        [DataMember(Name = "alias", Order = 6,
            EmitDefaultValue = false)]
        public string? Alias { get; set; }

        /// <summary>
        /// Data type if operand is a literal
        /// </summary>
        [DataMember(Name = "dataType", Order = 7,
            EmitDefaultValue = false)]
        public string? DataType { get; init; }
    }
}
