// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Services.Swagger.Runtime {
    using Microsoft.Azure.IIoT.Utils;
    using Microsoft.Extensions.Configuration;

    /// <summary>
    /// Swagger configuration with fall back to client configuration.
    /// </summary>
    public class SwaggerConfig : ConfigBase, ISwaggerConfig {

        /// <summary>
        /// Swagger configuration
        /// </summary>
        private const string kSwagger_EnabledKey = "Swagger:Enabled";
        private const string kSwagger_AppIdKey = "Swagger:AppId";
        private const string kSwagger_AppSecretKey = "Swagger:AppSecret";
        private const string kAuth_RequiredKey = "Auth:Required";

        /// <summary>Enabled</summary>
        public bool UIEnabled => GetBoolOrDefault(kSwagger_EnabledKey,
            !WithAuth || !string.IsNullOrEmpty(SwaggerAppId)); // Disable with auth but no appid
        /// <summary>Auth enabled</summary>
        public bool WithAuth => GetBoolOrDefault(kAuth_RequiredKey,
            GetBoolOrDefault("PCS_AUTH_REQUIRED", !string.IsNullOrEmpty(SwaggerAppId)));
        /// <summary>Application id</summary>
        public string SwaggerAppId => GetStringOrDefault(kSwagger_AppIdKey,
            GetStringOrDefault("PCS_AUTH_CLIENT_APPID",
            GetStringOrDefault("PCS_SWAGGER_APP_ID")))?.Trim();
        /// <summary>App secret</summary>
        public string SwaggerAppSecret => GetStringOrDefault(kSwagger_AppSecretKey,
            GetStringOrDefault("PCS_AUTH_CLIENT_SECRET",
            GetStringOrDefault("PCS_SWAGGER_APP_KEY")))?.Trim();

        /// <summary>
        /// Configuration constructor
        /// </summary>
        /// <param name="configuration"></param>
        public SwaggerConfig(IConfiguration configuration) :
            base(configuration) {
        }
    }
}
