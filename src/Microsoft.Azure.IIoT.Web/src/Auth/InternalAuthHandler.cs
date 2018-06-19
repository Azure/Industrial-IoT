// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Web.Auth {
    using Microsoft.Azure.IIoT.Http;
    using System.Threading.Tasks;
    using System;

    /// <summary>
    /// User requests are marked internally with x-source which is filtered
    /// by the reverse proxy.
    /// This means it is a service to service request running in the
    /// private network, so we skip the auth required for user requests
    /// </summary>
    public class InternalAuthHandler : IHttpAuthHandler {

        internal const string kSourceHeader = "X-Source";
        internal const string kRoleHeader = "X-Role";

        /// <summary>
        /// Create auth handler
        /// </summary>
        public InternalAuthHandler() : this (Guid.NewGuid().ToString()) {
        }

        /// <summary>
        /// Create auth handler
        /// </summary>
        /// <param name="sourceId"></param>
        public InternalAuthHandler(string sourceId) {
            _sourceId = sourceId ?? throw new ArgumentNullException(nameof(sourceId));
        }

        /// <summary>
        /// Authenticate request using provider
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public Task OnRequestAsync(IHttpRequest request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            request.AddHeader(kSourceHeader, _sourceId);
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task OnResponseAsync(IHttpResponse response) => Task.CompletedTask;

        private readonly string _sourceId;
    }
}
