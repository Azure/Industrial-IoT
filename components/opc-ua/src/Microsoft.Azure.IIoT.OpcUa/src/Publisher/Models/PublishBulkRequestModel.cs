// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Publisher.Models {
    using Microsoft.Azure.IIoT.OpcUa.Core.Models;
    using System.Collections.Generic;

    /// <summary>
    /// Publish in bulk request
    /// </summary>
    public class PublishBulkRequestModel {

        /// <summary>
        /// Node to add
        /// </summary>
        public List<PublishedItemModel> NodesToAdd { get; set; }

        /// <summary>
        /// Node to remove
        /// </summary>
        public List<string> NodesToRemove { get; set; }

        /// <summary>
        /// Optional request header
        /// </summary>
        public RequestHeaderModel Header { get; set; }
    }
}
