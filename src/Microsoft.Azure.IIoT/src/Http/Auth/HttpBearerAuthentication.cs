// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Http.Auth {
    using Microsoft.Azure.IIoT.Diagnostics;
    using Microsoft.Azure.IIoT.Auth;
    using System.Net.Http.Headers;
    using System.Threading.Tasks;
    using System.Net;
    using System;
    using System.Linq;

    public class HttpBearerAuthentication : IHttpAuthHandler {

        /// <summary>
        /// Create bearer auth handler
        /// </summary>
        /// <param name="provider"></param>
        /// <param name="logger"></param>
        public HttpBearerAuthentication(ITokenProvider provider, ILogger logger) {
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Authenticate request using provider
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task OnRequestAsync(IHttpRequest request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            if (!string.IsNullOrEmpty(request.ResourceId)) {

                // TODO: also get scopes/desired permissions from the request,
                // e.g. read/write, etc. A provider that
                var desiredPermissions = Enumerable.Empty<string>();
                // TODO...

                var result = await _provider.GetTokenForAsync(request.ResourceId,
                    desiredPermissions);

                if (result?.RawToken != null) {
                    request.Headers.Authorization = new AuthenticationHeaderValue(
                        "Bearer", result.RawToken);
                }
            }
        }

        /// <summary>
        /// Invalidate if needed
        /// </summary>
        /// <param name="response"></param>
        /// <returns></returns>
        public async Task OnResponseAsync(IHttpResponse response) {
            if (response == null) {
                throw new ArgumentNullException(nameof(response));
            }
            if (response.StatusCode == HttpStatusCode.Unauthorized) {
                await _provider.InvalidateAsync(response.ResourceId);
            }
        }

        private readonly ITokenProvider _provider;
        private readonly ILogger _logger;
    }
}
