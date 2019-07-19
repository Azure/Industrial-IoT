// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Auth.Clients {

    /// <summary>
    /// Configuration extensions
    /// </summary>
    public static class ClientConfigEx {

        /// <summary>
        /// Helper to create the autority url
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        public static string GetAuthorityUrl(this IClientConfig config) {
            var instanceUrl = config?.InstanceUrl;
            if (string.IsNullOrEmpty(instanceUrl)) {
                instanceUrl = kDefaultAuthorityUrl;
            }
            var tenantId = /* config?.AppSecret ?? */ config?.TenantId;
            if (string.IsNullOrEmpty(tenantId)) {
                tenantId = kDefaultTenantId;
            }
            return instanceUrl.TrimEnd('/') + "/" + tenantId;
        }

        private const string kDefaultAuthorityUrl = "https://login.microsoftonline.com/";
        private const string kDefaultTenantId = "common";
    }
}
