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
    /// Result of node browse continuation
    /// </summary>
    public class BrowsePathResponseApiModel {

        /// <summary>
        /// Default constructor
        /// </summary>
        public BrowsePathResponseApiModel() { }

        /// <summary>
        /// Create from service model
        /// </summary>
        /// <param name="model"></param>
        public BrowsePathResponseApiModel(BrowsePathResultModel model) {
            if (model == null) {
                throw new ArgumentNullException(nameof(model));
            }
            ErrorInfo = model.ErrorInfo == null ? null :
                new ServiceResultApiModel(model.ErrorInfo);
            Targets = model.Targets?
                .Select(r => r == null ? null : new NodePathTargetApiModel(r))
                .ToList();
        }

        /// <summary>
        /// Targets
        /// </summary>
        [JsonProperty(PropertyName = "targets",
            NullValueHandling = NullValueHandling.Ignore)]
        public List<NodePathTargetApiModel> Targets { get; set; }

        /// <summary>
        /// Service result in case of error
        /// </summary>
        [JsonProperty(PropertyName = "errorInfo",
            NullValueHandling = NullValueHandling.Ignore)]
        public ServiceResultApiModel ErrorInfo { get; set; }
    }
}
