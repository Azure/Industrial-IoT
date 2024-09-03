// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    using Furly.Extensions.Serializers;
    using System.ComponentModel.DataAnnotations;
    using System.Runtime.Serialization;

    /// <summary>
    /// Attribute and value to write to it
    /// </summary>
    [DataContract]
    public sealed record class AttributeWriteRequestModel
    {
        /// <summary>
        /// Node to write to (mandatory)
        /// </summary>
        [DataMember(Name = "nodeId", Order = 0)]
        [Required]
        public required string NodeId { get; set; }

        /// <summary>
        /// Attribute to write (mandatory)
        /// </summary>
        [DataMember(Name = "attribute", Order = 1)]
        [Required]
        public required NodeAttribute Attribute { get; set; }

        /// <summary>
        /// Value to write (mandatory)
        /// </summary>
        [DataMember(Name = "value", Order = 2)]
        [Required]
        public required VariantValue Value { get; set; }
    }
}
