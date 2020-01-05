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
        /// The AAD application id for the openapi UI client.
        /// </summary>
        string OpenApiAppId { get; }

        /// <summary>
        /// AAD Client / Application secret (optional)
        /// </summary>
        string OpenApiAppSecret { get; }
    }
}
