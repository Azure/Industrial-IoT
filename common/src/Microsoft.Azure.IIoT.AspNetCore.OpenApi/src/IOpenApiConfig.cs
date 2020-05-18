// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.AspNetCore.OpenApi {

    /// <summary>
    /// OpenApi / Swagger configuration
    /// </summary>
    public interface IOpenApiConfig {

        /// <summary>
        /// Whether openapi should be enabled
        /// </summary>
        bool UIEnabled { get; }

        /// <summary>
        /// Whether authentication should be added to openapi ui
        /// </summary>
        bool WithAuth { get; }

        /// <summary>
        /// Create v2 open api json
        /// </summary>
        bool UseV2 { get; }

        /// <summary>
        /// The Application id for the openapi UI client.
        /// (optional - if not set uses bearer)
        /// </summary>
        string OpenApiAppId { get; }

        /// <summary>
        /// Application secret (optional)
        /// </summary>
        string OpenApiAppSecret { get; }

        /// <summary>
        /// Authorization Url
        /// (optional - if not set uses bearer)
        /// </summary>
        string OpenApiAuthorizationUrl { get; }

        /// <summary>
        /// Server host for openapi (optional)
        /// </summary>
        string OpenApiServerHost { get; }
    }
}
