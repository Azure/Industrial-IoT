// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Publisher.Models {
    using Microsoft.Azure.IIoT.OpcUa.Core.Models;
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Describes the field metadata
    /// </summary>
    public class FieldMetaDataModel {

        /// <summary>
        /// Name of the field
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Description for the field
        /// </summary>
        public LocalizedTextModel Description { get; set; }

        /// <summary>
        /// Field Flags.
        /// </summary>
        public ushort? FieldFlags { get; set; }

        /// <summary>
        /// Built in type
        /// </summary>
        public string BuiltInType { get; set; }

        /// <summary>
        /// The Datatype Id
        /// </summary>
        public string DataTypeId { get; set; }

        /// <summary>
        /// ValueRank.
        /// </summary>
        public int? ValueRank { get; set; }

        /// <summary>
        /// Array dimensions
        /// </summary>
        public List<uint> ArrayDimensions { get; set; }

        /// <summary>
        /// Max String Length constraint.
        /// </summary>
        public uint? MaxStringLength { get; set; }

        /// <summary>
        /// The unique guid of the field in the dataset.
        /// </summary>
        public Guid? DataSetFieldId { get; set; }

        /// <summary>
        /// Additional properties
        /// </summary>
        public Dictionary<string, string> Properties { get; set; }
    }
}
