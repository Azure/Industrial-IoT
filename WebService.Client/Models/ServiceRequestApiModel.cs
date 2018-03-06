// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IoTSolutions.OpcTwin.WebService.Client.Models {
    using Newtonsoft.Json;

    /// <summary>
    /// Endpoint model for webservice api
    /// </summary>
    public class ServiceRequestApiModel<T> {
        /// <summary>
        /// Request
        /// </summary>
        [JsonProperty(PropertyName = "request")]
        public T Request { get; set; }

        /// <summary>
        /// User name to use
        /// </summary>
        [JsonProperty(PropertyName = "endpoint")]
        public EndpointApiModel Endpoint { get; set; }
    }
}
