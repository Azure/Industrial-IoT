// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Models {
    using Newtonsoft.Json;
    using System;

    /// <summary>
    /// Browse view model
    /// </summary>
    public class BrowseViewApiModel {

        /// <summary>
        /// Node of the view to browse
        /// </summary>
        [JsonProperty(PropertyName = "viewId")]
        public string ViewId { get; set; }

        /// <summary>
        /// Browses specific version of the view.
        /// </summary>
        [JsonProperty(PropertyName = "version",
            NullValueHandling = NullValueHandling.Ignore)]
        public uint? Version { get; set; }

        /// <summary>
        /// Browses at or before this timestamp.
        /// </summary>
        [JsonProperty(PropertyName = "timestamp",
            NullValueHandling = NullValueHandling.Ignore)]
        public DateTime? Timestamp { get; set; }
    }
}
