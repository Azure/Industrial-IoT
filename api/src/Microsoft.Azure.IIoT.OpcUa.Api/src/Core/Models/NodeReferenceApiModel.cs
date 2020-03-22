// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Core.Models {
    using System.Runtime.Serialization;
    using System;
    using System.ComponentModel.DataAnnotations;

    /// <summary>
    /// reference model
    /// </summary>
    [DataContract]
    public class NodeReferenceApiModel {

        /// <summary>
        /// Reference Type id
        /// </summary>
        [DataMember(Name = "referenceTypeId",
            EmitDefaultValue = false)]
        public string ReferenceTypeId { get; set; }

        /// <summary>
        /// Browse direction of reference
        /// </summary>
        [DataMember(Name = "direction",
            EmitDefaultValue = false)]
        public BrowseDirection? Direction { get; set; }

        /// <summary>
        /// Target node
        /// </summary>
        [DataMember(Name = "target")]
        [Required]
        public NodeApiModel Target { get; set; }

        // Legacy

        /// <ignore/>
        [IgnoreDataMember]
        [Obsolete]
        public string TypeId => ReferenceTypeId;

        /// <ignore/>
        [IgnoreDataMember]
        [Obsolete]
        public string BrowseName => Target?.BrowseName;

        /// <ignore/>
        [IgnoreDataMember]
        [Obsolete]
        public string DisplayName => Target?.DisplayName;

        /// <ignore/>
        [IgnoreDataMember]
        [Obsolete]
        public string TypeDefinition => Target?.TypeDefinitionId;
    }
}
