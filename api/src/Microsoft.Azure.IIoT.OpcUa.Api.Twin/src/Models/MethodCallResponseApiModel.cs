// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Twin.Models {
    using Newtonsoft.Json;
    using System.Collections.Generic;

    /// <summary>
    /// Method call response model
    /// </summary>
    public class MethodCallResponseApiModel {

        /// <summary>
        /// Output results
        /// </summary>
        [JsonProperty(PropertyName = "results")]
        public List<MethodCallArgumentApiModel> Results { get; set; }

        /// <summary>
        /// Service result in case of error
        /// </summary>
        [JsonProperty(PropertyName = "errorInfo",
            NullValueHandling = NullValueHandling.Ignore)]
        public ServiceResultApiModel ErrorInfo { get; set; }
    }
}
