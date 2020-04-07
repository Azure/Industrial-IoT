// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Twin.Models {
    using Microsoft.Azure.IIoT.Serializers;

    /// <summary>
    /// Service call result
    /// </summary>
    public class ServiceCallResultModel {

        /// <summary>
        /// Service call result type
        /// </summary>
        public ServiceCallType Type { get; set; }

        /// <summary>
        /// Result model
        /// </summary>
        public VariantValue Result { get; set; }
    }
}
