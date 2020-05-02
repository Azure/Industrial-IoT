// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Auth {
    using Microsoft.Azure.IIoT.Auth.Models;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    /// <summary>
    /// Token retriever uses a strategy to retrieve tokens for a
    /// particular <see cref="Http.Resource"/>.
    /// </summary>
    public interface ITokenSource {

        /// <summary>
        /// The token source is enabled
        /// </summary>
        bool IsEnabled { get; }

        /// <summary>
        /// Retrieves tokens for this <see cref="Http.Resource"/>
        /// </summary>
        string Resource { get; }

        /// <summary>
        /// Authenticate user and retrieve token.
        /// </summary>
        /// <param name="scopes">Scope permissions to request</param>
        /// <returns>null if no token could be retrieved</returns>
        Task<TokenResultModel> GetTokenAsync(
            IEnumerable<string> scopes = null);

        /// <summary>
        /// Invalidate any token in token cache for
        /// named resource.
        /// </summary>
        Task InvalidateAsync();
    }
}
