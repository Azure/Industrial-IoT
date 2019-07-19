// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Services.OpcUa.History.v2.Models {
    using Microsoft.Azure.IIoT.OpcUa.History.Models;
    using Newtonsoft.Json;
    using System;
    using System.ComponentModel;
    using System.ComponentModel.DataAnnotations;

    /// <summary>
    /// Request node history read continuation
    /// </summary>
    public class HistoryReadNextRequestApiModel {

        /// <summary>
        /// Default constructor
        /// </summary>
        public HistoryReadNextRequestApiModel() { }

        /// <summary>
        /// Create from service model
        /// </summary>
        /// <param name="model"></param>
        public HistoryReadNextRequestApiModel(HistoryReadNextRequestModel model) {
            if (model == null) {
                throw new ArgumentNullException(nameof(model));
            }
            ContinuationToken = model.ContinuationToken;
            Abort = model.Abort;
            Header = model.Header == null ? null :
                new RequestHeaderApiModel(model.Header);
        }

        /// <summary>
        /// Convert back to service model
        /// </summary>
        /// <returns></returns>
        public HistoryReadNextRequestModel ToServiceModel() {
            return new HistoryReadNextRequestModel {
                ContinuationToken = ContinuationToken,
                Abort = Abort,
                Header = Header?.ToServiceModel()
            };
        }

        /// <summary>
        /// Continuation token to continue reading more
        /// results.
        /// </summary>
        [JsonProperty(PropertyName = "continuationToken")]
        [Required]
        public string ContinuationToken { get; set; }

        /// <summary>
        /// Abort reading after this read
        /// </summary>
        [JsonProperty(PropertyName = "abort",
            NullValueHandling = NullValueHandling.Ignore)]
        [DefaultValue(false)]
        public bool? Abort { get; set; }

        /// <summary>
        /// Optional request header
        /// </summary>
        [JsonProperty(PropertyName = "header",
            NullValueHandling = NullValueHandling.Ignore)]
        [DefaultValue(null)]
        public RequestHeaderApiModel Header { get; set; }
    }
}
