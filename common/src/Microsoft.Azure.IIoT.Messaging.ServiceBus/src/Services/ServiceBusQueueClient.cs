// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Messaging.ServiceBus.Services {
    using Microsoft.Azure.IIoT.Hub;
    using Microsoft.Azure.IIoT.Utils;
    using Microsoft.Azure.ServiceBus;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    /// <summary>
    /// Service bus queue client
    /// </summary>
    public sealed class ServiceBusQueueClient : IEventQueueService {

        /// <summary>
        /// Create service client
        /// </summary>
        /// <param name="factory"></param>
        public ServiceBusQueueClient(IServiceBusClientFactory factory) {
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        }

        /// <inheritdoc/>
        public Task<IEventQueueClient> OpenAsync(string name) {
            if (string.IsNullOrEmpty(name)) {
                name = null;
            }
            return Task.FromResult<IEventQueueClient>(
                new QueueClientWrapper(this, name));
        }

        /// <summary>
        /// Wraps a service bus sdk client
        /// </summary>
        private class QueueClientWrapper : IEventQueueClient {

            /// <summary>
            /// Create wrapper
            /// </summary>
            /// <param name="outer"></param>
            /// <param name="name"></param>
            public QueueClientWrapper(ServiceBusQueueClient outer, string name) {
                _outer = outer;
                _name = name;
            }

            /// <inheritdoc/>
            public async Task SendAsync(byte[] payload, IDictionary<string, string> properties,
                string partitionKey) {
                var msg = new Message(payload);
                if (properties != null) {
                    foreach (var prop in properties) {
                        msg.UserProperties.Add(prop.Key, prop.Value);
                    }
                    if (properties.TryGetValue(EventProperties.ContentType,
                        out var contentType)) {
                        msg.ContentType = contentType;
                    }
                }
                var client = await _outer._factory.CreateOrGetGetQueueClientAsync(_name);
                await client.SendAsync(msg);
            }

            /// <inheritdoc/>
            public async Task SendEventAsync(byte[] data, string contentType,
                string eventSchema, string contentEncoding) {
                var client = await _outer._factory.CreateOrGetGetQueueClientAsync(_name);
                await client.SendAsync(CreateMessage(data, contentType, eventSchema, contentEncoding));
            }

            /// <inheritdoc/>
            public async Task SendEventAsync(IEnumerable<byte[]> batch, string contentType,
                string eventSchema, string contentEncoding) {
                var client = await _outer._factory.CreateOrGetGetQueueClientAsync(_name);
                await client.SendAsync(batch
                    .Select(b => CreateMessage(b, contentType, eventSchema, contentEncoding))
                    .ToList());
            }

            /// <inheritdoc/>
            public async Task CloseAsync() {
                var client = await _outer._factory.CreateOrGetGetQueueClientAsync(_name);
                await client.CloseAsync();
            }

            /// <inheritdoc/>
            public void Dispose() {
                Try.Op(() => CloseAsync().Wait());
            }

            /// <summary>
            /// Helper to create event from buffer and content type
            /// </summary>
            /// <param name="data"></param>
            /// <param name="contentType"></param>
            /// <param name="eventSchema"></param>
            /// <param name="contentEncoding"></param>
            /// <returns></returns>
            private static Message CreateMessage(byte[] data, string contentType,
                string eventSchema, string contentEncoding) {
                var ev = new Message(data) {
                    ContentType = contentType
                };
                ev.UserProperties.Add(EventProperties.ContentType, contentType);
                ev.UserProperties.Add(EventProperties.ContentEncoding, contentEncoding);
                ev.UserProperties.Add(EventProperties.EventSchema, eventSchema);
                return ev;
            }
            private readonly ServiceBusQueueClient _outer;
            private readonly string _name;
        }

        private readonly IServiceBusClientFactory _factory;
    }
}
