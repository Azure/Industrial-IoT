// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Services.Auth {
    using Microsoft.Azure.IIoT.Http;
    using System.Threading.Tasks;
    using System;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Threading;

    /// <summary>
    /// User requests are marked internally with x-source which is filtered
    /// by the reverse proxy.
    /// This means it is a service to service request running in the
    /// private network, so we skip the auth required for user requests
    /// </summary>
    public class InternalAuthHandler : HttpMessageHandlerBase {

        /// <summary>
        /// Create auth handler
        /// </summary>
        public InternalAuthHandler() : 
            this (Guid.NewGuid().ToString()) {
        }

        /// <summary>
        /// Create auth handler
        /// </summary>
        /// <param name="sourceId"></param>
        public InternalAuthHandler(string sourceId) {
            _sourceId = sourceId ?? throw new ArgumentNullException(nameof(sourceId));
        }

        /// <inheritdoc/>
        public override Task OnRequestAsync(string resourceId, 
            HttpRequestHeaders headers, HttpContent content, CancellationToken ct) {
            if (headers == null) {
                throw new ArgumentNullException(nameof(headers));
            }
            headers.TryAddWithoutValidation(HttpHeader.SourceId, _sourceId);
            return Task.CompletedTask;
        }

        private readonly string _sourceId;
    }
}
