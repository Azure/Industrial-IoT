// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Services
{
    using Furly.Extensions.Messaging;
    using Microsoft.Extensions.Logging;
    using System;
    using System.Diagnostics;
    using System.Diagnostics.Metrics;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Network message sink
    /// </summary>
    public sealed class NetworkMessageSink : IMessageSink, IDisposable
    {
        /// <inheritdoc/>
        public int MaxMessageSize => _clientAccessor.MaxEventPayloadSizeInBytes;

        /// <summary>
        /// Create network message sink
        /// </summary>
        /// <param name="clientAccessor"></param>
        /// <param name="metrics"></param>
        /// <param name="logger"></param>
        public NetworkMessageSink(IEventClient clientAccessor, IMetricsContext metrics,
            ILogger<NetworkMessageSink> logger)
        {
            _metrics = metrics
                ?? throw new ArgumentNullException(nameof(metrics));
            _clientAccessor = clientAccessor
                ?? throw new ArgumentNullException(nameof(clientAccessor));
            _logger = logger
                ?? throw new ArgumentNullException(nameof(logger));

            _cts = new CancellationTokenSource();
            InitMetricsContext();
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            try
            {
                _cts.Cancel();
                _meter.Dispose();
            }
            finally
            {
                _cts.Dispose();
            }
        }

        /// <inheritdoc/>
        public IEvent CreateMessage()
        {
            return _clientAccessor.CreateEvent();
        }

        /// <inheritdoc/>
        public async Task SendAsync(IEvent message)
        {
            if (message == null)
            {
                return;
            }
            try
            {
                var sw = Stopwatch.StartNew();
                try
                {
                    await message.SendAsync(_cts.Token).ConfigureAwait(false);
                    _messagesSentCount++;
                }
                catch (ObjectDisposedException) { }
                catch (OperationCanceledException) { }
                catch (Exception e)
                {
                    kMessagesErrors.Add(1, _tagList);
                    _logger.LogError(e, "Error sending network message(s)");
                }
                kSendingDuration.Record(sw.ElapsedMilliseconds, _tagList);
            }
            finally
            {
                message.Dispose();
            }
        }

        /// <summary>
        /// Create observable metric registrations
        /// </summary>
        private void InitMetricsContext()
        {
            _meter.CreateObservableCounter("iiot_edge_publisher_sent_iot_messages",
                () => new Measurement<long>(_messagesSentCount, _metrics.TagList), "Messages",
                "Number of IoT messages successfully sent to Sink (IoT Hub or Edge Hub).");
            _meter.CreateObservableGauge("iiot_edge_publisher_sent_iot_messages_per_second",
                () => new Measurement<double>(_messagesSentCount / UpTime, _metrics.TagList), "Messages/second",
                "IoT messages/second sent to Sink (IoT Hub or Edge Hub).");
            _meter.CreateObservableGauge("iiot_edge_publisher_estimated_message_chunks_per_day",
                () => new Measurement<double>(_messagesSentCount, _metrics.TagList), "Messages/day",
                "Estimated 4kb message chunks used from daily quota.");
        }
        static readonly Counter<long> kMessagesErrors = Diagnostics.Meter.CreateCounter<long>(
            "iiot_edge_publisher_failed_iot_messages", "messages", "Number of failures sending a network message.");
        static readonly Histogram<double> kSendingDuration = Diagnostics.Meter.CreateHistogram<double>(
            "iiot_edge_publisher_messages_duration", "milliseconds", "Histogram of message sending durations.");

        private double UpTime => (DateTime.UtcNow - _startTime).TotalSeconds;
        private readonly Meter _meter = Diagnostics.NewMeter();
        private readonly DateTime _startTime = DateTime.UtcNow;
        private readonly ILogger _logger;
        private readonly CancellationTokenSource _cts;
        private readonly IMetricsContext _metrics;
        private readonly IEventClient _clientAccessor;
        private readonly TagList _tagList;
        private long _messagesSentCount;
    }
}
