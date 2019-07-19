// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Registry.Models {
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;
    using System;

    /// <summary>
    /// State of the endpoint after activation
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum EndpointConnectivityState {

        /// <summary>
        /// Client connecting to endpoint
        /// </summary>
        Connecting,

        /// <summary>
        /// Server not reachable
        /// </summary>
        NotReachable,

        /// <summary>
        /// Server busy - try later
        /// </summary>
        Busy,

        /// <summary>
        /// Client is not trusted - update client cert
        /// </summary>
        NoTrust,

        /// <summary>
        /// Server certificate is invalid - update server certificate
        /// </summary>
        CertificateInvalid,

        /// <summary>
        /// Connected and ready
        /// </summary>
        Ready,

        /// <summary>
        /// Any other connection error
        /// </summary>
        Error
    }

    /// <summary>
    /// Activation state of the endpoint twin
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum EndpointActivationState {

        /// <summary>
        /// Endpoint twin is deactivated (default)
        /// </summary>
        Deactivated,

        /// <summary>
        /// Endpoint twin is activated but not connected
        /// </summary>
        Activated,

        /// <summary>
        /// Endoint twin is activated and connected to hub
        /// </summary>
        ActivatedAndConnected
    }

    /// <summary>
    /// Endpoint registration model
    /// </summary>
    public class EndpointInfoApiModel {

        /// <summary>
        /// Endpoint registration
        /// </summary>
        [JsonProperty(PropertyName = "registration")]
        public EndpointRegistrationApiModel Registration { get; set; }

        /// <summary>
        /// Application id endpoint is registered with.
        /// </summary>
        [JsonProperty(PropertyName = "applicationId")]
        public string ApplicationId { get; set; }

        /// <summary>
        /// Activation state of endpoint
        /// </summary>
        [JsonProperty(PropertyName = "activationState",
            NullValueHandling = NullValueHandling.Ignore)]
        public EndpointActivationState? ActivationState { get; set; }

        /// <summary>
        /// Last connectivity state of the activated endpoint
        /// </summary>
        [JsonProperty(PropertyName = "endpointState",
            NullValueHandling = NullValueHandling.Ignore)]
        public EndpointConnectivityState? EndpointState { get; set; }

        /// <summary>
        /// Whether the registration is out of sync
        /// </summary>
        [JsonProperty(PropertyName = "outOfSync",
            NullValueHandling = NullValueHandling.Ignore)]
        public bool? OutOfSync { get; set; }

        /// <summary>
        /// Last time endpoint was seen
        /// </summary>
        [JsonProperty(PropertyName = "notSeenSince",
            NullValueHandling = NullValueHandling.Ignore)]
        public DateTime? NotSeenSince { get; set; }

        /// <summary>
        /// Legacy activation state
        /// </summary>
        [Obsolete("Use ActivationState")]
        [JsonIgnore]
        public bool? Activated =>
            ActivationState == EndpointActivationState.Activated || Connected == true;

        /// <summary>
        /// Legacy connectivity state
        /// </summary>
        [Obsolete("Use ActivationState")]
        [JsonIgnore]
        public bool? Connected =>
            ActivationState == EndpointActivationState.ActivatedAndConnected;
    }
}
