// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Services.OpcUa.Twin.v2.Models {
    using Microsoft.Azure.IIoT.OpcUa.Twin.Models;
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// browse response model
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
            if (model == null) {
                throw new ArgumentNullException(nameof(model));
            }
            Node = model.Node == null ? null :
                new NodeApiModel(model.Node);
            ErrorInfo = model.ErrorInfo == null ? null :
                new ServiceResultApiModel(model?.ErrorInfo);
            ContinuationToken = model.ContinuationToken;
            References = model.References?
                .Select(r => new NodeReferenceApiModel(r))
                .ToList();
        }

        /// <summary>
        /// Node info for the currently browsed node
        /// </summary>
        [JsonProperty(PropertyName = "node")]
        public NodeApiModel Node { get; set; }

        /// <summary>
        /// References, if included, otherwise null.
        /// </summary>
        [JsonProperty(PropertyName = "references",
            NullValueHandling = NullValueHandling.Ignore)]
        public List<NodeReferenceApiModel> References { get; set; }

        /// <summary>
        /// Continuation token if more results pending.
        /// </summary>
        [JsonProperty(PropertyName = "continuationToken",
            NullValueHandling = NullValueHandling.Ignore)]
        public string ContinuationToken { get; set; }

        /// <summary>
        /// Service result in case of error
        /// </summary>
        [JsonProperty(PropertyName = "errorInfo",
            NullValueHandling = NullValueHandling.Ignore)]
        public ServiceResultApiModel ErrorInfo { get; set; }
    }
}
