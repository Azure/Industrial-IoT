// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Auth.Clients {
    using Microsoft.Azure.IIoT.Auth.Models;
    using Microsoft.Azure.IIoT.Auth;
    using Microsoft.Azure.IIoT.Utils;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using System;

    /// <summary>
    /// Use token provider as token source
    /// </summary>
    public class TokenServiceSource<T> : ITokenSource
        where T : ITokenClient {

        /// <inheritdoc/>
        public string Resource { get; } = Http.Resource.Platform;

        /// <inheritdoc/>
        public bool IsEnabled => _provider.Supports(Resource);

        /// <inheritdoc/>
        public TokenServiceSource(T provider) {
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        }

        /// <inheritdoc/>
        public async Task<TokenResultModel> GetTokenAsync(
            IEnumerable<string> scopes = null) {
            return await Try.Async(() => _provider.GetTokenForAsync(Resource, scopes));
        }

        /// <inheritdoc/>
        public async Task InvalidateAsync() {
            await Try.Async(() => _provider.InvalidateAsync(Resource));
        }

        private readonly T _provider;
    }
}
