// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Twin.Models {
    using System.Collections.Generic;

    /// <summary>
    /// List of published nodes
    /// </summary>
    public class PublishedNodeListResultModel {

        /// <summary>
        /// Continuation or null if final
        /// </summary>
        public string ContinuationToken { get; set; }

        /// <summary>
        /// Monitored items
        /// </summary>
        public List<PublishedNodeModel> Items { get; set; }
    }
}
