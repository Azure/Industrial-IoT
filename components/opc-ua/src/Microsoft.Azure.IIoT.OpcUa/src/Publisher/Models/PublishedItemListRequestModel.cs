// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Publisher.Models {

    /// <summary>
    /// Request list of published items
    /// </summary>
    public class PublishedItemListRequestModel {

        /// <summary>
        /// Continuation token or null to start
        /// </summary>
        public string ContinuationToken { get; set; }
    }
}
