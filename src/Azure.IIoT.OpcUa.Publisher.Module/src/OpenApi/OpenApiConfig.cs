// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Module.OpenApi
{
    using Azure.IIoT.OpcUa.Core.Configuration;
    using Microsoft.Extensions.Configuration;

    /// <summary>
    /// OpenApi configuration
    /// </summary>
    internal class OpenApiConfig : PostConfigureOptionBase<OpenApiOptions>
    {
        /// <summary> Whether create v2 openapi json </summary>
        private const string kOpenApiUseV2 = "ASPNETCORE_OPENAPI_USE_V2";
        /// <summary> Server host for openapi </summary>
        private const string kOpenApiServerHost = "ASPNETCORE_OPENAPI_SERVER_HOST";

        /// <inheritdoc/>
        public OpenApiConfig(IConfiguration configuration) :
            base(configuration)
        {
        }

        /// <inheritdoc/>
        public override void PostConfigure(string? name, OpenApiOptions options)
        {
            if (string.IsNullOrEmpty(options.OpenApiServerHost))
            {
                options.OpenApiServerHost =
                    GetStringOrDefault(kOpenApiServerHost)?.Trim();
            }

            if (options.SchemaVersion is not 2 and not 3)
            {
                var useV2 = GetBoolOrDefault(kOpenApiUseV2, true);
                options.SchemaVersion = useV2 ? 2 : 3;
            }
        }
    }
}
