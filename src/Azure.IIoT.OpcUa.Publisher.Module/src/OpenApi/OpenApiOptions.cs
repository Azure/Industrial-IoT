// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Module.OpenApi
{
    using Microsoft.OpenApi;
    using System;

    /// <summary>
    /// OpenApi / Swagger configuration
    /// </summary>
    public class OpenApiOptions
    {
        /// <summary>
        /// Open api version (v2 = json, v3 = yaml)
        /// </summary>
        public int SchemaVersion { get; set; }

        /// <summary>
        /// Server host for openapi (optional)
        /// </summary>
        public string? OpenApiServerHost { get; set; }

        /// <summary>
        /// Project uri
        /// </summary>
        public Uri? ProjectUri { get; set; }

        /// <summary>
        /// License to use
        /// </summary>
        public OpenApiLicense? License { get; set; }
    }
}
