// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Protocol.Transport {
    using Serilog;
    using Microsoft.AspNetCore.Http;
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// Secure channel over websocket middleware
    /// </summary>
    public class WebSocketMiddleware {

        /// <inheritdoc/>
        public Uri EndpointUrl { get; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="next"></param>
        /// <param name="listener"></param>
        /// <param name="logger"></param>
        public WebSocketMiddleware(RequestDelegate next,
            IWebSocketChannelListener listener, ILogger logger) {
            _next = next;
            _listener = listener ??
                throw new ArgumentNullException(nameof(listener));
            _logger = logger ??
                throw new ArgumentNullException(nameof(logger));

            EndpointUrl = null; // TODO
        }

        /// <summary>
        /// Handle websocket requests
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public async Task Invoke(HttpContext context) {
            if (context.WebSockets.IsWebSocketRequest) {
                var webSocket = await context.WebSockets.AcceptWebSocketAsync();
                if (webSocket != null) {
                    // pass to listener to decode secure channel binary stream
                    _listener.OnAccept(context, webSocket);
                    _logger.Verbose("Accepted new websocket.");
                }
                else {
                    _logger.Debug("Accepted websocket was null.");
                }
                return;
            }
            await _next(context);
        }

        private readonly RequestDelegate _next;
        private readonly IWebSocketChannelListener _listener;
        private readonly ILogger _logger;
    }
}
