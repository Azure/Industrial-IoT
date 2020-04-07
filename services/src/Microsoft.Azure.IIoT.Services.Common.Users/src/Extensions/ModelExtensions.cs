// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Services.Common.Users.Models {
    using Microsoft.Azure.IIoT.Auth.IdentityServer4.Models;
    using Microsoft.Azure.IIoT.Api.Identity.Models;
    using System.Security.Claims;

    /// <summary>
    /// Model conversion extensions
    /// </summary>
    public static class ModelExtensions {

        /// <summary>
        /// Convert to service model
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public static UserModel ToServiceModel(this UserApiModel entity) {
            if (entity == null) {
                return null;
            }
            return new UserModel {
                AccessFailedCount = entity.AccessFailedCount,
                Email = entity.Email,
                EmailConfirmed = entity.EmailConfirmed,
                Id = entity.Id,
                LockoutEnabled = entity.LockoutEnabled,
                LockoutEnd = entity.LockoutEnd,
                PhoneNumber = entity.PhoneNumber,
                PhoneNumberConfirmed = entity.PhoneNumberConfirmed,
                SecurityStamp = entity.SecurityStamp,
                TwoFactorEnabled = entity.TwoFactorEnabled,
                UserName = entity.UserName
            };
        }

        /// <summary>
        /// Convert to document model
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static UserApiModel ToApiModel(this UserModel model) {
            if (model == null) {
                return null;
            }
            return new UserApiModel {
                AccessFailedCount = model.AccessFailedCount,
                Email = model.Email,
                EmailConfirmed = model.EmailConfirmed,
                Id = model.Id,
                LockoutEnabled = model.LockoutEnabled,
                LockoutEnd = model.LockoutEnd?.UtcDateTime,
                PhoneNumber = model.PhoneNumber,
                PhoneNumberConfirmed = model.PhoneNumberConfirmed,
                SecurityStamp = model.SecurityStamp,
                TwoFactorEnabled = model.TwoFactorEnabled,
                UserName = model.UserName
            };
        }


        /// <summary>
        /// Convert to service model
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public static RoleModel ToServiceModel(this RoleApiModel entity) {
            if (entity == null) {
                return null;
            }
            return new RoleModel {
                Id = entity.Id,
                Name = entity.Name
            };
        }

        /// <summary>
        /// Convert to document model
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static RoleApiModel ToApiModel(this RoleModel model) {
            if (model == null) {
                return null;
            }
            return new RoleApiModel {
                Id = model.Id,
                Name = model.Name
            };
        }

        /// <summary>
        /// Convert to service model
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public static Claim ToClaim(this ClaimApiModel entity) {
            if (entity == null) {
                return null;
            }
            return new Claim(entity.Type, entity.Value);
        }

        /// <summary>
        /// Convert to service model
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public static ClaimModel ToServiceModel(this ClaimApiModel entity) {
            if (entity == null) {
                return null;
            }
            return new ClaimModel {
                Type = entity.Type,
                Value = entity.Value
            };
        }

        /// <summary>
        /// Convert to API model
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public static ClaimApiModel ToApiModel(this Claim entity) {
            if (entity == null) {
                return null;
            }
            return new ClaimApiModel {
                Type = entity.Type,
                Value = entity.Value
            };
        }

        /// <summary>
        /// Convert to API model
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public static ClaimApiModel ToApiModel(this ClaimModel entity) {
            if (entity == null) {
                return null;
            }
            return new ClaimApiModel {
                Type = entity.Type,
                Value = entity.Value
            };
        }
    }
}
