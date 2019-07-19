// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Auth.Server {

    /// <summary>
    /// Configuration extensions
    /// </summary>
    public static class AuthConfigEx {

        /// <summary>
        /// Url of the token issuing instance.  E.g. the JWT bearer
        /// authentication middleware will use this URI as base
        /// uri to retrieve the public key that can be used to
        /// validate the token's signature.
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        public static string GetAuthorityUrl(this IAuthConfig config) {
            var instanceUrl = config?.InstanceUrl;
            if (string.IsNullOrEmpty(instanceUrl)) {
                instanceUrl = "https://login.microsoftonline.com/";
            }
            var tenantId = config?.TenantId;
            if (string.IsNullOrEmpty(tenantId)) {
                tenantId = "common";
            }
            return instanceUrl.TrimEnd('/') + "/" + tenantId;
        }
    }
}
