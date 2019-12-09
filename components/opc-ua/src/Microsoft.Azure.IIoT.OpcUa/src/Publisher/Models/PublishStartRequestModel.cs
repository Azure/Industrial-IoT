// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Publisher.Models {
    using Microsoft.Azure.IIoT.OpcUa.Core.Models;

    /// <summary>
    /// Publish request
    /// </summary>
    public class PublishStartRequestModel {

        /// <summary>
        /// Node to publish
        /// </summary>
        public PublishedItemModel Item { get; set; }

        /// <summary>
        /// Optional request header
        /// </summary>
        public RequestHeaderModel Header { get; set; }
    }
}
