// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Auth {
    using Microsoft.Azure.IIoT.Auth.Models;
    using System.Threading.Tasks;

    /// <summary>
    /// Injectable token authentication
    /// </summary>
    public static class TokenProviderEx {

        /// <summary>
        /// Authenticate user and retrieve token.
        /// </summary>
        /// <param name="resource">Resource to authenticate</param>
        /// <returns></returns>
        /// <exception cref="System.Security.Authentication.AuthenticationException"/>
        public static Task<TokenResultModel> GetTokenForAsync(this ITokenProvider provider,
            string resource) => provider.GetTokenForAsync(resource, null);
    }
}