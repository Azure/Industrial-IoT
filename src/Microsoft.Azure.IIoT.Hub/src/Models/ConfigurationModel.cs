// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Hub.Models {
    using System;
    using System.Collections.Generic;
    using Newtonsoft.Json;

    public class ConfigurationModel {

        /// <summary>
        /// Configuration Identifier
        /// </summary>
        [JsonProperty(PropertyName = "id",
            Required = Required.Always)]
        public string Id { get; set; }

        /// <summary>
        /// The etag
        /// </summary>
        [JsonProperty(PropertyName = "etag")]
        public string Etag { get; set; }

        /// <summary>
        /// Gets Schema version for the configuration
        /// </summary>
        [JsonProperty(PropertyName = "schemaVersion",
            Required = Required.Always)]
        public string SchemaVersion { get; set; }

        /// <summary>
        /// Gets or sets labels for the configuration
        /// </summary>
        [JsonProperty(PropertyName = "labels",
            NullValueHandling = NullValueHandling.Ignore)]
        public IDictionary<string, string> Labels { get; set; }

        /// <summary>
        /// Gets or sets Content for the configuration
        /// </summary>
        [JsonProperty(PropertyName = "content",
            NullValueHandling = NullValueHandling.Ignore)]
        public ConfigurationContentModel Content { get; set; }

        /// <summary>
        /// Gets the content type for configuration
        /// </summary>
        [JsonProperty(PropertyName = "contentType")]
        public string ContentType { get; set; }

        /// <summary>
        /// Gets or sets Target Condition for the configuration
        /// </summary>
        [JsonProperty(PropertyName = "targetCondition")]
        public string TargetCondition { get; set; }

        /// <summary>
        /// Gets creation time for the configuration
        /// </summary>
        [JsonProperty(PropertyName = "createdTimeUtc")]
        public DateTime CreatedTimeUtc { get; set; }

        /// <summary>
        /// Gets last update time for the configuration
        /// </summary>
        [JsonProperty(PropertyName = "lastUpdatedTimeUtc")]
        public DateTime LastUpdatedTimeUtc { get; set; }

        /// <summary>
        /// Gets or sets Priority for the configuration
        /// </summary>
        [JsonProperty(PropertyName = "priority")]
        public int Priority { get; set; }

        /// <summary>
        /// System Configuration Metrics
        /// </summary>
        [JsonProperty(PropertyName = "systemMetrics",
            NullValueHandling = NullValueHandling.Ignore)]
        public ConfigurationMetricsModel SystemMetrics { get; set; }

        /// <summary>
        /// Custom Configuration Metrics
        /// </summary>
        [JsonProperty(PropertyName = "metrics",
            NullValueHandling = NullValueHandling.Ignore)]
        public ConfigurationMetricsModel Metrics { get; set; }
    }
}
