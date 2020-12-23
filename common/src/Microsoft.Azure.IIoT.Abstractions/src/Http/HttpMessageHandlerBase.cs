// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Http {
    using System.Net.Http.Headers;
    using System.Threading.Tasks;
    using System.Net;
    using System.Net.Http;
    using System.Threading;
    using System;

    /// <summary>
    /// Base message handler
    /// </summary>
    public abstract class HttpMessageHandlerBase : IHttpMessageHandler {

        /// <inheritdoc/>
        public int Order { get; protected set; }

        /// <inheritdoc/>
        public Func<string, bool> IsFor { get; protected set; }

        /// <inheritdoc/>
        public virtual void Configure(IHttpHandlerHost configuration) {
            // No op
        }

        /// <inheritdoc/>
        public virtual Task OnRequestAsync(string resourceId, Uri requestUri,
            HttpRequestHeaders headers, HttpContent content, CancellationToken ct) {
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public virtual Task OnResponseAsync(string resourceId, Uri requestUri,
            HttpStatusCode statusCode, HttpResponseHeaders headers,
            HttpContent content, CancellationToken ct) {
            return Task.CompletedTask;
        }
    }
}
