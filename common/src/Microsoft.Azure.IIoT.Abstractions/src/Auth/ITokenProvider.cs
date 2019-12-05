// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Auth {
    using Microsoft.Azure.IIoT.Auth.Models;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    /// <summary>
    /// Injectable token authentication
    /// </summary>
    public interface ITokenProvider {

        /// <summary>
        /// Authenticate user and retrieve token.
        /// </summary>
        /// in case authentication failed.
        /// <exception cref="System.Security.Authentication.AuthenticationException"/>
        /// <param name="resource">Resource to authenticate</param>
        /// <param name="scopes">Scope permissions to request</param>
        /// <returns></returns>
        Task<TokenResultModel> GetTokenForAsync(string resource,
            IEnumerable<string> scopes = null);

        /// <summary>
        /// Invalidate any token in token cache for
        /// named resource.
        /// </summary>
        /// <param name="resource"></param>
        Task InvalidateAsync(string resource);
    }
}
