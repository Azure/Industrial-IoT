// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Auth.Clients {
    using System;

    /// <summary>
    /// Configuration extensions
    /// </summary>
    public static class ClientConfigEx {

        /// <summary>
        /// Helper to get the autority url
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        public static string GetAuthorityUrl(this IOAuthClientConfig config) {
            var instanceUrl = config?.InstanceUrl;
            if (string.IsNullOrEmpty(instanceUrl)) {
                instanceUrl = kDefaultInstanceUrl;
            }
            var tenantId = config?.TenantId;
            if (string.IsNullOrEmpty(tenantId)) {
                tenantId = kDefaultTenantId;
            }
            return instanceUrl.TrimEnd('/') + "/" + tenantId;
        }

        /// <summary>
        /// Get domain
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        public static string GetDomain(this IOAuthClientConfig config) {
            return new Uri(config.GetAuthorityUrl()).DnsSafeHost;
        }

        private const string kDefaultInstanceUrl = "https://login.microsoftonline.com/";
        private const string kDefaultTenantId = "common";
    }
}
