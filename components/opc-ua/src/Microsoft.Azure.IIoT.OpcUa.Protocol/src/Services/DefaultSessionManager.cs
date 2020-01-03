// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Protocol.Services {
    using Microsoft.Azure.IIoT.OpcUa.Protocol.Models;
    using Microsoft.Azure.IIoT.OpcUa.Publisher.Models;
    using Microsoft.Azure.IIoT.OpcUa.Core.Models;
    using Microsoft.Azure.IIoT.Utils;
    using Opc.Ua;
    using Opc.Ua.Client;
    using Opc.Ua.Client.ComplexTypes;
    using Serilog;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Session manager
    /// </summary>
    public class DefaultSessionManager : ISessionManager {

        /// <inheritdoc/>
        public int SessionCount => _sessions.Count;

        /// <summary>
        /// Create session manager
        /// </summary>
        /// <param name="clientConfig"></param>
        /// <param name="logger"></param>
        public DefaultSessionManager(IClientServicesConfig clientConfig, ILogger logger) {
            _logger = logger;
            _clientConfig = clientConfig;
            _lock = new SemaphoreSlim(1, 1);
        }

        /// <inheritdoc/>
        public async Task<Session> GetOrCreateSessionAsync(ConnectionModel connection,
            bool createIfNotExists) {

            // Find session and if not exists create
            var id = new ConnectionIdentifier(connection);
            await _lock.WaitAsync();
            try {
                if (!_sessions.TryGetValue(id, out var wrapper) && createIfNotExists) {
                    var sessionName = id.ToString();
                    var applicationConfiguration = _clientConfig.ToApplicationConfiguration(true);
                    var endpointConfiguration = _clientConfig.ToEndpointConfiguration();
                    var endpointDescription = new EndpointDescription(id.Connection.Endpoint.Url);
                    var configuredEndpoint = new ConfiguredEndpoint(
                        null, endpointDescription, endpointConfiguration);

                    _logger.Information("Trying to create session {sessionName}...",
                        sessionName);
                    using (new PerfMarker(_logger, sessionName)) {
                        var userIdentity = connection.User.ToStackModel() ??
                            new UserIdentity(new AnonymousIdentityToken());
                        var session = await Session.Create(
                            applicationConfiguration, configuredEndpoint,
                            true, sessionName, _clientConfig.DefaultSessionTimeout,
                            userIdentity, null);

                        _logger.Information($"Session '{sessionName}' created.");

                        var complexTypeSystem = new ComplexTypeSystem(session);
                        await complexTypeSystem.Load();

                        if (_clientConfig.KeepAliveInterval > 0) {
                            session.KeepAliveInterval = _clientConfig.KeepAliveInterval;
                            session.KeepAlive += Session_KeepAlive;
                        }
                        wrapper = new SessionWrapper {
                            MissedKeepAlives = 0,
                            MaxKeepAlives = _clientConfig.MaxKeepAliveCount,
                            Session = session
                        };
                        _sessions.Add(id, wrapper);
                    }
                }
                return wrapper?.Session;
            }
            finally {
                _lock.Release();
            }
        }

        /// <inheritdoc/>
        public async Task RemoveSessionAsync(ConnectionModel connection, bool onlyIfEmpty = true) {
            var key = new ConnectionIdentifier(connection);
            await _lock.WaitAsync();
            try {
                if (!_sessions.TryGetValue(key, out var wrapper) || wrapper?.Session == null) {
                    return;
                }
                var session = wrapper.Session;
                if (onlyIfEmpty && session.SubscriptionCount > 0) {
                    return;
                }
                // Remove subscriptions
                if (session.SubscriptionCount > 0) {
                    if (onlyIfEmpty) {
                        return;
                    }
                    Try.Op(() => session.RemoveSubscriptions(session.Subscriptions));
                }
                _sessions.Remove(key);
                Try.Op(session.Close);
                Try.Op(session.Dispose);
            }
            finally {
                _lock.Release();
            }
        }

        /// <summary>
        /// Handle keep alives
        /// </summary>
        /// <param name="session"></param>
        /// <param name="e"></param>
        private void Session_KeepAlive(Session session, KeepAliveEventArgs e) {
            _logger.Debug($"Keep Alive received from session {session.SessionName}, state: {e.CurrentState}.");
            if (ServiceResult.IsGood(e.Status)) {
                return;
            }
            _lock.Wait();
            try {
                var entry = _sessions.SingleOrDefault(s => s.Value.Session.SessionName == session.SessionName);
                if (entry.Key == null) {
                    return;
                }
                entry.Value.MissedKeepAlives++;
                if (entry.Value.MissedKeepAlives >= entry.Value.MaxKeepAlives) {
                    _logger.Warning("Session '{name}' exceeded max keep alive count. Disconnecting and removing session...",
                        session.SessionName);
                    _sessions.Remove(entry.Key);
                    Try.Op(session.Close);
                    Try.Op(session.Dispose);
                }
                _logger.Warning("{missedKeepAlives}/{_maxKeepAlives} missed keep alives from session '{name}'...",
                    entry.Value.MissedKeepAlives, entry.Value.MaxKeepAlives, session.SessionName);
            }
            finally {
                _lock.Release();
            }
        }

        /// <summary>
        /// Wraps a session and keep alive information
        /// </summary>
        private sealed class SessionWrapper {

            /// <summary>
            /// Session
            /// </summary>
            public Session Session { get; set; }

            /// <summary>
            /// Missed keep alives
            /// </summary>
            public int MissedKeepAlives { get; set; }

            /// <summary>
            /// Missed keep alives
            /// </summary>
            public uint MaxKeepAlives { get; set; }
        }

        /// <summary>
        /// Get endpoint from session
        /// </summary>
        /// <param name="session"></param>
        /// <returns></returns>
        public static EndpointIdentifier GetEndpointId(SessionClient session) {
            var endpointModel = new EndpointModel {
                Url = session.Endpoint.EndpointUrl.TrimEnd('/'),
                SecurityPolicy = session.Endpoint.SecurityPolicyUri
            };
            switch (session.Endpoint.SecurityMode) {
                case MessageSecurityMode.Invalid:
                    throw new Exception("Invalid security mode: invalid");
                case MessageSecurityMode.None:
                    endpointModel.SecurityMode = SecurityMode.None;
                    break;
                case MessageSecurityMode.Sign:
                    endpointModel.SecurityMode = SecurityMode.Sign;
                    break;
                case MessageSecurityMode.SignAndEncrypt:
                    endpointModel.SecurityMode = SecurityMode.SignAndEncrypt;
                    break;
            }
            return new EndpointIdentifier(endpointModel);
        }

        private readonly ILogger _logger;
        private readonly IClientServicesConfig _clientConfig;
        private readonly Dictionary<ConnectionIdentifier, SessionWrapper> _sessions =
            new Dictionary<ConnectionIdentifier, SessionWrapper>();
        private readonly SemaphoreSlim _lock;
    }
}