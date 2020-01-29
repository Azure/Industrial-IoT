// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Hub.Models {
    using System.Collections.Generic;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// Twin properties
    /// </summary>
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
