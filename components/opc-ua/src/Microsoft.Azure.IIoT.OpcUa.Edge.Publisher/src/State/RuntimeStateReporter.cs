// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Edge.Publisher.State {

    using Microsoft.Azure.Devices.Client;
    using Microsoft.Azure.IIoT.Hub;
    using Microsoft.Azure.IIoT.Module.Framework.Client;
    using Microsoft.Azure.IIoT.OpcUa.Edge.Publisher.State.Models;
    using Microsoft.Azure.IIoT.Serializers;
    using Serilog;
    using System;
    using System.Text;
    using System.Threading.Tasks;

    /// <summary>
    /// This class manages reporting of runtime state.
    /// </summary>
    public class RuntimeStateReporter : IRuntimeStateReporter {

        private const string RuntimeStateReportingPath = "runtimeinfo";

        private readonly IClientAccessor _clientAccessor;
        private readonly IJsonSerializer _jsonSerializer;
        private readonly IRuntimeStateReporterConfiguration _config;
        private readonly ILogger _logger;

        /// <summary>
        /// Constructor for runtime state reporter.
        /// </summary>
        public RuntimeStateReporter(IClientAccessor clientAccessor,
            IJsonSerializer jsonSerializer,
            IRuntimeStateReporterConfiguration config,
            ILogger logger) {

            _clientAccessor = clientAccessor ?? throw new ArgumentNullException(nameof(clientAccessor));
            _jsonSerializer = jsonSerializer ?? throw new ArgumentNullException(nameof(jsonSerializer));
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc/>
        public async Task SendRestartAnnouncement() {
            if (!_config.EnableRuntimeStateReporting) {
                return;
            }

            if (_clientAccessor.Client is null) {
                _logger.Warning("Hub client is not initialized yet. Unable to send restart announcement.");
                return;
            }

            try {
                var body = new RuntimeStateModel {
                    MessageType = MessageTypeEnum.RestartAnnouncement
                };

                var bodyJson = _jsonSerializer.SerializeToString(body);

                var message = new Message(Encoding.UTF8.GetBytes(bodyJson)) {
                    ContentType = _jsonSerializer.MimeType,
                    ContentEncoding = Encoding.UTF8.WebName,
                };

                message.Properties.Add(SystemProperties.MessageSchema, _jsonSerializer.MimeType);
                message.Properties.Add(CommonProperties.ContentEncoding, Encoding.UTF8.WebName);

                await _clientAccessor.Client
                    .SendEventAsync(RuntimeStateReportingPath, message)
                    .ConfigureAwait(false);

                _logger.Information("Restart announcement sent successfully.");
            }
            catch (InvalidOperationException) {
                // In this case DeviceClient was used which does not support
                // sending messages to a specific output target.
                _logger.Information("Unable to send restart announcement as " +
                    "OPC Publisher is not running in IoT Edge context.");
            }
            catch (Exception ex) {
                _logger.Error(ex, "Failed to send restart announcement.");
            }
        }
    }
}
