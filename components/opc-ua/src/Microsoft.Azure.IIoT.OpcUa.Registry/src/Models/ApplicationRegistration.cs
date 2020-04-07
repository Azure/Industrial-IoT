// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Registry.Models {
    using Microsoft.Azure.IIoT.OpcUa.Core.Models;
    using Microsoft.Azure.IIoT.Hub;
    using System.Runtime.Serialization;
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Aapplication registration persisted and comparable
    /// </summary>
    [DataContract]
    public sealed class ApplicationRegistration : EntityRegistration {

        /// <inheritdoc/>
        [DataMember]
        public override string DeviceType => IdentityType.Application;

        /// <summary>
        /// Identity that owns the twin.
        /// </summary>
        [DataMember]
        public string DiscovererId { get; set; }

        /// <summary>
        /// Connected
        /// </summary>
        [DataMember]
        public override bool Connected => false;

        /// <summary>
        /// Device id is application id
        /// </summary>
        [DataMember]
        public override string DeviceId => base.DeviceId ?? Id;

        /// <summary>
        /// Site or gateway id
        /// </summary>
        [DataMember]
        public override string SiteOrGatewayId => this.GetSiteOrGatewayId();

        /// <summary>
        /// Type
        /// </summary>
        [DataMember]
        public override string Type => DeviceType;

        /// <summary>
        /// Device id is application id
        /// </summary>
        [DataMember]
        public string ApplicationId => DeviceId;

        /// <summary>
        /// Application uri
        /// </summary>
        [DataMember]
        public string ApplicationUri { get; set; }

        /// <summary>
        /// Lower case application url
        /// </summary>
        [DataMember]
        public string ApplicationUriLC => ApplicationUri?.ToLowerInvariant();

        /// <summary>
        /// Application name
        /// </summary>
        [DataMember]
        public string ApplicationName { get; set; }

        /// <summary>
        /// Application name locale
        /// </summary>
        [DataMember]
        public string Locale { get; set; }

        /// <summary>
        /// Application name locale
        /// </summary>
        [DataMember]
        public Dictionary<string, string> LocalizedNames { get; set; }

        /// <summary>
        /// Discovery profile uri
        /// </summary>
        [DataMember]
        public string DiscoveryProfileUri { get; set; }

        /// <summary>
        /// Gateway server uri
        /// </summary>
        [DataMember]
        public string GatewayServerUri { get; set; }

        /// <summary>
        /// Product uri
        /// </summary>
        [DataMember]
        public string ProductUri { get; set; }

        /// <summary>
        /// Application type
        /// </summary>
        [DataMember]
        public ApplicationType? ApplicationType { get; set; }

        /// <summary>
        /// Returns discovery urls of the application
        /// </summary>
        [DataMember]
        public Dictionary<string, string> DiscoveryUrls { get; set; }

        /// <summary>
        /// Host address of server application
        /// </summary>
        [DataMember]
        public Dictionary<string, string> HostAddresses { get; set; }

        /// <summary>
        /// Capabilities
        /// </summary>
        [DataMember]
        public Dictionary<string, bool> Capabilities { get; set; }

        /// <summary>
        /// Create time
        /// </summary>
        [DataMember]
        public DateTime? CreateTime { get; set; }

        /// <summary>
        /// Authority
        /// </summary>
        [DataMember]
        public string CreateAuthorityId { get; set; }

        /// <summary>
        /// Update time
        /// </summary>
        [DataMember]
        public DateTime? UpdateTime { get; set; }

        /// <summary>
        /// Authority
        /// </summary>
        [DataMember]
        public string UpdateAuthorityId { get; set; }

        /// <summary>
        /// Numeric id
        /// </summary>
        [DataMember]
        public uint? RecordId { get; set; }

        /// <summary>
        /// Application registration id
        /// </summary>
        [DataMember(Name = "id")]
        public string Id => ApplicationInfoModelEx.CreateApplicationId(
             SiteOrGatewayId, ApplicationUri, ApplicationType);


        /// <inheritdoc/>
        public override bool Equals(object obj) {
            if (!(obj is ApplicationRegistration registration)) {
                return false;
            }
            if (!base.Equals(registration)) {
                return false;
            }
            if (DiscovererId != registration.DiscovererId) {
                return false;
            }
            if (ApplicationId != registration.ApplicationId) {
                return false;
            }
            if (ApplicationType != registration.ApplicationType) {
                return false;
            }
            if (ApplicationUriLC != registration.ApplicationUriLC) {
                return false;
            }
            if (DiscoveryProfileUri != registration.DiscoveryProfileUri) {
                return false;
            }
            if (UpdateTime != registration.UpdateTime) {
                return false;
            }
            if (UpdateAuthorityId != registration.UpdateAuthorityId) {
                return false;
            }
            if (CreateAuthorityId != registration.CreateAuthorityId) {
                return false;
            }
            if (CreateTime != registration.CreateTime) {
                return false;
            }
            if (GatewayServerUri != registration.GatewayServerUri) {
                return false;
            }
            if (ProductUri != registration.ProductUri) {
                return false;
            }
            if (!HostAddresses.DecodeAsList().SequenceEqualsSafe(
               registration.HostAddresses.DecodeAsList())) {
                return false;
            }
            if (ApplicationName != registration.ApplicationName) {
                return false;
            }
            if (!LocalizedNames.DictionaryEqualsSafe(
                registration.LocalizedNames)) {
                return false;
            }
            if (!Capabilities.DecodeAsSet().SetEqualsSafe(
                registration.Capabilities.DecodeAsSet())) {
                return false;
            }
            if (!DiscoveryUrls.DecodeAsList().SequenceEqualsSafe(
                registration.DiscoveryUrls.DecodeAsList())) {
                return false;
            }
            return true;
        }

        /// <inheritdoc/>
        public static bool operator ==(ApplicationRegistration r1,
            ApplicationRegistration r2) =>
            EqualityComparer<ApplicationRegistration>.Default.Equals(r1, r2);
        /// <inheritdoc/>
        public static bool operator !=(ApplicationRegistration r1,
            ApplicationRegistration r2) => !(r1 == r2);

        /// <inheritdoc/>
        public override int GetHashCode() {
            var hashCode = base.GetHashCode();
            hashCode = (hashCode * -1521134295) +
                EqualityComparer<string>.Default.GetHashCode(DiscovererId);
            hashCode = (hashCode * -1521134295) +
                EqualityComparer<string>.Default.GetHashCode(ApplicationId);
            hashCode = (hashCode * -1521134295) +
                EqualityComparer<ApplicationType?>.Default.GetHashCode(ApplicationType);
            hashCode = (hashCode * -1521134295) +
                EqualityComparer<string>.Default.GetHashCode(ProductUri);
            hashCode = (hashCode * -1521134295) +
                EqualityComparer<string>.Default.GetHashCode(DiscoveryProfileUri);
            hashCode = (hashCode * -1521134295) +
                EqualityComparer<string>.Default.GetHashCode(GatewayServerUri);
            hashCode = (hashCode * -1521134295) +
                EqualityComparer<string>.Default.GetHashCode(ApplicationName);
            return hashCode;
        }
    }
}
