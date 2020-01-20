// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.AspNetCore.Cors {

    /// <summary>
    /// Configuration interface for auth
    /// </summary>
    public interface ICorsConfig {

        /// <summary>
        /// CORS whitelist, in form { "origins": [], "methods": [], "headers": [] }
        /// Defaults to empty, meaning No CORS.
        /// </summary>
        string CorsWhitelist { get; }

        /// <summary>
        /// Whether CORS support is enabled
        /// Default: false
        /// </summary>
        bool CorsEnabled { get; }
    }
}
