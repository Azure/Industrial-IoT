// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.State
{
    using Azure.IIoT.OpcUa.Publisher.State.Models;
    using Azure.IIoT.OpcUa.Publisher;
    using Furly.Extensions.Serializers;
    using Microsoft.Azure.IIoT.Module.Framework.Client;
    using Microsoft.Extensions.Logging;
    using System;
    using System.Text;
    using System.Threading.Tasks;

    /// <summary>
    /// This class manages reporting of runtime state.
    /// </summary>
    public class RuntimeStateReporter : IRuntimeStateReporter
    {
        private const string RuntimeStateReportingPath = "runtimeinfo";

        private readonly IClientAccessor _clientAccessor;
        private readonly IJsonSerializer _jsonSerializer;
        private readonly IRuntimeStateReporterConfiguration _config;
        private readonly ILogger _logger;

        /// <summary>
        /// Constructor for runtime state reporter.
        /// </summary>
        /// <param name="clientAccessor"></param>
        /// <param name="jsonSerializer"></param>
        /// <param name="config"></param>
        /// <param name="logger"></param>
        public RuntimeStateReporter(IClientAccessor clientAccessor,
            IJsonSerializer jsonSerializer,
            IRuntimeStateReporterConfiguration config,
            ILogger logger)
        {
            _clientAccessor = clientAccessor ?? throw new ArgumentNullException(nameof(clientAccessor));
            _jsonSerializer = jsonSerializer ?? throw new ArgumentNullException(nameof(jsonSerializer));
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc/>
        public async Task SendRestartAnnouncement()
        {
            if (!_config.EnableRuntimeStateReporting)
            {
                return;
            }

            var client = _clientAccessor.Client;
            if (client is null)
            {
                _logger.LogWarning(
                    "Hub client is not initialized yet. Unable to send restart announcement.");
                return;
            }

            try
            {
                var body = new RuntimeStateModel
                {
                    MessageType = MessageTypeEnum.RestartAnnouncement
                };

                await client.SendEventAsync(string.Empty,
                    _jsonSerializer.SerializeToMemory(body),
                    contentEncoding: Encoding.UTF8.WebName,
                    contentType: _jsonSerializer.MimeType,
                    messageSchema: _jsonSerializer.MimeType,
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
    }
}
