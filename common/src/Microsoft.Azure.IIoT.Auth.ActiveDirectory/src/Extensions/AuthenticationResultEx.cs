// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Auth.Models {
    using Microsoft.IdentityModel.Clients.ActiveDirectory;
    using System.IdentityModel.Tokens.Jwt;

    /// <summary>
    /// Model extensions
    /// </summary>
    public static class AuthenticationResultEx {

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
                Authority = result.Authority,
                TokenType = result.AccessTokenType,
                ExpiresOn = result.ExpiresOn,
                TenantId = result.TenantId,
                UserInfo = jwt.Payload.Claims.ToUserInfo(
                    result.UserInfo.ToUserInfo()),
                IdToken = result.IdToken,
                Cached = true // Always cached in adal
            };
        }
    }
}
