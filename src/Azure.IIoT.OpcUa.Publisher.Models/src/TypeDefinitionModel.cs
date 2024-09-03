// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.Runtime.Serialization;

    /// <summary>
    /// Type definition
    /// </summary>
    [DataContract]
    public sealed record class TypeDefinitionModel
    {
        /// <summary>
        /// The node id of the type of the node
        /// </summary>
        [DataMember(Name = "typeDefinitionId", Order = 0)]
        [Required]
        public required string TypeDefinitionId { get; set; }

        /// <summary>
        /// The type of the node
        /// </summary>
        [DataMember(Name = "nodeType", Order = 1)]
        public NodeType NodeType { get; set; }

        /// <summary>
        /// Display name
        /// </summary>
        [DataMember(Name = "displayName", Order = 2,
            EmitDefaultValue = false)]
        public string? DisplayName { get; set; }

        /// <summary>
        /// Browse name
        /// </summary>
        [DataMember(Name = "browseName", Order = 3,
            EmitDefaultValue = false)]
        public string? BrowseName { get; set; }

        /// <summary>
        /// Description if any
        /// </summary>
        [DataMember(Name = "description", Order = 4,
            EmitDefaultValue = false)]
        public string? Description { get; set; }

        /// <summary>
        /// Super types hierarchy starting from base type
        /// up to <see cref="TypeDefinitionId"/> which is
        /// not included.
        /// </summary>
        [DataMember(Name = "typeHierarchy", Order = 5,
            EmitDefaultValue = false)]
        public IReadOnlyList<NodeModel>? TypeHierarchy { get; set; }

        /// <summary>
        /// Fully inherited instance declarations of the type
        /// of the node.
        /// </summary>
        [DataMember(Name = "typeMembers", Order = 6,
            EmitDefaultValue = false)]
        public IReadOnlyList<InstanceDeclarationModel>? Declarations { get; set; }
    }
}
