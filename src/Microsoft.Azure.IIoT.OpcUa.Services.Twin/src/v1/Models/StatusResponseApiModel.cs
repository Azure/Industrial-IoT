// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Services.Twin.v1.Models {
    using Microsoft.Azure.IIoT.OpcUa.Services.Twin.v1;
    using Microsoft.Azure.IIoT.OpcUa.Services.Twin.Runtime;
    using Newtonsoft.Json;
    using System.Collections.Generic;

    /// <summary>
    /// Status response model
    /// </summary>
    public class StatusResponseApiModel {

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
        [JsonProperty(PropertyName = "Name", Order = 10)]
        public string Name => ServiceInfo.NAME;

        /// <summary>
        /// Operational status
        /// </summary>
        [JsonProperty(PropertyName = "Status", Order = 20)]
        public string Status { get; set; }

        /// <summary>
        /// Current time
        /// </summary>
        [JsonProperty(PropertyName = "CurrentTime", Order = 30)]
        public string CurrentTime =>
            System.DateTimeOffset.UtcNow.ToString(DATE_FORMAT);

        /// <summary>
        /// Start time of service
        /// </summary>
        [JsonProperty(PropertyName = "StartTime", Order = 40)]
        public string StartTime =>
            Uptime.Start.ToString(DATE_FORMAT);

        /// <summary>
        /// Up time of service
        /// </summary>
        [JsonProperty(PropertyName = "UpTime", Order = 50)]
        public long UpTime =>
            System.Convert.ToInt64(Uptime.Duration.TotalSeconds);

        /// <summary>
        /// Value generated at bootstrap by each instance of the service and
        /// used to correlate logs coming from the same instance. The value
        /// changes every time the service starts.
        /// </summary>
        [JsonProperty(PropertyName = "UID", Order = 60)]
        public string UID =>
            Uptime.ProcessId;

        /// <summary>
        /// A property bag with details about the service
        /// </summary>
        [JsonProperty(PropertyName = "Properties", Order = 70)]
        public Dictionary<string, string> Properties =>
            new Dictionary<string, string>();

        /// <summary>
        /// A property bag with details about the internal dependencies
        /// </summary>
        [JsonProperty(PropertyName = "Dependencies", Order = 80)]
        public Dictionary<string, string> Dependencies =>
            new Dictionary<string, string>
            {
                { "IoTHub", "OK:..." }
            };

        /// <summary>
        /// Optional meta data.
        /// </summary>
        [JsonProperty(PropertyName = "$metadata", Order = 1000)]
        public Dictionary<string, string> Metadata =>
            new Dictionary<string, string>
            {
                { "$type", "Status;" + VersionInfo.NUMBER },
                { "$uri", "/" + VersionInfo.PATH + "/status" }
            };

        private const string DATE_FORMAT = "yyyy-MM-dd'T'HH:mm:sszzz";
    }
}
