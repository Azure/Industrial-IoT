// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.AspNetCore.OpenApi.Runtime {
    using Microsoft.Azure.IIoT.Utils;
    using Microsoft.Extensions.Configuration;

    /// <summary>
    /// OpenApi configuration with fall back to client configuration.
    /// </summary>
    public class OpenApiConfig : ConfigBase, IOpenApiConfig {

        /// <summary>
        /// OpenApi configuration
        /// </summary>
        private const string kOpenApi_EnabledKey = "OpenApi:Enabled";
        private const string kOpenApi_UseV2Key = "OpenApi:UseV2";
        private const string kOpenApi_AppIdKey = "OpenApi:AppId";
        private const string kOpenApi_AppSecretKey = "OpenApi:AppSecret";
        private const string kOpenApi_AuthorizationUrlKey = "OpenApi:AuthorizationUrl";
        private const string kOpenApi_ServerHost = "OpenApi:ServerHost";
        private const string kAuth_RequiredKey = "Auth:Required";

        /// <inheritdoc/>
        public bool UIEnabled => GetBoolOrDefault(kOpenApi_EnabledKey,
            () => GetBoolOrDefault(PcsVariable.PCS_OPENAPI_ENABLED,
            () => !WithAuth || !string.IsNullOrEmpty(OpenApiAppId))); // Disable with auth but no appid
        /// <inheritdoc/>
        public bool WithAuth => GetBoolOrDefault(kAuth_RequiredKey,
            () => GetBoolOrDefault(PcsVariable.PCS_AUTH_REQUIRED,
            () => !string.IsNullOrEmpty(OpenApiAppId))); // Disable if no appid
        /// <inheritdoc/>
        public bool UseV2 => GetBoolOrDefault(kOpenApi_UseV2Key,
            () => GetBoolOrDefault(PcsVariable.PCS_OPENAPI_USE_V2,
            () => GetBoolOrDefault("PCS_SWAGGER_V2",
            () => true)));
        /// <inheritdoc/>
        public string OpenApiAppId => GetStringOrDefault(kOpenApi_AppIdKey,
            () => GetStringOrDefault(PcsVariable.PCS_OPENAPI_APPID,
            () => GetStringOrDefault(PcsVariable.PCS_AAD_CONFIDENTIAL_CLIENT_APPID,
            () => GetStringOrDefault("PCS_WEBUI_AUTH_AAD_APPID"))))?.Trim();
        /// <inheritdoc/>
        public string OpenApiAppSecret => GetStringOrDefault(kOpenApi_AppSecretKey,
            () => GetStringOrDefault(PcsVariable.PCS_OPENAPI_APP_SECRET,
            () => GetStringOrDefault(PcsVariable.PCS_AAD_CONFIDENTIAL_CLIENT_SECRET,
            () => GetStringOrDefault("PCS_APPLICATION_SECRET"))))?.Trim();
        /// <inheritdoc/>
        public string OpenApiAuthorizationUrl =>
            GetStringOrDefault(kOpenApi_AuthorizationUrlKey,
                () => $"{InstanceUrl.TrimEnd('/')}/{TenantId}/oauth2/authorize");
        /// <inheritdoc/>
        public string OpenApiServerHost => GetStringOrDefault(kOpenApi_ServerHost,
            () => GetStringOrDefault(PcsVariable.PCS_OPENAPI_SERVER_HOST))?.Trim();

        /// <summary>Get tenant id</summary>
        private string TenantId =>
            GetStringOrDefault(PcsVariable.PCS_AUTH_TENANT,
                () => "common")?.Trim();
        /// <summary>Get instance url</summary>
        private string InstanceUrl =>
            GetStringOrDefault(PcsVariable.PCS_AAD_INSTANCE,
                () => "https://login.microsoftonline.com")?.Trim();

        /// <summary>
        /// Configuration constructor
        /// </summary>
        /// <param name="configuration"></param>
        public OpenApiConfig(IConfiguration configuration) :
            base(configuration) {
        }
    }
}
