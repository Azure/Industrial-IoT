// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Core.Models {
    using System;

    /// <summary>
    /// Simple attribute operand model
    /// </summary>
    public class SimpleAttributeOperandModel {

        /// <summary>
        /// Type definition node id if operand is
        /// simple or full attribute operand.
        /// </summary>
        public string TypeDefinitionId { get; set; }

        /// <summary>
        /// Browse path of attribute operand
        /// </summary>
        public string[] BrowsePath { get; set; }

        /// <summary>
        /// Attribute id
        /// </summary>
        public NodeAttribute? AttributeId { get; set; }

        /// <summary>
        /// Index range of attribute operand
        /// </summary>
        public string IndexRange { get; set; }

        /// <summary>
        /// Optional display name
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        /// Optional field id
        /// </summary>
        public Guid DataSetClassFieldId { get; set; }
    }
}