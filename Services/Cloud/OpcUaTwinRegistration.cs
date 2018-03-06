// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IoTSolutions.OpcTwin.Services.Cloud {
    using Microsoft.Azure.IoTSolutions.OpcTwin.Services.External.Models;
    using Microsoft.Azure.IoTSolutions.OpcTwin.Services.Models;
    using Microsoft.Azure.IoTSolutions.Common;
    using Newtonsoft.Json.Linq;
    using System;
    using System.Collections.Generic;
    using System.Text;

    /// <summary>
    /// Twin (server and endpoint) registration persisted and comparable
    /// </summary>
    public class OpcUaTwinRegistration {

        /// <summary>
        /// Device id for registration
        /// </summary>
        public string DeviceId {
            get {
                if (string.IsNullOrEmpty(_id)) {
                    return "opc_" + ToString().ToSha1Hash();
                }
                return _id;
            }
            set => _id = value;
        }

        #region Twin Tags

        /// <summary>
        /// Whether twin is enabled (trusted) or not
        /// </summary>
        public bool IsEnabled { get; set; }

        /// <summary>
        /// Lower case endpoint url
        /// </summary>
        public string EndpointId { get; set; }

        /// <summary>
        /// Lower case Application url
        /// </summary>
        public string ApplicationId { get; set; }

        /// <summary>
        /// Edge supervisor that owns the edge device.
        /// </summary>
        public string SupervisorId { get; set; }

        /// <summary>
        /// Server application uri
        /// </summary>
        public string ApplicationUri { get; set; }

        /// <summary>
        /// Server application name
        /// </summary>
        public string ApplicationName { get; set; }

        /// <summary>
        /// Returns the public certificate presented by the server
        /// </summary>
        public Dictionary<string, string> ServerCertificate { get; set; }

        /// <summary>
        /// Certificate hash
        /// </summary>
        public string Thumbprint { get; set; }

        /// <summary>
        /// Server capabilities
        /// </summary>
        public List<string> Capabilities { get; set; }

        /// <summary>
        /// Server id (supervisor and application id)
        /// </summary>
        public string ServerId {
            get {
                if (_serverId == null) {
                    if (SupervisorId == null && ApplicationId == null) {
                        return null;
                    }
                    _serverId =
                        $"{SupervisorId ?? ""}{ApplicationId ?? ""}"
                        .ToSha1Hash();
                }
                return _serverId;
            }
            set => _serverId = value;
        }

        #endregion Twin Tags

        #region Twin Properties

        /// <summary>
        /// Whether endpoint is trusted
        /// </summary>
        public bool IsTrusted => IsEnabled;

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
        public TokenType TokenType { get; set; }

        /// <summary>
        /// Endpoint security policy to use - null = Best.
        /// </summary>
        public string SecurityPolicy { get; set; }

        /// <summary>
        /// Security mode to use for communication
        /// </summary>
        public SecurityMode SecurityMode { get; set; }

        /// <summary>
        /// Edge twin id when active - used by cloud router.
        /// </summary>
        public string TwinId { get; set; }

        #endregion Twin Properties

        /// <summary>
        /// Patch this registration and create patch twin model to upload
        /// </summary>
        /// <param name="serverEndpoint"></param>
        public TwinModel Patch(ServerEndpointModel serverEndpoint) {
            if (serverEndpoint == null) {
                throw new ArgumentNullException(nameof(serverEndpoint));
            }

            var twin = new TwinModel {
                Tags = new Dictionary<string, JToken>(),
                Properties = new TwinPropertiesModel() {
                    Desired = new Dictionary<string, JToken>()
                }
            };

            var updateEndpoint = false;

            // Tags + Endpoint Property

            if (EndpointUrl != serverEndpoint.Endpoint.Url) {
                EndpointId = serverEndpoint.Endpoint.Url?.ToLowerInvariant();
                twin.Tags.Add(nameof(EndpointId), EndpointId);
                EndpointUrl = serverEndpoint.Endpoint.Url;
                updateEndpoint = true;
            }

            if (IsEnabled != (serverEndpoint.Endpoint.IsTrusted ?? false)) {
                IsEnabled = (serverEndpoint.Endpoint.IsTrusted ?? false);
                twin.Tags.Add(nameof(IsEnabled), IsEnabled);
                updateEndpoint = true;
            }

            // Tags

            var updateServerId = false;

            if (ServerId != serverEndpoint.Server.ServerId) {
                ServerId = serverEndpoint.Server.ServerId;
                updateServerId = true;
            }

            if (SupervisorId != serverEndpoint.Endpoint.SupervisorId) {
                SupervisorId = serverEndpoint.Endpoint.SupervisorId;
                twin.Tags.Add(nameof(SupervisorId), SupervisorId);
                updateServerId = true;
            }

            if (ApplicationUri != serverEndpoint.Server.ApplicationUri) {
                ApplicationUri = serverEndpoint.Server.ApplicationUri;
                twin.Tags.Add(nameof(ApplicationUri), ApplicationUri);
                ApplicationId = serverEndpoint.Server.ApplicationUri?.ToLowerInvariant();
                twin.Tags.Add(nameof(ApplicationId), ApplicationId);
                updateServerId = true;
            }

            if (ApplicationName != serverEndpoint.Server.ApplicationName) {
                ApplicationName = serverEndpoint.Server.ApplicationName;
                twin.Tags.Add(nameof(ApplicationName), ApplicationName);
            }

            if (!FromDictionary(ServerCertificate)
                    .SequenceEqualsSafe(serverEndpoint.Server.ServerCertificate)) {
                ServerCertificate =
                    ToDictionary(serverEndpoint.Server.ServerCertificate);
                twin.Tags.Add(nameof(ServerCertificate),
                    JToken.FromObject(ServerCertificate));
                Thumbprint = serverEndpoint.Server.ServerCertificate?.ToSha1Hash();
                twin.Tags.Add(nameof(Thumbprint), Thumbprint);
            }

            if (updateServerId) {
                twin.Tags.Add(nameof(ServerId), ServerId);
            }

            // Endpoint Property

            if (User != serverEndpoint.Endpoint.User) {
                User = serverEndpoint.Endpoint.User;
                updateEndpoint = true;
            }

            if (!EqualityComparer<object>.Default.Equals(Token,
                    serverEndpoint.Endpoint.Token)) {
                Token = serverEndpoint.Endpoint.Token;
                updateEndpoint = true;
            }

            if (TokenType != (serverEndpoint.Endpoint.TokenType ??
                    TokenType.None)) {
                TokenType = serverEndpoint.Endpoint.TokenType ??
                    TokenType.None;
                updateEndpoint = true;
            }

            if (SecurityMode != (serverEndpoint.Endpoint.SecurityMode ??
                    SecurityMode.Best)) {
                SecurityMode = serverEndpoint.Endpoint.SecurityMode ??
                    SecurityMode.Best;
                updateEndpoint = true;
            }

            if (SecurityPolicy != serverEndpoint.Endpoint.SecurityPolicy) {
                SecurityPolicy = serverEndpoint.Endpoint.SecurityPolicy;
                updateEndpoint = true;
            }

            if (updateEndpoint) {
                twin.Properties.Desired.Add(k_endpointProperty,
                    ToEndpointObject());
            }

            twin.Id = DeviceId;
            return twin;
        }

        /// <summary>
        /// Decode tags and property into registration object
        /// </summary>
        /// <param name="tags"></param>
        /// <param name="endpointProperty"></param>
        /// <returns></returns>
        public static OpcUaTwinRegistration FromTwin(string id,
            Dictionary<string, JToken> tags, JObject endpointProperty) {
            return new OpcUaTwinRegistration {
                // Device

                DeviceId = id,

                // Tags

                ApplicationName =
                    tags.Get<string>(nameof(ApplicationName), null),
                ApplicationUri =
                    tags.Get<string>(nameof(ApplicationUri), null),
                IsEnabled =
                    tags.Get(nameof(IsEnabled), false),
                ServerCertificate =
                    tags.Get<Dictionary<string, string>>(nameof(ServerCertificate), null),
                Thumbprint =
                    tags.Get<string>(nameof(Thumbprint), null),
                SupervisorId =
                    tags.Get<string>(nameof(SupervisorId), null),
                EndpointId =
                    tags.Get<string>(nameof(EndpointId), null),
                ApplicationId =
                    tags.Get<string>(nameof(ApplicationId), null),
                ServerId =
                    tags.Get<string>(nameof(ServerId), null),

                // Endpoint Property

                EndpointUrl =
                    endpointProperty.Get<string>(nameof(EndpointUrl), null),
                User =
                    endpointProperty.Get<string>(nameof(User), null),
                Token =
                    endpointProperty.Get<dynamic>(nameof(Token), null),
                TokenType =
                    endpointProperty.Get(nameof(TokenType), TokenType.None),
                SecurityMode =
                    endpointProperty.Get(nameof(SecurityMode), SecurityMode.Best),
                SecurityPolicy =
                    endpointProperty.Get<string>(nameof(SecurityPolicy), null),
                TwinId =
                    endpointProperty.Get<string>(nameof(TwinId), null)
            };
        }

        /// <summary>
        /// Get registration from twin
        /// </summary>
        /// <param name="twin"></param>
        /// <param name="onlyServerState"></param>
        /// <returns></returns>
        public static OpcUaTwinRegistration FromTwin(TwinModel twin,
            bool onlyServerState = false) =>
            FromTwin(twin, onlyServerState, out var tmp);

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
        /// <param name="connected"></param>
        /// <returns></returns>
        public static OpcUaTwinRegistration FromTwin(TwinModel twin,
            bool onlyServerState, out bool connected) {

            connected = false;
            if (twin == null) {
                return null;
            }
            if (twin.Tags == null) {
                twin.Tags = new Dictionary<string, JToken>();
            }

            OpcUaTwinRegistration reported = null, desired = null;
            if (twin.Properties?.Reported != null &&
                twin.Properties.Reported.TryGetValue(k_endpointProperty,
                    out var reportedEndpoint)) {

                reported = FromTwin(twin.Id, twin.Tags, (JObject)reportedEndpoint);
                connected = !string.IsNullOrEmpty(reported.TwinId);
            }

            if (twin.Properties?.Desired != null &&
                twin.Properties.Desired.TryGetValue(k_endpointProperty,
                    out var desiredEndpoint)) {
                desired = FromTwin(twin.Id, twin.Tags, (JObject)desiredEndpoint);
            }

            if (!onlyServerState && reported != null) {
                reported.MarkAsInSyncWith(desired);
                return reported;
            }

            if (desired != null) {
                desired.MarkAsInSyncWith(reported);
                return desired;
            }
            return null;
        }

        /// <summary>
        /// Convert to service model
        /// </summary>
        /// <returns></returns>
        public ServerEndpointModel ToServiceModel() {
            return new ServerEndpointModel {
                Endpoint = new EndpointModel {
                    Url = string.IsNullOrEmpty(EndpointUrl) ?
                        EndpointId : EndpointUrl,
                    User = string.IsNullOrEmpty(User) ?
                        null : User,
                    Token = Token,
                    TokenType = TokenType == TokenType.None ?
                        (TokenType?)null : TokenType,
                    SecurityMode = SecurityMode == SecurityMode.Best ?
                        (SecurityMode?)null : SecurityMode,
                    SecurityPolicy = SecurityPolicy,
                    SupervisorId = string.IsNullOrEmpty(SupervisorId) ?
                        null : SupervisorId,
                    TwinId = string.IsNullOrEmpty(TwinId) ?
                        null : TwinId,
                    IsTrusted = IsTrusted ?
                        true : (bool?)null
                },
                Server = new ServerInfoModel {
                    ServerId = ServerId,
                    ApplicationName = ApplicationName,
                    ApplicationUri = string.IsNullOrEmpty(ApplicationUri) ?
                        ApplicationId : ApplicationUri,
                    ServerCertificate =
                        FromDictionary(ServerCertificate),
                    SupervisorId = string.IsNullOrEmpty(SupervisorId) ?
                        null : SupervisorId,
                    Capabilities = Capabilities
                }
            };
        }

        /// <summary>
        /// Returns true if this registration matches the server endpoint
        /// model provided.
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public bool Matches(ServerEndpointModel model) {
            return model != null &&
                Matches(model.Endpoint) &&
                Matches(model.Server);
        }

        /// <summary>
        /// Decode tags and property into registration object
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static OpcUaTwinRegistration FromServiceModel(ServerEndpointModel model) {
            return new OpcUaTwinRegistration {
                ApplicationName = model.Server.ApplicationName,
                ApplicationUri = model.Server.ApplicationUri,
                ApplicationId = model.Server.ApplicationUri?.ToLowerInvariant(),
                Capabilities = model.Server.Capabilities,
                ServerCertificate = ToDictionary(model.Server.ServerCertificate),
                Thumbprint = model.Server.ServerCertificate?.ToSha1Hash(),
                IsEnabled = model.Endpoint.IsTrusted ?? false,
                SupervisorId = model.Endpoint.SupervisorId ?? model.Server.SupervisorId,
                EndpointId = model.Endpoint.Url?.ToLowerInvariant(),
                ServerId = model.Server.ServerId,

                EndpointUrl = model.Endpoint.Url,
                User = model.Endpoint.User,
                Token = model.Endpoint.Token,
                TokenType = model.Endpoint.TokenType ?? TokenType.None,
                SecurityMode = model.Endpoint.SecurityMode ?? SecurityMode.Best,
                SecurityPolicy = model.Endpoint.SecurityPolicy
            };
        }

        /// <summary>
        /// Returns true if this registration matches the server
        /// model provided.
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public bool Matches(ServerInfoModel model) {
            return model != null &&
                ServerId == model.ServerId &&
                ApplicationUri == model.ApplicationUri &&
                SupervisorId == model.SupervisorId &&
                FromDictionary(ServerCertificate)
                    .SequenceEqualsSafe(model.ServerCertificate);
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
                TokenType == (endpoint.TokenType ?? TokenType.None) &&
                SecurityPolicy == endpoint.SecurityPolicy &&
                SecurityMode == (endpoint.SecurityMode ?? SecurityMode.Best) &&
                Token?.ToString() == endpoint.Token?.ToString();
        }

        /// <summary>
        /// Convert registration to endpoint object
        /// </summary>
        /// <returns></returns>
        private JObject ToEndpointObject() {
            var endpointProperty = new JObject {
                { nameof(EndpointUrl), EndpointUrl },
                { nameof(IsTrusted), IsTrusted },
                { nameof(TokenType), JToken.FromObject(TokenType) },
                { nameof(SecurityMode), JToken.FromObject(SecurityMode) }
            };
            if (User != null) {
                endpointProperty.Add(nameof(User),
                    User);
            }
            if (Token != null) {
                endpointProperty.Add(nameof(Token),
                    JToken.FromObject(Token));
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
        internal void MarkAsInSyncWith(OpcUaTwinRegistration other) {
            _isInSync =
                other != null &&
                EndpointId == other.EndpointId &&
                EndpointUrl == other.EndpointUrl &&
                User == other.User &&
                TokenType == other.TokenType &&
                SecurityPolicy == other.SecurityPolicy &&
                SecurityMode == other.SecurityMode &&
                Token?.ToString() == other.Token?.ToString();
        }
        internal bool IsInSync() => _isInSync;

        /// <summary>
        /// Provide custom serialization by chunking the cert
        /// </summary>
        /// <param name="certificateRaw"></param>
        /// <returns></returns>
        private static Dictionary<string, string> ToDictionary(byte[] certificateRaw) {
            if (certificateRaw == null) {
                return null;
            }
            var str = certificateRaw == null ? string.Empty :
                Convert.ToBase64String(certificateRaw);
            var result = new Dictionary<string, string>();
            for (var i = 0; ; i++) {
                if (str.Length < 512) {
                    result.Add($"part_{i}", str);
                    break;
                }
                var part = str.Substring(0, 512);
                result.Add($"part_{i}", part);
                str = str.Substring(512);
            }
            return result;
        }

        /// <summary>
        /// Provide custom serialization by chunking the cert
        /// </summary>
        /// <param name="chunks"></param>
        /// <returns></returns>
        private static byte[] FromDictionary(Dictionary<string, string> chunks) {
            if (chunks == null) {
                return null;
            }
            var str = new StringBuilder();
            for (var i = 0; ; i++) {
                if (!chunks.TryGetValue($"part_{i}", out var chunk)) {
                    break;
                }
                str.Append(chunk);
            }
            if (str.Length == 0) {
                return null;
            }
            return Convert.FromBase64String(str.ToString());
        }

        /// <summary>
        /// Stringify
        /// </summary>
        /// <returns></returns>
        public override string ToString() {
            return $"{EndpointId}-{ApplicationId}-{SupervisorId}-{User}-" +
                $"{Token?.ToString()}-{TokenType}-{SecurityPolicy}-{SecurityMode}";
        }

        /// <summary>
        /// Pure equality
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj) {
            var registration = obj as OpcUaTwinRegistration;
            return registration != null &&
                IsEnabled == registration.IsEnabled &&
                EndpointId == registration.EndpointId &&
                ServerId == registration.ServerId &&
                ApplicationId == registration.ApplicationId &&
                ApplicationUri == registration.ApplicationUri &&
                ApplicationName == registration.ApplicationName &&
                SupervisorId == registration.SupervisorId &&
                EndpointUrl == registration.EndpointUrl &&
                User == registration.User &&
                Token?.ToString() == registration.Token?.ToString() &&
                TokenType == registration.TokenType &&
                SecurityPolicy == registration.SecurityPolicy &&
                SecurityMode == registration.SecurityMode &&
                FromDictionary(ServerCertificate).SequenceEqualsSafe(
                    FromDictionary(registration.ServerCertificate));
        }

        public static bool operator ==(OpcUaTwinRegistration r1, OpcUaTwinRegistration r2) =>
            EqualityComparer<OpcUaTwinRegistration>.Default.Equals(r1, r2);
        public static bool operator !=(OpcUaTwinRegistration r1, OpcUaTwinRegistration r2) =>
            !(r1 == r2);

        /// <summary>
        /// Hash code
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode() {
            var hashCode = 1200389859;
            hashCode = hashCode * -1521134295 +
                IsEnabled.GetHashCode();
            hashCode = hashCode * -1521134295 +
                EqualityComparer<string>.Default.GetHashCode(EndpointId);
            hashCode = hashCode * -1521134295 +
                EqualityComparer<string>.Default.GetHashCode(ApplicationId);
            hashCode = hashCode * -1521134295 +
                EqualityComparer<string>.Default.GetHashCode(ApplicationUri);
            hashCode = hashCode * -1521134295 +
                EqualityComparer<string>.Default.GetHashCode(ApplicationName);
            hashCode = hashCode * -1521134295 +
                EqualityComparer<string>.Default.GetHashCode(ServerId);
            hashCode = hashCode * -1521134295 +
                EqualityComparer<string>.Default.GetHashCode(SupervisorId);
            hashCode = hashCode * -1521134295 +
                EqualityComparer<string>.Default.GetHashCode(Thumbprint);
            hashCode = hashCode * -1521134295 +
                EqualityComparer<string>.Default.GetHashCode(EndpointUrl);
            hashCode = hashCode * -1521134295 +
                EqualityComparer<string>.Default.GetHashCode(User);
            hashCode = hashCode * -1521134295 +
                EqualityComparer<string>.Default.GetHashCode(Token?.ToString());
            hashCode = hashCode * -1521134295 +
                TokenType.GetHashCode();
            hashCode = hashCode * -1521134295 +
                EqualityComparer<string>.Default.GetHashCode(SecurityPolicy);
            hashCode = hashCode * -1521134295 +
                SecurityMode.GetHashCode();
            return hashCode;
        }

        private bool _isInSync;
        private string _id;
        private string _serverId;
        private const string k_endpointProperty = "endpoint";
    }
}
