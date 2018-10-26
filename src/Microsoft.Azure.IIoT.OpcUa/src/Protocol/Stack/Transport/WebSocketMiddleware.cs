// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Opc.Ua.Transport {
    using Opc.Ua;
    using Opc.Ua.Bindings;
    using Microsoft.AspNetCore.Http;
    using System;
    using System.Linq;
    using System.Net.WebSockets;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Security.Cryptography.X509Certificates;

    // TODO: Remove when changes are in stack
    using TcpServerChannel = Bindings.Fork.TcpServerChannel;
    using ITcpServerChannelListener = Bindings.Fork.ITcpServerChannelListener;
    // END TODO

    /// <summary>
    /// Secure channel over websocket middleware
    /// </summary>
    public class WebSocketMiddleware : ITcpServerChannelListener {

        /// <inheritdoc/>
        public Uri EndpointUrl { get; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="next"></param>
        /// <param name="encoder"></param>
        /// <param name="callback"></param>
        public WebSocketMiddleware(RequestDelegate next, IMessageSerializer encoder,
            ITransportListenerCallback callback) {
            _next = next;
            _encoder = encoder ?? throw new ArgumentNullException(nameof(encoder));
            _callback = callback ?? throw new ArgumentNullException(nameof(callback));

            EndpointUrl = null; // TODO
        }

        /// <inheritdoc/>
        public bool ReconnectToExistingChannel(IMessageSocket socket, uint requestId,
            uint sequenceNumber, uint channelId, X509Certificate2 clientCertificate,
            ChannelToken token, OpenSecureChannelRequest request) {
            return false;
        }

        /// <inheritdoc/>
        public void ChannelClosed(uint channelId) {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Handle all websocket requests
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public async Task Invoke(HttpContext context) {
            var handled = await AcceptAsync(context).ConfigureAwait(false);
            if (!handled) {
                await _next(context).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Middleware invoke entry point which forwards to controller
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public async Task<bool> AcceptAsync(HttpContext context) {
            if (!context.WebSockets.IsWebSocketRequest) {
                return false;
            }

            // Check OPCUA-SecurityPolicy header
            var policy = SecurityPolicies.None;
            if (context.Request.Headers.Keys.Contains("OPCUA-SecurityPolicy")) {
                // Select security policy contained in header, or reject
                policy = context.Request.Headers["OPCUA-SecurityPolicy"].FirstOrDefault();
            }

            if (!context.Request.IsHttps && policy == SecurityPolicies.None) {
                return false;
            }

            var webSocket = await context.WebSockets.AcceptWebSocketAsync()
                .ConfigureAwait(false);
            if (webSocket.SubProtocol == "opcua+uajson") {
                // read json messages from fragements
                var t = ProcessJsonStream(webSocket);
            }
            else {
                // read binary messages from fragments
                var t = ProcessSecureChannel(webSocket);
            }
            return true;
        }

        /// <summary>
        /// Process json messages
        /// </summary>
        /// <param name="webSocket"></param>
        /// <returns></returns>
        private async Task ProcessJsonStream(WebSocket webSocket) {
            try {
                while (true) {

                    // var result = webSocket.ReceiveAsync();
                    await Task.Delay(1).ConfigureAwait(false);
                }
            }
            catch (WebSocketException) {
                await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure,
                    "Websocket closed.", CancellationToken.None).ConfigureAwait(false);
            }
            catch (Exception e) {
                await webSocket.CloseAsync(WebSocketCloseStatus.InternalServerError,
                    e.Message, CancellationToken.None).ConfigureAwait(false);
            }
            finally {
                webSocket.Dispose();
            }
        }

        /// <summary>
        /// Process SecureChannel messages
        /// </summary>
        /// <param name="webSocket"></param>
        /// <returns></returns>
        private async Task ProcessSecureChannel(WebSocket webSocket) {
            try {
                while (true) {

                    // var result = webSocket.ReceiveAsync();
                    await Task.Delay(1).ConfigureAwait(false);
                }
            }
            catch (WebSocketException) {
                await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure,
                    "Websocket closed.", CancellationToken.None).ConfigureAwait(false);
            }
            catch (Exception e) {
                await webSocket.CloseAsync(WebSocketCloseStatus.InternalServerError,
                    e.Message, CancellationToken.None).ConfigureAwait(false);
            }
            finally {
                webSocket.Dispose();
            }
        }

        private readonly RequestDelegate _next;
        private readonly ITransportListenerCallback _callback;
        private readonly IMessageSerializer _encoder;
    }
}
