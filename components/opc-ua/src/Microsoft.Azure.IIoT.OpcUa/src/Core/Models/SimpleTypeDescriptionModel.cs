// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Core.Models {

    /// <summary>
    /// Simple type
    /// </summary>
    public class SimpleTypeDescriptionModel {

        /// <summary>
        /// Data type id
        /// </summary>
        public string DataTypeId { get; set; }

        /// <summary>
        /// The qualified name of the data type.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Base data type of the type
        /// </summary>
        public string BaseDataTypeId { get; set; }

        /// <summary>
        /// The built in type
        /// </summary>
        public string BuiltInType { get; set; }
    }
}
