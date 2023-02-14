// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Edge.Twin.Twin {
    using Microsoft.Azure.IIoT.Diagnostics;
    using Microsoft.Azure.IIoT.Exceptions;
    using Microsoft.Azure.IIoT.Module;
    using Microsoft.Azure.IIoT.OpcUa.Core.Models;
    using Microsoft.Azure.IIoT.OpcUa.Protocol;
    using Microsoft.Azure.IIoT.OpcUa.Registry.Models;
    using Microsoft.Azure.IIoT.Serializers;
    using Serilog;
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Manages the endpoint identity information in the twin and reports
    /// the endpoint's status back to the hub.
    /// </summary>
    public class TwinServices : ITwinServices, IDisposable {

        /// <inheritdoc/>
        public EndpointConnectivityState State { get; private set; }
            = EndpointConnectivityState.Disconnected;

        /// <summary>
        /// Create twin services
        /// </summary>
        /// <param name="client"></param>
        /// <param name="events"></param>
        /// <param name="serializer"></param>
        /// <param name="identity"></param>
        /// <param name="logger"></param>
        public TwinServices(IEndpointServices client, IEventEmitter events,
            IJsonSerializer serializer, ILogger logger, IProcessIdentity identity = null) {
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _events = events ?? throw new ArgumentNullException(nameof(events));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _endpoint = new TaskCompletionSource<EndpointModel>();
            _identity = identity;
            _lock = new SemaphoreSlim(1, 1);
        }

        /// <inheritdoc/>
        public async Task SetEndpointAsync(EndpointModel endpoint) {
            await _lock.WaitAsync();
            try {
                var previous = _endpoint.Task.IsCompleted ? _endpoint.Task.Result : null;
                if (!endpoint.IsSameAs(previous)) {
                    _logger.Debug(
                        "Updating twin {device} endpoint from {previous} to {endpoint}...",
                        _identity?.ProcessId, previous?.Url, endpoint?.Url);

                    if (_endpoint.Task.IsCompleted) {
                        _endpoint = new TaskCompletionSource<EndpointModel>();
                    }

                    // Unregister old endpoint
                    if (previous != null) {
                        if (_session != null) {
                            _session.OnConnectionStateChange -= _session_OnConnectionStateChange;
                            await _session.DisposeAsync();
                            _session = null;
                        }

                        // Clear state
                        State = EndpointConnectivityState.Disconnected;
                    }

                    // Register callback to report endpoint state property
                    if (endpoint != null) {
                        var connection = new ConnectionModel {
                            Endpoint = endpoint
                        };
                        _session = _client.GetOrCreateSessionAsync(connection).Result;

                        // Set initial state
                        State = _session.State;

                        _session.OnConnectionStateChange += _session_OnConnectionStateChange;

                        _logger.Information("Endpoint {endpoint} ({device}, {module}) updated.",
                            endpoint?.Url, _identity.ProcessId, _identity.Id);

                        // ready to use
                        _endpoint.TrySetResult(endpoint);
                    }

                    _logger.Information("Twin {device} endpoint updated to {endpoint}.",
                         _identity?.ProcessId, endpoint?.Url);
                }
            }
            finally {
                _lock.Release();
            }
        }

        /// <inheritdoc/>
        public async Task<EndpointModel> GetEndpointAsync(CancellationToken ct) {
            Task<EndpointModel> waiter;
            await _lock.WaitAsync(ct);
            try {
                waiter = _endpoint.Task;
                if (waiter.IsCompleted) {
                    // Got endpoint - return waiter
                    return await waiter;
                }

                // wait below ...
            }
            finally {
                _lock.Release();
            }

            // Wait 5 seconds for endpoint to materialize if cancellation token is default
            using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5))) {
                if (ct == default) {
                    ct = cts.Token;
                }
                // Wait with cancellation
                try {
                    return await Task.Run(() => waiter, ct);
                }
                catch (OperationCanceledException) {
                    _logger.Error("Failed to get endpoint for twin {device} - " +
                        "timed out waiting for configuration!", _identity?.ProcessId);
                    throw new InvalidConfigurationException(
                        "Twin without endpoint configuration");
                }
            }
        }

        /// <inheritdoc/>
        public void Dispose() {
            if (_session != null) {
                _session.OnConnectionStateChange -= _session_OnConnectionStateChange;
                _session.DisposeAsync().AsTask().GetAwaiter().GetResult();
                _session = null;
            }
            _endpoint?.TrySetCanceled();
            _endpoint = null;
            _lock?.Dispose();
        }

        /// <summary>
        /// Report state changes
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="state"></param>
        private void _session_OnConnectionStateChange(object sender, EndpointConnectivityState state) {
            State = state;
            if (_events != null) {
                Task.Run(() => _events.ReportAsync("State", _serializer.FromObject(state)));
            }
        }

        private ISessionHandle _session;
        private TaskCompletionSource<EndpointModel> _endpoint;
        private readonly IJsonSerializer _serializer;
        private readonly IProcessIdentity _identity;
        private readonly SemaphoreSlim _lock;
        private readonly IEndpointServices _client;
        private readonly IEventEmitter _events;
        private readonly ILogger _logger;
    }
}
