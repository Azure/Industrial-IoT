// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Http.Default
{
    using Microsoft.Extensions.Logging;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Wraps stateless http handlers in a delegating handler instance to be
    /// used by http client and client factory
    /// </summary>
    public class HttpHandlerDelegate : DelegatingHandler
    {
        /// <summary>
        /// Create delegating handler
        /// </summary>
        /// <param name="next"></param>
        /// <param name="resourceId"></param>
        /// <param name="handlers"></param>
        /// <param name="proxy"></param>
        /// <param name="logger"></param>
        public HttpHandlerDelegate(HttpMessageHandler next, string resourceId,
            IEnumerable<IHttpHandler> handlers,
            IWebProxy proxy, ILogger logger) : base(next)
        {
            _resourceId = resourceId;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            var root = next.GetRoot();
            if (root == null)
            {
                _logger.LogError("Cannot configure root handler, inner " +
                    "most handler is not a configurable client handler");
                return;
            }

            if (proxy != null)
            {
                if (root.SupportsProxy)
                {
                    root.UseProxy = true;
                    root.Proxy = proxy;
                }
                else
                {
                    _logger.LogWarning("Proxy configuration provided, but " +
                        "underlying handler does not support proxy " +
                        "configuration.  Skipping proxy.");
                }
            }

            handlers ??= Enumerable.Empty<IHttpHandler>();

            // Register validators
            var validators = handlers
                .OfType<IHttpCertificateValidator>()
                .Cast<IHttpCertificateValidator>()
                .ToList();
            if (validators.Count > 0)
            {
                root.ServerCertificateCustomValidationCallback =
                    (req, cert, chain, err) => validators.All(
                        v => v.Validate(req.Headers, cert, chain, err));
            }

            // Save message handlers
            _handlers = handlers
                .OfType<IHttpMessageHandler>()
                .Cast<IHttpMessageHandler>()
                .OrderBy(h => h.Order)
                .ToList();
        }

        /// <summary>
        /// Returns max lifetime
        /// </summary>
        internal TimeSpan MaxLifetime { get; set; } = TimeSpan.FromMinutes(5);

        /// <inheritdoc/>
        protected override void Dispose(bool disposing)
        {
            _handlers.OfType<IDisposable>().ToList().ForEach(d => d.Dispose());
            base.Dispose(disposing);
        }

        /// <inheritdoc/>
        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            foreach (var h in _handlers)
            {
                await h.OnRequestAsync(_resourceId, request.RequestUri,
                    request.Headers, request.Content, cancellationToken).ConfigureAwait(false);
            }
            var response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
            foreach (var h in _handlers)
            {
                await h.OnResponseAsync(_resourceId, request.RequestUri,
                    response.StatusCode, response.Headers, response.Content,
                    cancellationToken).ConfigureAwait(false);
            }
            return response;
        }

        private readonly ILogger _logger;
        private readonly string _resourceId;
        private readonly List<IHttpMessageHandler> _handlers;
    }
}
