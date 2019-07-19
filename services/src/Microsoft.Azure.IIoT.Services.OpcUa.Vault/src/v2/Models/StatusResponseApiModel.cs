// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Services.OpcUa.Vault.v2.Models {
    using Microsoft.Azure.IIoT.Services.OpcUa.Vault.Runtime;
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.Threading;

    /// <summary>
    /// Status model
    /// </summary>
    public sealed class StatusResponseApiModel {

        /// <summary>
        /// Service name
        /// </summary>
        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }

        /// <summary>
        /// Status
        /// </summary>
        [JsonProperty(PropertyName = "status")]
        public string Status { get; set; }

        /// <summary>
        /// Current time
        /// </summary>
        [JsonProperty(PropertyName = "currentTime")]
        public string CurrentTime => DateTimeOffset.UtcNow.ToString(kDateFormat);

        /// <summary>
        /// Service start time
        /// </summary>
        [JsonProperty(PropertyName = "startTime")]
        public string StartTime => Uptime.Start.ToString(kDateFormat);

        /// <summary>
        /// Uptime
        /// </summary>
        [JsonProperty(PropertyName = "upTime")]
        public long UpTime => Convert.ToInt64(Uptime.Duration.TotalSeconds);

        /// <summary>
        /// Value generated at bootstrap by each instance of the service and
        /// used to correlate logs coming from the same instance. The value
        /// changes every time the service starts.
        /// </summary>
        [JsonProperty(PropertyName = "uid")]
        public string UID => Uptime.ProcessId;

        /// <summary>
        /// A property bag with details about the service
        /// </summary>
        [JsonProperty(PropertyName = "properties")]
        public Dictionary<string, string> Properties => new Dictionary<string, string>
        {
            { "Culture", Thread.CurrentThread.CurrentCulture.Name },
            { "Debugger", System.Diagnostics.Debugger.IsAttached ? "Attached" : "Detached"}
        };

        /// <summary>
        /// A property bag with details about the internal dependencies
        /// </summary>
        [JsonProperty(PropertyName = "dependencies")]
        public Dictionary<string, string> Dependencies => new Dictionary<string, string>
        {
            { "KeyVault", _kvMessage }
        };

        /// <summary>
        /// Meta data
        /// </summary>
        [JsonProperty(PropertyName = "$metadata")]
        public Dictionary<string, string> Metadata => new Dictionary<string, string>
        {
            { "$type", "Status;" + VersionInfo.NUMBER },
            { "$uri", "/" + VersionInfo.PATH + "/status" }
        };

        /// <summary>
        /// Create model
        /// </summary>
        /// <param name="kvOk"></param>
        /// <param name="kvMessage"></param>
        public StatusResponseApiModel(bool kvOk, string kvMessage) {
            Status = kvOk ? "OK" : "ERROR";
            _kvMessage = kvOk ? "OK" : "ERROR";
            if (!string.IsNullOrEmpty(kvMessage)) {
                _kvMessage += ":" + kvMessage;
            }
        }

        private const string kDateFormat = "yyyy-MM-dd'T'HH:mm:sszzz";
        private readonly string _kvMessage;
    }
}
