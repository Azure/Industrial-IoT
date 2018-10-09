// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Http.Default {
    using Microsoft.Azure.IIoT.Diagnostics;
    using System;
    using System.Linq;
    using System.Collections.Generic;
    using System.Net.Http;
    using System.Net;
    using System.Collections.Concurrent;

    /// <summary>
    /// Wraps http handles in a delegating handler
    /// </summary>
    public class HttpHandlerFactory : IHttpHandlerFactory {

        /// <summary>Constant to use as default resource id</summary>
        public static readonly string kDefaultResourceId = "$default$";

        /// <summary>
        /// Create handler factory
        /// </summary>
        /// <param name="logger"></param>
        public HttpHandlerFactory(ILogger logger) :
            this (null, logger) { }

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
            _cache = new ConcurrentDictionary<string, List<IHttpHandler>>();
            _handlers = handlers?.ToList() ?? new List<IHttpHandler>();
        }

        /// <inheritdoc/>
        public TimeSpan Create(string name, out HttpMessageHandler handler) {
            var resource = name == kDefaultResourceId ? null : name;
            var del = new HttpHandlerDelegate(new HttpClientHandler(), resource,
                _handlers.Where(h => h.IsFor?.Invoke(resource) ?? true),
                _proxy, _logger);
            handler = del;
            return del.MaxLifetime;
        }

        private readonly IWebProxy _proxy;
        private readonly ILogger _logger;
        private readonly ConcurrentDictionary<string, List<IHttpHandler>> _cache;
        private readonly List<IHttpHandler> _handlers;
    }
}
