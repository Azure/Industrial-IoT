// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Services
{
    using Azure.IIoT.OpcUa.Publisher;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Azure.IIoT.OpcUa.Encoders;
    using Furly.Extensions.Messaging;
    using Furly.Extensions.Serializers;
    using Furly.Extensions.Storage;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using System;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Collections.Generic;
    using System.Linq;
    using System.Diagnostics;
    using System.Security.Cryptography;

    /// <summary>
    /// This class manages reporting of runtime state.
    /// </summary>
    public class RuntimeStateReporter : IRuntimeStateReporter, IApiKeyProvider
    {
        /// <inheritdoc/>
        public string? ApiKey { get; private set; }

        /// <summary>
        /// Constructor for runtime state reporter.
        /// </summary>
        /// <param name="events"></param>
        /// <param name="serializer"></param>
        /// <param name="stores"></param>
        /// <param name="options"></param>
        /// <param name="logger"></param>
        public RuntimeStateReporter(IEnumerable<IEventClient> events,
            IJsonSerializer serializer, IEnumerable<IKeyValueStore> stores,
            IOptions<PublisherOptions> options, ILogger<RuntimeStateReporter> logger)
        {
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            ArgumentNullException.ThrowIfNull(stores);
            ArgumentNullException.ThrowIfNull(events);

            _events = events.ToList();
            _stores = stores.ToList();
            if (_stores.Count == 0)
            {
                throw new ArgumentException("No key value stores configured.",
                    nameof(stores));
            }
        }

        /// <inheritdoc/>
        public async Task SendRestartAnnouncementAsync(CancellationToken ct)
        {
            // Set runtime state in state stores
            foreach (var store in _stores)
            {
                store.State[OpcUa.Constants.TwinPropertySiteKey] =
                    _options.Value.Site;
                store.State[OpcUa.Constants.TwinPropertyTypeKey] =
                    OpcUa.Constants.EntityTypePublisher;
                store.State[OpcUa.Constants.TwinPropertyVersionKey] =
                    GetType().Assembly.GetReleaseVersion().ToString();
            }

            UpdateApiKey();

            if (_options.Value.EnableRuntimeStateReporting ?? false)
            {
                var body = new RuntimeStateEventModel
                {
                    MessageType = RuntimeStateEventType.RestartAnnouncement,
                    MessageVersion = 1
                };

                await SendRuntimeStateEvent(body, ct).ConfigureAwait(false);
                _logger.LogInformation("Restart announcement sent successfully.");
            }
        }

        /// <summary>
        /// Update cached api key
        /// </summary>
        private void UpdateApiKey()
        {
            var apiKeyStore = _stores.Find(s => s.State.TryGetValue(
                OpcUa.Constants.TwinPropertyApiKeyKey, out var key) && key.IsString);
            if (apiKeyStore != null)
            {
                ApiKey = (string?)apiKeyStore.State[OpcUa.Constants.TwinPropertyApiKeyKey];
                _logger.LogInformation("Api Key exists in {Store} store...", apiKeyStore.Name);
            }
            else
            {
                Debug.Assert(_stores.Count > 0);
                _logger.LogInformation("Generating new Api Key in {Store} store...",
                    _stores[0].Name);
                ApiKey = RandomNumberGenerator.GetBytes(20).ToBase64String();
                _stores[0].State.Add(OpcUa.Constants.TwinPropertyApiKeyKey, ApiKey);
            }
        }

        /// <summary>
        /// Send runtime state events
        /// </summary>
        /// <param name="runtimeStateEvent"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        private async Task SendRuntimeStateEvent(RuntimeStateEventModel runtimeStateEvent,
            CancellationToken ct)
        {
            await Task.WhenAll(_events.Select(SendEventAsync)).ConfigureAwait(false);

            async Task SendEventAsync(IEventClient events)
            {
                try
                {
                    await events.SendEventAsync(new TopicBuilder(_options).EventsTopic,
                        _serializer.SerializeToMemory(runtimeStateEvent), _serializer.MimeType,
                        Encoding.UTF8.WebName, configure: e =>
                        {
                            e.AddProperty(OpcUa.Constants.MessagePropertySchemaKey,
                                MessageSchemaTypes.RuntimeStateMessage);
                            if (_options.Value.RuntimeStateRoutingInfo != null)
                            {
                                e.AddProperty(OpcUa.Constants.MessagePropertyRoutingKey,
                                    _options.Value.RuntimeStateRoutingInfo);
                            }
                        }, ct).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "Failed sending {MessageType} runtime state event through {Transport}.",
                        runtimeStateEvent.MessageType, events.Name);
                }
            }
        }

        private readonly ILogger _logger;
        private readonly IJsonSerializer _serializer;
        private readonly IOptions<PublisherOptions> _options;
        private readonly List<IEventClient> _events;
        private readonly List<IKeyValueStore> _stores;
    }
}
