// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Core.Messaging.Clients.Dapr
{
    using global::Dapr.Client;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using System;
    using System.Collections.Generic;
    using System.Text.Json.Nodes;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Key-value store built on Dapr state store.
    /// </summary>
    public sealed class DaprStateStoreClient : SyncingKeyValueStore
    {
        /// <inheritdoc/>
        public override string Name => "Dapr";

        /// <summary>
        /// Create state store.
        /// </summary>
        /// <param name="options"></param>
        /// <param name="logger"></param>
        public DaprStateStoreClient(IOptions<DaprOptions> options,
            ILogger<DaprStateStoreClient> logger) : base(logger)
        {
            ArgumentNullException.ThrowIfNull(options);

            _store = string.IsNullOrEmpty(options.Value.StateStoreName)
                ? "default" : options.Value.StateStoreName;
            _client = options.Value.CreateClient(useJsonOptions: true);
            _checkHealth = options.Value.CheckSideCarHealthBeforeAccess;
            _logger = logger;

            StartStateSynchronization();
        }

        /// <inheritdoc/>
        public override async ValueTask<JsonNode?> TryPageInAsync(
            string key, CancellationToken ct = default)
        {
            try
            {
                var state = await _client.GetStateAsync<JsonNode?>(_store,
                    key, cancellationToken: ct).ConfigureAwait(false);

                ModifyState(s => s[key] = state);
                return state;
            }
            catch (Exception ex)
            {
                _logger.PageInFailed(ex, key);
                return null;
            }
        }

        /// <inheritdoc/>
        protected override async ValueTask OnChangesAsync(
            IDictionary<string, JsonNode?> batch, CancellationToken ct)
        {
            if (_checkHealth)
            {
                await _client.WaitForSidecarAsync(ct).ConfigureAwait(false);
            }

            foreach (var item in batch)
            {
                try
                {
                    if (item.Value == null)
                    {
                        await _client.DeleteStateAsync(_store, item.Key,
                            cancellationToken: ct).ConfigureAwait(false);
                    }
                    else
                    {
                        await _client.SaveStateAsync(_store, item.Key,
                            item.Value, cancellationToken: ct).ConfigureAwait(false);
                    }
                }
                catch (Exception ex)
                {
                    _logger.StateFailed(ex, item.Value == null ? "delete" : "save", item.Key);
                }
            }
        }

        /// <inheritdoc/>
        protected override async Task OnLoadState(CancellationToken ct)
        {
            try
            {
                if (_checkHealth)
                {
                    await _client.WaitForSidecarAsync(ct).ConfigureAwait(false);
                }

                var response = await _client.QueryStateAsync<JsonNode?>(_store,
                    "{}", cancellationToken: ct).ConfigureAwait(false);

                ModifyState(state =>
                {
                    foreach (var item in response.Results)
                    {
                        state[item.Key] = item.Data;
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LoadStateQueryFailed(ex);
            }
        }

        /// <inheritdoc/>
        protected override void Dispose(bool disposing)
        {
            try
            {
                if (disposing)
                {
                    _client.Dispose();
                }
            }
            finally
            {
                base.Dispose(disposing);
            }
        }

        private readonly string _store;
        private readonly DaprClient _client;
        private readonly bool _checkHealth;
        private readonly ILogger<DaprStateStoreClient> _logger;
    }

    /// <summary>
    /// Source-generated logging for <see cref="DaprStateStoreClient"/>.
    /// </summary>
    internal static partial class DaprStateStoreClientLogging
    {
        [LoggerMessage(EventId = 0, Level = LogLevel.Debug,
            Message = "Failed to page in state for key {Key}")]
        public static partial void PageInFailed(this ILogger logger, Exception ex, string key);

        [LoggerMessage(EventId = 1, Level = LogLevel.Error,
            Message = "Failed to {Action} state {Key}.")]
        public static partial void StateFailed(this ILogger logger, Exception ex,
            string action, string key);

        [LoggerMessage(EventId = 2, Level = LogLevel.Debug,
            Message = "Failed to load state using query. Query is optional api")]
        public static partial void LoadStateQueryFailed(this ILogger logger, Exception ex);
    }
}
