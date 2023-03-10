// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Services
{
    using Azure.IIoT.OpcUa.Publisher;
    using Azure.IIoT.OpcUa.Models;
    using Furly.Extensions.Messaging;
    using Furly.Extensions.Serializers;
    using Microsoft.Extensions.Logging;
    using System;
    using System.Text;
    using System.Threading.Tasks;
    using System.Collections.Generic;
    using System.Threading;

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
        /// <param name="config"></param>
        /// <param name="logger"></param>
        /// <param name="properties"></param>
        public RuntimeStateReporter(IEventClient events, IJsonSerializer serializer,
            IPublisherConfiguration config, ILogger<RuntimeStateReporter> logger,
            IDictionary<string, VariantValue> properties = null)
        {
            _events = events ?? throw new ArgumentNullException(nameof(events));
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _properties = properties;
        }

        /// <inheritdoc/>
        public async Task SendRestartAnnouncementAsync(CancellationToken ct)
        {
            if (_properties != null)
            {
                // Set runtime state in twin properties
                _properties[OpcUa.Constants.TwinPropertySiteKey] = _config.Site;
                _properties[OpcUa.Constants.TwinPropertyTypeKey] =
                    OpcUa.Constants.EntityTypePublisher;
                _properties[OpcUa.Constants.TwinPropertyVersionKey] =
                    GetType().Assembly.GetReleaseVersion().ToString();
            }

            if (_config.EnableRuntimeStateReporting)
            {
                var body = new RuntimeStateEventModel
                {
                    MessageType = RuntimeStateEventType.RestartAnnouncement,
                    MessageVersion = 1
                };

                await _events.SendEventAsync(string.Empty,
                    _serializer.SerializeToMemory(body), _serializer.MimeType,
                    Encoding.UTF8.WebName, configure: e => {
                        e.AddProperty(OpcUa.Constants.MessagePropertySchemaKey,
                            MessageSchemaTypes.RuntimeStateMessage);
                        if (_config.RuntimeStateRoutingInfo != null)
                        {
                            e.AddProperty(OpcUa.Constants.MessagePropertyRoutingKey,
                                _config.RuntimeStateRoutingInfo);
                        }
                    }, ct).ConfigureAwait(false);

                _logger.LogInformation("Restart announcement sent successfully.");
            }
        }

        private readonly ILogger _logger;
        private readonly IEventClient _events;
        private readonly IJsonSerializer _serializer;
        private readonly IPublisherConfiguration _config;
        private readonly IDictionary<string, VariantValue> _properties;
    }
}
