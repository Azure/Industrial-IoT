// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Core.Models {
    using System.Collections.Generic;

    /// <summary>
    /// Structure field
    /// </summary>
    public class StructureFieldModel {

        /// <summary>
        /// Structure name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Description
        /// </summary>
        public LocalizedTextModel Description { get; set; }

        /// <summary>
        /// Data type  of the structure field
        /// </summary>
        public string DataTypeId { get; set; }

        /// <summary>
        /// Value rank of the type
        /// </summary>
        public NodeValueRank? ValueRank { get; set; }

        /// <summary>
        /// Array dimensions
        /// </summary>
        public List<uint> ArrayDimensions { get; set; }

        /// <summary>
        /// Max length of a byte or character string
        /// </summary>
        public uint? MaxStringLength { get; set; }

        /// <summary>
        /// If the field is optional
        /// </summary>
        public bool? IsOptional { get; set; }
    }
}
