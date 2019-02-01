// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------


using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.Azure.IIoT.OpcUa.Services.Vault.Runtime;
using Newtonsoft.Json;

namespace Microsoft.Azure.IIoT.OpcUa.Services.Vault.v1.Models
{
    public sealed class StatusApiModel
    {
        private const string DateFormat = "yyyy-MM-dd'T'HH:mm:sszzz";
        private string appMessage;
        private string kvMessage;

        [JsonProperty(PropertyName = "name", Order = 10)]
        public string Name => "OpcVault";

        [JsonProperty(PropertyName = "status", Order = 20)]
        public string Status { get; set; }

        [JsonProperty(PropertyName = "currentTime", Order = 30)]
        public string CurrentTime => DateTimeOffset.UtcNow.ToString(DateFormat);

        [JsonProperty(PropertyName = "startTime", Order = 40)]
        public string StartTime => Uptime.Start.ToString(DateFormat);

        [JsonProperty(PropertyName = "upTime", Order = 50)]
        public long UpTime => Convert.ToInt64(Uptime.Duration.TotalSeconds);

        /// <summary>
        /// Value generated at bootstrap by each instance of the service and
        /// used to correlate logs coming from the same instance. The value
        /// changes every time the service starts.
        /// </summary>
        [JsonProperty(PropertyName = "uid", Order = 60)]
        public string UID => Uptime.ProcessId;

        /// <summary>A property bag with details about the service</summary>
        [JsonProperty(PropertyName = "properties", Order = 70)]
        public Dictionary<string, string> Properties => new Dictionary<string, string>
        {
            { "Culture", Thread.CurrentThread.CurrentCulture.Name },
            { "Debugger", System.Diagnostics.Debugger.IsAttached ? "Attached" : "Detached"}
        };

        /// <summary>A property bag with details about the internal dependencies</summary>
        [JsonProperty(PropertyName = "dependencies", Order = 80)]
        public Dictionary<string, string> Dependencies => new Dictionary<string, string>
        {
            { "ApplicationDatabase", appMessage },
            { "KeyVault", kvMessage }
        };

        [JsonProperty(PropertyName = "$metadata", Order = 1000)]
        public Dictionary<string, string> Metadata => new Dictionary<string, string>
        {
            { "$type", "Status;" + VersionInfo.NUMBER },
            { "$uri", "/" + VersionInfo.PATH + "/status" }
        };

        public StatusApiModel(
            bool appOk,
            string appMessage,
            bool kvOk,
            string kvMessage
            )
        {
            this.Status = appOk && kvOk ? "OK" : "ERROR";
            this.appMessage = appOk ? "OK" : "ERROR";
            if (!string.IsNullOrEmpty(appMessage))
            {
                this.appMessage += ":" + appMessage;
            }
            this.kvMessage = kvOk ? "OK" : "ERROR";
            if (!string.IsNullOrEmpty(kvMessage))
            {
                this.kvMessage += ":" + kvMessage;
            }
        }
    }
}
