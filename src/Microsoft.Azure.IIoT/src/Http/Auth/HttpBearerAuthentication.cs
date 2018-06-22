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
    using System.Net.Http;
    using System.Threading;

    public class HttpBearerAuthentication : HttpMessageHandlerBase {

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
        /// <param name="resourceId"></param>
        /// <param name="headers"></param>
        /// <param name="content"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public override async Task OnRequestAsync(string resourceId,
            HttpRequestHeaders headers, HttpContent content, CancellationToken ct) {
            if (headers == null) {
                throw new ArgumentNullException(nameof(headers));
            }
            if (!string.IsNullOrEmpty(resourceId)) {
                // TODO: also get scopes/desired permissions from the request,
                // e.g. read/write, etc. A provider that
                var desiredPermissions = Enumerable.Empty<string>();
                // TODO...

                var result = await _provider.GetTokenForAsync(resourceId,
                    desiredPermissions);

                if (result?.RawToken != null) {
                    headers.Authorization = new AuthenticationHeaderValue(
                        "Bearer", result.RawToken);
                }
            }
        }

        /// <summary>
        /// Invalidate if needed
        /// </summary>
        /// <param name="resourceId"></param>
        /// <param name="statusCode"></param>
        /// <param name="headers"></param>
        /// <param name="content"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public override async Task OnResponseAsync(string resourceId, HttpStatusCode statusCode,
            HttpResponseHeaders headers, HttpContent content, CancellationToken ct) {
            if (headers == null) {
                throw new ArgumentNullException(nameof(headers));
            }
            if (statusCode == HttpStatusCode.Unauthorized) {
                if (!string.IsNullOrEmpty(resourceId)) {
                    await _provider.InvalidateAsync(resourceId);
                }
            }
        }

        private readonly ITokenProvider _provider;
        private readonly ILogger _logger;
    }
}
