// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Services.Twin.v1.Models {
    using Microsoft.Azure.IIoT.OpcUa.Twin.Models;
    using Newtonsoft.Json;
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
            ContinuationToken = model.ContinuationToken;
            Abort = model.Abort;
            Elevation = model.Elevation == null ? null :
                new CredentialApiModel(model.Elevation);
            Diagnostics = model.Diagnostics == null ? null :
                new DiagnosticsApiModel(model.Diagnostics);
        }

        /// <summary>
        /// Convert back to service model
        /// </summary>
        /// <returns></returns>
        public HistoryReadNextRequestModel ToServiceModel() {
            return new HistoryReadNextRequestModel {
                ContinuationToken = ContinuationToken,
                Abort = Abort,
                Diagnostics = Diagnostics?.ToServiceModel(),
                Elevation = Elevation?.ToServiceModel()
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
        /// Optional User elevation
        /// </summary>
        [JsonProperty(PropertyName = "elevation",
            NullValueHandling = NullValueHandling.Ignore)]
        [DefaultValue(null)]
        public CredentialApiModel Elevation { get; set; }

        /// <summary>
        /// Optional diagnostics configuration
        /// </summary>
        [JsonProperty(PropertyName = "diagnostics",
            NullValueHandling = NullValueHandling.Ignore)]
        [DefaultValue(null)]
        public DiagnosticsApiModel Diagnostics { get; set; }
    }
}
