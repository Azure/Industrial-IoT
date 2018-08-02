// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Http.Diagnostics {
    using Microsoft.Azure.IIoT.Diagnostics;
    using System.Net.Http.Headers;
    using System.Threading.Tasks;
    using System.Net;
    using System;
    using System.Net.Http;
    using System.Threading;

    public class HttpDebugLogger : HttpMessageHandlerBase {

        /// <summary>
        /// Create bearer auth handler
        /// </summary>
        /// <param name="logger"></param>
        public HttpDebugLogger(ILogger logger) {
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
        public override Task OnRequestAsync(string resourceId,
            HttpRequestHeaders headers, HttpContent content, CancellationToken ct) {
            _logger.Debug($"REQUEST:", () => new {
                resourceId,
                headers,
                content
            });
            return Task.CompletedTask;
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
        public override Task OnResponseAsync(string resourceId, HttpStatusCode statusCode,
            HttpResponseHeaders headers, HttpContent content, CancellationToken ct) {
            _logger.Debug($"RESPONSE:", () => new {
                resourceId,
                statusCode,
                headers,
                content
            });
            return Task.CompletedTask;
        }

        private readonly ILogger _logger;
    }
}
