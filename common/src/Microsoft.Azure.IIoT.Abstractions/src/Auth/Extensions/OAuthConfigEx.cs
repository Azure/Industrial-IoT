// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Auth {
    using System;

    /// <summary>
    /// Configuration extensions
    /// </summary>
    public static class OAuthConfigEx {

        /// <summary>
        /// Url of the token issuing instance.  E.g. the JWT bearer
        /// authentication middleware will use this URI as base
        /// uri to retrieve the public key that can be used to
        /// validate the token's signature.
        /// </summary>
        /// <param name="config"></param>
        /// <param name="noVersion"></param>
        /// <returns></returns>
        public static string GetAuthorityUrl(this IOAuthConfig config,
            bool noVersion = false) {
            var authorityUrl = config?.InstanceUrl?.TrimEnd('/');

            var tenantId = config?.TenantId;
            var provider = config.GetProviderName();

            if (string.IsNullOrEmpty(authorityUrl)) {
                // Default to aad
                authorityUrl = kDefaultAuthorityUrl;
            }
            if (provider == AuthProvider.AzureAD) {

                // use v2.0 endpoint of AAD with tenant if set
                if (string.IsNullOrEmpty(tenantId)) {
                    tenantId = "common";
                }
                authorityUrl += "/" + tenantId;
                if (!noVersion) {
                    authorityUrl += "/v2.0";
                }
                return authorityUrl;
            }

            // Non aad e.g. identity server with optional tenant id.
            if (!string.IsNullOrEmpty(tenantId)) {
                authorityUrl += "/" + tenantId;
            }
            return authorityUrl;
        }

        /// <summary>
        /// Returns the provider name
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        public static string GetProviderName(this IOAuthConfig config) {
            var name = config.Provider;
            if (string.IsNullOrEmpty(name)) {
                return AuthProvider.Unknown;
            }
            return name;
        }

        private const string kDefaultAuthorityUrl = "https://login.microsoftonline.com";
    }
}
