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
    using Serilog;

    /// <summary>
    /// Bearer authentication handler
    /// </summary>
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

        /// <inheritdoc/>
        public override async Task OnRequestAsync(string resourceId, Uri requestUri,
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
                else {
                    _logger.Error("Failed to aquire token calling " +
                        "{request} ({resource}) - calling without...",
                        requestUri, resourceId);
                }
            }
        }

        /// <inheritdoc/>
        public override async Task OnResponseAsync(string resourceId, Uri requestUri,
            HttpStatusCode statusCode, HttpResponseHeaders headers, HttpContent content,
            CancellationToken ct) {
            if (headers == null) {
                throw new ArgumentNullException(nameof(headers));
            }

            if (resourceId != null) {
                if (statusCode == HttpStatusCode.Unauthorized) {
                    await _provider.InvalidateAsync(resourceId);
                }
            }
        }

        private readonly ITokenProvider _provider;
        private readonly ILogger _logger;
    }
}
