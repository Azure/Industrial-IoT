// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    using System.Collections.Generic;
    using System.Runtime.Serialization;

    /// <summary>
    /// Instance declaration meta data
    /// </summary>
    [DataContract]
    public sealed record class InstanceDeclarationModel
    {
        /// <summary>
        /// The type that the declaration belongs to.
        /// </summary>
        [DataMember(Name = "rootTypeId", Order = 0)]
        public string? RootTypeId { get; set; }

        /// <summary>
        /// The browse path
        /// </summary>
        [DataMember(Name = "browsePath", Order = 1)]
        public IReadOnlyList<string>? BrowsePath { get; set; }

        /// <summary>
        /// A localized path to the instance declaration.
        /// </summary>
        [DataMember(Name = "displayPath", Order = 2)]
        public string? DisplayPath { get; set; }

        /// <summary>
        /// The modelling rule for the instance
        /// declaration (i.e. Mandatory or Optional).
        /// </summary>
        [DataMember(Name = "modellingRule", Order = 3,
            EmitDefaultValue = false)]
        public string? ModellingRule { get; set; }

        /// <summary>
        /// The node id for the instance.
        /// </summary>
        [DataMember(Name = "nodeId", Order = 4,
            EmitDefaultValue = false)]
        public string? NodeId { get; set; }

        /// <summary>
        /// The node class of the instance declaration.
        /// </summary>
        [DataMember(Name = "nodeClass", Order = 5)]
        public NodeClass NodeClass { get; set; }

        /// <summary>
        /// The browse name for the instance declaration.
        /// </summary>
        [DataMember(Name = "browseName", Order = 6,
            EmitDefaultValue = false)]
        public string? BrowseName { get; set; }

        /// <summary>
        /// The display name for the instance declaration.
        /// </summary>
        [DataMember(Name = "displayName", Order = 7,
            EmitDefaultValue = false)]
        public string? DisplayName { get; set; }

        /// <summary>
        /// The description for the instance declaration.
        /// </summary>
        [DataMember(Name = "description", Order = 8,
            EmitDefaultValue = false)]
        public string? Description { get; set; }

        /// <summary>
        /// Variable meta data
        /// </summary>
        [DataMember(Name = "variable", Order = 9,
            EmitDefaultValue = false)]
        public VariableMetadataModel? VariableMetadata { get; set; }

        /// <summary>
        /// Method meta data
        /// </summary>
        [DataMember(Name = "method", Order = 10,
            EmitDefaultValue = false)]
        public MethodMetadataModel? MethodMetadata { get; set; }

        /// <summary>
        /// An instance declaration that has been overridden
        /// by the current instance.
        /// </summary>
        [DataMember(Name = "overriddenDeclaration", Order = 11,
            EmitDefaultValue = false)]
        public InstanceDeclarationModel? OverriddenDeclaration { get; set; }

        /// <summary>
        /// The modelling rule node id.
        /// </summary>
        [DataMember(Name = "modellingRuleId", Order = 12,
            EmitDefaultValue = false)]
        public string? ModellingRuleId { get; set; }
    }
}
