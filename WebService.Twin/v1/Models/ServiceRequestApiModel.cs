// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcTwin.WebService.v1.Models {
    using Newtonsoft.Json;
    using System.ComponentModel.DataAnnotations;

    /// <summary>
    /// Service request model for webservice api
    /// </summary>
    public class ServiceRequestApiModel<T> {
        /// <summary>
        /// Default constructor
        /// </summary>
        public ServiceRequestApiModel() { }

        /// <summary>
        /// Create service request
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="content"></param>
        public ServiceRequestApiModel(EndpointApiModel endpoint, T content) {
            Endpoint = endpoint;
            Content = content;
        }

        /// <summary>
        /// User name to use
        /// </summary>
        [JsonProperty(PropertyName = "endpoint")]
        [Required]
        public EndpointApiModel Endpoint { get; set; }

        /// <summary>
        /// Request
        /// </summary>
        [JsonProperty(PropertyName = "content")]
        [Required]
        public T Content { get; set; }
    }
}
