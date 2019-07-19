// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Services.Swagger {

    /// <summary>
    /// Swagger configuration
    /// </summary>
    public interface ISwaggerConfig {

        /// <summary>
        /// Whether swagger should be enabled
        /// </summary>
        bool UIEnabled { get; }

        /// <summary>
        /// Whether authentication should be added to swagger ui
        /// </summary>
        bool WithAuth { get; }

        /// <summary>
        /// Support http scheme in addition to https
        /// </summary>
        bool WithHttpScheme { get; }

        /// <summary>
        /// The AAD application id for the swagger client.
        /// </summary>
        string SwaggerAppId { get; }

        /// <summary>
        /// AAD Client / Application secret (optional)
        /// </summary>
        string SwaggerAppSecret { get; }
    }
}
