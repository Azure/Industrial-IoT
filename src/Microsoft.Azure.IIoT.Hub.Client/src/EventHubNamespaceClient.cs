// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Hub.Client {
    using Microsoft.Azure.IIoT.Hub;
    using Microsoft.Azure.IIoT.Utils;
    using Microsoft.Azure.EventHubs;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    /// <summary>
    /// Event hub namespace client
    /// </summary>
    public sealed class EventHubNamespaceClient : IMessageBrokerClient {

        /// <summary>
        /// Create service client
        /// </summary>
        /// <param name="config"></param>
        public EventHubNamespaceClient(IEventHubConfig config) {
            if (string.IsNullOrEmpty(config.EventHubConnString)) {
                throw new ArgumentException(nameof(config));
            }
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        /// <inheritdoc/>
        public Task<IMessageClient> OpenAsync(string name) {
            var cs = new EventHubsConnectionStringBuilder(_config.EventHubConnString) {
                EntityPath = name ?? _config.EventHubPath
            }.ToString();
            var client = EventHubClient.CreateFromConnectionString(cs);
            return Task.FromResult<IMessageClient>(new EventHubClientWrapper(client));
        }

        /// <summary>
        /// Wraps an event hub sdk client
        /// </summary>
        private class EventHubClientWrapper : IMessageClient {

            /// <summary>
            /// Create wrapper
            /// </summary>
            /// <param name="client"></param>
            public EventHubClientWrapper(EventHubClient client) {
                _client = client;
            }

            /// <inheritdoc/>
            public Task SendAsync(byte[] payload, IDictionary<string, string> properties,
                string partitionKey) {
                var ev = new EventData(payload);
                if (properties != null) {
                    foreach (var prop in properties) {
                        ev.Properties.Add(prop.Key, prop.Value);
                    }
                }
                if (partitionKey != null) {
                    return _client.SendAsync(ev, partitionKey);
                }
                return _client.SendAsync(ev);
            }

            /// <inheritdoc/>
            public Task SendAsync(byte[] data, string contentType) =>
                _client.SendAsync(CreateEvent(data, contentType));

            /// <inheritdoc/>
            public Task SendAsync(IEnumerable<byte[]> batch, string contentType) =>
                _client.SendAsync(batch.Select(b => CreateEvent(b, contentType)));

            /// <inheritdoc/>
            public Task CloseAsync() =>
                _client.CloseAsync();

            /// <inheritdoc/>
            public void Dispose() =>
                Try.Op(_client.Close);


            /// <summary>
            /// Helper to create event from buffer and content type
            /// </summary>
            /// <param name="data"></param>
            /// <param name="contentType"></param>
            /// <returns></returns>
            private static EventData CreateEvent(byte[] data, string contentType) {
                var ev = new EventData(data);
                ev.Properties.Add(EventProperties.kContentType, contentType);
                return ev;
            }
            private readonly EventHubClient _client;
        }

        private readonly IEventHubConfig _config;
    }
}
