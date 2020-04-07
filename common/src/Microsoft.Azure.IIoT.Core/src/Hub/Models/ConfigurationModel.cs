// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Hub.Models {
    using System;
    using System.Collections.Generic;
    using System.Runtime.Serialization;

    /// <summary>
    /// Configuration
    /// </summary>
    [DataContract]
    public class ConfigurationModel {

        /// <summary>
        /// Configuration Identifier
        /// </summary>
        [DataMember(Name = "id",
            IsRequired = true)]
        public string Id { get; set; }

        /// <summary>
        /// The etag
        /// </summary>
        [DataMember(Name = "etag")]
        public string Etag { get; set; }

        /// <summary>
        /// Gets Schema version for the configuration
        /// </summary>
        [DataMember(Name = "schemaVersion",
            IsRequired = true)]
        public string SchemaVersion { get; set; }

        /// <summary>
        /// Gets or sets labels for the configuration
        /// </summary>
        [DataMember(Name = "labels",
            EmitDefaultValue = false)]
        public IDictionary<string, string> Labels { get; set; }

        /// <summary>
        /// Gets or sets Content for the configuration
        /// </summary>
        [DataMember(Name = "content",
            EmitDefaultValue = false)]
        public ConfigurationContentModel Content { get; set; }

        /// <summary>
        /// Gets the content type for configuration
        /// </summary>
        [DataMember(Name = "contentType")]
        public string ContentType { get; set; }

        /// <summary>
        /// Gets or sets Target Condition for the configuration
        /// </summary>
        [DataMember(Name = "targetCondition")]
        public string TargetCondition { get; set; }

        /// <summary>
        /// Gets creation time for the configuration
        /// </summary>
        [DataMember(Name = "createdTimeUtc")]
        public DateTime CreatedTimeUtc { get; set; }

        /// <summary>
        /// Gets last update time for the configuration
        /// </summary>
        [DataMember(Name = "lastUpdatedTimeUtc")]
        public DateTime LastUpdatedTimeUtc { get; set; }

        /// <summary>
        /// Gets or sets Priority for the configuration
        /// </summary>
        [DataMember(Name = "priority")]
        public int Priority { get; set; }

        /// <summary>
        /// System Configuration Metrics
        /// </summary>
        [DataMember(Name = "systemMetrics",
            EmitDefaultValue = false)]
        public ConfigurationMetricsModel SystemMetrics { get; set; }

        /// <summary>
        /// Custom Configuration Metrics
        /// </summary>
        [DataMember(Name = "metrics",
            EmitDefaultValue = false)]
        public ConfigurationMetricsModel Metrics { get; set; }
    }
}
