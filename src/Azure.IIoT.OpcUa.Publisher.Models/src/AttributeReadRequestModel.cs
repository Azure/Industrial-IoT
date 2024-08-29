// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    using System.ComponentModel.DataAnnotations;
    using System.Runtime.Serialization;

    /// <summary>
    /// Attribute to read
    /// </summary>
    [DataContract]
    public sealed record class AttributeReadRequestModel
    {
        /// <summary>
        /// Node to read from or write to (mandatory)
        /// </summary>
        [DataMember(Name = "nodeId", Order = 0)]
        [Required]
        public required string NodeId { get; set; }

        /// <summary>
        /// Attribute to read or write
        /// </summary>
        [DataMember(Name = "attribute", Order = 1)]
        [Required]
        public required NodeAttribute Attribute { get; set; }
    }
}
