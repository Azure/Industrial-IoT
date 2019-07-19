// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Services.OpcUa.History.v2.Models {
    using Microsoft.Azure.IIoT.OpcUa.History.Models;
    using Newtonsoft.Json;
    using System;

    /// <summary>
    /// History read continuation result
    /// </summary>
    public class HistoryReadNextResponseApiModel<T> {

        /// <summary>
        /// Create from service model
        /// </summary>
        /// <typeparam name="S"></typeparam>
        /// <param name="model"></param>
        /// <param name="convert"></param>
        public static HistoryReadNextResponseApiModel<T> Create<S>(
            HistoryReadNextResultModel<S> model, Func<S, T> convert) {
            if (model == null) {
                throw new ArgumentNullException(nameof(model));
            }
            return new HistoryReadNextResponseApiModel<T> {
                History = convert(model.History),
                ContinuationToken = model.ContinuationToken,
                ErrorInfo = model.ErrorInfo == null ? null :
                    new ServiceResultApiModel(model.ErrorInfo)
            };
        }

        /// <summary>
        /// History as json encoded extension object
        /// </summary>
        [JsonProperty(PropertyName = "history")]
        public T History { get; set; }

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
