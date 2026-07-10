// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Core.Messaging.Clients.Dapr
{
    using Azure.IIoT.OpcUa.Core.Storage;
    using Microsoft.Extensions.Logging;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using System.Text.Json.Nodes;
    using System.Threading;
    using System.Threading.Channels;
    using System.Threading.Tasks;

    /// <summary>
    /// Key-value store base with asynchronous state synchronization.
    /// </summary>
    public abstract class SyncingKeyValueStore : IKeyValueStore,
        IAwaitable<IKeyValueStore>, IDictionary<string, JsonNode?>,
        IAsyncDisposable, IDisposable
    {
        /// <inheritdoc/>
        public IDictionary<string, JsonNode?> State => this;

        /// <inheritdoc/>
        public abstract string Name { get; }

        /// <inheritdoc/>
        public ICollection<string> Keys
        {
            get
            {
                lock (_state)
                {
                    return [.. _state.Keys];
                }
            }
        }

        /// <inheritdoc/>
        public ICollection<JsonNode?> Values
        {
            get
            {
                lock (_state)
                {
                    return [.. _state.Values];
                }
            }
        }

        /// <inheritdoc/>
        public int Count
        {
            get
            {
                lock (_state)
                {
                    return _state.Count;
                }
            }
        }

        /// <inheritdoc/>
        public bool IsReadOnly => false;

        /// <inheritdoc/>
        public JsonNode? this[string key]
        {
            get
            {
                lock (_state)
                {
                    return _state[key];
                }
            }
            set
            {
                lock (_state)
                {
                    if (_write.Writer.TryWrite((key, value)))
                    {
                        _state[key] = value;
                    }
                }
            }
        }

        /// <summary>
        /// Create store.
        /// </summary>
        /// <param name="logger"></param>
        protected SyncingKeyValueStore(ILogger logger)
        {
            _logger = logger;
            _write = Channel.CreateUnbounded<(string, JsonNode?)>();
            _cts = new CancellationTokenSource();
        }

        /// <inheritdoc/>
        public abstract ValueTask<JsonNode?> TryPageInAsync(string key,
            CancellationToken ct = default);

        /// <inheritdoc/>
        public IAwaiter<IKeyValueStore> GetAwaiter()
        {
            return _loaded.AsAwaiter<IKeyValueStore>(this);
        }

        /// <inheritdoc/>
        public async ValueTask DisposeAsync()
        {
            try
            {
                await DisposeAsync(true).ConfigureAwait(false);
            }
            finally
            {
                _cts.Dispose();
                GC.SuppressFinalize(this);
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <inheritdoc/>
        public void Add(string key, JsonNode? value)
        {
            lock (_state)
            {
                if (_write.Writer.TryWrite((key, value)))
                {
                    _state.Add(key, value);
                }
            }
        }

        /// <inheritdoc/>
        public bool ContainsKey(string key)
        {
            lock (_state)
            {
                return _state.ContainsKey(key);
            }
        }

        /// <inheritdoc/>
        public bool Remove(string key)
        {
            lock (_state)
            {
                if (_state.ContainsKey(key) && _write.Writer.TryWrite((key, null)))
                {
                    return _state.Remove(key);
                }
                return false;
            }
        }

        /// <inheritdoc/>
        public bool TryGetValue(string key, [MaybeNullWhen(false)] out JsonNode? value)
        {
            lock (_state)
            {
                return _state.TryGetValue(key, out value);
            }
        }

        /// <inheritdoc/>
        public void Add(KeyValuePair<string, JsonNode?> item)
        {
            Add(item.Key, item.Value);
        }

        /// <inheritdoc/>
        public void Clear()
        {
            lock (_state)
            {
                foreach (var key in _state.Keys.ToList())
                {
                    if (_write.Writer.TryWrite((key, null)))
                    {
                        _state.Remove(key);
                    }
                }
            }
        }

        /// <inheritdoc/>
        public bool Contains(KeyValuePair<string, JsonNode?> item)
        {
            lock (_state)
            {
                return ((ICollection<KeyValuePair<string, JsonNode?>>)_state).Contains(item);
            }
        }

        /// <inheritdoc/>
        public void CopyTo(KeyValuePair<string, JsonNode?>[] array, int arrayIndex)
        {
            lock (_state)
            {
                ((ICollection<KeyValuePair<string, JsonNode?>>)_state).CopyTo(
                    array, arrayIndex);
            }
        }

        /// <inheritdoc/>
        public bool Remove(KeyValuePair<string, JsonNode?> item)
        {
            lock (_state)
            {
                if (((ICollection<KeyValuePair<string, JsonNode?>>)_state).Contains(item) &&
                    _write.Writer.TryWrite((item.Key, null)))
                {
                    return _state.Remove(item.Key);
                }
                return false;
            }
        }

        /// <inheritdoc/>
        public IEnumerator<KeyValuePair<string, JsonNode?>> GetEnumerator()
        {
            lock (_state)
            {
                return _state.ToList().GetEnumerator();
            }
        }

        /// <inheritdoc/>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// Start state synchronization.
        /// </summary>
        protected void StartStateSynchronization()
        {
            _loaded = OnLoadState(_cts.Token);
            _processor = _loaded.ContinueWith(_ =>
                Task.Factory.StartNew(() => SyncAsync(_cts.Token), _cts.Token,
                    TaskCreationOptions.LongRunning, TaskScheduler.Default).Unwrap(),
                TaskScheduler.Default).Unwrap();
        }

        /// <summary>
        /// Process changed state.
        /// </summary>
        /// <param name="batch"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        protected abstract ValueTask OnChangesAsync(
            IDictionary<string, JsonNode?> batch, CancellationToken ct);

        /// <summary>
        /// Load initial state.
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        protected virtual Task OnLoadState(CancellationToken ct)
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Modify state without synchronizing.
        /// </summary>
        /// <param name="processor"></param>
        protected void ModifyState(Action<IDictionary<string, JsonNode?>> processor)
        {
            lock (_state)
            {
                processor(_state);
            }
        }

        /// <summary>
        /// Dispose.
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing)
        {
            try
            {
                DisposeAsync(true).AsTask().GetAwaiter().GetResult();
            }
            finally
            {
                _cts.Dispose();
            }
        }

        /// <summary>
        /// Dispose.
        /// </summary>
        /// <param name="disposing"></param>
        /// <returns></returns>
        protected virtual async ValueTask DisposeAsync(bool disposing)
        {
            if (disposing && _processor != null)
            {
                await _cts.CancelAsync().ConfigureAwait(false);
                try
                {
                    await _processor.ConfigureAwait(false);
                }
                catch (OperationCanceledException) { }
                finally
                {
                    _processor = null;
                }
            }
        }

        private async Task SyncAsync(CancellationToken ct)
        {
            try
            {
                while (!ct.IsCancellationRequested)
                {
                    var batch = new Dictionary<string, JsonNode?>();
                    var item = await _write.Reader.ReadAsync(ct).ConfigureAwait(false);
                    do
                    {
                        batch[item.Item1] = item.Item2;
                    }
                    while (_write.Reader.TryRead(out item));
                    await OnChangesAsync(batch, ct).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                _logger.ProcessingChangesFailed(ex);
            }
        }

        private readonly ILogger _logger;
        private readonly CancellationTokenSource _cts;
        private readonly Dictionary<string, JsonNode?> _state = [];
        private readonly Channel<(string, JsonNode?)> _write;
        private Task _loaded = Task.CompletedTask;
        private Task? _processor;
    }

    /// <summary>
    /// Source-generated logging for <see cref="SyncingKeyValueStore"/>.
    /// </summary>
    internal static partial class SyncingKeyValueStoreLogging
    {
        [LoggerMessage(EventId = 20, Level = LogLevel.Error,
            Message = "Failed to process changes, exiting.")]
        public static partial void ProcessingChangesFailed(this ILogger logger, Exception ex);
    }
}
