// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Auth.Clients.Default {
    using Microsoft.IdentityModel.Clients.ActiveDirectory;

    /// <summary>
    /// Default token cache adapter
    /// </summary>
    public class DefaultAdalTokenCacheProvider : IAdalTokenCacheProvider {

        /// <summary>Singleton</summary>
        public static IAdalTokenCacheProvider Instance =>
            new DefaultAdalTokenCacheProvider();

        /// <summary>
        /// Returns the default shared cache
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public TokenCache GetCache(string name) {
            return TokenCache.DefaultShared;
        }
    }
}
