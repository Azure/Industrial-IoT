// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.State
{
    using Azure.IIoT.OpcUa.Publisher.State.Models;
    using Azure.IIoT.OpcUa.Publisher;
    using Furly.Extensions.Serializers;
    using Furly.Extensions.Messaging;
    using Microsoft.Extensions.Logging;
    using System;
    using System.Text;
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
        /// <param name="config"></param>
        /// <param name="logger"></param>
        public RuntimeStateReporter(IEventClient events,
            IJsonSerializer serializer,
            IRuntimeStateReporterConfiguration config,
            ILogger logger)
        {
            _events = events ?? throw new ArgumentNullException(nameof(events));
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc/>
        public async Task SendRestartAnnouncementAsync()
        {
            if (!_config.EnableRuntimeStateReporting)
            {
                return;
            }
            try
            {
                var body = new RuntimeStateModel
                {
                    MessageType = MessageTypeEnum.RestartAnnouncement
                };

                await _events.SendEventAsync(string.Empty,
                    _serializer.SerializeToMemory(body),
                    contentEncoding: Encoding.UTF8.WebName,
                    contentType: _serializer.MimeType,
                    messageSchema: _serializer.MimeType,
                    routingInfo: RuntimeStateReportingPath).ConfigureAwait(false);

                _logger.LogInformation("Restart announcement sent successfully.");
            }
            catch (InvalidOperationException)
            {
                // In this case DeviceClient was used which does not support
                // sending messages to a specific output target.
                _logger.LogInformation("Unable to send restart announcement as " +
                    "OPC Publisher is not running in IoT Edge context.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send restart announcement.");
            }
        }

        private const string RuntimeStateReportingPath = "runtimeinfo";
        private readonly IEventClient _events;
        private readonly IJsonSerializer _serializer;
        private readonly IRuntimeStateReporterConfiguration _config;
        private readonly ILogger _logger;
    }
}
