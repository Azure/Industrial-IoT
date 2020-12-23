// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Core.Models {

    /// <summary>
    /// A Structure description
    /// </summary>
    public class StructureDescriptionModel {

        /// <summary>
        /// Data type id
        /// </summary>
        public string DataTypeId { get; set; }

        /// <summary>
        /// The qualified name of the data type.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Structure definition
        /// </summary>
        public StructureDefinitionModel StructureDefinition { get; set; }
    }
}
