// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Auth.Azure {
    using Microsoft.IdentityModel.Clients.ActiveDirectory;

    public class DefaultTokenCacheProvider : ITokenCacheProvider {

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