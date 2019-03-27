// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Registry.Models {
    using Microsoft.Azure.IIoT.Hub.Models;
    using Microsoft.Azure.IIoT.Hub;
    using Newtonsoft.Json.Linq;
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Twin (endpoint) registration persisted and comparable
    /// </summary>
    public sealed class EndpointRegistration : BaseRegistration {

        /// <summary>
        /// Logical comparison of endpoint registrations
        /// </summary>
        public static IEqualityComparer<EndpointRegistration> Logical =>
            new LogicalEquality();

        /// <inheritdoc/>
        public override string DeviceType => "Endpoint";

        /// <summary>
        /// Device id is twin id
        /// </summary>
        public override string DeviceId => base.DeviceId ?? Id;

        #region Twin Tags

        /// <summary>
        /// Lower case endpoint url
        /// </summary>
        public string EndpointUrlLC =>
            EndpointRegistrationUrl?.ToLowerInvariant();

        /// <summary>
        /// Reported endpoint description url as opposed to the
        /// one that can be used to connect with.
        /// </summary>
        public string EndpointRegistrationUrl { get; set; }

        /// <summary>
        /// Security level of endpoint
        /// </summary>
        public int? SecurityLevel { get; set; }

        /// <summary>
        /// Whether endpoint is activated
        /// </summary>
        public bool? Activated { get; set; }

        /// <summary>
        /// The credential policies supported by the registered endpoint
        /// </summary>
        public Dictionary<string, JToken> AuthenticationMethods { get; set; }

        #endregion Twin Tags

        #region Twin Properties

        /// <summary>
        /// Endoint url for direct server access
        /// </summary>
        public string EndpointUrl { get; set; }

        /// <summary>
        /// Alternative urls
        /// </summary>
        public Dictionary<string, string> AlternativeUrls { get; set; }

        /// <summary>
        /// Default user authentication credential type
        /// </summary>
        public CredentialType? CredentialType { get; set; }

        /// <summary>
        /// Default user authentication credential to use on endpoint
        /// </summary>
        public JToken Credential { get; set; }

        /// <summary>
        /// Endpoint security policy to use.
        /// </summary>
        public string SecurityPolicy { get; set; }

        /// <summary>
        /// Security mode to use for communication
        /// </summary>
        public SecurityMode? SecurityMode { get; set; }

        /// <summary>
        /// The thumbprint to validate endpoints against
        /// </summary>
        public Dictionary<string, string> ServerThumbprint { get; set; }

        /// <summary>
        /// Use this certificate as client certificate
        /// </summary>
        public Dictionary<string, string> ClientCertificate { get; set; }

        /// <summary>
        /// Endpoint connectivity status
        /// </summary>
        public EndpointConnectivityState State { get; set; }

        #endregion Twin Properties

        /// <summary>
        /// Device id is the endpoint id
        /// </summary>
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

        /// <summary>
        /// Create patch twin model to upload
        /// </summary>
        /// <param name="existing"></param>
        /// <param name="update"></param>
        public static DeviceTwinModel Patch(
            EndpointRegistration existing,
            EndpointRegistration update) {

            var twin = BaseRegistration.Patch(existing, update);

            // Tags

            if (update?.EndpointRegistrationUrl != null &&
                update.EndpointRegistrationUrl != existing?.EndpointRegistrationUrl) {
                twin.Tags.Add(nameof(EndpointUrlLC),
                    update.EndpointUrlLC);
                twin.Tags.Add(nameof(EndpointRegistrationUrl),
                    update.EndpointRegistrationUrl);
            }

            if (update?.SecurityLevel != existing?.SecurityLevel) {
                twin.Tags.Add(nameof(SecurityLevel), update?.SecurityLevel == null ?
                    null : JToken.FromObject(update?.SecurityLevel));
            }

            if (update?.Activated != null &&
                update.Activated != existing?.Activated) {
                twin.Tags.Add(nameof(Activated), update?.Activated);
            }

            var methodEqual = update?.AuthenticationMethods.DecodeAsList().SetEqualsSafe(
                existing?.AuthenticationMethods?.DecodeAsList(), JToken.DeepEquals);
            if (!(methodEqual ?? true)) {
                twin.Tags.Add(nameof(AuthenticationMethods), update?.AuthenticationMethods == null ?
                    null : JToken.FromObject(update.AuthenticationMethods,
                        new JsonSerializer { NullValueHandling = NullValueHandling.Ignore }));
            }

            // Endpoint Property

            if (update?.EndpointUrl != null &&
                update.EndpointUrl != existing?.EndpointUrl) {
                twin.Properties.Desired.Add(nameof(EndpointUrl),
                    update.EndpointUrl);
            }

            var urlsEqual = update?.AlternativeUrls.DecodeAsList().ToHashSetSafe().SetEqualsSafe(
                existing?.AlternativeUrls?.DecodeAsList());
            if (!(urlsEqual ?? true)) {
                twin.Properties.Desired.Add(nameof(AlternativeUrls), update?.AlternativeUrls == null ?
                    null : JToken.FromObject(update.AlternativeUrls));
            }

            if (update?.SecurityMode != null &&
                update.SecurityMode != existing?.SecurityMode) {
                twin.Properties.Desired.Add(nameof(SecurityMode),
                    JToken.FromObject(update.SecurityMode));
            }

            if (update?.SecurityPolicy != null &&
                update?.SecurityPolicy != existing?.SecurityPolicy) {
                twin.Properties.Desired.Add(nameof(SecurityPolicy),
                    update.SecurityPolicy);
            }

            if (update?.CredentialType != existing?.CredentialType) {
                twin.Properties.Desired.Add(nameof(CredentialType), update?.CredentialType == null ?
                    null : JToken.FromObject(update?.CredentialType));
            }

            if (!JToken.DeepEquals(update?.Credential, existing?.Credential)) {
                twin.Properties.Desired.Add(nameof(Credential), update?.Credential);
            }

            var thumbEqual = update?.ServerThumbprint.DecodeAsByteArray().SequenceEqualsSafe(
                existing?.ServerThumbprint.DecodeAsByteArray());
            if (!(thumbEqual ?? true)) {
                twin.Properties.Desired.Add(nameof(ServerThumbprint), update?.ServerThumbprint == null ?
                    null : JToken.FromObject(update.ServerThumbprint));
            }

            var certEqual = update?.ClientCertificate.DecodeAsByteArray().SequenceEqualsSafe(
                existing?.ClientCertificate.DecodeAsByteArray());
            if (update?.ClientCertificate != null && !(certEqual ?? true)) {
                twin.Properties.Desired.Add(nameof(ClientCertificate), update?.ClientCertificate == null ?
                    null : JToken.FromObject(update.ClientCertificate));
            }

            // Recalculate identity

            var reportedEndpointUrl = existing?.EndpointRegistrationUrl;
            if (update?.EndpointRegistrationUrl != null) {
                reportedEndpointUrl = update.EndpointRegistrationUrl;
            }
            if (reportedEndpointUrl == null) {
                throw new ArgumentException(nameof(EndpointUrl));
            }
            var applicationId = existing?.ApplicationId;
            if (update?.ApplicationId != null) {
                applicationId = update.ApplicationId;
            }
            if (applicationId == null) {
                throw new ArgumentException(nameof(ApplicationId));
            }
            var securityMode = existing?.SecurityMode;
            if (update?.SecurityMode != null) {
                securityMode = update.SecurityMode;
            }
            var securityPolicy = existing?.SecurityPolicy;
            if (update?.SecurityPolicy != null) {
                securityPolicy = update.SecurityPolicy;
            }

            twin.Id = EndpointInfoModelEx.CreateEndpointId(
                applicationId, reportedEndpointUrl, securityMode, securityPolicy);

            if (existing?.DeviceId != twin.Id) {
                twin.Etag = null; // Force creation of new identity
            }
            return twin;
        }

        /// <summary>
        /// Decode tags and property into registration object
        /// </summary>
        /// <param name="twin"></param>
        /// <param name="properties"></param>
        /// <returns></returns>
        public static EndpointRegistration FromTwin(DeviceTwinModel twin,
            Dictionary<string, JToken> properties) {
            if (twin == null) {
                return null;
            }

            var tags = twin.Tags ?? new Dictionary<string, JToken>();
            var connected = twin.IsConnected();

            var registration = new EndpointRegistration {
                // Device

                DeviceId = twin.Id,
                Etag = twin.Etag,

                // Tags
                IsDisabled =
                    tags.GetValueOrDefault(nameof(IsDisabled), twin.IsDisabled()),
                NotSeenSince =
                    tags.GetValueOrDefault<DateTime>(nameof(NotSeenSince), null),

                Certificate =
                    tags.GetValueOrDefault<Dictionary<string, string>>(nameof(Certificate), null),
                Thumbprint =
                    tags.GetValueOrDefault<string>(nameof(Thumbprint), null),
                SupervisorId =
                    tags.GetValueOrDefault<string>(nameof(SupervisorId), null),
                Activated =
                    tags.GetValueOrDefault<bool>(nameof(Activated), null),
                ApplicationId =
                    tags.GetValueOrDefault<string>(nameof(ApplicationId), null),
                SecurityLevel =
                    tags.GetValueOrDefault<int>(nameof(SecurityLevel), null),
                AuthenticationMethods =
                    tags.GetValueOrDefault<Dictionary<string, JToken>>(nameof(AuthenticationMethods), null),
                EndpointRegistrationUrl =
                    tags.GetValueOrDefault<string>(nameof(EndpointRegistrationUrl), null),

                // Properties

                Connected = connected ??
                    properties.GetValueOrDefault(TwinProperty.kConnected, false),
                Type =
                    properties.GetValueOrDefault<string>(TwinProperty.kType, null),
                State =
                    properties.GetValueOrDefault(nameof(State), EndpointConnectivityState.Connecting),
                SiteId =
                    properties.GetValueOrDefault(TwinProperty.kSiteId,
                        tags.GetValueOrDefault<string>(nameof(SiteId), null)),
                EndpointUrl =
                    properties.GetValueOrDefault<string>(nameof(EndpointUrl), null),
                AlternativeUrls =
                    properties.GetValueOrDefault<Dictionary<string, string>>(nameof(AlternativeUrls), null),
                Credential =
                    properties.GetValueOrDefault<JToken>(nameof(Credential), null),
                CredentialType =
                    properties.GetValueOrDefault<CredentialType>(nameof(CredentialType), null),
                SecurityMode =
                    properties.GetValueOrDefault<SecurityMode>(nameof(SecurityMode), null),
                SecurityPolicy =
                    properties.GetValueOrDefault<string>(nameof(SecurityPolicy), null),
                ClientCertificate =
                    properties.GetValueOrDefault<Dictionary<string, string>>(nameof(ClientCertificate), null),
                ServerThumbprint =
                    properties.GetValueOrDefault<Dictionary<string, string>>(nameof(ServerThumbprint), null)
            };
            return registration;
        }

        /// <summary>
        /// Make sure to get the registration information from the right place.
        /// Reported (truth) properties take precedence over desired. However,
        /// if there is nothing reported, it means the endpoint is not currently
        /// serviced, thus we use desired as if they are attributes of the
        /// endpoint.
        /// </summary>
        /// <param name="twin"></param>
        /// <param name="onlyServerState">Only desired endpoint should be returned
        /// this means that you will look at stale information.</param>
        /// <returns></returns>
        public static EndpointRegistration FromTwin(DeviceTwinModel twin,
            bool onlyServerState) {

            if (twin == null) {
                return null;
            }
            if (twin.Tags == null) {
                twin.Tags = new Dictionary<string, JToken>();
            }

            var consolidated =
                FromTwin(twin, twin.GetConsolidatedProperties());
            var desired = (twin.Properties?.Desired == null) ? null :
                FromTwin(twin, twin.Properties.Desired);

            if (!onlyServerState) {
                consolidated.MarkAsInSyncWith(desired);
                return consolidated;
            }
            desired?.MarkAsInSyncWith(consolidated);
            return desired;
        }

        /// <summary>
        /// Convert to service model
        /// </summary>
        /// <returns></returns>
        public EndpointInfoModel ToServiceModel() {
            return new EndpointInfoModel {
                ApplicationId = ApplicationId,
                Registration = new EndpointRegistrationModel {
                    Id = DeviceId,
                    SiteId = string.IsNullOrEmpty(SiteId) ?
                        null : SiteId,
                    SupervisorId = string.IsNullOrEmpty(SupervisorId) ?
                        null : SupervisorId,
                    Certificate = Certificate.DecodeAsByteArray(),
                    AuthenticationMethods = AuthenticationMethods?.DecodeAsList(j =>
                        j.ToObject<AuthenticationMethodModel>()),
                    SecurityLevel = SecurityLevel,
                    EndpointUrl = string.IsNullOrEmpty(EndpointRegistrationUrl) ?
                        (string.IsNullOrEmpty(EndpointUrl) ?
                            EndpointUrlLC : EndpointUrl) : EndpointRegistrationUrl,
                    Endpoint = new EndpointModel {
                        Url = string.IsNullOrEmpty(EndpointUrl) ?
                            EndpointUrlLC : EndpointUrl,
                        AlternativeUrls = AlternativeUrls?.DecodeAsList().ToHashSetSafe(),
                        User = CredentialType == null ? null :
                            new CredentialModel {
                                Value = Credential,
                                Type = CredentialType == Models.CredentialType.None ?
                                    null : CredentialType
                        },
                        SecurityMode = SecurityMode == Models.SecurityMode.Best ?
                            null : SecurityMode,
                        SecurityPolicy = string.IsNullOrEmpty(SecurityPolicy) ?
                            null : SecurityPolicy,
                        ClientCertificate = ClientCertificate.DecodeAsByteArray(),
                        ServerThumbprint = ServerThumbprint.DecodeAsByteArray()
                    }
                },
                ActivationState = ActivationState,
                NotSeenSince = NotSeenSince,
                EndpointState = ActivationState == EndpointActivationState.ActivatedAndConnected ?
                    State : (EndpointConnectivityState?)null,
                OutOfSync = Connected && !_isInSync ? true : (bool?)null
            };
        }

        /// <summary>
        /// Returns true if this registration matches the server endpoint
        /// model provided.
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public bool Matches(EndpointInfoModel model) {
            return model != null &&
                Matches(model.Registration?.Endpoint) &&
                NotSeenSince == model.NotSeenSince &&
                ApplicationId == model.ApplicationId &&
                (ActivationState ?? EndpointActivationState.Deactivated) ==
                    (model.ActivationState ?? EndpointActivationState.Deactivated) &&
                Certificate.DecodeAsByteArray().SequenceEqualsSafe(
                    model.Registration?.Certificate);
        }

        /// <summary>
        /// Decode tags and property into registration object
        /// </summary>
        /// <param name="model"></param>
        /// <param name="disabled"></param>
        /// <returns></returns>
        public static EndpointRegistration FromServiceModel(EndpointInfoModel model,
            bool? disabled = null) {
            if (model == null) {
                throw new ArgumentNullException(nameof(model));
            }
            return new EndpointRegistration {
                IsDisabled = disabled,
                NotSeenSince = model.NotSeenSince,
                ApplicationId = model.ApplicationId,
                SiteId = model.Registration?.SiteId,
                SupervisorId = model.Registration?.SupervisorId,
                Certificate = model.Registration?.Certificate?.EncodeAsDictionary(),
                Thumbprint = model.Registration?.Certificate?.ToSha1Hash(),
                SecurityLevel = model.Registration?.SecurityLevel,
                EndpointRegistrationUrl = model.Registration?.EndpointUrl ??
                    model.Registration?.Endpoint.Url,
                EndpointUrl = model.Registration?.Endpoint.Url,
                AlternativeUrls = model.Registration?.Endpoint.AlternativeUrls?.ToList()?
                    .EncodeAsDictionary(),
                AuthenticationMethods = model.Registration?.AuthenticationMethods?
                    .EncodeAsDictionary(JToken.FromObject),
                Credential = model.Registration?.Endpoint.User?.Value,
                CredentialType = model.Registration?.Endpoint.User?.Type ??
                    Models.CredentialType.None,
                SecurityMode = model.Registration?.Endpoint.SecurityMode ??
                    Models.SecurityMode.Best,
                SecurityPolicy = model.Registration?.Endpoint.SecurityPolicy,
                ServerThumbprint = model.Registration?.Endpoint?
                    .ServerThumbprint.EncodeAsDictionary(),
                ClientCertificate = model.Registration?.Endpoint?
                    .ClientCertificate.EncodeAsDictionary(),
                ActivationState = model.ActivationState
            };
        }

        /// <summary>
        /// Returns true if this registration matches the endpoint
        /// model provided.
        /// </summary>
        /// <param name="endpoint"></param>
        /// <returns></returns>
        public bool Matches(EndpointModel endpoint) {
            return endpoint != null &&
                EndpointUrl == endpoint.Url &&
                AlternativeUrls.DecodeAsList().ToHashSetSafe().SetEqualsSafe(
                    endpoint.AlternativeUrls) &&
                CredentialType == (endpoint.User?.Type ?? Models.CredentialType.None) &&
                JToken.DeepEquals(Credential, endpoint.User?.Value) &&
                SecurityMode == (endpoint.SecurityMode ?? Models.SecurityMode.Best) &&
                SecurityPolicy == endpoint.SecurityPolicy &&
                endpoint.ClientCertificate.SequenceEqualsSafe(
                    ClientCertificate.DecodeAsByteArray()) &&
                endpoint.ServerThumbprint.SequenceEqualsSafe(
                    ServerThumbprint.DecodeAsByteArray());
        }

        /// <summary>
        /// Flag endpoint as synchronized - i.e. it matches the other.
        /// </summary>
        /// <param name="other"></param>
        internal void MarkAsInSyncWith(EndpointRegistration other) {
            _isInSync =
                other != null &&
                EndpointUrl == other.EndpointUrl &&
                AlternativeUrls.DecodeAsList().ToHashSetSafe().SetEqualsSafe(
                    other.AlternativeUrls.DecodeAsList()) &&
                CredentialType == other.CredentialType &&
                JToken.DeepEquals(Credential, other.Credential) &&
                SecurityPolicy == other.SecurityPolicy &&
                SecurityMode == other.SecurityMode &&
                ClientCertificate.DecodeAsByteArray().SequenceEqualsSafe(
                    other.ClientCertificate.DecodeAsByteArray()) &&
                ServerThumbprint.DecodeAsByteArray().SequenceEqualsSafe(
                    other.ServerThumbprint.DecodeAsByteArray());
        }
        internal bool IsInSync() => _isInSync;

        /// <inheritdoc/>
        public override bool Equals(object obj) {
            var registration = obj as EndpointRegistration;
            return base.Equals(registration) &&
                (Activated ?? false) == (registration.Activated ?? false) &&
                EndpointUrlLC == registration.EndpointUrlLC &&
                SupervisorId == registration.SupervisorId &&
                JToken.DeepEquals(Credential, registration.Credential) &&
                State == registration.State &&
                CredentialType == registration.CredentialType &&
                SecurityLevel == registration.SecurityLevel &&
                SecurityPolicy == registration.SecurityPolicy &&
                SecurityMode == registration.SecurityMode &&
                AuthenticationMethods.DecodeAsList().SetEqualsSafe(
                    AuthenticationMethods.DecodeAsList(), JToken.DeepEquals) &&
                ClientCertificate.DecodeAsByteArray().SequenceEqualsSafe(
                    registration.ClientCertificate.DecodeAsByteArray()) &&
                ServerThumbprint.DecodeAsByteArray().SequenceEqualsSafe(
                    registration.ServerThumbprint.DecodeAsByteArray());
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
            hashCode = hashCode * -1521134295 +
                EqualityComparer<string>.Default.GetHashCode(EndpointUrlLC);
            hashCode = hashCode * -1521134295 +
                EqualityComparer<string>.Default.GetHashCode(ApplicationId);
            hashCode = hashCode * -1521134295 +
                EqualityComparer<bool>.Default.GetHashCode(Activated ?? false);
            hashCode = hashCode * -1521134295 +
                JToken.EqualityComparer.GetHashCode(Credential);
            hashCode = hashCode * -1521134295 +
                EqualityComparer<int?>.Default.GetHashCode(SecurityLevel);
            hashCode = hashCode * -1521134295 +
                EqualityComparer<CredentialType?>.Default.GetHashCode(CredentialType);
            hashCode = hashCode * -1521134295 +
                EqualityComparer<EndpointConnectivityState?>.Default.GetHashCode(State);
            hashCode = hashCode * -1521134295 +
                EqualityComparer<SecurityMode?>.Default.GetHashCode(SecurityMode);
            hashCode = hashCode * -1521134295 +
                EqualityComparer<string>.Default.GetHashCode(SecurityPolicy);
            hashCode = hashCode * -1521134295 +
                EqualityComparer<string>.Default.GetHashCode(
                    ClientCertificate.DecodeAsByteArray().ToSha1Hash());
            hashCode = hashCode * -1521134295 +
                EqualityComparer<string>.Default.GetHashCode(
                    ServerThumbprint.DecodeAsByteArray().ToSha1Hash());
            return hashCode;
        }

        /// <summary>
        /// Compares for logical equality
        /// </summary>
        private class LogicalEquality : IEqualityComparer<EndpointRegistration> {
            /// <inheritdoc />
            public bool Equals(EndpointRegistration x, EndpointRegistration y) {
                return
                    x.EndpointUrlLC == y.EndpointUrlLC &&
                    x.ApplicationId == y.ApplicationId &&
                    x.SecurityPolicy == y.SecurityPolicy &&
                    x.SecurityMode == y.SecurityMode;
            }

            /// <inheritdoc />
            public int GetHashCode(EndpointRegistration obj) {
                var hashCode = 1200389859;
                hashCode = hashCode * -1521134295 +
                    EqualityComparer<string>.Default.GetHashCode(obj.EndpointUrlLC);
                hashCode = hashCode * -1521134295 +
                    EqualityComparer<string>.Default.GetHashCode(obj.ApplicationId);
                hashCode = hashCode * -1521134295 +
                    EqualityComparer<SecurityMode?>.Default.GetHashCode(obj.SecurityMode);
                hashCode = hashCode * -1521134295 +
                    EqualityComparer<string>.Default.GetHashCode(obj.SecurityPolicy);
                return hashCode;
            }
        }

        private bool _isInSync;
    }
}
