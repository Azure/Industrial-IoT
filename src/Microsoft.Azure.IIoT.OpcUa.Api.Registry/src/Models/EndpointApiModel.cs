// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Registry.Models {
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
    /// Endpoint model
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
        [JsonProperty(PropertyName = "user",
            NullValueHandling = NullValueHandling.Ignore)]
        public CredentialApiModel User { get; set; }

        /// <summary>
        /// Security Mode to use for communication.
        /// default to best.
        /// </summary>
        [JsonProperty(PropertyName = "securityMode",
            NullValueHandling = NullValueHandling.Ignore)]
        public SecurityMode? SecurityMode { get; set; }

        /// <summary>
        /// Security policy uri to use for communication.
        /// default to best.
        /// </summary>
        [JsonProperty(PropertyName = "securityPolicy",
            NullValueHandling = NullValueHandling.Ignore)]
        public string SecurityPolicy { get; set; }

        /// <summary>
        /// Thumbprint to validate against or null to trust any.
        /// </summary>
        [JsonProperty(PropertyName = "serverThumbprint",
            NullValueHandling = NullValueHandling.Ignore)]
        public byte[] ServerThumbprint { get; set; }
    }
}
