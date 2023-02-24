// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Services.Handlers
{
    using Azure.IIoT.OpcUa.Models;
    using Furly.Extensions.Serializers;
    using Microsoft.Azure.IIoT.Hub;
    using Microsoft.Extensions.Logging;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    /// <summary>
    /// Discovery progress handling
    /// </summary>
    public sealed class DiscoveryProgressHandler : IDeviceTelemetryHandler
    {
        /// <inheritdoc/>
        public string MessageSchema => MessageSchemaTypes.DiscoveryMessage;

        /// <summary>
        /// Create handler
        /// </summary>
        /// <param name="handlers"></param>
        /// <param name="serializer"></param>
        /// <param name="logger"></param>
        public DiscoveryProgressHandler(IEnumerable<IDiscoveryProgressProcessor> handlers,
            IJsonSerializer serializer, ILogger logger)
        {
            _serializer = serializer ??
                throw new ArgumentNullException(nameof(serializer));
            _logger = logger ??
                throw new ArgumentNullException(nameof(logger));
            _handlers = handlers?.ToList() ??
                throw new ArgumentNullException(nameof(handlers));
        }

        /// <inheritdoc/>
        public async Task HandleAsync(string deviceId, string moduleId,
            byte[] payload, IDictionary<string, string> properties, Func<Task> checkpoint)
        {
            DiscoveryProgressModel discovery;
            try
            {
                discovery = _serializer.Deserialize<DiscoveryProgressModel>(payload);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to convert discovery message {Json}",
                    Encoding.UTF8.GetString(payload));
                return;
            }
            try
            {
                await Task.WhenAll(_handlers.Select(h => h.OnDiscoveryProgressAsync(discovery))).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Publishing discovery message failed with exception - skip");
            }
        }

        /// <inheritdoc/>
        public Task OnBatchCompleteAsync()
        {
            return Task.CompletedTask;
        }

        private readonly IJsonSerializer _serializer;
        private readonly ILogger _logger;
        private readonly List<IDiscoveryProgressProcessor> _handlers;
    }
}
