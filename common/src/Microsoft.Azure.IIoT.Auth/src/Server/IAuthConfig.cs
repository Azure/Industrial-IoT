// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Auth.Server {
    using System;

    /// <summary>
    /// Configuration interface for server token validation
    /// </summary>
    public interface IAuthConfig {

        /// <summary>
        /// Whether the authentication and authorization is
        /// required or optional.
        /// </summary>
        bool AuthRequired { get; }

        /// <summary>
        /// null value allows http. Should always be set to
        /// the https port except for local development.
        /// JWT tokens are not encrypted and if not sent over
        /// HTTPS will allow an attacker to get the same
        /// authorization.
        /// </summary>
        int HttpsRedirectPort { get; }

        /// <summary>
        /// Our service's id or url that was registered and
        /// that we must validate to be the audience of the
        /// token so to ensure the token was actually issued
        /// for us.
        /// </summary>
        string Audience { get; }

        /// <summary>
        /// The instance url is the base address of the
        /// token-issuing authentication server instance.
        /// Defaults to https://login.microsoft.com/ for
        /// Azure global cloud.
        /// </summary>
        string InstanceUrl { get; }

        /// <summary>
        /// Tenant id if any. Defaults to "common" for
        /// universal Azure AD endpoint.
        /// </summary>
        string TenantId { get; }

        /// <summary>
        /// The token's iss parameter must match this string
        /// to ensure the correct issuer. If the value is not
        /// set, the issuer is validated against the instance
        /// url and that it contains a tenant.
        /// </summary>
        string TrustedIssuer { get; }

        /// <summary>
        /// Optionally the tolerated clock skew allowed when
        /// validating tokens expiration.
        /// </summary>
        TimeSpan AllowedClockSkew { get; }
    }
}
