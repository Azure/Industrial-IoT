// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IoTSolutions.OpcTwin.Services.Cloud {
    using Microsoft.Azure.IoTSolutions.OpcTwin.Services.External.Models;
    using Microsoft.Azure.IoTSolutions.OpcTwin.Services.Models;
    using Newtonsoft.Json.Linq;
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Twin (endpoint) registration persisted and comparable
    /// </summary>
    public sealed class OpcUaEndpointRegistration : OpcUaTwinRegistration {

        public static IEqualityComparer<OpcUaEndpointRegistration> Logical =>
            new LogicalEquality();

        /// <summary>
        /// Device id for registration
        /// </summary>
        public override string DeviceId {
            get {
                if (string.IsNullOrEmpty(_deviceId)) {
                    _deviceId = "opc_" + ToString().ToSha1Hash();
                }
                return _deviceId;
            }
            set => _deviceId = value;
        }

        public override string DeviceType => "Endpoint";

        /// <summary>
        /// Wether the twin is connected
        /// </summary>
        internal bool Connected { get; set; }

        #region Twin Tags

        /// <summary>
        /// Lower case endpoint url
        /// </summary>
        public string EndpointUrlLC { get; set; }

        #endregion Twin Tags

        #region Twin Properties

        /// <summary>
        /// Whether endpoint is trusted
        /// </summary>
        public bool IsTrusted => IsEnabled ?? false;

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
        public object Token { get; set; }

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
        /// Edge twin id when active - used by cloud router.
        /// </summary>
        public string TwinId { get; set; }

        #endregion Twin Properties

        /// <summary>
        /// Patch this registration and create patch twin model to upload
        /// </summary>
        /// <param name="twinRegistration"></param>
        public DeviceTwinModel Patch(TwinInfoModel twinRegistration) {
            if (twinRegistration == null) {
                throw new ArgumentNullException(nameof(twinRegistration));
            }

            var twin = new DeviceTwinModel {
                Etag = Etag,
                Tags = new Dictionary<string, JToken>(),
                Properties = new TwinPropertiesModel() {
                    Desired = new Dictionary<string, JToken>()
                }
            };

            var updateEndpoint = false;

            // Tags + Endpoint Property

            if (EndpointUrl != twinRegistration.Endpoint.Url) {
                EndpointUrlLC = twinRegistration.Endpoint.Url?.ToLowerInvariant();
                twin.Tags.Add(nameof(EndpointUrlLC), EndpointUrlLC);
                EndpointUrl = twinRegistration.Endpoint.Url;
                updateEndpoint = true;
            }

            if (IsEnabled != (twinRegistration.Endpoint.IsTrusted ?? false)) {
                IsEnabled = (twinRegistration.Endpoint.IsTrusted ?? false);
                twin.Tags.Add(nameof(IsEnabled), IsEnabled);
                updateEndpoint = true;
            }

            // Tags

            if (ApplicationId != twinRegistration.ApplicationId) {
                ApplicationId = twinRegistration.ApplicationId;
                twin.Tags.Add(nameof(ApplicationId), ApplicationId);
            }

            if (SupervisorId != twinRegistration.Endpoint.SupervisorId) {
                SupervisorId = twinRegistration.Endpoint.SupervisorId;
                twin.Tags.Add(nameof(SupervisorId), SupervisorId);
            }

            // Endpoint Property

            if (User != twinRegistration.Endpoint.User) {
                User = twinRegistration.Endpoint.User;
                updateEndpoint = true;
            }

            if (!EqualityComparer<object>.Default.Equals(Token,
                    twinRegistration.Endpoint.Token)) {
                Token = twinRegistration.Endpoint.Token;
                updateEndpoint = true;
            }

            if (TokenType != (twinRegistration.Endpoint.TokenType ??
                    Models.TokenType.None)) {
                TokenType = twinRegistration.Endpoint.TokenType;
                updateEndpoint = true;
            }

            if (SecurityMode != (twinRegistration.Endpoint.SecurityMode ??
                    Models.SecurityMode.Best)) {
                SecurityMode = twinRegistration.Endpoint.SecurityMode;
                updateEndpoint = true;
            }

            if (SecurityPolicy != twinRegistration.Endpoint.SecurityPolicy) {
                SecurityPolicy = twinRegistration.Endpoint.SecurityPolicy;
                updateEndpoint = true;
            }

            if (updateEndpoint) {
                twin.Properties.Desired.Add(k_endpointProperty,
                    ToEndpointObject());
            }

            twin.Tags.Add(nameof(DeviceType), DeviceType);
            twin.Id = DeviceId;
            return twin;
        }

        /// <summary>
        /// Decode tags and property into registration object
        /// </summary>
        /// <param name="id"></param>
        /// <param name="etag"></param>
        /// <param name="tags"></param>
        /// <param name="endpointProperty"></param>
        /// <returns></returns>
        public static OpcUaEndpointRegistration FromTwin(string id, string etag,
            Dictionary<string, JToken> tags, JObject endpointProperty) {
            return new OpcUaEndpointRegistration {
                // Device

                DeviceId = id,
                Etag = etag,

                // Tags

                IsEnabled =
                    tags.Get(nameof(IsEnabled), false),
                SupervisorId =
                    tags.Get<string>(nameof(SupervisorId), null),
                EndpointUrlLC =
                    tags.Get<string>(nameof(EndpointUrlLC), null),
                ApplicationId =
                    tags.Get<string>(nameof(ApplicationId), null),

                // Endpoint Property

                EndpointUrl =
                    endpointProperty.Get<string>(nameof(EndpointUrl), null),
                User =
                    endpointProperty.Get<string>(nameof(User), null),
                Token =
                    endpointProperty.Get<dynamic>(nameof(Token), null),
                TokenType =
                    endpointProperty.Get<TokenType>(nameof(TokenType), null),
                SecurityMode =
                    endpointProperty.Get<SecurityMode>(nameof(SecurityMode), null),
                SecurityPolicy =
                    endpointProperty.Get<string>(nameof(SecurityPolicy), null),
                TwinId =
                    endpointProperty.Get<string>(nameof(TwinId), null)
            };
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
        public static OpcUaEndpointRegistration FromTwin(DeviceTwinModel twin,
            bool onlyServerState) {

            var connected = false;
            if (twin == null) {
                return null;
            }
            if (twin.Tags == null) {
                twin.Tags = new Dictionary<string, JToken>();
            }

            OpcUaEndpointRegistration reported = null, desired = null;
            if (twin.Properties?.Reported != null &&
                twin.Properties.Reported.TryGetValue(k_endpointProperty,
                    out var reportedEndpoint)) {

                reported = FromTwin(twin.Id, twin.Etag, twin.Tags,
                    (JObject)reportedEndpoint);
                connected = !string.IsNullOrEmpty(reported.TwinId);
            }

            if (twin.Properties?.Desired != null &&
                twin.Properties.Desired.TryGetValue(k_endpointProperty,
                    out var desiredEndpoint)) {
                desired = FromTwin(twin.Id, twin.Etag, twin.Tags,
                    (JObject)desiredEndpoint);
            }

            if (!onlyServerState && reported != null) {
                reported.MarkAsInSyncWith(desired);
                reported.Connected = connected;
                return reported;
            }

            if (desired != null) {
                desired.MarkAsInSyncWith(reported);
                desired.Connected = connected;
                return desired;
            }
            return null;
        }

        /// <summary>
        /// Convert to service model
        /// </summary>
        /// <returns></returns>
        public TwinInfoModel ToServiceModel() {
            return new TwinInfoModel {
                ApplicationId = ApplicationId,
                Id = DeviceId,
                Endpoint = new EndpointModel {
                    Url = string.IsNullOrEmpty(EndpointUrl) ?
                        EndpointUrlLC : EndpointUrl,
                    User = string.IsNullOrEmpty(User) ?
                        null : User,
                    Token = Token,
                    TokenType = TokenType == Models.TokenType.None ?
                        null : TokenType,
                    SecurityMode = SecurityMode == Models.SecurityMode.Best ?
                        null : SecurityMode,
                    SecurityPolicy = string.IsNullOrEmpty(SecurityPolicy) ?
                        null : SecurityPolicy,
                    SupervisorId = string.IsNullOrEmpty(SupervisorId) ?
                        null : SupervisorId,
                    TwinId = string.IsNullOrEmpty(TwinId) ?
                        null : TwinId,
                    IsTrusted = IsTrusted ?
                        true : (bool?)null
                },
                Connected = Connected ? true : (bool?)null,
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
                Matches(model.Endpoint) &&
                ApplicationId == model?.ApplicationId;
        }

        /// <summary>
        /// Decode tags and property into registration object
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static OpcUaEndpointRegistration FromServiceModel(TwinInfoModel model) {
            return new OpcUaEndpointRegistration {
                ApplicationId = model.ApplicationId,
                IsEnabled = model.Endpoint.IsTrusted ?? false,
                SupervisorId = model.Endpoint.SupervisorId,
                EndpointUrlLC = model.Endpoint.Url?.ToLowerInvariant(),
                EndpointUrl = model.Endpoint.Url,
                User = model.Endpoint.User,
                Token = model.Endpoint.Token,
                TokenType = model.Endpoint.TokenType ?? Models.TokenType.None,
                SecurityMode = model.Endpoint.SecurityMode ?? Models.SecurityMode.Best,
                SecurityPolicy = model.Endpoint.SecurityPolicy
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
                User == endpoint.User &&
                TokenType == (endpoint.TokenType ?? Models.TokenType.None) &&
                SecurityMode == (endpoint.SecurityMode ?? Models.SecurityMode.Best) &&
                SecurityPolicy == endpoint.SecurityPolicy &&
                Token?.ToString() == endpoint.Token?.ToString();
        }

        /// <summary>
        /// Convert registration to endpoint object
        /// </summary>
        /// <returns></returns>
        private JObject ToEndpointObject() {
            var endpointProperty = new JObject {
                { nameof(EndpointUrl), EndpointUrl },
                { nameof(IsTrusted), IsTrusted }
            };
            if (User != null) {
                endpointProperty.Add(nameof(User),
                    User);
            }
            if (TokenType != null) {
                endpointProperty.Add(nameof(TokenType),
                    JToken.FromObject(TokenType));
            }
            if (Token != null) {
                endpointProperty.Add(nameof(Token),
                    JToken.FromObject(Token));
            }
            if (SecurityMode != null) {
                endpointProperty.Add(nameof(SecurityMode),
                    JToken.FromObject(SecurityMode));
            }
            if (SecurityPolicy != null) {
                endpointProperty.Add(nameof(SecurityPolicy),
                    SecurityPolicy);
            }
            return endpointProperty;
        }

        /// <summary>
        /// Flag endpoint as synchronized - i.e. it matches the other.
        /// These test only the items that are exchanged between edge
        /// and server, i.e. reported and desired.  Other properties are
        /// reported out of band, or only apply to the server side, etc.
        /// </summary>
        /// <param name="other"></param>
        internal void MarkAsInSyncWith(OpcUaEndpointRegistration other) {
            _isInSync =
                other != null &&
                EndpointUrlLC == other.EndpointUrlLC &&
                EndpointUrl == other.EndpointUrl &&
                User == other.User &&
                TokenType == other.TokenType &&
                SecurityPolicy == other.SecurityPolicy &&
                SecurityMode == other.SecurityMode &&
                Token?.ToString() == other.Token?.ToString();
        }
        internal bool IsInSync() => _isInSync;

        /// <summary>
        /// Stringify
        /// </summary>
        /// <returns></returns>
        public override string ToString() {
            return
                $"{EndpointUrlLC}-{ApplicationId}--{SecurityPolicy}-{SecurityMode}-" +
                $"{Token?.ToString()}-{TokenType}-{User}";
        }

        /// <summary>
        /// Pure equality
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj) {
            var registration = obj as OpcUaEndpointRegistration;
            return registration != null &&
                IsEnabled == registration.IsEnabled &&
                EndpointUrlLC == registration.EndpointUrlLC &&
                ApplicationId == registration.ApplicationId &&
                SupervisorId == registration.SupervisorId &&
                User == registration.User &&
                Token?.ToString() == registration.Token?.ToString() &&
                TokenType == registration.TokenType &&
                SecurityPolicy == registration.SecurityPolicy &&
                SecurityMode == registration.SecurityMode;
        }

        public static bool operator ==(OpcUaEndpointRegistration r1,
            OpcUaEndpointRegistration r2) =>
            EqualityComparer<OpcUaEndpointRegistration>.Default.Equals(r1, r2);
        public static bool operator !=(OpcUaEndpointRegistration r1,
            OpcUaEndpointRegistration r2) =>
            !(r1 == r2);

        /// <summary>
        /// Hash code
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode() {
            var hashCode = 1200389859;
            hashCode = hashCode * -1521134295 +
                EqualityComparer<string>.Default.GetHashCode(EndpointUrlLC);
            hashCode = hashCode * -1521134295 +
                EqualityComparer<string>.Default.GetHashCode(ApplicationId);
            hashCode = hashCode * -1521134295 +
               IsEnabled.GetHashCode();
            hashCode = hashCode * -1521134295 +
                EqualityComparer<string>.Default.GetHashCode(SupervisorId);
            hashCode = hashCode * -1521134295 +
                EqualityComparer<string>.Default.GetHashCode(User);
            hashCode = hashCode * -1521134295 +
                EqualityComparer<string>.Default.GetHashCode(Token?.ToString());
            hashCode = hashCode * -1521134295 +
                EqualityComparer<TokenType?>.Default.GetHashCode(TokenType);
            hashCode = hashCode * -1521134295 +
                EqualityComparer<SecurityMode?>.Default.GetHashCode(SecurityMode);
            hashCode = hashCode * -1521134295 +
                EqualityComparer<string>.Default.GetHashCode(SecurityPolicy);
            return hashCode;
        }

        /// <summary>
        /// Compares for logical equality
        /// </summary>
        private class LogicalEquality : IEqualityComparer<OpcUaEndpointRegistration> {
            /// <inheritdoc />
            public bool Equals(OpcUaEndpointRegistration x, OpcUaEndpointRegistration y) {
                return
                    x.EndpointUrlLC == y.EndpointUrlLC &&
                    x.ApplicationId == y.ApplicationId &&
                    x.SecurityPolicy == y.SecurityPolicy &&
                    x.SecurityMode == y.SecurityMode;
            }

            /// <inheritdoc />
            public int GetHashCode(OpcUaEndpointRegistration obj) {
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
        private const string k_endpointProperty = "endpoint";
    }
}
