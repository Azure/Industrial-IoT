// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Auth.IdentityServer4.Models {
    using System.Linq;

    /// <summary>
    /// Convert model to document and back
    /// </summary>
    public static class UserDocumentModelEx {

        /// <summary>
        /// Convert to service model
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public static UserModel ToServiceModel(this UserDocumentModel entity) {
            if (entity == null) {
                return null;
            }
            return new UserModel {
                AccessFailedCount = entity.AccessFailedCount,
                ConcurrencyStamp = entity.ConcurrencyStamp,
                Email = entity.Email,
                EmailConfirmed = entity.EmailConfirmed,
                Id = entity.Id,
                LockoutEnabled = entity.LockoutEnabled,
                LockoutEnd = entity.LockoutEnd,
                NormalizedEmail = entity.NormalizedEmail,
                NormalizedUserName = entity.NormalizedUserName,
                PasswordHash = entity.PasswordHash,
                PhoneNumber = entity.PhoneNumber,
                PhoneNumberConfirmed = entity.PhoneNumberConfirmed,
                SecurityStamp = entity.SecurityStamp,
                TwoFactorEnabled = entity.TwoFactorEnabled,
                UserName = entity.UserName
            };
        }

        /// <summary>
        /// Convert to service model
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="model"></param>
        /// <returns></returns>
        public static UserDocumentModel UpdateFrom(
            this UserDocumentModel entity, UserModel model) {
            if (entity == null) {
                return model.ToDocumentModel();
            }
            entity = entity.Clone();
            if (model == null) {
                return entity;
            }
            entity.AccessFailedCount = model.AccessFailedCount;
            entity.ConcurrencyStamp = model.ConcurrencyStamp;
            entity.Email = model.Email;
            entity.EmailConfirmed = model.EmailConfirmed;
            entity.Id = model.Id;
            entity.LockoutEnabled = model.LockoutEnabled;
            entity.LockoutEnd = model.LockoutEnd;
            entity.NormalizedEmail = model.NormalizedEmail;
            entity.NormalizedUserName = model.NormalizedUserName;
            entity.PasswordHash = model.PasswordHash;
            entity.PhoneNumber = model.PhoneNumber;
            entity.PhoneNumberConfirmed = model.PhoneNumberConfirmed;
            entity.SecurityStamp = model.SecurityStamp;
            entity.TwoFactorEnabled = model.TwoFactorEnabled;
            entity.UserName = model.UserName;
            return entity;
        }

        /// <summary>
        /// Convert to document model
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static UserDocumentModel ToDocumentModel(this UserModel model) {
            if (model == null) {
                return null;
            }
            return new UserDocumentModel {
                AccessFailedCount = model.AccessFailedCount,
                ConcurrencyStamp = model.ConcurrencyStamp,
                Email = model.Email,
                EmailConfirmed = model.EmailConfirmed,
                Id = model.Id,
                LockoutEnabled = model.LockoutEnabled,
                LockoutEnd = model.LockoutEnd,
                NormalizedEmail = model.NormalizedEmail,
                NormalizedUserName = model.NormalizedUserName,
                PasswordHash = model.PasswordHash,
                PhoneNumber = model.PhoneNumber,
                PhoneNumberConfirmed = model.PhoneNumberConfirmed,
                SecurityStamp = model.SecurityStamp,
                TwoFactorEnabled = model.TwoFactorEnabled,
                UserName = model.UserName
            };
        }

        /// <summary>
        /// Clone document model
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static UserDocumentModel Clone(this UserDocumentModel model) {
            if (model == null) {
                return null;
            }
            return new UserDocumentModel {
                AccessFailedCount = model.AccessFailedCount,
                ConcurrencyStamp = model.ConcurrencyStamp,
                Email = model.Email,
                EmailConfirmed = model.EmailConfirmed,
                Id = model.Id,
                LockoutEnabled = model.LockoutEnabled,
                LockoutEnd = model.LockoutEnd,
                NormalizedEmail = model.NormalizedEmail,
                NormalizedUserName = model.NormalizedUserName,
                PasswordHash = model.PasswordHash,
                PhoneNumber = model.PhoneNumber,
                PhoneNumberConfirmed = model.PhoneNumberConfirmed,
                SecurityStamp = model.SecurityStamp,
                TwoFactorEnabled = model.TwoFactorEnabled,
                UserName = model.UserName,
                AuthenticatorKey = model.AuthenticatorKey,
                Logins = model.Logins.Select(l => l.Clone()).ToList(),
                RecoveryCodes = model.RecoveryCodes?.ToList(),
                Roles = model.Roles?.ToList(),
                Claims = model.Claims.Select(c => c.Clone()).ToList()
            };
        }
    }
}