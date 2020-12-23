// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Twin.Models {
    using Microsoft.Azure.IIoT.OpcUa.Core.Models;

    /// <summary>
    /// Requests method metadata
    /// </summary>
    public class MethodMetadataRequestModel {

        /// <summary>
        /// Method id of method to call.
        /// (Required)
        /// </summary>
        public string MethodId { get; set; }

        /// <summary>
        /// An optional component path from the node identified by
        /// MethodId to the actual method node.
        /// </summary>
        public string[] MethodBrowsePath { get; set; }

        /// <summary>
        /// Optional header
        /// </summary>
        public RequestHeaderModel Header { get; set; }
    }
}
