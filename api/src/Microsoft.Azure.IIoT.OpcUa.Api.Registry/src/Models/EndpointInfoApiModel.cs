// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Registry.Models {
    using System.Runtime.Serialization;
    using System;
    using System.ComponentModel.DataAnnotations;

    /// <summary>
    /// Endpoint registration model
    /// </summary>
    [DataContract]
    public class EndpointInfoApiModel {

        /// <summary>
        /// Endpoint registration
        /// </summary>
        [DataMember(Name = "registration", Order = 0)]
        [Required]
        public EndpointRegistrationApiModel Registration { get; set; }

        /// <summary>
        /// Application id endpoint is registered under.
        /// </summary>
        [DataMember(Name = "applicationId", Order = 1)]
        [Required]
        public string ApplicationId { get; set; }

        /// <summary>
        /// Activation state of endpoint
        /// </summary>
        [DataMember(Name = "activationState", Order = 2,
            EmitDefaultValue = false)]
        public EndpointActivationState? ActivationState { get; set; }

        /// <summary>
        /// Last state of the activated endpoint
        /// </summary>
        [DataMember(Name = "endpointState", Order = 3,
            EmitDefaultValue = false)]
        public EndpointConnectivityState? EndpointState { get; set; }

        /// <summary>
        /// Whether the registration is out of sync
        /// </summary>
        [DataMember(Name = "outOfSync", Order = 4,
            EmitDefaultValue = false)]
        public bool? OutOfSync { get; set; }

        /// <summary>
        /// Last time endpoint was seen
        /// </summary>
        [DataMember(Name = "notSeenSince", Order = 5,
            EmitDefaultValue = false)]
        public DateTime? NotSeenSince { get; set; }

        /// <summary>
        /// Legacy activation state
        /// </summary>
        [Obsolete("Use ActivationState")]
        [IgnoreDataMember]
        public bool? Activated =>
            ActivationState == EndpointActivationState.Activated || Connected == true;

        /// <summary>
        /// Legacy connectivity state
        /// </summary>
        [Obsolete("Use ActivationState")]
        [IgnoreDataMember]
        public bool? Connected =>
            ActivationState == EndpointActivationState.ActivatedAndConnected;
    }
}
