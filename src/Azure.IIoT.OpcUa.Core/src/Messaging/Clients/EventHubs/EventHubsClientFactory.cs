// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Core.Messaging.Clients.EventHubs
{
    using Azure.IIoT.OpcUa.Core.AzureSdk;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using System;

    /// <summary>
    /// Create Event Hubs event clients.
    /// </summary>
    public sealed class EventHubsClientFactory : IEventClientFactory
    {
        /// <inheritdoc/>
        public string Name => "EventHub";

        /// <summary>
        /// Create factory.
        /// </summary>
        /// <param name="scopeFactory"></param>
        public EventHubsClientFactory(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }

        /// <inheritdoc/>
        public IDisposable CreateEventClient(string connectionString,
            out IEventClient client)
        {
            var scope = _scopeFactory.CreateScope();
            try
            {
                var services = scope.ServiceProvider;
                var baseOptions = services.GetRequiredService<IOptions<EventHubsClientOptions>>().Value;
                var options = new EventHubsClientOptions
                {
                    ConnectionString = connectionString,
                    MaxEventPayloadSizeInBytes = baseOptions.MaxEventPayloadSizeInBytes,
                    SchemaRegistry = baseOptions.SchemaRegistry
                };
                var eventClient = new EventHubsClient(Options.Create(options),
                    services.GetRequiredService<ICredentialProvider>(),
                    services.GetRequiredService<ILogger<EventHubsClient>>(),
                    services.GetService<ISchemaRegistry>());
                client = eventClient;
                return new EventClientScope(scope, eventClient);
            }
            catch
            {
                scope.Dispose();
                throw;
            }
        }

        private sealed class EventClientScope : IDisposable
        {
            public EventClientScope(IServiceScope scope, IDisposable client)
            {
                _scope = scope;
                _client = client;
            }

            /// <inheritdoc/>
            public void Dispose()
            {
                _client.Dispose();
                _scope.Dispose();
            }

            private readonly IServiceScope _scope;
            private readonly IDisposable _client;
        }

        private readonly IServiceScopeFactory _scopeFactory;
    }
}

