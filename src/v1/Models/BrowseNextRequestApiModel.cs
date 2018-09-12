// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Services.Twin.v1.Models {
    using Microsoft.Azure.IIoT.OpcUa.Models;
    using Newtonsoft.Json;
    using System.ComponentModel;
    using System.ComponentModel.DataAnnotations;

    /// <summary>
    /// Request node browsing continuation
    /// </summary>
    public class BrowseNextRequestApiModel {

        /// <summary>
        /// Default constructor
        /// </summary>
        public BrowseNextRequestApiModel() { }

        /// <summary>
        /// Create from service model
        /// </summary>
        /// <param name="model"></param>
        public BrowseNextRequestApiModel(BrowseNextRequestModel model) {
            Abort = model.Abort;
            ContinuationToken = model.ContinuationToken;
            Elevation = model.Elevation == null ? null :
                new AuthenticationApiModel(model.Elevation);
        }

        /// <summary>
        /// Convert back to service model
        /// </summary>
        /// <returns></returns>
        public BrowseNextRequestModel ToServiceModel() {
            return new BrowseNextRequestModel {
                Abort = Abort,
                Elevation = Elevation?.ToServiceModel(),
                ContinuationToken = ContinuationToken
            };
        }

        /// <summary>
        /// Continuation token from previews browse request.
        /// (mandatory)
        /// </summary>
        [JsonProperty(PropertyName = "continuationToken")]
        [Required]
        public string ContinuationToken { get; set; }

        /// <summary>
        /// Whether to abort browse and release.
        /// (default: false)
        /// </summary>
        [JsonProperty(PropertyName = "abort",
            NullValueHandling = NullValueHandling.Ignore)]
        [DefaultValue(false)]
        public bool? Abort { get; set; }

        /// <summary>
        /// Whether to collapse all references into a set of
        /// unique target nodes and not show reference
        /// information.
        /// (default is false)
        /// </summary>
        [JsonProperty(PropertyName = "targetNodesOnly",
            NullValueHandling = NullValueHandling.Ignore)]
        [DefaultValue(false)]
        public bool? TargetNodesOnly { get; set; }

        /// <summary>
        /// Optional User elevation
        /// </summary>
        [JsonProperty(PropertyName = "elevation",
            NullValueHandling = NullValueHandling.Ignore)]
        public AuthenticationApiModel Elevation { get; set; }
    }
}
