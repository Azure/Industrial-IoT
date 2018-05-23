// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Web.Auth {
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Configuration interface for auth
    /// </summary>
    public interface IClientAuthConfig {

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

        /// <summary>
        /// Whether the authentication and authorization is required or optional.
        /// Default: true
        /// </summary>
        bool AuthRequired { get; }

        /// <summary>
        /// Auth type: currently supports only "JWT"
        /// Default: JWT
        /// </summary>
        string AuthType { get; }

        /// <summary>
        /// The list of allowed signing algoritms
        /// Default: RS256, RS384, RS512
        /// </summary>
        IEnumerable<string> JwtAllowedAlgos { get; }

        /// <summary>
        /// The trusted issuer
        /// </summary>
        string JwtIssuer { get; }

        /// <summary>
        /// The required audience
        /// </summary>
        string JwtAudience { get; }

        /// <summary>
        /// Clock skew allowed when validating tokens expiration
        /// Default: 2 minutes
        /// </summary>
        TimeSpan JwtClockSkew { get; }
    }
}
