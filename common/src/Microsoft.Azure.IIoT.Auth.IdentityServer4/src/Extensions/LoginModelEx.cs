// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Auth.IdentityServer4.Models {
    using Microsoft.AspNetCore.Identity;

    /// <summary>
    /// Convert model to document and back
    /// </summary>
    internal static class LoginModelEx {

        /// <summary>
        /// Convert to service model
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public static UserLoginInfo ToServiceModel(this LoginModel entity) {
            if (entity == null) {
                return null;
            }
            return new UserLoginInfo(entity.LoginProvider,
                entity.ProviderKey, entity.ProviderDisplayName);
        }

        /// <summary>
        /// Maps to document
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public static LoginModel ToDocumentModel(this UserLoginInfo entity) {
            if (entity == null) {
                return null;
            }
            return new LoginModel {
                LoginProvider = entity.LoginProvider,
                ProviderKey = entity.ProviderKey,
                ProviderDisplayName = entity.ProviderDisplayName
            };
        }

        /// <summary>
        /// Maps to document
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public static LoginModel Clone(this LoginModel entity) {
            if (entity == null) {
                return null;
            }
            return new LoginModel {
                LoginProvider = entity.LoginProvider,
                ProviderKey = entity.ProviderKey,
                ProviderDisplayName = entity.ProviderDisplayName
            };
        }
    }
}