// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Publisher.Models {
    using Microsoft.Azure.IIoT.OpcUa.Core.Models;

    /// <summary>
    /// Unpublish request
    /// </summary>
    public class PublishStopRequestModel {

        /// <summary>
        /// Node of published item to unpublish
        /// </summary>
        public string NodeId { get; set; }

        /// <summary>
        /// Optional request header
        /// </summary>
        public RequestHeaderModel Header { get; set; }
    }
}
