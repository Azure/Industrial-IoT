// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Auth.Models {
    using Microsoft.Identity.Client;
    using System.IdentityModel.Tokens.Jwt;
    using System.Security.Claims;

    /// <summary>
    /// Msal extensions
    /// </summary>
    public static class MsalExtensions {

        /// <summary>
        /// Gets the Account identifier for an MSAL.NET account from a
        /// <see cref="ClaimsPrincipal"/>
        /// </summary>
        /// <param name="principal"></param>
        /// <returns></returns>
        public static string GetMsalAccountId(this ClaimsPrincipal principal) {
            var userIdentifier = principal.GetObjectId(false);
            var nameIdentifierId = principal.GetNameIdentifierId();
            var tenantId = principal.GetTenantId();
            var userFlowId = principal.GetUserFlowId();

            if (!string.IsNullOrWhiteSpace(nameIdentifierId) &&
                !string.IsNullOrWhiteSpace(tenantId) &&
                !string.IsNullOrWhiteSpace(userFlowId)) {
                // B2C pattern: {oid}-{userFlow}.{tid}
                return $"{nameIdentifierId}.{tenantId}";
            }
            if (!string.IsNullOrWhiteSpace(userIdentifier) &&
                !string.IsNullOrWhiteSpace(tenantId)) {
                // AAD pattern: {oid}.{tid}
                return $"{userIdentifier}.{tenantId}";
            }
            return null;
        }

        /// <summary>
        /// Convert to Token model
        /// </summary>
        /// <param name="result"></param>
        /// <returns></returns>
        public static TokenResultModel ToTokenResult(
            this AuthenticationResult result) {
            var jwt = new JwtSecurityToken(result.AccessToken);
            return new TokenResultModel {
                RawToken = result.AccessToken,
                SignatureAlgorithm = jwt.SignatureAlgorithm,
                Authority = jwt.Issuer,
                TokenType = "Bearer",
                ExpiresOn = result.ExpiresOn,
                TenantId = result.TenantId,
                IdToken = result.IdToken,
                Cached = true // Always cached in msal
            };
        }
    }
}
