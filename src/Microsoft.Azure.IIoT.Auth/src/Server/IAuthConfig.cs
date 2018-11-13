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
        /// The token's iss parameter must match this string to
        /// ensure the correct issuer. This is typically set as
        /// https://sts.windows.net/{TenantId}/ for aad.
        /// </summary>
        string TrustedIssuer { get; }

        /// <summary>
        /// Our service's application id that was registered and
        /// that we must validate to be the audience of the
        /// token so to ensure the token was actually issued
        /// for us.
        /// </summary>
        string Audience { get; }

        /// <summary>
        /// The instance url is the base address of the
        /// token-issuing authentication server instance.
        /// Defaults to https://login.microsoft.com/ for azure
        /// global cloud.
        /// </summary>
        string InstanceUrl { get; }

        /// <summary>
        /// Tenant id if any (optional - defaults
        /// to "common" for universal endpoint.)
        /// </summary>
        string TenantId { get; }

        /// <summary>
        /// Optionally the tolerated clock skew allowed when
        /// validating tokens expiration.
        /// </summary>
        TimeSpan AllowedClockSkew { get; }
    }
}
