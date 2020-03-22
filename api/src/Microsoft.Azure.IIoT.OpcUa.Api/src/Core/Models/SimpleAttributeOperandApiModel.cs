// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Core.Models {
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
        [DataMember(Name = "nodeId")]
        public string NodeId { get; set; }

        /// <summary>
        /// Browse path of attribute operand
        /// </summary>
        [DataMember(Name = "browsePath",
            EmitDefaultValue = false)]
        public string[] BrowsePath { get; set; }

        /// <summary>
        /// Attribute id
        /// </summary>
        [DataMember(Name = "attributeId",
            EmitDefaultValue = false)]
        public NodeAttribute? AttributeId { get; set; }

        /// <summary>
        /// Index range of attribute operand
        /// </summary>
        [DataMember(Name = "indexRange",
            EmitDefaultValue = false)]
        public string IndexRange { get; set; }
    }
}