//-----------------------------------------------------------------------
// <copyright file="ClaimExtensions.cs" company="Microsoft">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace ModernIoT.Common.Azure.Extensions {
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Security.Claims;

    public static class ClaimExtensions {

        public const string UpnClaimSchema = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/upn";

        public static Claim GetUserIdentifierClaim(this IEnumerable<Claim> claims) {
            // Check for UPN, then if that doesn't exist check for name identity
            return claims.FirstOrDefault(x => string.Equals(x.Type, UpnClaimSchema,
                    StringComparison.OrdinalIgnoreCase)) ??
                claims.FirstOrDefault(x => string.Equals(x.Type, nameClaimSchema,
                    StringComparison.OrdinalIgnoreCase));
        }

        public static bool IsInDomain(this Claim userIdentifierClaim, string domainName) {
            return userIdentifierClaim.Value?.EndsWith(domainName,
                StringComparison.OrdinalIgnoreCase) ?? false;
        }

        public static bool IsApplicationToken(this IEnumerable<Claim> claims) {
            return claims.GetUserIdentifierClaim() == null &&
                claims.FirstOrDefault(x => string.Equals(x.Type, appIdClaimName,
                    StringComparison.OrdinalIgnoreCase))?.Value != null &&
                !string.Equals(claims.FirstOrDefault(x => string.Equals(x.Type, applicationTokenScope,
                    StringComparison.OrdinalIgnoreCase))?.Value,
                        userImpersonation, StringComparison.OrdinalIgnoreCase);
        }

        private const string nameClaimSchema =
            "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name";
        private const string appIdClaimName =
            "appid";
        private const string applicationTokenScope =
            "http://schemas.microsoft.com/identity/claims/scope";
        private const string userImpersonation =
            "user_impersonation";
    }
}
