// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Models {
    using Newtonsoft.Json;
    using System;
    using Newtonsoft.Json.Converters;

    /// <summary>
    /// Type of callback method to use
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum CallbackMethodType {
        Get,
        Post,
        Put,
        Delete
    }

    /// <summary>
    /// A registered callback
    /// </summary>
    public class CallbackApiModel {

        /// <summary>
        /// Uri to call - should use https scheme in which
        /// case security is enforced.
        /// </summary>
        [JsonProperty(PropertyName = "uri")]
        public Uri Uri { get; set; }

        /// <summary>
        /// Method to use for callback
        /// </summary>
        [JsonProperty(PropertyName = "method",
            NullValueHandling = NullValueHandling.Ignore)]
        public CallbackMethodType? Method { get; set; }

        /// <summary>
        /// Authentication header to add or null if not needed
        /// </summary>
        [JsonProperty(PropertyName = "authenticationHeader",
            NullValueHandling = NullValueHandling.Ignore)]
        public string AuthenticationHeader { get; set; }
    }
}
