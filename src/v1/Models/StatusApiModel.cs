// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------


using System;
using System.Collections.Generic;
using Microsoft.Azure.IIoT.OpcUa.Services.GdsVault.Runtime;
using Newtonsoft.Json;

namespace Microsoft.Azure.IIoT.OpcUa.Services.GdsVault.v1.Models
{
    public sealed class StatusApiModel
    {
        private const string DateFormat = "yyyy-MM-dd'T'HH:mm:sszzz";

        [JsonProperty(PropertyName = "Name", Order = 10)]
        public string Name => "GdsVault";

        [JsonProperty(PropertyName = "Status", Order = 20)]
        public string Status { get; set; }

        [JsonProperty(PropertyName = "CurrentTime", Order = 30)]
        public string CurrentTime => DateTimeOffset.UtcNow.ToString(DateFormat);

        [JsonProperty(PropertyName = "StartTime", Order = 40)]
        public string StartTime => Uptime.Start.ToString(DateFormat);

        [JsonProperty(PropertyName = "UpTime", Order = 50)]
        public long UpTime => Convert.ToInt64(Uptime.Duration.TotalSeconds);

        /// <summary>
        /// Value generated at bootstrap by each instance of the service and
        /// used to correlate logs coming from the same instance. The value
        /// changes every time the service starts.
        /// </summary>
        [JsonProperty(PropertyName = "UID", Order = 60)]
        public string UID => Uptime.ProcessId;

        /// <summary>A property bag with details about the service</summary>
        [JsonProperty(PropertyName = "Properties", Order = 70)]
        public Dictionary<string, string> Properties => new Dictionary<string, string>
        {
            { "Foo", "Bar" },
            { "Simulation", "on" },
            { "Region", "US" },
            { "DebugMode", "off" }
        };

        /// <summary>A property bag with details about the internal dependencies</summary>
        [JsonProperty(PropertyName = "Dependencies", Order = 80)]
        public Dictionary<string, string> Dependencies => new Dictionary<string, string>
        {
            // TODO: implement meaningful status output
            { "KeyVaultAPI", "OK:...msg..." }
        };

        [JsonProperty(PropertyName = "$metadata", Order = 1000)]
        public Dictionary<string, string> Metadata => new Dictionary<string, string>
        {
            { "$type", "Status;" + VersionInfo.NUMBER },
            { "$uri", "/" + VersionInfo.PATH + "/status" }
        };

        public StatusApiModel(bool isOk, string msg)
        {
            this.Status = isOk ? "OK" : "ERROR";
            if (!string.IsNullOrEmpty(msg))
            {
                this.Status += ":" + msg;
            }
        }
    }
}
