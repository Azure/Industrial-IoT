// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Models {
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;

    /// <summary>
    /// Direction to browse
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum BrowseDirection {
        Forward,
        Backward,
        Both
    }

    /// <summary>
    /// browse request model for webservice api
    /// </summary>
    public class BrowseRequestApiModel {

        /// <summary>
        /// Node to browse, or null for root.
        /// </summary>
        [JsonProperty(PropertyName = "nodeId")]
        public string NodeId { get; set; }

        /// <summary>
        /// Direction to browse in
        /// </summary>
        [JsonProperty(PropertyName = "direction",
            NullValueHandling = NullValueHandling.Ignore)]
        public BrowseDirection? Direction { get; set; }

        /// <summary>
        /// Reference types to browse.
        /// (default: hierarchical).
        /// </summary>
        [JsonProperty(PropertyName = "referenceTypeId",
            NullValueHandling = NullValueHandling.Ignore)]
        public string ReferenceTypeId { get; set; }

        /// <summary>
        /// Whether to include subtypes of the reference type.
        /// (default is false)
        /// </summary>
        [JsonProperty(PropertyName = "noSubtypes",
            NullValueHandling = NullValueHandling.Ignore)]
        public bool? NoSubtypes { get; set; }

        /// <summary>
        /// If not set, implies default client value, 0
        /// to exclude references.
        /// </summary>
        [JsonProperty(PropertyName = "maxReferencesToReturn",
            NullValueHandling = NullValueHandling.Ignore)]
        public uint? MaxReferencesToReturn { get; set; }

        /// <summary>
        /// Optional User elevation
        /// </summary>
        [JsonProperty(PropertyName = "elevation",
            NullValueHandling = NullValueHandling.Ignore)]
        public AuthenticationApiModel Elevation { get; set; }
    }
}
