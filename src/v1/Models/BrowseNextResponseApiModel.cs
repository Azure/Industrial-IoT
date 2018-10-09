// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Modules.Twin.v1.Models {
    using Microsoft.Azure.IIoT.OpcUa.Twin.Models;
    using Newtonsoft.Json.Linq;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Result of node browse continuation
    /// </summary>
    public class BrowseNextResponseApiModel {

        /// <summary>
        /// Default constructor
        /// </summary>
        public BrowseNextResponseApiModel() { }

        /// <summary>
        /// Create from service model
        /// </summary>
        /// <param name="model"></param>
        public BrowseNextResponseApiModel(BrowseNextResultModel model) {
            Diagnostics = model.Diagnostics;
            ContinuationToken = model.ContinuationToken;
            if (model.References != null) {
                References = model.References
                    .Select(r => new NodeReferenceApiModel(r))
                    .ToList();
            }
            else {
                model.References = new List<NodeReferenceModel>();
            }
        }

        /// <summary>
        /// References, if included, otherwise null.
        /// </summary>
        public List<NodeReferenceApiModel> References { get; set; }

        /// <summary>
        /// Continuation token if more results pending.
        /// </summary>
        public string ContinuationToken { get; set; }

        /// <summary>
        /// Diagnostics in case of error
        /// </summary>
        public JToken Diagnostics { get; set; }
    }
}
