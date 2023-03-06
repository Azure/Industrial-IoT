// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Discovery
{
    using Azure.IIoT.OpcUa.Models;
    using Furly.Extensions.Serializers;
    using Microsoft.Azure.IIoT;
    using Microsoft.Azure.IIoT.Diagnostics;
    using Microsoft.Azure.IIoT.Module.Framework.Client;
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
        /// <param name="identity"></param>
        /// <param name="logger"></param>
        public ProgressPublisher(IClientAccessor events,
            IJsonSerializer serializer, IProcessInfo identity, ILogger logger)
            : base(logger)
        {
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _events = events ?? throw new ArgumentNullException(nameof(events));
            _identity = identity ?? throw new ArgumentNullException(nameof(identity));
            _channel = Channel.CreateUnbounded<DiscoveryProgressModel>();
            _sender = Task.Factory.StartNew(() => SendProgressAsync(),
                default, TaskCreationOptions.LongRunning, TaskScheduler.Default).Unwrap();
        }

        /// <inheritdoc/>
        protected override void Send(DiscoveryProgressModel progress)
        {
            progress.DiscovererId = PublisherModelEx.CreatePublisherId(
                _identity.ProcessId, _identity.Id);
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
                var client = _events.Client;
                if (client == null)
                {
                    continue;
                }
                try
                {
                     await client.SendEventAsync(string.Empty,
                         _serializer.SerializeToMemory((object)progress),
                         Encoding.UTF8.WebName, ContentMimeType.Json,
                         MessageSchemaTypes.DiscoveryMessage).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send discovery progress.");
                }
            }
        }

        private readonly ILogger _logger;
        private readonly IJsonSerializer _serializer;
        private readonly IProcessInfo _identity;
        private readonly Task _sender;
        private readonly Channel<DiscoveryProgressModel> _channel;
        private readonly IClientAccessor _events;
    }
}
