// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Core.Models {

    /// <summary>
    /// Describes the enumeration
    /// </summary>
    public class EnumDescriptionModel {

        /// <summary>
        /// Data type id
        /// </summary>
        public string DataTypeId { get; set; }

        /// <summary>
        /// The qualified name of the enum
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Enum definition
        /// </summary>
        public EnumDefinitionModel EnumDefinition { get; set; }

        /// <summary>
        /// The built in type of the enum
        /// </summary>
        public string BuiltInType { get; set; }
    }
}
