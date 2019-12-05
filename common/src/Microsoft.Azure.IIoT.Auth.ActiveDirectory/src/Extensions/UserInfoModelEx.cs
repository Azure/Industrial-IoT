// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Auth.Models {
    using Microsoft.IdentityModel.Clients.ActiveDirectory;

    /// <summary>
    /// Model extensions
    /// </summary>
    public static class UserInfoModelEx {

        /// <summary>
        /// Convert to user model
        /// </summary>
        /// <param name="info"></param>
        /// <returns></returns>
        public static UserInfoModel ToUserInfo(
            this UserInfo info) {
            return new UserInfoModel {
                DisplayableId = info.DisplayableId,
                FamilyName = info.FamilyName,
                GivenName = info.GivenName,
                Email = null,
                IdentityProvider = info.IdentityProvider,
                PasswordChangeUrl = info.PasswordChangeUrl,
                PasswordExpiresOn = info.PasswordExpiresOn,
                UniqueId = info.UniqueId
            };
        }
    }
}
