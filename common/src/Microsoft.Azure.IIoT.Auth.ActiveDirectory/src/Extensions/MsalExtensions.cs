// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Auth.Models {
    using Microsoft.Identity.Client;
    using System;
    using System.IdentityModel.Tokens.Jwt;
    using System.Linq;
    using System.Security.Claims;
    using System.Threading.Tasks;

    /// <summary>
    /// Msal extensions
    /// </summary>
    public static class MsalExtensions {

        /// <summary>
        /// Get account info for the user
        /// </summary>
        /// <param name="application"></param>
        /// <param name="user"></param>
        /// <returns></returns>
        public static async Task<IAccount> GetUserAccountAsync(
            this IClientApplicationBase application, ClaimsPrincipal user) {
            var accountId = user.GetMsalAccountId();
            if (accountId != null) {
                var account = await application.GetAccountAsync(accountId);
                // Special case for guest users as the Guest oid / tenant id are not surfaced.
                if (account == null) {
                    var loginHint = user.GetLoginHint();
                    if (loginHint == null) {
                        throw new ArgumentNullException(nameof(loginHint));
                    }
                    var accounts = await application.GetAccountsAsync();
                    account = accounts.FirstOrDefault(a => a.Username == loginHint);
                }
            }
            return null;
        }

        /// <summary>
        /// Gets the domain-hint associated with an identity
        /// </summary>
        /// <param name="principal"></param>
        /// <returns></returns>
        public static string GetDomainHint(this ClaimsPrincipal principal) {
            // Tenant for MSA accounts
            const string msaTenantId = "9188040d-6c67-4c5b-b112-36a304b66dad";
            var tenantId = principal.GetTenantId();
            var domainHint = string.IsNullOrWhiteSpace(tenantId) ? null
                : tenantId.Equals(msaTenantId, StringComparison.OrdinalIgnoreCase) ?
                    "consumers" : "organizations";
            return domainHint;
        }

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
                UserInfo = jwt.Payload.Claims.ToUserInfo(
                    new UserInfoModel { UniqueId = result.UniqueId }),
                IdToken = result.IdToken,
                Cached = true // Always cached in msal
            };
        }
    }
}
