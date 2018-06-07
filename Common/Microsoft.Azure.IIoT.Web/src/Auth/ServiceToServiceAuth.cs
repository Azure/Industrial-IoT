// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Web.Auth {
    using Microsoft.Azure.IIoT.Http;
    using System.Threading.Tasks;
    using System;

    /// <summary>
    /// Adds x-source id
    /// </summary>
    public class ServiceToServiceAuth : IHttpAuthHandler {

        /// <summary>
        /// Create auth handler
        /// </summary>
        public ServiceToServiceAuth() : this (Guid.NewGuid().ToString()) {
        }

        /// <summary>
        /// Create auth handler
        /// </summary>
        /// <param name="sourceId"></param>
        public ServiceToServiceAuth(string sourceId) {
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
            request.AddHeader("X-Source", _sourceId);
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task OnResponseAsync(IHttpResponse response) => Task.CompletedTask;

        private readonly string _sourceId;
    }
}
