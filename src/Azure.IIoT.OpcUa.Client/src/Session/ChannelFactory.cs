// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation, The OPC Foundation, Inc.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Opc.Ua.Client
{
    using System;
    using Opc.Ua.Bindings;
    using System.Net.Sockets;
    using System.Net;
    using System.Security.Cryptography.X509Certificates;

    /// <summary>
    /// Channel factory
    /// </summary>
    internal sealed class ChannelFactory : IChannelFactory
    {
        /// <summary>
        /// Callback to register channel diagnostics
        /// </summary>
        public event Action<ITransportChannel, ChannelDiagnostic>? OnDiagnostics;

        /// <summary>
        /// Create channel factory
        /// </summary>
        /// <param name="configuration"></param>
        /// <param name="observability"></param>
        public ChannelFactory(ApplicationConfiguration configuration,
            IObservability observability)
        {
            _observability = observability;
            _configuration = configuration;
        }

        /// <inheritdoc/>
        public ITransportChannel CreateChannel(ConfiguredEndpoint endpoint,
            IServiceMessageContext context, X509Certificate2? clientCertificate,
            X509Certificate2Collection? clientCertificateChain,
            ITransportWaitingConnection? connection = null)
        {
            // initialize the channel which will be created with the server.
            ITransportChannel channel;
            if (connection != null)
            {
                channel = SessionChannel.CreateUaBinaryChannel(_configuration,
                    connection, endpoint.Description, endpoint.Configuration,
                    clientCertificate, clientCertificateChain, context);
            }
            else
            {
                channel = SessionChannel.CreateUaBinaryChannel(_configuration,
                    endpoint.Description, endpoint.Configuration, clientCertificate,
                    clientCertificateChain, context);
            }
            //   channel.OnTokenActivated += OnChannelTokenActivated;
            return channel;
        }

        public void CloseChannel(ITransportChannel channel)
        {
            //   channel.OnTokenActivated -= OnChannelTokenActivated;
            channel.Dispose();
        }

        /// <summary>
        /// Called when the token is changing
        /// </summary>
        /// <param name="token"></param>
        /// <param name="channel"></param>
        /// <param name="previousToken"></param>
        internal void OnChannelTokenActivated(ITransportChannel channel,
            ChannelToken? token, ChannelToken? previousToken)
        {
            if (token == null || OnDiagnostics == null)
            {
                // Closed
                return;
            }

            if (previousToken == null)
            {
                // Created
            }

            // Get effective ip address and port
            var socket = (channel as UaSCUaBinaryTransportChannel)?.Socket;
            var remoteIpAddress = GetIPAddress(socket?.RemoteEndpoint);
            var remotePort = GetPort(socket?.RemoteEndpoint);
            var localIpAddress = GetIPAddress(socket?.LocalEndpoint);
            var localPort = GetPort(socket?.LocalEndpoint);

            OnDiagnostics?.Invoke(channel, new ChannelDiagnostic
            {
                Endpoint = channel.EndpointDescription,
                TimeStamp = _observability.TimeProvider.GetUtcNow(),
                RemoteIpAddress = remoteIpAddress,
                RemotePort = remotePort == -1 ? null : remotePort,
                LocalIpAddress = localIpAddress,
                LocalPort = localPort == -1 ? null : localPort,
                ChannelId = token?.ChannelId,
                TokenId = token?.TokenId,
                CreatedAt = token?.CreatedAt,
                Lifetime = token == null ? null :
                    TimeSpan.FromMilliseconds(token.Lifetime),
                Client = ToChannelKey(token?.ClientInitializationVector,
                    token?.ClientEncryptingKey, token?.ClientSigningKey),
                Server = ToChannelKey(token?.ServerInitializationVector,
                    token?.ServerEncryptingKey, token?.ServerSigningKey)
            });

            static ChannelKey? ToChannelKey(byte[]? iv, byte[]? key, byte[]? sk)
            {
                if (iv == null || key == null || sk == null ||
                    iv.Length == 0 || key.Length == 0 || sk.Length == 0)
                {
                    return null;
                }
                return new ChannelKey(iv, key, sk.Length);
            }
        }

        /// <summary>
        /// Get ip address from endpoint if the endpoint is an
        /// IPEndPoint.  Otherwise return null.
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="preferv4"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        private static IPAddress? GetIPAddress(EndPoint? endpoint, bool preferv4 = false)
        {
            if (endpoint is not IPEndPoint ipe)
            {
                return null;
            }
            var address = ipe.Address;
            if (preferv4 &&
                address.AddressFamily == AddressFamily.InterNetworkV6 &&
                address.IsIPv4MappedToIPv6)
            {
                return address.MapToIPv4();
            }
            return address;
        }

        /// <summary>
        /// Get port from endpoint if the endpoint is an
        /// IPEndPoint.  Otherwise return -1.
        /// </summary>
        /// <param name="endpoint"></param>
        /// <returns></returns>
        private static int GetPort(EndPoint? endpoint)
        {
            if (endpoint is IPEndPoint ipe)
            {
                return ipe.Port;
            }
            return -1;
        }

        private readonly IObservability _observability;
        private readonly ApplicationConfiguration _configuration;
    }
}
