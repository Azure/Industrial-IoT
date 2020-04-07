// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Module.Default {
    using Microsoft.Azure.IIoT.Module;
    using Microsoft.Azure.IIoT.Messaging;
    using Microsoft.Azure.IIoT.Http;
    using Microsoft.Azure.IIoT.Http.Default;
    using Microsoft.Azure.IIoT.Serializers;
    using Serilog;
    using System;
    using System.Net;
    using System.Net.Http;
    using System.Collections.Generic;

    /// <summary>
    /// Makes the tunnel configurable
    /// </summary>
    public sealed class HttpTunnelConfigurableFactory : IHttpHandlerFactory, IHttpTunnelConfig {

        /// <inheritdoc/>
        public bool UseTunnel { get; set; }

        /// <inheritdoc/>
        public HttpTunnelConfigurableFactory(IEventClient client,
            IJsonSerializer serializer, IEnumerable<IHttpHandler> handlers, ILogger logger) {
            _tunnel = new HttpTunnelHandlerFactory(client, serializer, handlers, logger);
            _fallback = new HttpHandlerFactory(handlers, logger);
        }

        /// <inheritdoc/>
        public HttpTunnelConfigurableFactory(IEventClient client, IWebProxy proxy,
            IJsonSerializer serializer, IEnumerable<IHttpHandler> handlers, ILogger logger) {
            _tunnel = new HttpTunnelHandlerFactory(client, serializer, handlers, logger);
            _fallback = new HttpHandlerFactory(handlers, proxy, logger);
        }

        /// <inheritdoc/>
        public TimeSpan Create(string name, out HttpMessageHandler handler) {
            return UseTunnel ?
                _tunnel.Create(name, out handler) :
                _fallback.Create(name, out handler);
        }

        private readonly HttpTunnelHandlerFactory _tunnel;
        private readonly HttpHandlerFactory _fallback;
    }
}
