// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Core.Messaging.Clients.IoTEdge
{
    using Azure.IIoT.OpcUa.Core.IoTEdge;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using System;
    using System.Collections.Generic;
    using System.Threading;

    /// <summary>
    /// Creates an isolated IoT Hub event client for a writer-group connection
    /// string.
    /// </summary>
    /// <remarks>
    /// The default edge transport is a singleton created from the IoT Edge
    /// workload environment. A writer group can instead carry a device
    /// connection string in its transport configuration; that requires a
    /// separate client with its own identity and lifetime rather than mutating
    /// the singleton.
    /// </remarks>
    public sealed class IoTHubEventClientFactory : IEventClientFactory
    {
        /// <inheritdoc/>
        public string Name => "IoTHub";

        /// <summary>
        /// Create the factory.
        /// </summary>
        public IoTHubEventClientFactory(
            IOptions<IoTEdgeClientOptions> options,
            IEnumerable<IIoTEdgeClientState> stateHandlers,
            ILoggerFactory loggerFactory)
            : this(options, stateHandlers, loggerFactory,
                IoTHubModuleClientFactory.Instance)
        {
        }

        internal IoTHubEventClientFactory(
            IOptions<IoTEdgeClientOptions> options,
            IEnumerable<IIoTEdgeClientState> stateHandlers,
            ILoggerFactory loggerFactory,
            IIoTHubModuleClientFactory clientFactory)
        {
            ArgumentNullException.ThrowIfNull(options);
            ArgumentNullException.ThrowIfNull(stateHandlers);
            _options = options.Value;
            _stateHandlers = [.. stateHandlers];
            _loggerFactory = loggerFactory ??
                throw new ArgumentNullException(nameof(loggerFactory));
            _clientFactory = clientFactory ??
                throw new ArgumentNullException(nameof(clientFactory));
        }

        /// <inheritdoc/>
        public IDisposable CreateEventClient(string connectionString,
            out IEventClient client)
        {
            ArgumentException.ThrowIfNullOrEmpty(connectionString);
            var options = new IoTEdgeClientOptions
            {
                EdgeHubConnectionString = connectionString,
                Product = _options.Product,
                KeepAlivePeriodSeconds = _options.KeepAlivePeriodSeconds,
                DefaultMethodCallTimeout = _options.DefaultMethodCallTimeout
            };
            var wrapped = Options.Create(options);
            var identity = new IoTEdgeIdentity(wrapped,
                _loggerFactory.CreateLogger<IoTEdgeIdentity>());
#pragma warning disable CA2000 // Ownership transfers to EventClientScope below.
            var moduleClient = new IoTEdgeModuleClient(wrapped, identity,
                _stateHandlers, _loggerFactory, _clientFactory);
#pragma warning restore CA2000
            try
            {
                var transport = new IoTEdgeTransport(moduleClient,
                    _loggerFactory.CreateLogger<IoTEdgeTransport>());
                client = transport;
                return new EventClientScope(transport, moduleClient);
            }
            catch
            {
                moduleClient.DisposeAsync().AsTask().GetAwaiter().GetResult();
                throw;
            }
        }

        private sealed class EventClientScope : IDisposable
        {
            public EventClientScope(IoTEdgeTransport transport,
                IoTEdgeModuleClient client)
            {
                _transport = transport;
                _client = client;
            }

            /// <inheritdoc/>
            public void Dispose()
            {
                if (Interlocked.Exchange(ref _disposed, 1) != 0)
                {
                    return;
                }
                try
                {
                    _transport.DisposeAsync().AsTask().GetAwaiter().GetResult();
                }
                finally
                {
                    _client.DisposeAsync().AsTask().GetAwaiter().GetResult();
                }
            }

            private readonly IoTEdgeTransport _transport;
            private readonly IoTEdgeModuleClient _client;
            private int _disposed;
        }

        private readonly IoTEdgeClientOptions _options;
        private readonly IIoTEdgeClientState[] _stateHandlers;
        private readonly ILoggerFactory _loggerFactory;
        private readonly IIoTHubModuleClientFactory _clientFactory;
    }
}
