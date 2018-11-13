// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Protocol.Transport {
    using Microsoft.Azure.IIoT.Diagnostics;
    using Opc.Ua.Bindings;
    using Opc.Ua;
    using System;
    using System.Collections.Concurrent;
    using System.Net;
    using System.Net.Sockets;
    using System.Security.Cryptography.X509Certificates;
    using System.Threading;
    using Autofac;

    /// <summary>
    /// Manages the raw tcp connections for a UA TCP server.
    /// </summary>
    public class TcpChannelListener : ITcpChannelListener, IStartable, IDisposable {

        /// <inheritdoc/>
        public Uri EndpointUrl { get; }

        /// <summary>
        /// Creates the listener and starts accepting connections over
        /// tcp and tcpv6.
        /// </summary>
        /// <param name="config"></param>
        /// <param name="controller"></param>
        /// <param name="logger"></param>
        public TcpChannelListener(IServer controller, ITcpListenerConfig config,
            ILogger logger) {
            _logger = logger ??
                throw new ArgumentNullException(nameof(logger));
            _callback = controller?.Callback??
                throw new ArgumentNullException(nameof(controller));
            if (config == null) {
                throw new ArgumentNullException(nameof(config));
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

            _serverCertificate = config.TcpListenerCertificate;
            _serverCertificateChain = config.TcpListenerCertificateChain;
            _bufferManager = new BufferManager("Server", int.MaxValue,
                _quotas.MaxBufferSize);
            _controller = controller;

            // Create and bind sockets for listening.
            var port = config.Port == 0 ? Utils.UaTcpDefaultPort : config.Port;
            _listeningSocket = BindSocket(IPAddress.Any, port);
            _listeningSocketIPv6 = BindSocket(IPAddress.IPv6Any, port);
            if (_listeningSocketIPv6 == null && _listeningSocket == null) {
                throw ServiceResultException.Create(StatusCodes.BadNoCommunication,
                    "Failed to bind sockets for both Ipv4 and IPv6.");
            }

            var host = config.PublicDnsAddress ?? Utils.GetHostName();
            EndpointUrl = new Uri($"opc.tcp://{host}:{port}");
        }

        /// <inheritdoc/>
        public void Start() {
            _controller.Register(GetEndpoints());
        }

        /// <inheritdoc/>
        public void Dispose() {
            _listeningSocket.SafeDispose();
            _listeningSocketIPv6.SafeDispose();
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
                throw ServiceResultException.Create(StatusCodes.BadTcpSecureChannelUnknown,
                    "Could not find channel referenced in the OpenSecureChannel request.");
            }
            channel.Reconnect(socket, requestId, sequenceNumber, clientCertificate,
                token, request);
            _logger.Info($"Channel {channelId} reconnected");
            return true;
        }

        /// <inheritdoc/>
        public void ChannelClosed(uint channelId) {
            if (_channels.TryRemove(channelId, out var channel)) {
                Utils.SilentDispose(channel);
                _logger.Info($"Channel {channelId} closed");
            }
        }

        /// <summary>
        /// Binds and listens for connections on the specified port.
        /// </summary>
        /// <param name="address"></param>
        /// <param name="port"></param>
        /// <returns></returns>
        private Socket BindSocket(IPAddress address, int port) {
            try {
                var endpoint = new IPEndPoint(address, port);
                var socket = new Socket(endpoint.AddressFamily,
                    SocketType.Stream, ProtocolType.Tcp);
                var args = new SocketAsyncEventArgs();
                args.Completed += OnAccept;
                args.UserToken = socket;
                socket.Bind(endpoint);
                socket.Listen(int.MaxValue);
                if (!socket.AcceptAsync(args)) {
                    OnAccept(null, args);
                }
                return socket;
            }
            catch (Exception ex) {
                _logger.Warn("failed to create listening socket.", ex);
                return null;
            }
        }

        /// <summary>
        /// Handles a new connection.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnAccept(object sender, SocketAsyncEventArgs e) {
            SecureChannel channel = null;
            while (true) {
                var error = e.SocketError;
                // check if the accept socket has been created.
                if (e.AcceptSocket != null && e.SocketError == SocketError.Success) {
                    try {
                        // Wrap socket in channel to read and write.
                        channel = new SecureChannel(_listenerId, this,
                            _bufferManager, _quotas,
                            _serverCertificate ?? _controller.Certificate,
                            _serverCertificateChain ?? _controller.CertificateChain,
                            GetEndpoints());
                        channel.SetRequestReceivedCallback(OnRequestReceived);

                        var channelId = (uint)Interlocked.Increment(ref _lastChannelId);
                        var socket = new TcpMessageSocket(channel, e.AcceptSocket,
                            _bufferManager, _quotas.MaxBufferSize);
                        channel.Attach(channelId, socket);

                        _channels.TryAdd(channelId, channel);
                        _logger.Debug($"Started channel {channelId} on {socket.Handle}...");
                    }
                    catch (Exception ex) {
                        _logger.Error("Unexpected error accepting a new connection.", ex);
                    }
                }
                var listeningSocket = e.UserToken as Socket;
                e.Dispose();

                if (error != SocketError.OperationAborted && listeningSocket != null) {
                    // Schedule new accept
                    try {
                        e = new SocketAsyncEventArgs(); // Should cache it
                        e.Completed += OnAccept;
                        e.UserToken = listeningSocket;
                        if (!listeningSocket.AcceptAsync(e)) {
                            continue; // Handle synchronously
                        }
                    }
                    catch (Exception ex) {
                        _logger.Error("Unexpected error listening for a connections.", ex);
                        // Stop listening
                    }
                }
                break;
            }
        }

        /// <summary>
        /// Handles requests arriving from a channel.
        /// </summary>
        /// <param name="channel"></param>
        /// <param name="requestId"></param>
        /// <param name="request"></param>
        private void OnRequestReceived(TcpServerChannel channel,
            uint requestId, IServiceRequest request) {
            try {
                var result = _controller.Callback.BeginProcessRequest(
                    channel.GlobalChannelId, channel.EndpointDescription,
                    request, OnProcessRequestComplete, new object[] {
                            channel, requestId, request
                    });
            }
            catch (Exception e) {
                _logger.Error("Unexpected error processing request.", e);
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
            catch (Exception e) {
                _logger.Error("Unexpected error sending result.", e);
            }
        }

        /// <summary>
        /// Get all endpoints
        /// </summary>
        /// <returns></returns>
        private EndpointDescriptionCollection GetEndpoints() {
            return new EndpointDescriptionCollection {
                new EndpointDescription {
                    EndpointUrl = EndpointUrl.ToString(),
                    SecurityLevel = 255,
                    SecurityMode = MessageSecurityMode.None,
                    SecurityPolicyUri = SecurityPolicies.None,
                    ServerCertificate = null,
                    Server = _controller.ServerDescription,
                    UserIdentityTokens = null,
                    ProxyUrl = null,
                    TransportProfileUri = Profiles.UaTcpTransport
                },
                new EndpointDescription {
                    EndpointUrl = EndpointUrl.ToString(),
                    SecurityLevel = 1,
                    SecurityMode = MessageSecurityMode.SignAndEncrypt,
                    SecurityPolicyUri = SecurityPolicies.Basic256Sha256,
                    ServerCertificate = _serverCertificate?.RawData ??
                        _controller.Certificate?.RawData,
                    Server = _controller.ServerDescription,
                    UserIdentityTokens = null,
                    ProxyUrl = null,
                    TransportProfileUri = Profiles.UaTcpTransport
                }
            };
        }

        private int _lastChannelId;
        private readonly IServer _controller;
        private readonly string _listenerId;
        private readonly BufferManager _bufferManager;
        private readonly ChannelQuotas _quotas;
        private readonly X509Certificate2 _serverCertificate;
        private readonly X509Certificate2Collection _serverCertificateChain;
        private readonly Socket _listeningSocket;
        private readonly Socket _listeningSocketIPv6;
        private readonly ITransportListenerCallback _callback;
        private readonly ILogger _logger;
        private readonly ConcurrentDictionary<uint, SecureChannel> _channels =
            new ConcurrentDictionary<uint, SecureChannel>();
    }
}
