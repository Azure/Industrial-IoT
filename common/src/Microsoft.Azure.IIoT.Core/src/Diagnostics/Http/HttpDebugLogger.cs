// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Http.Diagnostics {
    using Serilog;
    using System.Net.Http.Headers;
    using System.Threading.Tasks;
    using System.Net;
    using System;
    using System.Net.Http;
    using System.Threading;

    /// <summary>
    /// Insertable Logging handler
    /// </summary>
    public class HttpDebugLogger : HttpMessageHandlerBase {

        /// <summary>
        /// Create bearer auth handler
        /// </summary>
        /// <param name="logger"></param>
        public HttpDebugLogger(ILogger logger) {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc/>
        public override Task OnRequestAsync(string resourceId, Uri requestUri,
            HttpRequestHeaders headers, HttpContent content, CancellationToken ct) {
            _logger.Debug("REQUEST: {resourceId} {uri} {@headers} {@content}",
                resourceId, requestUri, headers, content);
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public override Task OnResponseAsync(string resourceId, Uri requestUri,
            HttpStatusCode statusCode, HttpResponseHeaders headers, HttpContent content,
            CancellationToken ct) {
            _logger.Debug("RESPONSE: {resourceId} {uri} {statusCode} {@headers} {@content}",
                resourceId, requestUri, statusCode, headers, content);
            return Task.CompletedTask;
        }

        private readonly ILogger _logger;
    }
}
