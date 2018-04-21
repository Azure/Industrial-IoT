// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcTwin.Services.Models {
    using Newtonsoft.Json.Linq;
    using System.Collections.Generic;

    /// <summary>
    /// Result of node browse
    /// </summary>
    public class BrowseResultModel {

        /// <summary>
        /// Node info for the currently browsed node
        /// </summary>
        public NodeModel Node { get; set; }

        /// <summary>
        /// References, if included, otherwise null.
        /// </summary>
        public List<NodeReferenceModel> References { get; set; }

        /// <summary>
        /// Diagnostics in case of error
        /// </summary>
        public JToken Diagnostics { get; set; }
    }
}
