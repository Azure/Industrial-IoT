// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Registry.Models {
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;

    /// <summary>
    /// Publisher event type
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum PublisherEventType {

        /// <summary>
        /// New
        /// </summary>
        New,

        /// <summary>
        /// Updated
        /// </summary>
        Updated,

        /// <summary>
        /// Connected
        /// </summary>
        Connected,

        /// <summary>
        /// Disconnected
        /// </summary>
        Disconnected,

        /// <summary>
        /// Deleted
        /// </summary>
        Deleted,
    }

    /// <summary>
    /// Publisher event
    /// </summary>
    public class PublisherEventApiModel {

        /// <summary>
        /// Event type
        /// </summary>
        [JsonProperty(PropertyName = "eventType")]
        public PublisherEventType EventType { get; set; }

        /// <summary>
        /// Application
        /// </summary>
        [JsonProperty(PropertyName = "publisher",
            NullValueHandling = NullValueHandling.Ignore)]
        public PublisherApiModel Publisher { get; set; }
    }
}