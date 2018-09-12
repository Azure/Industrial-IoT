// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Registry.Models {
    using Microsoft.Azure.IIoT.OpcUa.Models;
    using Microsoft.Azure.IIoT.Hub.Models;
    using Microsoft.Azure.IIoT;
    using Newtonsoft.Json.Linq;
    using System;
    using System.Collections.Generic;

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
        public override string DeviceId {
            get => base.DeviceId ?? Id;
        }

        #region Twin Tags

        /// <summary>
        /// Lower case endpoint url
        /// </summary>
        public string EndpointUrlLC =>
            EndpointUrl?.ToLowerInvariant();

        /// <summary>
        /// Security level of endpoint
        /// </summary>
        public int? SecurityLevel { get; set; }

        /// <summary>
        /// Whether endpoint is activated
        /// </summary>
        public bool? Activated { get; set; }

        #endregion Twin Tags

        #region Twin Properties

        /// <summary>
        /// Endoint url for direct server access
        /// </summary>
        public string EndpointUrl { get; set; }

        /// <summary>
        /// User name to use
        /// </summary>
        public string User { get; set; }

        /// <summary>
        /// User token to pass to server
        /// </summary>
        public JToken Token { get; set; }

        /// <summary>
        /// Type of token
        /// </summary>
        public TokenType? TokenType { get; set; }

        /// <summary>
        /// Endpoint security policy to use.
        /// </summary>
        public string SecurityPolicy { get; set; }

        /// <summary>
        /// Security mode to use for communication
        /// </summary>
        public SecurityMode? SecurityMode { get; set; }

        /// <summary>
        /// The certificate to validate endpoints with
        /// </summary>
        public Dictionary<string, string> Validation { get; set; }

        #endregion Twin Properties

        /// <summary>
        /// Device id is the twin/endpoint id
        /// </summary>
        public string Id => TwinInfoModelEx.CreateTwinId(
            ApplicationId, EndpointUrl, User, SecurityMode, SecurityPolicy);

        /// <summary>
        /// Create patch twin model to upload
        /// </summary>
        /// <param name="existing"></param>
        /// <param name="update"></param>
        public static DeviceTwinModel Patch(
            EndpointRegistration existing,
            EndpointRegistration update) {

            var twin = BaseRegistration.Patch(existing, update);

            // Tags + Endpoint Property

            if (update?.EndpointUrl != null &&
                update.EndpointUrl != existing?.EndpointUrl) {
                twin.Tags.Add(nameof(EndpointUrlLC),
                    update.EndpointUrlLC);
                twin.Properties.Desired.Add(nameof(EndpointUrl),
                    update.EndpointUrl);
            }

            // Tags

            if (update?.SecurityLevel != existing?.SecurityLevel) {
                twin.Tags.Add(nameof(SecurityLevel), update?.SecurityLevel == null ?
                    null : JToken.FromObject(update?.SecurityLevel));
            }

            if (update?.Activated != null &&
                update.Activated != existing?.Activated) {
                twin.Tags.Add(nameof(Activated), update?.Activated);
            }

            // Endpoint Property

            if (update.User != existing?.User) {
                twin.Properties.Desired.Add(nameof(User), update?.User);
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

            if (update?.TokenType != existing?.TokenType) {
                twin.Properties.Desired.Add(nameof(TokenType), update?.TokenType == null ?
                    null : JToken.FromObject(update?.TokenType));
            }

            if (!JToken.DeepEquals(update?.Token, existing?.Token)) {
                twin.Properties.Desired.Add(nameof(Token), update?.Token);
            }

            var certUpdate = update?.Validation.DecodeAsByteArray().SequenceEqualsSafe(
                existing?.Validation.DecodeAsByteArray());
            if (!(certUpdate ?? true)) {
                twin.Properties.Desired.Add(nameof(Validation), update?.Validation == null ?
                    null : JToken.FromObject(update.Validation));
            }

            // Recalculate identity

            var endpointUrl = existing?.EndpointUrl;
            if (update?.EndpointUrl != null) {
                endpointUrl = update.EndpointUrl;
            }
            if (endpointUrl == null) {
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

            twin.Id = TwinInfoModelEx.CreateTwinId(
                applicationId, endpointUrl, update?.User, securityMode, securityPolicy);

            if (existing?.DeviceId != twin.Id) {
                twin.Etag = null; // Force creation of new identity
            }
            return twin;
        }

        /// <summary>
        /// Decode tags and property into registration object
        /// </summary>
        /// <param name="deviceId"></param>
        /// <param name="etag"></param>
        /// <param name="tags"></param>
        /// <param name="properties"></param>
        /// <returns></returns>
        public static EndpointRegistration FromTwin(string deviceId, string etag,
            Dictionary<string, JToken> tags, Dictionary<string, JToken> properties) {
            var registration = new EndpointRegistration {
                // Device

                DeviceId = deviceId,
                Etag = etag,

                // Tags
                IsDisabled =
                    tags.Get<bool>(nameof(IsDisabled), null),
                NotSeenSince =
                    tags.Get<DateTime>(nameof(NotSeenSince), null),

                Certificate =
                    tags.Get<Dictionary<string, string>>(nameof(Certificate), null),
                Thumbprint =
                    tags.Get<string>(nameof(Thumbprint), null),
                SupervisorId =
                    tags.Get<string>(nameof(SupervisorId), null),

                Activated =
                    tags.Get<bool>(nameof(Activated), null),
                ApplicationId =
                    tags.Get<string>(nameof(ApplicationId), null),
                SecurityLevel =
                    tags.Get<int>(nameof(SecurityLevel), null),

                // Properties

                Connected =
                    properties.Get(kConnectedProp, false),
                Type =
                    properties.Get<string>(kTypeProp, null),
                SiteId =
                    properties.Get(kSiteIdProp, tags.Get<string>(nameof(SiteId), null)),

                EndpointUrl =
                    properties.Get<string>(nameof(EndpointUrl), null),
                User =
                    properties.Get<string>(nameof(User), null),
                Token =
                    properties.Get<JToken>(nameof(Token), null),
                TokenType =
                    properties.Get<TokenType>(nameof(TokenType), null),
                SecurityMode =
                    properties.Get<SecurityMode>(nameof(SecurityMode), null),
                SecurityPolicy =
                    properties.Get<string>(nameof(SecurityPolicy), null),
                Validation =
                    properties.Get<Dictionary<string, string>>(nameof(Validation), null)
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

            var consolidated = FromTwin(twin.Id, twin.Etag, twin.Tags,
                twin.GetConsolidatedProperties());
            var desired = (twin.Properties?.Desired == null) ? null :
                FromTwin(twin.Id, twin.Etag, twin.Tags, twin.Properties.Desired);

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
        public TwinInfoModel ToServiceModel() {
            return new TwinInfoModel {
                ApplicationId = ApplicationId,
                Registration = new TwinRegistrationModel {
                    Id = DeviceId,
                    SiteId = string.IsNullOrEmpty(SiteId) ?
                        null : SiteId,
                    SupervisorId = string.IsNullOrEmpty(SupervisorId) ?
                        null : SupervisorId,
                    Certificate = Certificate.DecodeAsByteArray(),
                    SecurityLevel = SecurityLevel,
                    Endpoint = new EndpointModel {
                        Url = string.IsNullOrEmpty(EndpointUrl) ?
                            EndpointUrlLC : EndpointUrl,
                        Authentication = TokenType == null ? null :
                            new AuthenticationModel {
                                User = string.IsNullOrEmpty(User) ?
                                    null : User,
                                Token = Token,
                                TokenType = TokenType == OpcUa.Models.TokenType.None ?
                                    null : TokenType
                        },
                        SecurityMode = SecurityMode == OpcUa.Models.SecurityMode.Best ?
                            null : SecurityMode,
                        SecurityPolicy = string.IsNullOrEmpty(SecurityPolicy) ?
                            null : SecurityPolicy,
                        Validation = Validation.DecodeAsByteArray()
                    }
                },
                Connected = Connected ? true : (bool?)null,
                Activated = Activated == true ? true : (bool?)null,
                NotSeenSince = NotSeenSince,
                OutOfSync = Connected && !_isInSync ? true : (bool?)null
            };
        }

        /// <summary>
        /// Returns true if this registration matches the server endpoint
        /// model provided.
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public bool Matches(TwinInfoModel model) {
            return model != null &&
                Matches(model.Registration?.Endpoint) &&
                NotSeenSince == model.NotSeenSince &&
                ApplicationId == model.ApplicationId &&
                (Activated ?? false) == (model.Activated ?? false) &&
                Certificate.DecodeAsByteArray().SequenceEqualsSafe(
                    model.Registration?.Certificate);
        }

        /// <summary>
        /// Decode tags and property into registration object
        /// </summary>
        /// <param name="model"></param>
        /// <param name="disabled"></param>
        /// <returns></returns>
        public static EndpointRegistration FromServiceModel(TwinInfoModel model,
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
                EndpointUrl = model.Registration?.Endpoint.Url,
                User = model.Registration?.Endpoint.Authentication?.User,
                Token = model.Registration?.Endpoint.Authentication?.Token,
                TokenType = model.Registration?.Endpoint.Authentication?.TokenType ??
                    OpcUa.Models.TokenType.None,
                SecurityMode = model.Registration?.Endpoint.SecurityMode ??
                    OpcUa.Models.SecurityMode.Best,
                SecurityPolicy = model.Registration?.Endpoint.SecurityPolicy,
                Validation = model.Registration?.Endpoint?.Validation.EncodeAsDictionary(),
                Activated = model.Activated,
                Connected = model.Connected ?? false
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
                User == endpoint.Authentication?.User &&
                TokenType == (endpoint.Authentication?.TokenType ?? OpcUa.Models.TokenType.None) &&
                JToken.DeepEquals(Token, endpoint.Authentication?.Token) &&
                SecurityMode == (endpoint.SecurityMode ?? OpcUa.Models.SecurityMode.Best) &&
                SecurityPolicy == endpoint.SecurityPolicy &&
                endpoint.Validation.SequenceEqualsSafe(
                    Validation.DecodeAsByteArray());
        }

        /// <summary>
        /// Flag endpoint as synchronized - i.e. it matches the other.
        /// </summary>
        /// <param name="other"></param>
        internal void MarkAsInSyncWith(EndpointRegistration other) {
            _isInSync =
                other != null &&
                EndpointUrl == other.EndpointUrl &&
                User == other.User &&
                TokenType == other.TokenType &&
                JToken.DeepEquals(Token, other.Token) &&
                SecurityPolicy == other.SecurityPolicy &&
                SecurityMode == other.SecurityMode &&
                Validation.DecodeAsByteArray().SequenceEqualsSafe(
                    other.Validation.DecodeAsByteArray());
        }
        internal bool IsInSync() => _isInSync;

        /// <inheritdoc/>
        public override bool Equals(object obj) {
            var registration = obj as EndpointRegistration;
            return base.Equals(registration) &&
                (Activated ?? false) == (registration.Activated ?? false) &&
                EndpointUrlLC == registration.EndpointUrlLC &&
                SupervisorId == registration.SupervisorId &&
                User == registration.User &&
                JToken.DeepEquals(Token, registration.Token) &&
                TokenType == registration.TokenType &&
                SecurityLevel == registration.SecurityLevel &&
                SecurityPolicy == registration.SecurityPolicy &&
                SecurityMode == registration.SecurityMode &&
                Validation.DecodeAsByteArray().SequenceEqualsSafe(
                    registration.Validation.DecodeAsByteArray());
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
                EqualityComparer<string>.Default.GetHashCode(User);
            hashCode = hashCode * -1521134295 +
                JToken.EqualityComparer.GetHashCode(Token);
            hashCode = hashCode * -1521134295 +
                EqualityComparer<int?>.Default.GetHashCode(SecurityLevel);
            hashCode = hashCode * -1521134295 +
                EqualityComparer<TokenType?>.Default.GetHashCode(TokenType);
            hashCode = hashCode * -1521134295 +
                EqualityComparer<SecurityMode?>.Default.GetHashCode(SecurityMode);
            hashCode = hashCode * -1521134295 +
                EqualityComparer<string>.Default.GetHashCode(SecurityPolicy);
            hashCode = hashCode * -1521134295 +
                EqualityComparer<string>.Default.GetHashCode(
                    Validation.DecodeAsByteArray().ToSha1Hash());
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
