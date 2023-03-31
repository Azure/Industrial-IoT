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
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// This class manages reporting of runtime state.
    /// </summary>
    public class RuntimeStateReporter : IRuntimeStateReporter
    {
        /// <summary>
        /// Constructor for runtime state reporter.
        /// </summary>
        /// <param name="events"></param>
        /// <param name="serializer"></param>
        /// <param name="options"></param>
        /// <param name="logger"></param>
        /// <param name="properties"></param>
        public RuntimeStateReporter(IEventClient events, IJsonSerializer serializer,
            IOptions<PublisherOptions> options, ILogger<RuntimeStateReporter> logger,
            IDictionary<string, VariantValue>? properties = null)
        {
            _events = events ?? throw new ArgumentNullException(nameof(events));
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _properties = properties;
        }

        /// <inheritdoc/>
        public async Task SendRestartAnnouncementAsync(CancellationToken ct)
        {
            if (_properties != null)
            {
                // Set runtime state in twin properties
                _properties[OpcUa.Constants.TwinPropertySiteKey] =
                    _options.Value.Site;
                _properties[OpcUa.Constants.TwinPropertyTypeKey] =
                    OpcUa.Constants.EntityTypePublisher;
                _properties[OpcUa.Constants.TwinPropertyVersionKey] =
                    GetType().Assembly.GetReleaseVersion().ToString();

                if (!_properties.ContainsKey(OpcUa.Constants.TwinPropertyApiKeyKey))
                {
                    _logger.LogInformation("Generating new Api Key ...");
                    var apiKey = Guid.NewGuid().ToString();
                    _properties.Add(OpcUa.Constants.TwinPropertyApiKeyKey, apiKey);
                }
            }

            if (_options.Value.EnableRuntimeStateReporting ?? false)
            {
                var body = new RuntimeStateEventModel
                {
                    MessageType = RuntimeStateEventType.RestartAnnouncement,
                    MessageVersion = 1
                };

                await _events.SendEventAsync(new TopicBuilder(_options).EventsTopic,
                    _serializer.SerializeToMemory(body), _serializer.MimeType,
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

                _logger.LogInformation("Restart announcement sent successfully.");
            }
        }

        private readonly ILogger _logger;
        private readonly IEventClient _events;
        private readonly IJsonSerializer _serializer;
        private readonly IOptions<PublisherOptions> _options;
        private readonly IDictionary<string, VariantValue>? _properties;
    }
}
