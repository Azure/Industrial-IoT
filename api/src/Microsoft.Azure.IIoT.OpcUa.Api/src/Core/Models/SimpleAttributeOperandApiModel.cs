// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Core.Models {
    using System;
    using System.Runtime.Serialization;

    /// <summary>
    /// Simple attribute operand model
    /// </summary>
    [DataContract]
    public class SimpleAttributeOperandApiModel {

        /// <summary>
        /// Type definition node id if operand is
        /// simple or full attribute operand.
        /// </summary>
        [DataMember(Name = "typeDefinitionId", Order = 0)]
        public string TypeDefinitionId { get; set; }

        /// <summary>
        /// Browse path of attribute operand
        /// </summary>
        [DataMember(Name = "browsePath", Order = 1,
            EmitDefaultValue = false)]
        public string[] BrowsePath { get; set; }

        /// <summary>
        /// Attribute id
        /// </summary>
        [DataMember(Name = "attributeId", Order = 2,
            EmitDefaultValue = false)]
        public NodeAttribute? AttributeId { get; set; }

        /// <summary>
        /// Index range of attribute operand
        /// </summary>
        [DataMember(Name = "indexRange", Order = 3,
            EmitDefaultValue = false)]
        public string IndexRange { get; set; }

        /// <summary>
        /// Optional display name (Publisher extension)
        /// </summary>
        [DataMember(Name = "displayName", Order = 4,
            EmitDefaultValue = false)]
        public string DisplayName { get; set; }

        /// <summary>
        /// Optional data set class field id (Publisher extension)
        /// </summary>
        [DataMember(Name = "dataSetClassFieldId", Order = 5,
            EmitDefaultValue = false)]
        public Guid DataSetClassFieldId { get; set; }
    }
}