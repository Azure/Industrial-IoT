// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Auth.Models {
    using System.Collections.Generic;
    using System.Security.Claims;

    /// <summary>
    /// Model extensions
    /// </summary>
    public static class UserInfoModelEx {

        /// <summary>
        /// Convert to user model
        /// </summary>
        /// <param name="claims"></param>
        /// <param name="existing"></param>
        /// <returns></returns>
        public static UserInfoModel ToUserInfo(
            this IEnumerable<Claim> claims, UserInfoModel existing = null) {
            var result = existing ?? new UserInfoModel();
            foreach (var claim in claims) {
                if (string.IsNullOrEmpty(claim.Value)) {
                    continue;
                }
                switch (claim.Type.ToLowerInvariant()) {
                    case ClaimTypes.Upn:
                    case "upn":
                        result.DisplayableId = claim.Value;
                        break;
                    case "oid":
                    case "http://schemas.microsoft.com/identity/claims/objectidentifier":
                        result.UniqueId = claim.Value;
                        break;
                    case ClaimTypes.GivenName:
                    case "given_name":
                        result.GivenName = claim.Value;
                        break;
                    case ClaimTypes.Surname:
                    case "family_name":
                        result.FamilyName = claim.Value;
                        break;
                    case ClaimTypes.Email:
                    case "email":
                        if (result.Email == null) {
                            result.Email = claim.Value;
                        }
                        else if (!result.Email.Contains(claim.Value)) {
                            result.Email += ";" + claim.Value;
                        }
                        break;
                }
            }
            return result;
        }
    }
}
