// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Auth.IdentityServer4.Models {
    using System.Linq;
    using global::IdentityServer4.Models;

    /// <summary>
    /// Convert model to document and back
    /// </summary>
    internal static class ClientDocumentModelEx {

        /// <summary>
        /// Convert to service model
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public static Client ToServiceModel(this ClientDocumentModel entity) {
            if (entity == null) {
                return null;
            }
            return new Client {
                AbsoluteRefreshTokenLifetime = entity.AbsoluteRefreshTokenLifetime,
                AccessTokenLifetime = entity.AccessTokenLifetime,
                AccessTokenType = (AccessTokenType)entity.AccessTokenType,
                AllowAccessTokensViaBrowser = entity.AllowAccessTokensViaBrowser,
                AllowedCorsOrigins = entity.AllowedCorsOrigins?.ToList(),
                AllowedGrantTypes = entity.AllowedGrantTypes?.ToList(),
                AllowedScopes = entity.AllowedScopes?.ToList(),
                AllowOfflineAccess = entity.AllowOfflineAccess,
                AllowPlainTextPkce = entity.AllowPlainTextPkce,
                AllowRememberConsent = entity.AllowRememberConsent,
                AlwaysIncludeUserClaimsInIdToken = entity.AlwaysIncludeUserClaimsInIdToken,
                AlwaysSendClientClaims = entity.AlwaysSendClientClaims,
                AuthorizationCodeLifetime = entity.AuthorizationCodeLifetime,
                BackChannelLogoutSessionRequired = entity.BackChannelLogoutSessionRequired,
                BackChannelLogoutUri = entity.BackChannelLogoutUri,
                Claims = entity.Claims?
                    .Select(c => c.ToServiceModel()).ToList(),
                ClientClaimsPrefix = entity.ClientClaimsPrefix,
                ClientId = entity.ClientId,
                ClientName = entity.ClientName,
                ClientSecrets = entity.ClientSecrets
                    .Select(c => c.ToServiceModel()).ToList(),
                ClientUri = entity.ClientUri,
                ConsentLifetime = entity.ConsentLifetime,
                Enabled = entity.Enabled,
                EnableLocalLogin = entity.EnableLocalLogin,
                FrontChannelLogoutSessionRequired = entity.FrontChannelLogoutSessionRequired,
                FrontChannelLogoutUri = entity.FrontChannelLogoutUri,
                IdentityProviderRestrictions = entity.IdentityProviderRestrictions?.ToList(),
                IdentityTokenLifetime = entity.IdentityTokenLifetime,
                IncludeJwtId = entity.IncludeJwtId,
                LogoUri = entity.LogoUri,
                PairWiseSubjectSalt = entity.PairWiseSubjectSalt,
                PostLogoutRedirectUris = entity.PostLogoutRedirectUris?.ToList(),
                Properties = entity.Properties?
                    .ToDictionary(kv => kv.Key, kv => kv.Value),
                ProtocolType = entity.ProtocolType,
                RedirectUris = entity.RedirectUris?.ToList(),
                RefreshTokenExpiration = (TokenExpiration)entity.RefreshTokenExpiration,
                RefreshTokenUsage = (TokenUsage)entity.RefreshTokenUsage,
                RequireClientSecret = entity.RequireClientSecret,
                RequireConsent = entity.RequireConsent,
                RequirePkce = entity.RequirePkce,
                SlidingRefreshTokenLifetime = entity.SlidingRefreshTokenLifetime,
                UpdateAccessTokenClaimsOnRefresh = entity.UpdateAccessTokenClaimsOnRefresh
            };
        }

        /// <summary>
        /// Maps to document
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static ClientDocumentModel ToDocumentModel(this Client model) {
            if (model == null) {
                return null;
            }
            return new ClientDocumentModel {
                AbsoluteRefreshTokenLifetime = model.AbsoluteRefreshTokenLifetime,
                AccessTokenLifetime = model.AccessTokenLifetime,
                AccessTokenType = (int)model.AccessTokenType,
                AllowAccessTokensViaBrowser = model.AllowAccessTokensViaBrowser,
                AllowedCorsOrigins = model.AllowedCorsOrigins?
                    .Select(o => o.ToLowerInvariant()).ToList(),
                AllowedGrantTypes = model.AllowedGrantTypes?.ToList(),
                AllowedScopes = model.AllowedScopes?.ToList(),
                AllowOfflineAccess = model.AllowOfflineAccess,
                AllowPlainTextPkce = model.AllowPlainTextPkce,
                AllowRememberConsent = model.AllowRememberConsent,
                AlwaysIncludeUserClaimsInIdToken = model.AlwaysIncludeUserClaimsInIdToken,
                AlwaysSendClientClaims = model.AlwaysSendClientClaims,
                AuthorizationCodeLifetime = model.AuthorizationCodeLifetime,
                BackChannelLogoutSessionRequired = model.BackChannelLogoutSessionRequired,
                BackChannelLogoutUri = model.BackChannelLogoutUri,
                Claims = model.Claims?
                    .Select(c => c.ToDocumentModel()).ToList(),
                ClientClaimsPrefix = model.ClientClaimsPrefix,
                ClientId = model.ClientId,
                ClientName = model.ClientName,
                ClientSecrets = model.ClientSecrets
                    .Select(c => c.ToDocumentModel()).ToList(),
                ClientUri = model.ClientUri,
                ConsentLifetime = model.ConsentLifetime,
                Enabled = model.Enabled,
                EnableLocalLogin = model.EnableLocalLogin,
                FrontChannelLogoutSessionRequired = model.FrontChannelLogoutSessionRequired,
                FrontChannelLogoutUri = model.FrontChannelLogoutUri,
                IdentityProviderRestrictions = model.IdentityProviderRestrictions?.ToList(),
                IdentityTokenLifetime = model.IdentityTokenLifetime,
                IncludeJwtId = model.IncludeJwtId,
                LogoUri = model.LogoUri,
                PairWiseSubjectSalt = model.PairWiseSubjectSalt,
                PostLogoutRedirectUris = model.PostLogoutRedirectUris?.ToList(),
                Properties = model.Properties?
                    .ToDictionary(kv => kv.Key, kv => kv.Value),
                ProtocolType = model.ProtocolType,
                RedirectUris = model.RedirectUris?.ToList(),
                RefreshTokenExpiration = (int)model.RefreshTokenExpiration,
                RefreshTokenUsage = (int)model.RefreshTokenUsage,
                RequireClientSecret = model.RequireClientSecret,
                RequireConsent = model.RequireConsent,
                RequirePkce = model.RequirePkce,
                SlidingRefreshTokenLifetime = model.SlidingRefreshTokenLifetime,
                UpdateAccessTokenClaimsOnRefresh = model.UpdateAccessTokenClaimsOnRefresh
            };
        }
    }
}