// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Deployment.Authentication {

    using System;
    using System.Net.Http.Headers;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.Rest;

    class DelegatingTokenProvider : ITokenProvider {

        private readonly Func<CancellationToken, Task<string>> _accessTokenProvider;

        public DelegatingTokenProvider(
            Func<CancellationToken, Task<string>> accessTokenProvider
        ) {
            _accessTokenProvider = accessTokenProvider;
        }

        public async Task<AuthenticationHeaderValue> GetAuthenticationHeaderAsync(
            CancellationToken cancellationToken
        ) {
            var accessToken = await _accessTokenProvider(cancellationToken);
            var headerValue = new AuthenticationHeaderValue("Bearer", accessToken);
            return headerValue;
        }
    }
}
