// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Service.Handlers
{
    using Furly;
    using Furly.Azure.IoT;
    using Furly.Extensions.Messaging;
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Default iot hub device event handler implementation
    /// </summary>
    public sealed class DeviceTelemetryEventHandler : IIoTHubTelemetryHandler,
        IAwaitable<DeviceTelemetryEventHandler>, IDisposable
    {
        /// <summary>
        /// Create processor factory
        /// </summary>
        /// <param name="events"></param>
        /// <param name="handlers"></param>
        public DeviceTelemetryEventHandler(IEventRegistration<IIoTHubTelemetryHandler> events,
            IEnumerable<IMessageHandler> handlers)
        {
            ArgumentNullException.ThrowIfNull(handlers);
            _handlers = new ConcurrentDictionary<string, IMessageHandler>(
                handlers.Select(h => KeyValuePair.Create(h.MessageSchema.ToUpperInvariant(), h)));
            _registration = events.Register(this);
        }

        /// <inheritdoc/>
        public IAwaiter<DeviceTelemetryEventHandler> GetAwaiter()
        {
            return Task.CompletedTask.AsAwaiter(this);
        }

        /// <inheritdoc/>
        public async ValueTask HandleAsync(string deviceId, string? moduleId, string topic,
            ReadOnlyMemory<byte> data, string contentType, string contentEncoding,
            IReadOnlyDictionary<string, string?> properties, CancellationToken ct)

        {
            if (!properties.TryGetValue(Constants.MessagePropertySchemaKey, out var schema) ||
                schema == null)
            {
                schema = topic;
            }

            if (_handlers.TryGetValue(schema.ToUpperInvariant(), out var handler))
            {
                await handler.HandleAsync(deviceId, moduleId, data.ToArray(),
                    properties, ct).ConfigureAwait(false);
            }
            else
            {
                //  TODO: when handling third party OPC UA PubSub Messages
                //  the schemaType might not exist
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            _registration.Dispose();
        }

        private readonly ConcurrentDictionary<string, IMessageHandler> _handlers;
        private readonly IDisposable _registration;
    }
}
