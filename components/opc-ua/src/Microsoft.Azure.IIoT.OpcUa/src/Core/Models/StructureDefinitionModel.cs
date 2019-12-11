// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Core.Models {
    using System.Collections.Generic;

    /// <summary>
    /// Structure definition
    /// </summary>
    public class StructureDefinitionModel {

        /// <summary>
        /// Base data type of the structure
        /// </summary>
        public string BaseDataTypeId { get; set; }

        /// <summary>
        /// Type of structure
        /// </summary>
        public StructureType StructureType { get; set; }

        /// <summary>
        /// Fields in the structure or union
        /// </summary>
        public List<StructureFieldModel> Fields { get; set; }
    }
}
