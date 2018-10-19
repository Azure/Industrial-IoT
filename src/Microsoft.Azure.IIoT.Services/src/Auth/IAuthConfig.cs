// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Services.Auth {
    using System;

    /// <summary>
    /// Configuration interface for auth
    /// </summary>
    public interface IAuthConfig {

        /// <summary>
        /// Whether the authentication and authorization is
        /// required or optional.
        /// </summary>
        bool AuthRequired { get; }

        /// <summary>
        /// The token's iss parameter must match this string to
        /// ensure the correct issuer.
        /// </summary>
        string TrustedIssuer { get; }

        /// <summary>
        /// Our service's application or resource id that we must
        /// validate to be the audience of the token so to ensure
        /// the token was actually issued for us.
        /// </summary>
        string Audience { get; }

        /// <summary>
        /// The authority is the address of the token-issuing
        /// authentication server. The JWT bearer authentication
        /// middleware will use this URI to find and retrieve the
        /// public key that can be used to validate the token's
        /// signature.
        /// </summary>
        string Authority { get; }

        /// <summary>
        /// Optionally the tolerated clock skew allowed when
        /// validating tokens expiration.
        /// </summary>
        TimeSpan AllowedClockSkew { get; }
    }
}
