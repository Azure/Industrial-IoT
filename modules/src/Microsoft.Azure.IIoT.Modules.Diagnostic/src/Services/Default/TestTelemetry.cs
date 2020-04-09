// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Modules.Diagnostic.Services.Default {
    using Microsoft.Azure.IIoT.Messaging;
    using Microsoft.Azure.IIoT.Serializers;
    using Microsoft.Azure.IIoT.Utils;
    using Serilog;
    using System;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Test telemetry sender - continouusly publishes test messages
    /// </summary>
    public class TestTelemetry : ITelemetrySender, IDisposable {

        /// <inheritdoc/>
        public TimeSpan Interval { get; set; } = TimeSpan.Zero;

        /// <summary>
        /// Create test telemetry publisher
        /// </summary>
        /// <param name="events"></param>
        /// <param name="serializer"></param>
        /// <param name="logger"></param>
        public TestTelemetry(IEventClient events, IJsonSerializer serializer,
            ILogger logger) {
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            _events = events ?? throw new ArgumentNullException(nameof(events));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc/>
        public async Task StartAsync() {
            await _lock.WaitAsync();
            try {
                if (_task != null) {
                    return;
                }
                _cts = new CancellationTokenSource();
                _task = Task.Run(() => PublishAsync(_cts.Token));
            }
            finally {
                _lock.Release();
            }
        }

        /// <inheritdoc/>
        public async Task StopAsync() {
            await _lock.WaitAsync();
            try {
                if (_task == null) {
                    return;
                }
                _cts.Cancel();
                await _task;
            }
            catch (OperationCanceledException) { }
            finally {
                _lock.Release();
            }
        }

        /// <inheritdoc/>
        public void Dispose() {
            Try.Op(() => StopAsync().Wait());

            _lock.Dispose();
            _cts.Dispose();
        }

        /// <summary>
        /// Publish test messages
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        private async Task PublishAsync(CancellationToken ct) {
            _logger.Information("Starting to publish...");
            for (var index = 0; !ct.IsCancellationRequested; index++) {
                var message = _serializer.SerializePretty(new {
                    GeneratedAt = DateTime.UtcNow,
                    Index = index
                });
                _logger.Information("Publishing test message {Index}", index);
                await _events.SendEventAsync(Encoding.UTF8.GetBytes(message),
                    ContentMimeType.Json, null, "utf-8");
                if (Interval != TimeSpan.Zero) {
                    await Task.Delay(Interval, ct);
                }
            }
            _logger.Information("Publishing complete.");
        }

        private readonly IEventClient _events;
        private readonly ILogger _logger;
        private readonly IJsonSerializer _serializer;
        private readonly SemaphoreSlim _lock = new SemaphoreSlim(1, 1);
        private CancellationTokenSource _cts;
        private Task _task;
    }
}
