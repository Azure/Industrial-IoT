// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Hub.Client {
    using Microsoft.Azure.IIoT.Hub;
    using Microsoft.Azure.IIoT.Utils;
    using Microsoft.Azure.ServiceBus;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    /// <summary>
    /// Service bus client
    /// </summary>
    public sealed class ServiceBusQueueClient : IMessageBrokerClient {

        /// <summary>
        /// Create service client
        /// </summary>
        /// <param name="config"></param>
        public ServiceBusQueueClient(IServiceBusConfig config) {
            if (string.IsNullOrEmpty(config.ServiceBusConnString)) {
                throw new ArgumentException(nameof(config));
            }
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        /// <inheritdoc/>
        public Task<IMessageClient> OpenAsync(string name) {
            if (string.IsNullOrEmpty(name)) {
                throw new ArgumentNullException(nameof(name));
            }
            return Task.FromResult<IMessageClient>(new QueueClientWrapper(
                new QueueClient(_config.ServiceBusConnString, name)));
        }

        /// <summary>
        /// Wraps a service bus sdk client
        /// </summary>
        private class QueueClientWrapper : IMessageClient {

            /// <summary>
            /// Create wrapper
            /// </summary>
            /// <param name="client"></param>
            public QueueClientWrapper(QueueClient client) {
                _client = client;
            }

            /// <inheritdoc/>
            public Task SendAsync(byte[] payload, IDictionary<string, string> properties,
                string partitionKey) {
                var msg = new Message(payload);
                if (properties != null) {
                    foreach (var prop in properties) {
                        msg.UserProperties.Add(prop.Key, prop.Value);
                    }
                    if (properties.TryGetValue(EventProperties.kContentType,
                        out var contentType)) {
                        msg.ContentType = contentType;
                    }
                }
                return _client.SendAsync(msg);
            }

            /// <inheritdoc/>
            public Task SendAsync(byte[] data, string contentType) =>
                _client.SendAsync(CreateMessage(data, contentType));

            /// <inheritdoc/>
            public Task SendAsync(IEnumerable<byte[]> batch, string contentType) =>
                _client.SendAsync(batch.Select(b => CreateMessage(b, contentType)).ToList());

            /// <inheritdoc/>
            public Task CloseAsync() =>
                _client.CloseAsync();

            /// <inheritdoc/>
            public void Dispose() =>
                Try.Op(() => CloseAsync().Wait());

            /// <summary>
            /// Helper to create event from buffer and content type
            /// </summary>
            /// <param name="data"></param>
            /// <param name="contentType"></param>
            /// <returns></returns>
            private static Message CreateMessage(byte[] data, string contentType) {
                var ev = new Message(data) {
                    ContentType = contentType
                };
                ev.UserProperties.Add(EventProperties.kContentType, contentType);
                return ev;
            }
            private readonly QueueClient _client;
        }

        private readonly IServiceBusConfig _config;
    }
}
