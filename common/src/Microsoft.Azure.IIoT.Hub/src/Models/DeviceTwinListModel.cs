// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Hub.Models {
    using System.Collections.Generic;
    using Newtonsoft.Json;

    /// <summary>
    /// List of device twins with continuation token
    /// </summary>
    public class DeviceTwinListModel {

        /// <summary>
        /// Continuation token to use for next call or null
        /// </summary>
        [JsonProperty(PropertyName = "continuationToken",
            NullValueHandling = NullValueHandling.Ignore)]
        public string ContinuationToken { get; set; }

        /// <summary>
        /// Items returned
        /// </summary>
        [JsonProperty(PropertyName = "items")]
        public List<DeviceTwinModel> Items { get; set; }
    }
}
