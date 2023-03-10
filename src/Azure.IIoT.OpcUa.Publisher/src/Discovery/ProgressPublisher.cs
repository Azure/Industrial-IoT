// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Discovery
{
    using Azure.IIoT.OpcUa.Models;
    using Furly.Extensions.Messaging;
    using Furly.Extensions.Serializers;
    using Microsoft.Azure.IIoT;
    using Microsoft.Extensions.Logging;
    using System;
    using System.Text;
    using System.Threading.Channels;
    using System.Threading.Tasks;

    /// <summary>
    /// Discovery progress message sender
    /// </summary>
    public sealed class ProgressPublisher : ProgressLogger, IDisposable
    {
        /// <summary>
        /// Create listener
        /// </summary>
        /// <param name="events"></param>
        /// <param name="serializer"></param>
        /// <param name="logger"></param>
        public ProgressPublisher(IEventClient events, IJsonSerializer serializer,
            ILogger<ProgressPublisher> logger)
            : base(logger)
        {
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _events = events ?? throw new ArgumentNullException(nameof(events));
            _channel = Channel.CreateUnbounded<DiscoveryProgressModel>();
            _sender = Task.Factory.StartNew(() => SendProgressAsync(),
                default, TaskCreationOptions.LongRunning, TaskScheduler.Default).Unwrap();
        }

        /// <inheritdoc/>
        protected override void Send(DiscoveryProgressModel progress)
        {
            progress.DiscovererId = _events.Identity;
            base.Send(progress);
            if (!_channel.Writer.TryWrite(progress))
            {
                throw new ObjectDisposedException(
                    "Cannot send if progress publisher is already disposed.");
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            if (!_sender.IsCompleted)
            {
                _channel.Writer.Complete();
                _sender.GetAwaiter().GetResult();
            }
        }

        /// <summary>
        /// Send progress
        /// </summary>
        /// <returns></returns>
        private async Task SendProgressAsync()
        {
            await foreach (var progress in _channel.Reader.ReadAllAsync())
            {
                try
                {
                    await _events.SendEventAsync(string.Empty,
                        _serializer.SerializeToMemory((object)progress),
                        _serializer.MimeType, Encoding.UTF8.WebName,
                        e => e.AddProperty(OpcUa.Constants.MessagePropertySchemaKey,
                            MessageSchemaTypes.DiscoveryMessage)).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send discovery progress.");
                }
            }
        }

        private readonly ILogger _logger;
        private readonly IJsonSerializer _serializer;
        private readonly Task _sender;
        private readonly Channel<DiscoveryProgressModel> _channel;
        private readonly IEventClient _events;
    }
}
