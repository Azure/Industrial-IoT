// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Auth.Server {
    using System;

    /// <summary>
    /// Configuration extensions
    /// </summary>
    public static class AuthSchemeConfigEx {

        /// <summary>
        /// Url of the token issuing instance.  E.g. the JWT bearer
        /// authentication middleware will use this URI as base
        /// uri to retrieve the public key that can be used to
        /// validate the token's signature.
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        public static string GetAuthorityUrl(this IOAuthServerConfig config) {
            var authorityUrl = config?.InstanceUrl?.TrimEnd('/');

            if (string.IsNullOrEmpty(authorityUrl)) {
                // Default to aad
                authorityUrl = kDefaultAuthorityUrl;
            }

            var tenantId = config?.TenantId;
            if (authorityUrl.Equals(kDefaultAuthorityUrl,
                StringComparison.InvariantCultureIgnoreCase)) {

                // use v2.0 endpoint of AAD with tenant if set
                if (string.IsNullOrEmpty(tenantId)) {
                    tenantId = "common";
                }
                return authorityUrl + "/" + tenantId + "/v2.0";
            }

            // Non aad e.g. identity server with optional tenant id.
            if (string.IsNullOrEmpty(tenantId)) {
                authorityUrl += "/" + tenantId;
            }
            return authorityUrl;
        }

        /// <summary>
        /// Returns the scheme name
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        public static string GetSchemeName(this IOAuthServerConfig config) {
            var name = config.Scheme;
            if (string.IsNullOrEmpty(name)) {
                return "Bearer";
            }
            return name;
        }

        private const string kDefaultAuthorityUrl = "https://login.microsoftonline.com";
    }
}
