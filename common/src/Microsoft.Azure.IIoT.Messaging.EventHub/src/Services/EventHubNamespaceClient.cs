// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Messaging.EventHub.Services {
    using Microsoft.Azure.IIoT.Messaging;
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
    public sealed class EventHubNamespaceClient : IEventQueueService {

        /// <summary>
        /// Create service client
        /// </summary>
        /// <param name="config"></param>
        public EventHubNamespaceClient(IEventHubClientConfig config) {
            if (string.IsNullOrEmpty(config.EventHubConnString)) {
                throw new ArgumentException(nameof(config));
            }
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        /// <inheritdoc/>
        public Task<IEventQueueClient> OpenAsync(string name) {
            var cs = new EventHubsConnectionStringBuilder(_config.EventHubConnString) {
                EntityPath = name ?? _config.EventHubPath
            }.ToString();
            var client = EventHubClient.CreateFromConnectionString(cs);
            return Task.FromResult<IEventQueueClient>(new EventHubClientWrapper(client));
        }

        /// <summary>
        /// Wraps an event hub sdk client
        /// </summary>
        private class EventHubClientWrapper : IEventQueueClient {

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
                using (var ev = new EventData(payload)) {
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
            }

            /// <inheritdoc/>
            public Task SendEventAsync(byte[] data, string contentType,
                string eventSchema, string contentEncoding) {
                using (var ev = CreateEvent(data, contentType, eventSchema, contentEncoding)) {
                    return _client.SendAsync(ev);
                }
            }

            /// <inheritdoc/>
            public Task SendEventAsync(IEnumerable<byte[]> batch, string contentType,
                string eventSchema, string contentEncoding) {
                var events = batch
                    .Select(b => CreateEvent(b, contentType, eventSchema, contentEncoding))
                    .ToList();
                try {
                    return _client.SendAsync(events);
                }
                finally {
                    events.ForEach(e => e?.Dispose());
                }
            }

            /// <inheritdoc/>
            public Task CloseAsync() {
                return _client.CloseAsync();
            }

            /// <inheritdoc/>
            public void Dispose() {
                Try.Op(_client.Close);
            }


            /// <summary>
            /// Helper to create event from buffer and content type
            /// </summary>
            /// <param name="data"></param>
            /// <param name="contentType"></param>
            /// <param name="eventSchema"></param>
            /// <param name="contentEncoding"></param>
            /// <returns></returns>
            private static EventData CreateEvent(byte[] data, string contentType,
                string eventSchema, string contentEncoding) {
                var ev = new EventData(data);
                ev.Properties.Add(EventProperties.ContentEncoding, contentEncoding);
                ev.Properties.Add(EventProperties.ContentType, contentType);
                ev.Properties.Add(EventProperties.EventSchema, eventSchema);
                return ev;
            }
            private readonly EventHubClient _client;
        }

        private readonly IEventHubClientConfig _config;
    }
}
