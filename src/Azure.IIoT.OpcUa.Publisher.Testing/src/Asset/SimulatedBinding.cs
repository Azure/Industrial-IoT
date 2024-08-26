// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

#nullable enable

namespace Asset
{
    using Newtonsoft.Json;

    public sealed class SimulatedForm : Form
    {
        [JsonProperty("sim:type")]
        public string? PayloadType { get; set; }
        [JsonProperty("sim:array")]
        public bool IsArray { get; set; }
        [JsonProperty("sim:pollingTime")]
        public long PollingTime { get; set; }
    }
}
