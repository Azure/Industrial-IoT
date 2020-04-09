// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Publisher.Models {
    using Microsoft.Azure.IIoT.OpcUa.Core.Models;
    using System.Collections.Generic;

    /// <summary>
    /// Result of bulk request
    /// </summary>
    public class PublishBulkResultModel {

        /// <summary>
        /// Node to add
        /// </summary>
        public List<ServiceResultModel> NodesToAdd { get; set; }

        /// <summary>
        /// Node to remove
        /// </summary>
        public List<ServiceResultModel> NodesToRemove { get; set; }
    }
}
