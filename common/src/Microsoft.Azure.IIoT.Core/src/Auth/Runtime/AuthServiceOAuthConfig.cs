// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Auth.Runtime {
    using Microsoft.Azure.IIoT.Utils;
    using Microsoft.Extensions.Configuration;
    using System;
    using System.Net;
    using System.Net.Sockets;

    /// <summary>
    /// Auth service oauth configuration
    /// </summary>
    public class AuthServiceOAuthConfig : ConfigBase, IOAuthClientConfig, IOAuthServerConfig {

        /// <summary>
        /// Client configuration
        /// </summary>
        private const string kAuth_IsDisabledKey = "Auth:IsDisabled";
        private const string kAuth_TrustedIssuerKey = "Auth:TrustedIssuer";
        private const string kAuth_AllowedClockSkewKey = "Auth:AllowedClockSkewSeconds";
        private const string kAuth_AudienceKey = "Auth:Audience";
        private const string kAuth_AppIdKey = "Auth:AppId";
        private const string kAuth_AppSecretKey = "Auth:AppSecret";
        private const string kAuth_InstanceUrlKey = "Auth:InstanceUrl";

        /// <inheritdoc/>
        public bool IsValid => GetBoolOrDefault(kAuth_IsDisabledKey,
            () => GetBoolOrDefault(PcsVariable.PCS_AUTH_SERVICE_DISABLED,
                () => false));
        /// <summary>Scheme</summary>
        public string Provider => AuthProvider.AuthService;
        /// <summary>Applicable resource</summary>
        public string Resource => Http.Resource.Platform;
        /// <summary>Application id</summary>
        public string ClientId => ClientSecret == null ? null : GetStringOrDefault(kAuth_AppIdKey,
            () => GetStringOrDefault(PcsVariable.PCS_AUTH_SERVICE_SERVICE_APPID,
                () => "1A98502758864C06BAA055E25F917644"))?.Trim();
        /// <summary>App secret</summary>
        public string ClientSecret => GetStringOrDefault(kAuth_AppSecretKey,
            () => GetStringOrDefault(PcsVariable.PCS_AUTH_SERVICE_SERVICE_SECRET,
                () => GetStringOrDefault(PcsVariable.PCS_AAD_SERVICE_SECRET,
                    () => null)))?.Trim();
        /// <summary>Auth server instance url</summary>
        public string InstanceUrl =>
            GetStringOrDefault(kAuth_InstanceUrlKey,
                () => GetStringOrDefault(PcsVariable.PCS_AUTH_SERVICE_URL,
                    () => GetDefaultUrl("9090", "auth")));
        /// <summary>Trusted issuer</summary>
        public string TrustedIssuer =>
            GetStringOrDefault(kAuth_TrustedIssuerKey,
                () => GetStringOrDefault(PcsVariable.PCS_AUTH_SERVICE_ISSUER,
                    () => null))?.Trim();
        /// <summary>Allowed clock skew</summary>
        public TimeSpan AllowedClockSkew =>
            TimeSpan.FromSeconds(GetIntOrDefault(kAuth_AllowedClockSkewKey,
                () => 120));
        /// <summary>Valid audience</summary>
        public string Audience =>
            GetStringOrDefault(kAuth_AudienceKey,
                () => GetStringOrDefault(PcsVariable.PCS_SERVICE_NAME,
                    () => "iiot"))?.Trim();
        /// <summary>No tenant</summary>
        public string TenantId => null;

        /// <summary>
        /// Configuration constructor
        /// </summary>
        /// <param name="configuration"></param>
        public AuthServiceOAuthConfig(IConfiguration configuration) :
            base(configuration) {
        }

        /// <summary>
        /// Get endpoint url
        /// </summary>
        /// <param name="port"></param>
        /// <param name="path"></param>
        /// <returns></returns>
        protected string GetDefaultUrl(string port, string path) {
            var cloudEndpoint = GetStringOrDefault(PcsVariable.PCS_SERVICE_URL)?
                .Trim()?.TrimEnd('/');
            if (string.IsNullOrEmpty(cloudEndpoint)) {
                // Test port is open
                if (!int.TryParse(port, out var nPort)) {
                    return $"http://localhost:9080/{path}";
                }
                using (var socket = new Socket(AddressFamily.InterNetwork,
                    SocketType.Stream, ProtocolType.Unspecified)) {
                    try {
                        socket.Connect(IPAddress.Loopback, nPort);
                        return $"http://localhost:{port}";
                    }
                    catch {
                        return $"http://localhost:9080/{path}";
                    }
                }
            }
            return $"{cloudEndpoint}/{path}";
        }
    }
}
