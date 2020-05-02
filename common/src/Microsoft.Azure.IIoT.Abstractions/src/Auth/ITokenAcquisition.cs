// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Auth {
    using Microsoft.Azure.IIoT.Auth.Models;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    /// <summary>
    /// Injectable token provider implementation
    /// </summary>
    public interface ITokenAcquisition {

        /// <summary>
        /// The token provider supports this resource
        /// </summary>
        bool Supports(string resource);

        /// <summary>
        /// Acquires a token for a target resource.  In case token
        /// cannot be retrieved should not throw but return null and
        /// log errors as appropriate.
        /// </summary>
        /// <param name="resource"><see cref="Http.Resource"/> to
        /// acquire token for</param>
        /// <param name="scopes">Scope permissions to request</param>
        /// <returns>null if no token could be retrieved</returns>
        Task<TokenResultModel> GetTokenForAsync(string resource,
            IEnumerable<string> scopes = null);

        /// <summary>
        /// Invalidate tokens for a target resource after
        /// failed authentication.
        /// </summary>
        /// <param name="resource"><see cref="Http.Resource"/> to
        /// invalidate</param>
        Task InvalidateAsync(string resource);
    }
}
