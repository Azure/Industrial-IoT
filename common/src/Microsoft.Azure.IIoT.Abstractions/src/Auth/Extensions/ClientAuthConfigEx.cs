// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Auth {
    using System.Linq;

    /// <summary>
    /// Client auth configuration extensions
    /// </summary>
    public static class ClientAuthConfigEx {

        /// <summary>
        /// Retrieve configuration for resource and scheme
        /// </summary>
        /// <param name="client"></param>
        /// <param name="resource"></param>
        /// <param name="scheme"></param>
        /// <param name="config"></param>
        /// <returns></returns>
        public static bool TryGetConfig(this IClientAuthConfig client,
            string resource, string scheme, out IOAuthClientConfig config) {
            config = client?.ClientSchemes?
                .FirstOrDefault(c => c.Audience == resource && c.Scheme == scheme);
            if (config == null) {
                config = client?.ClientSchemes
                    ?.FirstOrDefault(c => c.Audience == null && c.Scheme == scheme);
            }
            return config != null;
        }
    }
}
