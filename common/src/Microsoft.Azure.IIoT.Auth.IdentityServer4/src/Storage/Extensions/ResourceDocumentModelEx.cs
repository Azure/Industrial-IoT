// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Auth.IdentityServer4.Models {
    using Microsoft.Azure.IIoT.Exceptions;
    using System.Linq;
    using global::IdentityServer4.Models;

    /// <summary>
    /// Convert model to document and back
    /// </summary>
    internal static class ResourceDocumentModelEx {

        /// <summary>
        /// Convert to service model
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public static Resource ToServiceModel(this ResourceDocumentModel entity) {
            if (entity == null) {
                return null;
            }
            switch (entity.ResourceType) {
                case nameof(ApiResource):
                    return new ApiResource {
                        Description = entity.Description,
                        DisplayName = entity.DisplayName,
                        Enabled = entity.Enabled,
                        Name = entity.Name,
                        Scopes = entity.Scopes?.Select(s => s.ToServiceModel()).ToList(),
                        ApiSecrets = entity.ApiSecrets?.Select(s => s.ToServiceModel()).ToList(),
                        UserClaims = entity.UserClaims?.ToList()
                    };
                case nameof(IdentityResource):
                    return new IdentityResource {
                        Description = entity.Description,
                        DisplayName = entity.DisplayName,
                        Emphasize = entity.Emphasize,
                        Enabled = entity.Enabled,
                        Name = entity.Name,
                        Required = entity.Required,
                        ShowInDiscoveryDocument = entity.ShowInDiscoveryDocument,
                        UserClaims = entity.UserClaims?.ToList()
                    };
                default:
                    throw new ResourceInvalidStateException(
                        $"Unknown resource type {entity.ResourceType}");
            }
        }

        /// <summary>
        /// Convert to document model
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public static ResourceDocumentModel ToDocumentModel(this Resource entity) {
            if (entity == null) {
                return null;
            }
            switch (entity) {
                case ApiResource api:
                    return new ResourceDocumentModel {
                        ResourceType = nameof(ApiResource),
                        Description = api.Description,
                        DisplayName = api.DisplayName,
                        Enabled = api.Enabled,
                        Name = api.Name,
                        Scopes = api.Scopes?.Select(s => s.ToDocumentModel()).ToList(),
                        ApiSecrets = api.ApiSecrets?.Select(s => s.ToDocumentModel()).ToList(),
                        UserClaims = api.UserClaims?.ToList()
                    };
                case IdentityResource id:
                    return new ResourceDocumentModel {
                        ResourceType = nameof(IdentityResource),
                        Description = id.Description,
                        DisplayName = id.DisplayName,
                        Emphasize = id.Emphasize,
                        Enabled = id.Enabled,
                        Name = id.Name,
                        Required = id.Required,
                        ShowInDiscoveryDocument = id.ShowInDiscoveryDocument,
                        UserClaims = id.UserClaims?.ToList()
                    };
                default:
                    throw new ResourceInvalidStateException(
                        $"Unknown resource type {entity.GetType()}");
            }
        }
    }
}