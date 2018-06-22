// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Services.Auth {
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Configuration interface for auth
    /// </summary>
    public interface IAuthConfig {

        /// <summary>
        /// Whether the authentication and authorization is
        /// required or optional. Default: true
        /// </summary>
        bool AuthRequired { get; }

        /// <summary>
        /// The trusted issuer the address of the token-issuing
        /// authentication server. The JWT bearer authentication
        /// middleware will use this URI to find and retrieve the
        /// public key that can be used to validate the token's
        /// signature. It will also confirm that the iss parameter
        /// in the token matches this URI.
        /// </summary>
        string TrustedIssuer { get; }

        /// <summary>
        /// Optionally the tolerated clock skew allowed when
        /// validating tokens expiration.
        /// </summary>
        TimeSpan AllowedClockSkew { get; }
    }
}
