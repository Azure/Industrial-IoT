// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Services.OpcUa.Vault.v2.Models {
    using Microsoft.Azure.IIoT.Services.OpcUa.Vault.Runtime;
    using Newtonsoft.Json;
    using System.Collections.Generic;

    /// <summary>
    /// Status model
    /// </summary>
    public sealed class StatusResponseApiModel {

        /// <summary>
        /// Default constructor
        /// </summary>
        public StatusResponseApiModel() { }

        /// <summary>
        /// Create status model
        /// </summary>
        /// <param name="isOk"></param>
        /// <param name="msg"></param>
        public StatusResponseApiModel(bool isOk, string msg) {
            Status = isOk ? "OK" : "ERROR";
            if (!string.IsNullOrEmpty(msg)) {
                Status += ":" + msg;
            }
        }

        /// <summary>
        /// Name of this service
        /// </summary>
        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }

        /// <summary>
        /// Operational status
        /// </summary>
        [JsonProperty(PropertyName = "status")]
        public string Status { get; set; }

        /// <summary>
        /// Current time
        /// </summary>
        [JsonProperty(PropertyName = "currentTime")]
        public string CurrentTime =>
            System.DateTimeOffset.UtcNow.ToString(DATE_FORMAT);

        /// <summary>
        /// Start time of service
        /// </summary>
        [JsonProperty(PropertyName = "startTime")]
        public string StartTime =>
            Uptime.Start.ToString(DATE_FORMAT);

        /// <summary>
        /// Up time of service
        /// </summary>
        [JsonProperty(PropertyName = "upTime")]
        public long UpTime =>
            System.Convert.ToInt64(Uptime.Duration.TotalSeconds);

        /// <summary>
        /// Value generated at bootstrap by each instance of the service and
        /// used to correlate logs coming from the same instance. The value
        /// changes every time the service starts.
        /// </summary>
        [JsonProperty(PropertyName = "uid")]
        public string UID =>
            Uptime.ProcessId;

        /// <summary>
        /// A property bag with details about the service
        /// </summary>
        [JsonProperty(PropertyName = "properties")]
        public Dictionary<string, string> Properties =>
            new Dictionary<string, string>();

        /// <summary>
        /// A property bag with details about the internal dependencies
        /// </summary>
        [JsonProperty(PropertyName = "dependencies")]
        public Dictionary<string, string> Dependencies =>
            new Dictionary<string, string> {
                { "KeyVault", Status }
            };

        /// <summary>
        /// Optional meta data.
        /// </summary>
        [JsonProperty(PropertyName = "$metadata")]
        public Dictionary<string, string> Metadata =>
            new Dictionary<string, string> {
                { "$type", "Status;" + VersionInfo.NUMBER },
                { "$uri", "/" + VersionInfo.PATH + "/status" }
            };

        private const string DATE_FORMAT = "yyyy-MM-dd'T'HH:mm:sszzz";
    }
}
