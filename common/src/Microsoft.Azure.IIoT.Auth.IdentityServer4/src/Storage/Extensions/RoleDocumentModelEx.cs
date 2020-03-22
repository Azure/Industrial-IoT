// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Auth.IdentityServer4.Models {
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Convert model to document and back
    /// </summary>
    public static class RoleDocumentModelEx {

        /// <summary>
        /// Convert to service model
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public static RoleModel ToServiceModel(this RoleDocumentModel entity) {
            if (entity == null) {
                return null;
            }
            return new RoleModel {
                NormalizedName = entity.NormalizedName,
                ConcurrencyStamp = entity.ConcurrencyStamp,
                Id = entity.Id,
                Name = entity.Name
            };
        }

        /// <summary>
        /// Convert to service model
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="model"></param>
        /// <returns></returns>
        public static RoleDocumentModel UpdateFrom(
            this RoleDocumentModel entity, RoleModel model) {
            if (entity == null) {
                return model.ToDocumentModel();
            }
            entity = entity.Clone();
            if (model == null) {
                return entity;
            }
            entity.NormalizedName = model.NormalizedName;
            entity.Name = model.Name;
            return entity;
        }

        /// <summary>
        /// Convert to document model
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static RoleDocumentModel ToDocumentModel(this RoleModel model) {
            if (model == null) {
                return null;
            }
            return new RoleDocumentModel {
                NormalizedName = model.NormalizedName,
                ConcurrencyStamp = model.ConcurrencyStamp,
                Id = model.Id,
                Name = model.Name,
                Claims = new List<ClaimModel>()
            };
        }

        /// <summary>
        /// Convert to document model
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static RoleDocumentModel Clone(this RoleDocumentModel model) {
            if (model == null) {
                return null;
            }
            return new RoleDocumentModel {
                NormalizedName = model.NormalizedName,
                ConcurrencyStamp = model.ConcurrencyStamp,
                Id = model.Id,
                Name = model.Name,
                Claims = model.Claims.Select(c => c.Clone()).ToList()
            };
        }
    }
}