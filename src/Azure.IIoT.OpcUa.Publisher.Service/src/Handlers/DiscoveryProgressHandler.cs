// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Service.Handlers
{
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Azure.IIoT.OpcUa.Encoders;
    using Furly.Extensions.Serializers;
    using Microsoft.Extensions.Logging;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Discovery progress handling
    /// </summary>
    public sealed class DiscoveryProgressHandler : IMessageHandler
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
            IJsonSerializer serializer, ILogger<DiscoveryProgressHandler> logger)
        {
            _serializer = serializer ??
                throw new ArgumentNullException(nameof(serializer));
            _logger = logger ??
                throw new ArgumentNullException(nameof(logger));
            _handlers = handlers?.ToList() ??
                throw new ArgumentNullException(nameof(handlers));
        }

        /// <inheritdoc/>
        public async ValueTask HandleAsync(string deviceId, string? moduleId, ReadOnlyMemory<byte> payload,
            IReadOnlyDictionary<string, string?> properties, CancellationToken ct)
        {
            DiscoveryProgressModel? discovery;
            try
            {
                discovery = _serializer.Deserialize<DiscoveryProgressModel>(payload);
                if (discovery == null)
                {
                    throw new FormatException($"Bad payload for scheme {MessageSchema}.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to convert discovery message {Json}",
                    Encoding.UTF8.GetString(payload.Span));
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

        private readonly IJsonSerializer _serializer;
        private readonly ILogger _logger;
        private readonly List<IDiscoveryProgressProcessor> _handlers;
    }
}
