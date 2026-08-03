// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Core.Messaging.Clients.IoTEdge
{
    using Azure.IIoT.OpcUa.Core.Messaging.Clients.Dapr;
    using Microsoft.Extensions.Logging;
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Collections.Generic;
    using System.Text.Json.Nodes;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// IoT Hub twin reported-property state store.
    /// </summary>
    /// <remarks>
    /// Excluded from coverage for the same reason as
    /// <see cref="IoTEdgeModuleClient"/>. The syncing behaviour it inherits is
    /// covered on the base <see cref="SyncingKeyValueStore"/>, which is
    /// testable; what remains here is the twin traffic through that sealed
    /// client.
    /// </remarks>
    [ExcludeFromCodeCoverage(Justification =
        "Twin traffic through the sealed IoTEdgeModuleClient; the syncing behaviour is covered on the base class.")]
    public sealed class IoTEdgeTwinStore : SyncingKeyValueStore
    {
        /// <inheritdoc/>
        public override string Name => "IoTEdge";

        /// <summary>
        /// Create store.
        /// </summary>
        public IoTEdgeTwinStore(IoTEdgeModuleClient client,
            ILogger<IoTEdgeTwinStore> logger) : base(logger)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            StartStateSynchronization();
        }

        /// <inheritdoc/>
        public override async ValueTask<JsonNode?> TryPageInAsync(string key,
            CancellationToken ct = default)
        {
            await _client.EnsureConnectedAsync(ct).ConfigureAwait(false);
            var twin = await _client.Client.GetTwinAsync(ct).ConfigureAwait(false);
            var desired = JsonNode.Parse(twin.Desired.RawJson.Span);
            var reported = JsonNode.Parse(twin.Reported.RawJson.Span);
            var value = reported?[key] ?? desired?[key];
            ModifyState(state => state[key] = value?.DeepClone());
            return value;
        }

        /// <inheritdoc/>
        protected override async ValueTask OnChangesAsync(
            IDictionary<string, JsonNode?> batch, CancellationToken ct)
        {
            try
            {
                await _client.EnsureConnectedAsync(ct).ConfigureAwait(false);
                var patch = new JsonObject();
                foreach (var item in batch)
                {
                    patch[item.Key] = item.Value?.DeepClone();
                }
                await _client.Client.UpdateReportedPropertiesAsync(
                    patch.ToJsonString(), ct).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.SynchronizeFailed(ex);
            }
        }

        /// <inheritdoc/>
        protected override async Task OnLoadState(CancellationToken ct)
        {
            try
            {
                await _client.EnsureConnectedAsync(ct).ConfigureAwait(false);
                var twin = await _client.Client.GetTwinAsync(ct).ConfigureAwait(false);
                var reported = JsonNode.Parse(twin.Reported.RawJson.Span) as JsonObject;
                var desired = JsonNode.Parse(twin.Desired.RawJson.Span) as JsonObject;
                ModifyState(state =>
                {
                    if (reported != null)
                    {
                        foreach (var property in reported)
                        {
                            if (!property.Key.StartsWith('$'))
                            {
                                state[property.Key] = property.Value?.DeepClone();
                            }
                        }
                    }
                    if (desired != null)
                    {
                        foreach (var property in desired)
                        {
                            if (!property.Key.StartsWith('$'))
                            {
                                state[property.Key] = property.Value?.DeepClone();
                            }
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LoadFailed(ex);
            }
        }

        private readonly IoTEdgeModuleClient _client;
        private readonly ILogger<IoTEdgeTwinStore> _logger;
    }

    /// <summary>
    /// Source-generated logging for IoTEdgeTwinStore.
    /// </summary>
    internal static partial class IoTEdgeTwinStoreLogging
    {
        private const int EventClass = 940;

        [LoggerMessage(EventId = EventClass + 0, Level = LogLevel.Debug,
            Message = "Failed to load IoT Edge twin state.")]
        public static partial void LoadFailed(this ILogger logger, Exception ex);

        [LoggerMessage(EventId = EventClass + 1, Level = LogLevel.Error,
            Message = "Failed to synchronize IoT Edge twin state.")]
        public static partial void SynchronizeFailed(this ILogger logger, Exception ex);
    }
}
