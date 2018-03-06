// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IoTSolutions.OpcTwin.EdgeService.v1.Models {
    using Microsoft.Azure.IoTSolutions.OpcTwin.Services.Models;
    using Newtonsoft.Json.Linq;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// browse response model for webservice api
    /// </summary>
    public class BrowseResponseApiModel {

        /// <summary>
        /// Default constructor
        /// </summary>
        public BrowseResponseApiModel() {}

        /// <summary>
        /// Create from service model
        /// </summary>
        /// <param name="model"></param>
        public BrowseResponseApiModel(BrowseResultModel model) {
            Node = new NodeApiModel(model.Node);
            Diagnostics = model.Diagnostics;
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
        /// Node info for the currently browsed node
        /// </summary>
        public NodeApiModel Node { get; set; }

        /// <summary>
        /// References, if included, otherwise null.
        /// </summary>
        public List<NodeReferenceApiModel> References { get; set; }

        /// <summary>
        /// Optional error diagnostics
        /// </summary>
        public JToken Diagnostics { get; set; }
    }
}
