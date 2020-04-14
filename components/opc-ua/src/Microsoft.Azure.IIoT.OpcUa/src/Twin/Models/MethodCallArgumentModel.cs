// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Twin.Models {
    using Microsoft.Azure.IIoT.Serializers;

    /// <summary>
    /// Method argument model
    /// </summary>
    public class MethodCallArgumentModel {

        /// <summary>
        /// Value to use
        /// </summary>
        public VariantValue Value { get; set; }

        /// <summary>
        /// Data type of the value
        /// </summary>
        public string DataType { get; set; }
    }
}
