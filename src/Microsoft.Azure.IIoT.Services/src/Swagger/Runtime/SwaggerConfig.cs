// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Services.Swagger.Runtime {
    using Microsoft.Azure.IIoT.Services.Swagger;
    using Microsoft.Azure.IIoT.Utils;
    using Microsoft.Extensions.Configuration;
    using System;

    /// <summary>
    /// Web service configuration - wraps a configuration root as well
    /// as reads simple configuration from environment.
    /// </summary>
    public class SwaggerConfig : ConfigBase, ISwaggerConfig {

        /// <summary>
        /// Swagger configuration
        /// </summary>
        private const string kSwagger_EnabledKey = "Swagger:Enabled";
        private const string kSwagger_AppIdKey = "Swagger:AppId";
        private const string kSwagger_AppSecretKey = "Swagger:AppSecret";
        /// <summary>Enabled</summary>
        public bool UIEnabled => GetBoolOrDefault(kSwagger_EnabledKey,
            !AuthRequired || !string.IsNullOrEmpty(SwaggerAppSecret));
        /// <summary>Auth enabled</summary>
        public bool WithAuth =>
            AuthRequired;
        /// <summary>Application id</summary>
        public string SwaggerAppId => GetStringOrDefault(kSwagger_AppIdKey, GetStringOrDefault(
            _serviceId + "_SWAGGER_APP_ID", GetStringOrDefault("IIOT_AUTH_SWAGGER_APP_ID"))).Trim();
        /// <summary>Application key</summary>
        public string SwaggerAppSecret => GetStringOrDefault(kSwagger_AppSecretKey, GetStringOrDefault(
            _serviceId + "_SWAGGER_APP_KEY", GetStringOrDefault("IIOT_AUTH_SWAGGER_APP_KEY"))).Trim();

        /// <summary>
        /// Auth configuration
        /// </summary>
        private const string kAuth_RequiredKey = "Auth:Required";
        private const string kAuth_AppSecretKey = "Auth:AppSecret";
        /// <summary>Whether required</summary>
        private bool AuthRequired =>
            GetBoolOrDefault(kAuth_RequiredKey, !string.IsNullOrEmpty(ClientSecret));
        /// <summary>Application id</summary>
        private string ClientSecret => GetStringOrDefault(kAuth_AppSecretKey, GetStringOrDefault(
            _serviceId + "_APP_KEY", GetStringOrDefault("IIOT_AUTH_APP_KEY"))).Trim();

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
