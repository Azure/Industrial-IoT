// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Models {
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;

    /// <summary>
    /// Security mode of endpoint
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum SecurityMode {

        /// <summary>
        /// Best
        /// </summary>
        Best,

        /// <summary>
        /// Sign
        /// </summary>
        Sign,

        /// <summary>
        /// Sign and Encrypt
        /// </summary>
        SignAndEncrypt,

        /// <summary>
        /// No security
        /// </summary>
        None
    }

    /// <summary>
    /// Endpoint model for webservice api
    /// </summary>
    public class EndpointApiModel {

        /// <summary>
        /// Endpoint
        /// </summary>
        [JsonProperty(PropertyName = "url")]
        public string Url { get; set; }

        /// <summary>
        /// User Authentication
        /// </summary>
        [JsonProperty(PropertyName = "authentication",
            NullValueHandling = NullValueHandling.Ignore)]
        public AuthenticationApiModel Authentication { get; set; }

        /// <summary>
        /// Security Mode to use for communication - default to best.
        /// </summary>
        [JsonProperty(PropertyName = "securityMode",
            NullValueHandling = NullValueHandling.Ignore)]
        public SecurityMode? SecurityMode { get; set; }

        /// <summary>
        /// Security policy uri to use for communication - default to best.
        /// </summary>
        [JsonProperty(PropertyName = "securityPolicy",
            NullValueHandling = NullValueHandling.Ignore)]
        public string SecurityPolicy { get; set; }

        /// <summary>
        /// Certificate to validate against or null to trust any.
        /// </summary>
        [JsonProperty(PropertyName = "validation",
            NullValueHandling = NullValueHandling.Ignore)]
        public byte[] Validation { get; set; }
    }
}
