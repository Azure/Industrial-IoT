// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Auth.IdentityServer4.Models {
    using global::IdentityServer4.Models;
    using System.Linq;

    /// <summary>
    /// Convert model to document and back
    /// </summary>
    internal static class ScopeModelEx {

        /// <summary>
        /// Convert to service model
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public static Scope ToServiceModel(this ScopeModel entity) {
            if (entity == null) {
                return null;
            }
            return new Scope {
                Description = entity.Description,
                DisplayName = entity.DisplayName,
                Emphasize = entity.Emphasize,
                Name = entity.Name,
                UserClaims = entity.UserClaims?.ToList(),
                Required = entity.Required,
                ShowInDiscoveryDocument = entity.ShowInDiscoveryDocument
            };
        }

        /// <summary>
        /// Maps to document
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public static ScopeModel ToDocumentModel(this Scope entity) {
            if (entity == null) {
                return null;
            }
            return new ScopeModel {
                Description = entity.Description,
                DisplayName = entity.DisplayName,
                Emphasize = entity.Emphasize,
                Name = entity.Name,
                UserClaims = entity.UserClaims?.ToList(),
                Required = entity.Required,
                ShowInDiscoveryDocument = entity.ShowInDiscoveryDocument
            };
        }
    }
}