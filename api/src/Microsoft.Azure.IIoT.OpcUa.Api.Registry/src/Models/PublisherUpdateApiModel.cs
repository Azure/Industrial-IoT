// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Registry.Models {
    using Newtonsoft.Json;

    /// <summary>
    /// Publisher registration update request
    /// </summary>
    public class PublisherUpdateApiModel {

        /// <summary>
        /// Site of the publisher
        /// </summary>
        [JsonProperty(PropertyName = "siteId",
            NullValueHandling = NullValueHandling.Ignore)]
        public string SiteId { get; set; }

        /// <summary>
        /// Publisher discovery configuration
        /// </summary>
        [JsonProperty(PropertyName = "configuration",
            NullValueHandling = NullValueHandling.Ignore)]
        public PublisherConfigApiModel Configuration { get; set; }

        /// <summary>
        /// Current log level
        /// </summary>
        [JsonProperty(PropertyName = "logLevel",
            NullValueHandling = NullValueHandling.Ignore)]
        public TraceLogLevel? LogLevel { get; set; }
    }
}
