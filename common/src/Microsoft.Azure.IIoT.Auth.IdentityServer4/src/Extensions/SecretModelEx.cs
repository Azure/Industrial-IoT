// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Auth.IdentityServer4.Models {
    using global::IdentityServer4.Models;

    /// <summary>
    /// Convert model to document and back
    /// </summary>
    internal static class SecretModelEx {

        /// <summary>
        /// Convert to service model
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public static Secret ToServiceModel(this SecretModel entity) {
            if (entity == null) {
                return null;
            }
            return new Secret {
                Description = entity.Description,
                Expiration = entity.Expiration,
                Type = entity.Type,
                Value = entity.Value
            };
        }

        /// <summary>
        /// Maps to document
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public static SecretModel ToDocumentModel(this Secret entity) {
            if (entity == null) {
                return null;
            }
            return new SecretModel {
                Description = entity.Description,
                Expiration = entity.Expiration,
                Type = entity.Type,
                Value = entity.Value
            };
        }
    }
}