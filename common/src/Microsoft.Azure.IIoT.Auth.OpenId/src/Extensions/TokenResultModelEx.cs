// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Auth.Models {
    using System.IdentityModel.Tokens.Jwt;
    using System.Linq;

    /// <summary>
    /// Model extensions
    /// </summary>
    public static class TokenResultModelEx {

        /// <summary>
        /// Convert to Token model
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        public static TokenResultModel ToTokenResult(
            this JwtSecurityToken token) {
            return new TokenResultModel {
                RawToken = token.RawData,
                Authority = token.Issuer,
                TokenType = "Bearer",
                ExpiresOn = token.ValidTo,
                SignatureAlgorithm = token.SignatureAlgorithm,
                TenantId = token.Payload.Claims.FirstOrDefault(
                    c => c.Type?.ToLowerInvariant() == "tid")?.Value,
                UserInfo = token.Payload.Claims.ToUserInfo(),
                IdToken = null // TODO
            };
        }
    }
}
