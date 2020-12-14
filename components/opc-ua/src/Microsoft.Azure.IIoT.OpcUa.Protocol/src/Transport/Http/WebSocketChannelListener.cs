// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Protocol.Transport {
    using Serilog;
    using Microsoft.AspNetCore.Http;
    using Opc.Ua;
    using Opc.Ua.Bindings;
    using System;
    using System.Collections.Concurrent;
    using System.Linq;
    using System.Net.WebSockets;
    using System.Security.Cryptography.X509Certificates;
    using System.Threading;
    using Autofac;
    using System.Threading.Tasks;

    /// <summary>
    /// Enables websocket middleware to pass sockets on to listener
    /// </summary>
    public interface IWebSocketChannelListener {

        /// <summary>
        /// Accept websocket on endpoint
        /// </summary>
        /// <param name="context"></param>
        /// <param name="webSocket"></param>
        void OnAccept(HttpContext context, WebSocket webSocket);
    }

    /// <summary>
    /// Manages the websocket connections for a UA websocket server.
    /// </summary>
    public class WebSocketChannelListener : ITcpChannelListener,
        IWebSocketChannelListener, IStartable, IDisposable {

        private const string kWssTransport =
            "http://opcfoundation.org/UA-Profile/Transport/wss-uabinary";

        /// <inheritdoc/>
        public Uri EndpointUrl => new Uri("https://localhost:443/UA");

        /// <summary>
        /// Creates the listener and starts accepting connections over
        /// tcp and tcpv6.
        /// </summary>
        /// <param name="controller"></param>
        /// <param name="config"></param>
        /// <param name="logger"></param>
        public WebSocketChannelListener(IServer controller,
            IWebListenerConfig config, ILogger logger) {
            _logger = logger ??
                throw new ArgumentNullException(nameof(logger));
            if (controller?.Callback == null) {
                throw new ArgumentNullException(nameof(controller));
            }
            _listenerId = Guid.NewGuid().ToString();
            var configuration = EndpointConfiguration.Create();
            _quotas = new ChannelQuotas {
                MaxBufferSize = configuration.MaxBufferSize,
                MaxMessageSize = configuration.MaxMessageSize,
                ChannelLifetime = configuration.ChannelLifetime,
                SecurityTokenLifetime = configuration.SecurityTokenLifetime,
                MessageContext = controller.MessageContext,
                CertificateValidator = controller.CertificateValidator
                    .GetChannelValidator()
            };
            _bufferManager = new BufferManager("Server", int.MaxValue,
                _quotas.MaxBufferSize);
            _urls = (config?.ListenUrls?.Length ?? 0) != 0 ? config.ListenUrls :
                new string[] { "http://localhost:9040" };
            _controller = controller;
        }

        /// <inheritdoc/>
        public void Start() {
            _controller.Register(GetEndpoints());
        }

        /// <inheritdoc/>
        public void Dispose() {
            foreach (TcpServerChannel channel in _channels.Values) {
                Utils.SilentDispose(channel);
            }
            _controller.Unregister(GetEndpoints());
        }

        /// <inheritdoc/>
        public bool ReconnectToExistingChannel(IMessageSocket socket, uint requestId,
            uint sequenceNumber, uint channelId, X509Certificate2 clientCertificate,
            ChannelToken token, OpenSecureChannelRequest request) {
            if (!_channels.TryGetValue(channelId, out var channel)) {
                throw ServiceResultException.Create(Opc.Ua.StatusCodes.BadTcpSecureChannelUnknown,
                    "Could not find channel referenced in the OpenSecureChannel request.");
            }
            channel.Reconnect(socket, requestId, sequenceNumber, clientCertificate,
                token, request);
            _logger.Information("Channel {channelId} reconnected", channelId);
            return true;
        }

        /// <inheritdoc/>
        public void ChannelClosed(uint channelId) {
            if (_channels.TryRemove(channelId, out var channel)) {
                Utils.SilentDispose(channel);
                _logger.Information("Channel {channelId} closed", channelId);
            }
        }

        /// <inheritdoc/>
        public void OnAccept(HttpContext context, WebSocket webSocket) {
            SecureChannel channel = null;
            // check if the accept socket has been created.
            if (webSocket != null) {
                try {
                    channel = new SecureChannel(_listenerId, this,
                        _bufferManager, _quotas, _controller.Certificate,
                        _controller.CertificateChain, GetEndpoints());
                    channel.SetRequestReceivedCallback(OnRequestReceived);

                    // Wrap socket in channel to read and write.
#pragma warning disable IDE0068 // Use recommended dispose pattern
                    var socket = new WebSocketMessageSocket(channel, webSocket,
                        _bufferManager, _quotas.MaxBufferSize, _logger);
#pragma warning restore IDE0068 // Use recommended dispose pattern
                    var channelId = (uint)Interlocked.Increment(ref _lastChannelId);
                    channel.Attach(channelId, socket);
                    if (!_channels.TryAdd(channelId, channel)) {
                        throw new InvalidProgramException("Failed to add channel");
                    }
                    channel = null;
                    _logger.Debug("Started channel {channelId} on {socket.Handle}...",
                        channelId, socket.Handle);
                }
                catch (Exception ex) {
                    _logger.Error(ex, "Unexpected error accepting a new connection.");
                }
                finally {
                    channel?.Dispose();
                }
            }
        }

        /// <summary>
        /// Handles requests arriving from a channel.
        /// </summary>
        /// <param name="channel"></param>
        /// <param name="requestId"></param>
        /// <param name="request"></param>
        private void OnRequestReceived(TcpListenerChannel channel,
            uint requestId, IServiceRequest request) {
            try {
                var result = _controller.Callback.BeginProcessRequest(
                    channel.GlobalChannelId, channel.EndpointDescription,
                    request, OnProcessRequestComplete, new object[] {
                            channel, requestId, request
                    });
            }
            catch (Exception ex) {
                _logger.Error(ex, "Unexpected error processing request.");
            }
        }

        /// <summary>
        /// Process completion
        /// </summary>
        /// <param name="result"></param>
        private void OnProcessRequestComplete(IAsyncResult result) {
            try {
                var args = (object[])result.AsyncState;
                var channel = (SecureChannel)args[0];
                var response = _controller.Callback.EndProcessRequest(result);
                channel.SendResponse((uint)args[1], response);
            }
            catch (Exception ex) {
                _logger.Error(ex, "Unexpected error sending result.");
            }
        }

        /// <summary>
        /// Get all endpoints
        /// </summary>
        /// <returns></returns>
        private EndpointDescriptionCollection GetEndpoints() {
            return new EndpointDescriptionCollection(_urls
                .Select(Utils.ParseUri)
                .Select(u => u.ChangeScheme("opc.wss"))
                .Select(u => u.ToString())
                .SelectMany(url => new EndpointDescriptionCollection {
                    new EndpointDescription {
                        EndpointUrl = url,
                        SecurityLevel = 0,
                        SecurityMode = MessageSecurityMode.None,
                        SecurityPolicyUri = SecurityPolicies.None,
                        ServerCertificate = null,
                        Server = _controller.ServerDescription,
                        UserIdentityTokens = null,
                        ProxyUrl = null,
                        TransportProfileUri = kWssTransport
                    },
                    new EndpointDescription {
                        EndpointUrl = url,
                        SecurityLevel = 1,
                        SecurityMode = MessageSecurityMode.Sign,
                        SecurityPolicyUri = SecurityPolicies.Basic256,
                        ServerCertificate = _controller.Certificate?.RawData,
                        Server = _controller.ServerDescription,
                        UserIdentityTokens = null,
                        ProxyUrl = null,
                        TransportProfileUri = kWssTransport
                    }
                }));
        }

        /// <inheritdoc/>
        public Task<bool> TransferListenerChannel(uint channelId, string serverUri, Uri endpointUrl) {
            throw new NotImplementedException();
        }

        private int _lastChannelId;
        private readonly string _listenerId;
        private readonly BufferManager _bufferManager;
        private readonly IServer _controller;
        private readonly ChannelQuotas _quotas;
        private readonly string[] _urls;
        private readonly ILogger _logger;
        private readonly ConcurrentDictionary<uint, SecureChannel> _channels =
            new ConcurrentDictionary<uint, SecureChannel>();
    }
}
