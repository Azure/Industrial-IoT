/* Copyright (c) 1996-2016, OPC Foundation. All rights reserved.
   The source code in this file is covered under a dual-license scenario:
     - RCL: for OPC Foundation members in good-standing
     - GPL V2: everybody else
   RCL license terms accompanied with this source code. See http://opcfoundation.org/License/RCL/1.00/
   GNU General Public License as published by the Free Software Foundation;
   version 2 of the License are accompanied with this source code. See http://opcfoundation.org/License/GPLv2
   This source code is distributed in the hope that it will be useful,
   but WITHOUT ANY WARRANTY; without even the implied warranty of
   MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.
*/

namespace Opc.Ua.Transport {
    using Opc.Ua.Bindings;
    using Microsoft.Azure.IIoT.Diagnostics;
    using System;
    using System.Collections.Concurrent;
    using System.Net;
    using System.Net.Sockets;
    using System.Security.Cryptography.X509Certificates;
    using System.Threading;
    // TODO: Remove when changes are in stack
    using TcpServerChannel = Bindings.Fork.TcpServerChannel;
    using ITcpChannelListener = Bindings.Fork.ITcpServerChannelListener;
    // END TODO

    /// <summary>
    /// Manages the connections for a UA TCP server.
    /// </summary>
    public class TcpChannelListener : ITcpChannelListener, IDisposable {

        /// <inheritdoc/>
        public Uri EndpointUrl { get; set; }

        /// <summary>
        /// Creates the listener and starts accepting connections over tcp and
        /// tcpv6.
        /// </summary>
        /// <param name="settings">The settings to use when creating the
        /// listener.</param>
        /// <param name="callback">The callback to use when requests arrive via
        /// the channel.</param>
        /// <param name="logger"></param>
        /// <param name="port"></param>
        public TcpChannelListener(TransportListenerSettings settings,
            ITransportListenerCallback callback, ILogger logger,
            int port = Utils.UaTcpDefaultPort) {

            // assign a unique guid to the listener.
            _listenerId = Guid.NewGuid().ToString();
            _descriptions = settings.Descriptions;
            _logger = logger;

            var configuration = settings.Configuration;
            _quotas = new ChannelQuotas {
                MaxBufferSize = configuration.MaxBufferSize,
                MaxMessageSize = configuration.MaxMessageSize,
                ChannelLifetime = configuration.ChannelLifetime,
                SecurityTokenLifetime = configuration.SecurityTokenLifetime,
                MessageContext = new ServiceMessageContext {
                    MaxArrayLength = configuration.MaxArrayLength,
                    MaxByteStringLength = configuration.MaxByteStringLength,
                    MaxMessageSize = configuration.MaxMessageSize,
                    MaxStringLength = configuration.MaxStringLength,
                    NamespaceUris = settings.NamespaceUris,
                    ServerUris = new StringTable(),
                    Factory = settings.Factory
                },
                CertificateValidator = settings.CertificateValidator
            };

            // save the server certificate.
            _serverCertificate = settings.ServerCertificate;
            _serverCertificateChain = settings.ServerCertificateChain;
            _bufferManager = new BufferManager("Server", int.MaxValue,
                _quotas.MaxBufferSize);

            // save the callback to the server.
            _callback = callback;

            // Create and bind sockets for listening.
            _listeningSocket = BindSocket(IPAddress.Any, port);
            _listeningSocketIPv6 = BindSocket(IPAddress.IPv6Any, port);
            if (_listeningSocketIPv6 == null && _listeningSocket == null) {
                throw ServiceResultException.Create(StatusCodes.BadNoCommunication,
                    "Failed to bind sockets for both Ipv4 and IPv6.");
            }
        }

        /// <inheritdoc/>
        public void Dispose() {
            _listeningSocket.SafeDispose();
            _listeningSocketIPv6.SafeDispose();
            foreach (TcpServerChannel channel in _channels.Values) {
                Utils.SilentDispose(channel);
            }
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
                        var socket = new TcpMessageSocket(channel, e.AcceptSocket,
                            _bufferManager, _quotas.MaxBufferSize);
                        var channelId = (uint)Interlocked.Increment(ref _lastChannelId);

                        channel = new SecureChannel(_listenerId, this,
                            _bufferManager, _quotas, _serverCertificate,
                            _serverCertificateChain, _descriptions);
                        channel.SetRequestReceivedCallback(OnRequestReceived);
                        channel.Attach(channelId, socket);

                        _channels.TryAdd(channelId, channel);
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
                if (_callback != null) {
                    var result = _callback.BeginProcessRequest(
                        channel.GlobalChannelId, channel.EndpointDescription,
                        request, OnProcessRequestComplete, new object[] {
                            channel, requestId, request
                        });
                }
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
                if (_callback != null) {
                    var channel = (SecureChannel)args[0];
                    var response = _callback.EndProcessRequest(result);
                    channel.SendResponse((uint)args[1], response);
                }
            }
            catch (Exception e) {
                _logger.Error("Unexpected error sending result.", e);
            }
        }

        private int _lastChannelId;
        private readonly string _listenerId;
        private readonly BufferManager _bufferManager;
        private readonly ChannelQuotas _quotas;
        private readonly X509Certificate2 _serverCertificate;
        private readonly X509Certificate2Collection _serverCertificateChain;
        private readonly Socket _listeningSocket;
        private readonly Socket _listeningSocketIPv6;
        private readonly ITransportListenerCallback _callback;
        private readonly EndpointDescriptionCollection _descriptions;
        private readonly ILogger _logger;
        private readonly ConcurrentDictionary<uint, SecureChannel> _channels =
            new ConcurrentDictionary<uint, SecureChannel>();
    }
}
