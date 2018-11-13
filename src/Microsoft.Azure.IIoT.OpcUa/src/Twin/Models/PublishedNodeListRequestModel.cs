// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Twin.Models {

    /// <summary>
    /// Request list of published nodes
    /// </summary>
    public class PublishedNodeListRequestModel {

        /// <summary>
        /// Continuation token or null to start
        /// </summary>
        public string ContinuationToken { get; set; }
    }
}
