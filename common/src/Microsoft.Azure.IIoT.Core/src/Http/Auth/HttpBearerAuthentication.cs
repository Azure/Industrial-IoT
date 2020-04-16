// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Http.Auth {
    using Microsoft.Azure.IIoT.Auth;
    using System.Net.Http.Headers;
    using System.Threading.Tasks;
    using System.Net;
    using System;
    using System.Linq;
    using System.Net.Http;
    using System.Threading;

    /// <summary>
    /// Bearer authentication handler
    /// </summary>
    public class HttpBearerAuthentication : HttpMessageHandlerBase {

        /// <summary>
        /// Create bearer auth handler
        /// </summary>
        /// <param name="provider"></param>
        public HttpBearerAuthentication(ITokenProvider provider) {
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
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
                //
                // TODO: Eventually we also need scopes/desired permissions
                // for the token, e.g. read, read/write, etc.
                // We need a way to plumb them through to here, ideally add
                // them to the api signatures or less ideal, parse them out
                // of the resource id.
                //
                var desiredPermissions = Enumerable.Empty<string>();

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
    }
}
