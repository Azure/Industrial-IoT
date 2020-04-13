// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------


namespace Microsoft.Azure.IIoT.Auth.Clients {
    using Microsoft.IdentityModel.Clients.ActiveDirectory;

    /// <summary>
    /// Token cache provider interface
    /// </summary>
    public interface IAdalTokenCacheProvider {

        /// <summary>
        /// Return a token cache object that maps to the
        /// store and that can be used with adal.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        TokenCache GetCache(string name);
    }
}
