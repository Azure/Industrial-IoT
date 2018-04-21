// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcTwin.WebService.Client.Models {
    using Newtonsoft.Json;
    using System.Collections.Generic;

    /// <summary>
    /// call request model for webservice api
    /// </summary>
    public class MethodCallRequestApiModel {
        /// <summary>
        /// Method id of method to call
        /// </summary>
        [JsonProperty(PropertyName = "methodId")]
        public string MethodId { get; set; }

        /// <summary>
        /// If not global (= null), object or type scope
        /// </summary>
        [JsonProperty(PropertyName = "objectId",
            NullValueHandling = NullValueHandling.Ignore)]
        public string ObjectId { get; set; }

        /// <summary>
        /// Arguments for the method - null means no args
        /// </summary>
        [JsonProperty(PropertyName = "arguments",
            NullValueHandling = NullValueHandling.Ignore)]
        public List<MethodArgumentApiModel> Arguments { get; set; }
    }
}
