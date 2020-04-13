// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Auth.Clients {
    using System;
    using System.Collections.Generic;
    using System.Linq;

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
        /// Get Resource or audience
        /// </summary>
        /// <param name="config"></param>
        /// <param name="scopes"></param>
        /// <returns></returns>
        public static string GetAudience(this IOAuthClientConfig config,
            IEnumerable<string> scopes = null) {
            var audience = config?.Audience;
            if (!string.IsNullOrEmpty(audience)) {
                return audience;
            }
            // Get audience uri from scopes
            return SplitScope(scopes?.FirstOrDefault()).Item1;
        }

        /// <summary>
        /// Get scopes
        /// </summary>
        /// <param name="config"></param>
        /// <param name="scopes"></param>
        /// <returns></returns>
        public static IEnumerable<string> GetScopeNames(this IOAuthClientConfig config,
            IEnumerable<string> scopes = null) {
            var audience = config?.Audience;
            if (string.IsNullOrEmpty(audience)) {
                return scopes;
            }
            return scopes?.Select(s => SplitScope(s).Item2);
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

        /// <summary>
        /// Split audience from scope name and return both
        /// </summary>
        /// <param name="scope"></param>
        /// <returns></returns>
        private static (string, string) SplitScope(string scope) {
            if (!string.IsNullOrEmpty(scope)) {
                if (Uri.TryCreate(scope, UriKind.RelativeOrAbsolute, out var uri)) {
                    var scopeName = uri.PathAndQuery;
                    var idx = scopeName.LastIndexOf('/');
                    var pathRemaining = string.Empty;
                    if (idx != -1) {
                        pathRemaining = scopeName.Substring(0, idx);
                    }
                    var audience = new UriBuilder(uri) {
                        Path = pathRemaining
                    }.Uri.ToString().TrimEnd('/');
                    return (audience, scopeName.TrimStart('/'));
                }
                return (string.Empty, scope);
            }
            return (string.Empty, string.Empty);
        }

        private const string kDefaultAuthorityUrl = "https://login.microsoftonline.com/";
    }
}
