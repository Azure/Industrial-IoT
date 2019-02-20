// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Services.Swagger.Runtime {
    using Microsoft.Azure.IIoT.Services.Swagger;
    using Microsoft.Azure.IIoT.Auth.Runtime;
    using Microsoft.Extensions.Configuration;
    using System;

    /// <summary>
    /// Swagger configuration with fall back to client configuration.
    /// </summary>
    public class SwaggerConfig : ClientConfig, ISwaggerConfig {

        /// <summary>
        /// Swagger configuration
        /// </summary>
        private const string kSwagger_EnabledKey = "Swagger:Enabled";
        private const string kSwagger_AppIdKey = "Swagger:AppId";
        private const string kSwagger_AppSecretKey = "Swagger:AppSecret";
        private const string kAuth_RequiredKey = "Auth:Required";
        private const string kAuth_HttpsRedirectPortKey = "Auth:HttpsRedirectPort";

        /// <summary>Enabled</summary>
        public bool UIEnabled => GetBoolOrDefault(kSwagger_EnabledKey,
            !WithAuth || !string.IsNullOrEmpty(SwaggerAppId)); // Disable with auth but no appid
        /// <summary>Auth enabled</summary>
        public bool WithAuth => GetBoolOrDefault(kAuth_RequiredKey,
            GetBoolOrDefault("PCS_AUTH_REQUIRED", !string.IsNullOrEmpty(SwaggerAppId)));
        /// <summary>Https enforced</summary>
        public bool WithHttpScheme => 0 == GetIntOrDefault(kAuth_HttpsRedirectPortKey,
            GetIntOrDefault("PCS_AUTH_HTTPSREDIRECTPORT", 0));
        /// <summary>Application id</summary>
        public string SwaggerAppId => GetStringOrDefault(kSwagger_AppIdKey,
            GetStringOrDefault(_serviceId + "_SWAGGER_APP_ID", AppId)).Trim();
        /// <summary>Application key</summary>
        public string SwaggerAppSecret => GetStringOrDefault(kSwagger_AppSecretKey,
            GetStringOrDefault(_serviceId + "_SWAGGER_APP_KEY", AppSecret)).Trim();

        /// <summary>
        /// Configuration constructor
        /// </summary>
        /// <param name="configuration"></param>
        /// <param name="serviceId"></param>
        public SwaggerConfig(IConfigurationRoot configuration, string serviceId = "") :
            base(configuration) {
            _serviceId = serviceId?.ToUpperInvariant() ??
                throw new ArgumentNullException(nameof(serviceId));
        }

        private readonly string _serviceId;
    }
}
