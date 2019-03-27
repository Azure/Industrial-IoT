// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Services.OpcUa.History.v2.Models {
    using Microsoft.Azure.IIoT.OpcUa.History.Models;
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// History update results
    /// </summary>
    public class HistoryUpdateResponseApiModel {

        /// <summary>
        /// Default constructor
        /// </summary>
        public HistoryUpdateResponseApiModel() { }

        /// <summary>
        /// Create from service model
        /// </summary>
        /// <param name="model"></param>
        public HistoryUpdateResponseApiModel(HistoryUpdateResultModel model) {
            if (model == null) {
                throw new ArgumentNullException(nameof(model));
            }
            Results = model.Results?
                .Select(r => r == null ? null : new ServiceResultApiModel(r))
                .ToList();
            ErrorInfo = model.ErrorInfo == null ? null :
                new ServiceResultApiModel(model.ErrorInfo);
        }

        /// <summary>
        /// List of results from the update operation
        /// </summary>
        [JsonProperty(PropertyName = "results",
            NullValueHandling = NullValueHandling.Ignore)]
        public List<ServiceResultApiModel> Results { get; set; }

        /// <summary>
        /// Service result in case of service call error
        /// </summary>
        [JsonProperty(PropertyName = "errorInfo",
            NullValueHandling = NullValueHandling.Ignore)]
        public ServiceResultApiModel ErrorInfo { get; set; }
    }
}
