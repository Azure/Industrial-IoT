// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Auth.Clients {
    using System;

    /// <summary>
    /// Configuration extensions
    /// </summary>
    public static class OAuthClientConfigEx {

        /// <summary>
        /// Helper to get the autority url
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        public static string GetAuthorityUrl(this IOAuthClientConfig config) {
            var authorityUrl = config?.InstanceUrl?.TrimEnd('/');

            var tenantId = config?.TenantId;
            if (config.GetSchemeName() == AuthScheme.Aad) {
                if (string.IsNullOrEmpty(authorityUrl)) {
                    // Default to aad
                    authorityUrl = kDefaultAuthorityUrl;
                }
                if (string.IsNullOrEmpty(tenantId)) {
                    tenantId = "common";
                }
                return authorityUrl + "/" + tenantId;
            }

            if (string.IsNullOrEmpty(authorityUrl)) {
                throw new ArgumentNullException(nameof(config.InstanceUrl));
            }
            // Non aad e.g. identity server with optional tenant id.
            if (!string.IsNullOrEmpty(tenantId)) {
                authorityUrl += "/" + tenantId;
            }
            return authorityUrl;
        }

        /// <summary>
        /// Get domain
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        public static string GetDomain(this IOAuthClientConfig config) {
            return new Uri(config.GetAuthorityUrl()).DnsSafeHost;
        }

        /// <summary>
        /// Returns the scheme name
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        public static string GetSchemeName(this IOAuthClientConfig config) {
            var name = config.Scheme;
            if (string.IsNullOrEmpty(name)) {
                return AuthScheme.Unknown;
            }
            return name;
        }

        private const string kDefaultAuthorityUrl = "https://login.microsoftonline.com/";
    }
}
