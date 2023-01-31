﻿// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Protocol.Services {
    using Microsoft.Azure.IIoT.Module;
    using Microsoft.Azure.IIoT.OpcUa.Core.Models;
    using Microsoft.Azure.IIoT.OpcUa.Protocol.Models;
    using Microsoft.Azure.IIoT.Utils;
    using Opc.Ua;
    using Opc.Ua.Client;
    using Opc.Ua.Client.ComplexTypes;
    using Serilog;
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Session manager
    /// </summary>
    public class OpcUaClientManager : ISessionManager, ISubscriptionManager,
        IDisposable {

        /// <inheritdoc/>
        public int SessionCount => _clients.Count;

        /// <summary>
        /// Create client manager
        /// </summary>
        /// <param name="clientConfig"></param>
        /// <param name="identity"></param>
        /// <param name="codec"></param>
        /// <param name="logger"></param>
        public OpcUaClientManager(IClientServicesConfig clientConfig,
            IIdentity identity, IVariantEncoderFactory codec, ILogger logger) {
            _clientConfig = clientConfig ??
                throw new ArgumentNullException(nameof(clientConfig));
            _codec = codec ??
                throw new ArgumentNullException(nameof(codec));
            _logger = logger ??
                throw new ArgumentNullException(nameof(logger));
            _identity = identity ??
                throw new ArgumentNullException(nameof(identity));
            _configuration = _clientConfig.BuildApplicationConfigurationAsync(
                _identity, OnValidate, _logger).GetAwaiter().GetResult();
            _lock = new SemaphoreSlim(1, 1);
            _cts = new CancellationTokenSource();
            _processor = Task.Factory.StartNew(() => RunClientManagerAsync(
                TimeSpan.FromSeconds(5), _cts.Token), _cts.Token,
                    TaskCreationOptions.LongRunning, TaskScheduler.Default).Unwrap();
        }

        /// <inheritdoc/>
        public int GetNumberOfConnectionRetries(ConnectionModel connection) {
            var key = new ConnectionIdentifier(connection);
            _lock.Wait();
            try {
                if (!_clients.TryGetValue(key, out var client)) {
                    return 0;
                }
                return client.NumberOfConnectRetries;
            }
            finally {
                _lock.Release();
            }
        }

        /// <inheritdoc/>
        public bool IsConnectionOk(ConnectionModel connection) {
            var key = new ConnectionIdentifier(connection);
            _lock.Wait();
            try {
                if (!_clients.TryGetValue(key, out var client)) {
                    return false;
                }
                return client.Session.Connected;
            }
            finally {
                _lock.Release();
            }
        }

        /// <inheritdoc/>
        public Task<ISubscription> CreateSubscriptionAsync(SubscriptionModel subscriptionModel) {
            return OpcUaSubscription.CreateAsync(this, _clientConfig, _codec,
                subscriptionModel, _logger);
        }

        /// <inheritdoc/>
        public async Task<ISessionHandle> GetOrCreateSessionAsync(ConnectionModel connection) {
            // Find session and if not exists create
            var id = new ConnectionIdentifier(connection);
            _lock.Wait();
            try {
                // try to get an existing session
                if (!_clients.TryGetValue(id, out var client)) {
                    client = CreateClient(id);
                    _clients.AddOrUpdate(id, client);
                    _logger.Information(
                        "New session {Id} added, current number of sessions is {count}.",
                        id, _clients.Count);
                }
                // Try and connect the session
                try {
                    await client.ConnectAsync();
                }
                catch (Exception ex) {
                    _logger.Error(ex, "Failed to connect session {id}. " +
                        "Continue with unconnected session.", id);
                }
                return client;
            }
            finally {
                _lock.Release();
            }
        }

        /// <inheritdoc/>
        public ISessionHandle FindSession(ConnectionModel connection) {
            // Find session and if not exists create
            var id = new ConnectionIdentifier(connection);
            _lock.Wait();
            try {
                // try to get an existing session
                if (!_clients.TryGetValue(id, out var client)) {
                    return null;
                }
                return client;
            }
            finally {
                _lock.Release();
            }
        }

        /// <inheritdoc/>
        public ComplexTypeSystem GetComplexTypeSystem(ISession session) {
            return (session?.Handle as OpcUaClient)?.ComplexTypeSystem;
        }

        /// <inheritdoc/>
        public async Task DisconnectSessionAsync(ConnectionModel connection,
            bool onlyIfEmpty = true) {
            var key = new ConnectionIdentifier(connection);
            await _lock.WaitAsync().ConfigureAwait(false);
            try {
                if (!_clients.TryGetValue(key, out var client)) {
                    return;
                }
                if (onlyIfEmpty && !client.HasSubscriptions) {
                    _clients.Remove(key, out _);
                    client.Dispose();
                    _logger.Information(
                        "Session removed, current number of sessions is {count}.",
                        _clients.Count);
                }
            }
            finally {
                _lock.Release();
            }
        }

        /// <summary>
        /// Dispose
        /// </summary>
        public void Dispose() {
            try {
                _cts.Cancel();
                _processor.Wait();
            }
            finally {
                _logger.Information("Session manager process stopped.");
                _cts.Dispose();
            }

            _logger.Information("Stopping all sessions");
            _lock.Wait();
            try {
                foreach (var client in _clients.ToList()) {
                    try {
                        client.Value.Dispose();
                    }
                    catch (OperationCanceledException) { }
                    catch (Exception ex) {
                        _logger.Error(ex, "Unexpected exception disposing session {id}",
                            client.Key);
                    }
                }
                _clients.Clear();
            }
            finally {
                _lock.Release();
                _lock.Dispose();
                _logger.Information("Removed all sessions, current number of sessions is 0");
            }
        }

        /// <summary>
        /// Manage the clients in the client manager.
        /// </summary>
        /// <param name="period"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        private async Task RunClientManagerAsync(TimeSpan period, CancellationToken ct) {
            var timer = new PeriodicTimer(period);
            while (ct.IsCancellationRequested) {
                if (!await timer.WaitForNextTickAsync(ct)) {
                    return;
                }
                await _lock.WaitAsync(ct);
                try {
                    foreach (var client in _clients.Values) {
                        var connect = client.ConnectAsync(ct);
                        if (connect.IsCompleted) {
                            continue;
                        }
                        await Try.Async(() => connect);
                    }
                }
                finally {
                    _lock.Release();
                }
            }
        }

        // Validate certificates
        private void OnValidate(CertificateValidator sender, CertificateValidationEventArgs e) {
            if (e.Accept == true) {
                return;
            }
            if (e.Error.StatusCode == StatusCodes.BadCertificateUntrusted) {
                if (_configuration.SecurityConfiguration.AutoAcceptUntrustedCertificates) {
                    _logger.Warning("Accepting untrusted peer certificate {Thumbprint}, '{Subject}' " +
                        "due to AutoAccept(UntrustedCertificates) set!",
                        e.Certificate.Thumbprint, e.Certificate.Subject);
                    e.Accept = true;
                }

                // Validate thumbprint
                if (e.Certificate.RawData != null && !string.IsNullOrWhiteSpace(e.Certificate.Thumbprint)) {

                    if (_clients.Keys.Any(id => id?.Connection?.Endpoint?.Certificate != null &&
                        e.Certificate.Thumbprint == id.Connection.Endpoint.Certificate)) {
                        e.Accept = true;

                        _logger.Information("Accepting untrusted peer certificate {Thumbprint}, '{Subject}' " +
                            "since it was specified in the endpoint!",
                            e.Certificate.Thumbprint, e.Certificate.Subject);

                        // add the certificate to trusted store
                        _configuration.SecurityConfiguration.AddTrustedPeer(e.Certificate.RawData);
                        try {
                            var store = _configuration.
                                SecurityConfiguration.TrustedPeerCertificates.OpenStore();

                            try {
                                store.Delete(e.Certificate.Thumbprint);
                                store.Add(e.Certificate);
                            }
                            finally {
                                store.Close();
                            }
                        }
                        catch (Exception ex) {
                            _logger.Warning(ex, "Failed to add peer certificate {Thumbprint}, '{Subject}' " +
                                "to trusted store", e.Certificate.Thumbprint, e.Certificate.Subject);
                        }
                    }
                }
            }
            if (!e.Accept) {
                _logger.Information("Rejecting peer certificate {Thumbprint}, '{Subject}' " +
                    "because of {Status}.", e.Certificate.Thumbprint, e.Certificate.Subject,
                    e.Error.StatusCode);
            }
        }

        /// <summary>
        /// Create new client
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        private OpcUaClient CreateClient(ConnectionIdentifier id) {
            return new OpcUaClient(_configuration, id, _logger) {
                KeepAliveInterval = _clientConfig.KeepAliveInterval,
                SessionLifeTime = _clientConfig.DefaultSessionTimeout
            };
        }

        private readonly ILogger _logger;
        private readonly IClientServicesConfig _clientConfig;
        private readonly IIdentity _identity;
        private readonly ConcurrentDictionary<ConnectionIdentifier, OpcUaClient> _clients =
            new ConcurrentDictionary<ConnectionIdentifier, OpcUaClient>();
        private readonly SemaphoreSlim _lock;
        private readonly CancellationTokenSource _cts;
        private readonly Task _processor;
        private readonly ApplicationConfiguration _configuration;
        private readonly IVariantEncoderFactory _codec;
    }
}
