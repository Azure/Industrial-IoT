// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Auth.Azure {
    using Microsoft.IdentityModel.Clients.ActiveDirectory;

    /// <summary>
    /// Default token cache adapter
    /// </summary>
    public class DefaultTokenCacheProvider : ITokenCacheProvider {

        /// <summary>Singleton</summary>
        public static ITokenCacheProvider Instance =>
            new DefaultTokenCacheProvider();

        /// <summary>
        /// Returns the default shared cache
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public TokenCache GetCache(string name) =>
            TokenCache.DefaultShared;
    }
}
