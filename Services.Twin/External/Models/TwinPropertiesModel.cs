// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.Azure.IIoT.OpcTwin.Services.External.Models {
    using System.Collections.Generic;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    public class TwinPropertiesModel {

        [JsonProperty(PropertyName = "Reported", 
            NullValueHandling = NullValueHandling.Ignore)]
        public Dictionary<string, JToken> Reported { get; set; }

        [JsonProperty(PropertyName = "Desired", 
            NullValueHandling = NullValueHandling.Ignore)]
        public Dictionary<string, JToken> Desired { get; set; }
    }
}
