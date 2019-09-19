// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Modules.OpcUa.Twin.v2.Models {
    using Microsoft.Azure.IIoT.OpcUa.History.Models;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using System;

    /// <summary>
    /// History read continuation result
    /// </summary>
    public class HistoryReadNextResponseApiModel {

        /// <summary>
        /// Default constructor
        /// </summary>
        public HistoryReadNextResponseApiModel() { }

        /// <summary>
        /// Create from service model
        /// </summary>
        /// <param name="model"></param>
        public HistoryReadNextResponseApiModel(HistoryReadNextResultModel<JToken> model) {
            if (model == null) {
                throw new ArgumentNullException(nameof(model));
            }
            History = model.History;
            ContinuationToken = model.ContinuationToken;
            ErrorInfo = model.ErrorInfo == null ? null :
                new ServiceResultApiModel(model.ErrorInfo);
        }

        /// <summary>
        /// History as json encoded extension object
        /// </summary>
        [JsonProperty(PropertyName = "History")]
        public JToken History { get; set; }

        /// <summary>
        /// Continuation token if more results pending.
        /// </summary>
        [JsonProperty(PropertyName = "ContinuationToken",
            NullValueHandling = NullValueHandling.Ignore)]
        public string ContinuationToken { get; set; }

        /// <summary>
        /// Service result in case of error
        /// </summary>
        [JsonProperty(PropertyName = "ErrorInfo",
            NullValueHandling = NullValueHandling.Ignore)]
        public ServiceResultApiModel ErrorInfo { get; set; }
    }
}
