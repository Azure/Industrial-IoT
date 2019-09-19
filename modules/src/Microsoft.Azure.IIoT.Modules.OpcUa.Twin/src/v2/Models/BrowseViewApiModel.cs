// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Modules.OpcUa.Twin.v2.Models {
    using Microsoft.Azure.IIoT.OpcUa.Twin.Models;
    using Newtonsoft.Json;
    using System;

    /// <summary>
    /// browse view model
    /// </summary>
    public class BrowseViewApiModel {

        /// <summary>
        /// Default constructor
        /// </summary>
        public BrowseViewApiModel() { }

        /// <summary>
        /// Create from service model
        /// </summary>
        /// <param name="model"></param>
        public BrowseViewApiModel(BrowseViewModel model) {
            if (model == null) {
                throw new ArgumentNullException(nameof(model));
            }
            ViewId = model.ViewId;
            Version = model.Version;
            Timestamp = model.Timestamp;
        }

        /// <summary>
        /// Convert back to service model
        /// </summary>
        /// <returns></returns>
        public BrowseViewModel ToServiceModel() {
            return new BrowseViewModel {
                ViewId = ViewId,
                Version = Version,
                Timestamp = Timestamp
            };
        }

        /// <summary>
        /// Node of the view to browse
        /// </summary>
        [JsonProperty(PropertyName = "ViewId")]
        public string ViewId { get; set; }

        /// <summary>
        /// Browses specific version of the view.
        /// </summary>
        [JsonProperty(PropertyName = "Version",
            NullValueHandling = NullValueHandling.Ignore)]
        public uint? Version { get; set; }

        /// <summary>
        /// Browses at or before this timestamp.
        /// </summary>
        [JsonProperty(PropertyName = "Timestamp",
            NullValueHandling = NullValueHandling.Ignore)]
        public DateTime? Timestamp { get; set; }
    }
}
