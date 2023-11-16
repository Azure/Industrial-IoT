// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Service.Services.Models
{
    using Azure.IIoT.OpcUa.Publisher.Models;
    using System.Collections.Generic;
    using System.Runtime.Serialization;

    /// <summary>
    /// Twin (endpoint) registration persisted and comparable
    /// </summary>
    [DataContract]
    public sealed class EndpointRegistration : EntityRegistration
    {
        /// <inheritdoc/>
        [DataMember]
        public override string DeviceType => Constants.EntityTypeEndpoint;

        /// <summary>
        /// Device id is twin id
        /// </summary>
        [DataMember]
        public override string? DeviceId => base.DeviceId ?? Id;

        /// <summary>
        /// Site or gateway id
        /// </summary>
        [DataMember]
        public override string SiteOrGatewayId => this.GetSiteOrGatewayId();

        /// <summary>
        /// Identity that owns the twin.
        /// </summary>
        [DataMember]
        public string? DiscovererId { get; set; }

        /// <summary>
        /// Application id of twin
        /// </summary>
        [DataMember]
        public string? ApplicationId { get; set; }

        /// <summary>
        /// Lower case endpoint url
        /// </summary>
        [DataMember]
#pragma warning disable CA1308 // Normalize strings to uppercase
        public string? EndpointUrlLC =>
            EndpointRegistrationUrl?.ToLowerInvariant();
#pragma warning restore CA1308 // Normalize strings to uppercase

        /// <summary>
        /// Reported endpoint description url as opposed to the
        /// one that can be used to connect with.
        /// </summary>
        [DataMember]
        public string? EndpointRegistrationUrl { get; set; }

        /// <summary>
        /// Security level of endpoint
        /// </summary>
        [DataMember]
        public int? SecurityLevel { get; set; }

        /// <summary>
        /// The credential policies supported by the registered endpoint
        /// </summary>
        [DataMember]
        public IReadOnlyDictionary<string, AuthenticationMethodModel>? AuthenticationMethods { get; set; }

        /// <summary>
        /// Endoint url for direct server access
        /// </summary>
        [DataMember]
        public string? EndpointUrl { get; set; }

        /// <summary>
        /// Alternative urls
        /// </summary>
        [DataMember]
        public IReadOnlyDictionary<string, string>? AlternativeUrls { get; set; }

        /// <summary>
        /// Endpoint security policy to use.
        /// </summary>
        [DataMember]
        public string? SecurityPolicy { get; set; }

        /// <summary>
        /// Security mode to use for communication
        /// </summary>
        [DataMember]
        public SecurityMode? SecurityMode { get; set; }

        /// <summary>
        /// Endpoint connectivity status
        /// </summary>
        [DataMember]
        public EndpointConnectivityState? State { get; set; }

        /// <summary>
        /// Certificate Thumbprint
        /// </summary>
        [DataMember]
        public string? Thumbprint { get; set; }

        /// <summary>
        /// Device id is the endpoint id
        /// </summary>
        [DataMember(Name = "id")]
        public string? Id => EndpointInfoModelEx.CreateEndpointId(
            ApplicationId, EndpointRegistrationUrl, SecurityMode, SecurityPolicy);

        /// <inheritdoc/>
        public override bool Equals(object? obj)
        {
            if (obj is not EndpointRegistration registration)
            {
                return false;
            }
            if (!base.Equals(registration))
            {
                return false;
            }
            if (DiscovererId != registration.DiscovererId)
            {
                return false;
            }
            if (ApplicationId != registration.ApplicationId)
            {
                return false;
            }
            if (EndpointUrlLC != registration.EndpointUrlLC)
            {
                return false;
            }
            if (SecurityLevel != registration.SecurityLevel)
            {
                return false;
            }
            if (SecurityPolicy != registration.SecurityPolicy)
            {
                return false;
            }
            if (SecurityMode != registration.SecurityMode)
            {
                return false;
            }
            if (Thumbprint != registration.Thumbprint)
            {
                return false;
            }
            if (!AuthenticationMethods.DecodeAsList().SetEqualsSafe(
                    AuthenticationMethods.DecodeAsList(), (a, b) => a.IsSameAs(b)))
            {
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
        public override int GetHashCode()
        {
            var hashCode = base.GetHashCode();
            hashCode = (hashCode * -1521134295) +
                EqualityComparer<string>.Default.GetHashCode(EndpointUrlLC ?? string.Empty);
            hashCode = (hashCode * -1521134295) +
                EqualityComparer<string>.Default.GetHashCode(DiscovererId ?? string.Empty);
            hashCode = (hashCode * -1521134295) +
                EqualityComparer<string>.Default.GetHashCode(ApplicationId ?? string.Empty);
            hashCode = (hashCode * -1521134295) +
                EqualityComparer<string>.Default.GetHashCode(Thumbprint ?? string.Empty);
            hashCode = (hashCode * -1521134295) +
                EqualityComparer<int?>.Default.GetHashCode(SecurityLevel ?? 0);
            hashCode = (hashCode * -1521134295) +
                EqualityComparer<SecurityMode>.Default.GetHashCode(
                    SecurityMode ?? Publisher.Models.SecurityMode.Best);
            hashCode = (hashCode * -1521134295) +
                EqualityComparer<string>.Default.GetHashCode(SecurityPolicy ?? string.Empty);
            return hashCode;
        }

        internal bool _isInSync;
    }
}
