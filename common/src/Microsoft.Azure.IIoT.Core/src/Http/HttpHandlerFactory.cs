// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Http.Default {
    using Serilog;
    using System;
    using System.Linq;
    using System.Collections.Generic;
    using System.Net.Http;
    using System.Net;

    /// <summary>
    /// Wraps http handles in a delegating handler
    /// </summary>
    public sealed class HttpHandlerFactory : IHttpHandlerFactory {

        /// <summary>Constant to use as default resource id</summary>
        public static readonly string DefaultResourceId = "$default$";

        /// <summary>
        /// Create handler factory
        /// </summary>
        /// <param name="logger"></param>
        public HttpHandlerFactory(ILogger logger) :
            this(null, logger) { }

        /// <summary>
        /// Create handler factory
        /// </summary>
        /// <param name="handlers"></param>
        /// <param name="logger"></param>
        public HttpHandlerFactory(IEnumerable<IHttpHandler> handlers, ILogger logger) :
            this(handlers, null, logger) { }

        /// <summary>
        /// Create handler factory
        /// </summary>
        /// <param name="handlers"></param>
        /// <param name="proxy"></param>
        /// <param name="logger"></param>
        public HttpHandlerFactory(IEnumerable<IHttpHandler> handlers,
            IWebProxy proxy, ILogger logger) {
            _proxy = proxy;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _handlers = handlers?.ToList() ?? new List<IHttpHandler>();
        }

        /// <inheritdoc/>
        public TimeSpan Create(string name, out HttpMessageHandler handler) {
            var resource = name == DefaultResourceId ? Resource.None : name;
            if (resource != null && resource.StartsWith(Resource.Local)) {
                resource = resource.Remove(0, Resource.Local.Length);
            }
#pragma warning disable IDE0067 // Dispose objects before losing scope
            var del = new HttpHandlerDelegate(new HttpClientHandler(), resource,
#pragma warning restore IDE0067 // Dispose objects before losing scope
                _handlers.Where(h => h.IsFor?.Invoke(resource) ?? true),
                _proxy, _logger);
            handler = del;
            return del.MaxLifetime;
        }

        private readonly IWebProxy _proxy;
        private readonly ILogger _logger;
        private readonly List<IHttpHandler> _handlers;
    }
}
