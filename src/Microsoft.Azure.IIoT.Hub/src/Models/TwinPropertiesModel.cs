// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.Azure.IIoT.Hub.Models {
    using System.Collections.Generic;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    public class TwinPropertiesModel {

        /// <summary>
        /// Reported settings
        /// </summary>
        [JsonProperty(PropertyName = "reported",
            NullValueHandling = NullValueHandling.Ignore)]
        public Dictionary<string, JToken> Reported { get; set; }

        /// <summary>
        /// Desired settings
        /// </summary>
        [JsonProperty(PropertyName = "desired",
            NullValueHandling = NullValueHandling.Ignore)]
        public Dictionary<string, JToken> Desired { get; set; }
    }
}
