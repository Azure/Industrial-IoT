// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Registry.Models {
    using Microsoft.Azure.IIoT.OpcUa.Core.Models;
    using Microsoft.Azure.IIoT.Hub;
    using System.Runtime.Serialization;
    using Microsoft.Azure.IIoT.Serializers;
    using System.Collections.Generic;

    /// <summary>
    /// Twin (endpoint) registration persisted and comparable
    /// </summary>
    [DataContract]
    public sealed class EndpointRegistration : EntityRegistration {

        /// <inheritdoc/>
        [DataMember]
        public override string DeviceType => IdentityType.Endpoint;

        /// <summary>
        /// Device id is twin id
        /// </summary>
        [DataMember]
        public override string DeviceId => base.DeviceId ?? Id;

        /// <summary>
        /// Site or gateway id
        /// </summary>
        [DataMember]
        public override string SiteOrGatewayId => this.GetSiteOrGatewayId();

        /// <summary>
        /// Identity that owns the twin.
        /// </summary>
        [DataMember]
        public string DiscovererId { get; set; }

        /// <summary>
        /// Identity that manages the endpoint twin.
        /// </summary>
        [DataMember]
        public string SupervisorId { get; set; }

        /// <summary>
        /// Application id of twin
        /// </summary>
        [DataMember]
        public string ApplicationId { get; set; }

        /// <summary>
        /// Lower case endpoint url
        /// </summary>
        [DataMember]
        public string EndpointUrlLC =>
            EndpointRegistrationUrl?.ToLowerInvariant();

        /// <summary>
        /// Reported endpoint description url as opposed to the
        /// one that can be used to connect with.
        /// </summary>
        [DataMember]
        public string EndpointRegistrationUrl { get; set; }

        /// <summary>
        /// Security level of endpoint
        /// </summary>
        [DataMember]
        public int? SecurityLevel { get; set; }

        /// <summary>
        /// Whether endpoint is activated
        /// </summary>
        [DataMember]
        public bool? Activated { get; set; }

        /// <summary>
        /// The credential policies supported by the registered endpoint
        /// </summary>
        [DataMember]
        public Dictionary<string, VariantValue> AuthenticationMethods { get; set; }

        /// <summary>
        /// Endoint url for direct server access
        /// </summary>
        [DataMember]
        public string EndpointUrl { get; set; }

        /// <summary>
        /// Alternative urls
        /// </summary>
        [DataMember]
        public Dictionary<string, string> AlternativeUrls { get; set; }

        /// <summary>
        /// Endpoint security policy to use.
        /// </summary>
        [DataMember]
        public string SecurityPolicy { get; set; }

        /// <summary>
        /// Security mode to use for communication
        /// </summary>
        [DataMember]
        public SecurityMode? SecurityMode { get; set; }

        /// <summary>
        /// Endpoint connectivity status
        /// </summary>
        [DataMember]
        public EndpointConnectivityState State { get; set; }

        /// <summary>
        /// Certificate Thumbprint
        /// </summary>
        [DataMember]
        public string Thumbprint { get; set; }

        /// <summary>
        /// Device id is the endpoint id
        /// </summary>
        [DataMember(Name = "id")]
        public string Id => EndpointInfoModelEx.CreateEndpointId(
            ApplicationId, EndpointRegistrationUrl, SecurityMode, SecurityPolicy);

        /// <summary>
        /// Activation state
        /// </summary>
        /// <returns></returns>
        public EndpointActivationState? ActivationState {
            get {
                if (Activated == true) {
                    if (Connected) {
                        return EndpointActivationState.ActivatedAndConnected;
                    }
                    return EndpointActivationState.Activated;
                }
                return EndpointActivationState.Deactivated;
            }
            set {
                if (value == EndpointActivationState.Activated ||
                    value == EndpointActivationState.ActivatedAndConnected) {
                    Activated = true;
                }
#pragma warning disable RECS0093 // Convert 'if' to '&&' expression
                else if (value == EndpointActivationState.Deactivated) {
#pragma warning restore RECS0093 // Convert 'if' to '&&' expression
                    Activated = false;
                }
            }
        }

        /// <inheritdoc/>
        public override bool Equals(object obj) {
            if (!(obj is EndpointRegistration registration)) {
                return false;
            }
            if (!base.Equals(registration)) {
                return false;
            }
            if (DiscovererId != registration.DiscovererId) {
                return false;
            }
            if (SupervisorId != registration.SupervisorId) {
                return false;
            }
            if (ApplicationId != registration.ApplicationId) {
                return false;
            }
            if (EndpointUrlLC != registration.EndpointUrlLC) {
                return false;
            }
            if (SupervisorId != registration.SupervisorId) {
                return false;
            }
            if (SecurityLevel != registration.SecurityLevel) {
                return false;
            }
            if (SecurityPolicy != registration.SecurityPolicy) {
                return false;
            }
            if (SecurityMode != registration.SecurityMode) {
                return false;
            }
            if (Thumbprint != registration.Thumbprint) {
                return false;
            }
            if (!AuthenticationMethods.DecodeAsList().SetEqualsSafe(
                    AuthenticationMethods.DecodeAsList(), VariantValue.DeepEquals)) {
                return false;
            }
            return true;
        }

        /// <inheritdoc/>
        public static bool operator ==(EndpointRegistration r1,
            EndpointRegistration r2) =>
            EqualityComparer<EndpointRegistration>.Default.Equals(r1, r2);
        /// <inheritdoc/>
        public static bool operator !=(EndpointRegistration r1,
            EndpointRegistration r2) =>
            !(r1 == r2);

        /// <inheritdoc/>
        public override int GetHashCode() {
            var hashCode = base.GetHashCode();
            hashCode = (hashCode * -1521134295) +
                EqualityComparer<string>.Default.GetHashCode(EndpointUrlLC);
            hashCode = (hashCode * -1521134295) +
                EqualityComparer<string>.Default.GetHashCode(DiscovererId);
            hashCode = (hashCode * -1521134295) +
                EqualityComparer<string>.Default.GetHashCode(SupervisorId);
            hashCode = (hashCode * -1521134295) +
                EqualityComparer<string>.Default.GetHashCode(ApplicationId);
            hashCode = (hashCode * -1521134295) +
                EqualityComparer<string>.Default.GetHashCode(Thumbprint);
            hashCode = (hashCode * -1521134295) +
                EqualityComparer<int?>.Default.GetHashCode(SecurityLevel);
            hashCode = (hashCode * -1521134295) +
                EqualityComparer<SecurityMode?>.Default.GetHashCode(SecurityMode);
            hashCode = (hashCode * -1521134295) +
                EqualityComparer<string>.Default.GetHashCode(SecurityPolicy);
            return hashCode;
        }

        internal bool IsInSync() {
            return _isInSync;
        }

        internal bool _isInSync;
    }
}
