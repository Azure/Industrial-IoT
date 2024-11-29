// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation, The OPC Foundation, Inc.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Opc.Ua.Client
{
    using Microsoft.Extensions.Logging;
    using System;
    using System.Threading;
    using Opc.Ua.Bindings;

    /// <summary>
    /// Channel factory
    /// </summary>
    public sealed class ChannelFactory
    {
        /// <summary>
        /// Create channel manager
        /// </summary>
        /// <param name="observability"></param>
        public ChannelFactory(IObservability observability)
        {

        }

        /// <summary>
        /// Update session diagnostics
        /// </summary>
        /// <param name="session"></param>
        private void UpdateConnectionDiagnosticFromSession(OpcUaSession session)
        {
            // Called under lock

            var channel = session.TransportChannel;
            var token = channel?.CurrentToken;

            var now = _observability.TimeProvider.GetUtcNow();

            var lastDiagnostics = LastDiagnostics;
            var elapsed = now - lastDiagnostics.TimeStamp;

            var channelChanged = false;
            if (token != null)
            {
                //
                // Monitor channel's token lifetime and update diagnostics
                // Check wether the token or channel changed. If so set a
                // timer to monitor the new token lifetime, if not then
                // try again after the remaining lifetime or every second
                // until it changed unless the token is then later gone.
                //
                channelChanged = !(lastDiagnostics != null &&
                    lastDiagnostics.ChannelId == token.ChannelId &&
                    lastDiagnostics.TokenId == token.TokenId &&
                    lastDiagnostics.CreatedAt == token.CreatedAt);

                var lifetime = TimeSpan.FromMilliseconds(Math.Min(token.Lifetime,
                    _configuration.TransportQuotas.SecurityTokenLifetime));
                if (channelChanged)
                {
                    _channelMonitor.Change(lifetime, Timeout.InfiniteTimeSpan);
                    _logger.LogInformation(
                        "Channel {Channel} got new token {TokenId} ({Created}).",
                        token.ChannelId, token.TokenId, token.CreatedAt);
                }
                else
                {
                    //
                    // Token has not yet been updated, let's retry later
                    // It is also assumed that the port/ip are still the same
                    //
                    if (lifetime > elapsed)
                    {
                        _channelMonitor.Change(lifetime - elapsed,
                            Timeout.InfiniteTimeSpan);
                    }
                    else
                    {
                        _channelMonitor.Change(TimeSpan.FromSeconds(1),
                            Timeout.InfiniteTimeSpan);
                    }
                }
            }

            var sessionId = session.SessionId?.AsString(session.MessageContext,
                NamespaceFormat.Index);

            // Get effective ip address and port
            var socket = (channel as UaSCUaBinaryTransportChannel)?.Socket;
            var remoteIpAddress = socket?.RemoteEndpoint?.GetIPAddress()?.ToString();
            var remotePort = socket?.RemoteEndpoint?.GetPort();
            var localIpAddress = socket?.LocalEndpoint?.GetIPAddress()?.ToString();
            var localPort = socket?.LocalEndpoint?.GetPort();

            if (LastDiagnostics.SessionCreated == session.CreatedAt &&
                LastDiagnostics.SessionId == sessionId &&
                LastDiagnostics.RemoteIpAddress == remoteIpAddress &&
                LastDiagnostics.RemotePort == remotePort &&
                LastDiagnostics.LocalIpAddress == localIpAddress &&
                LastDiagnostics.LocalPort == localPort &&
                !channelChanged)
            {
                return;
            }

            LastDiagnostics = new ChannelDiagnosticModel
            {
                Connection = _connection,
                TimeStamp = now,
                SessionCreated = session.CreatedAt,
                SessionId = sessionId,
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
            };
            _diagnosticsCb(LastDiagnostics);

            _logger.LogInformation("Channel diagnostics for session {SessionId} updated.",
                sessionId);

            static ChannelKeyModel? ToChannelKey(byte[]? iv, byte[]? key, byte[]? sk)
            {
                if (iv == null || key == null || sk == null ||
                    iv.Length == 0 || key.Length == 0 || sk.Length == 0)
                {
                    return null;
                }
                return new ChannelKeyModel
                {
                    Iv = iv,
                    Key = key,
                    SigLen = sk.Length
                };
            }
        }

    }
}
