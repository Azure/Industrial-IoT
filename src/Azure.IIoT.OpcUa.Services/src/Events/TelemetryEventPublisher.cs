// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Sdk.Publisher.Clients
{
    using Azure.IIoT.OpcUa.Models;
    using Microsoft.Azure.IIoT.Messaging;
    using System;
    using System.Threading.Tasks;
    using Azure.IIoT.OpcUa.Services.Subscriber;
    using Azure.IIoT.OpcUa.Services;

    /// <summary>
    /// Telemetry message publishing
    /// </summary>
    /// <typeparam name="THub"></typeparam>
    public sealed class TelemetryEventPublisher<THub> : ISubscriberMessageProcessor
    {
        /// <summary>
        /// Create publisher
        /// </summary>
        /// <param name="callback"></param>
        public TelemetryEventPublisher(ICallbackInvokerT<THub> callback)
        {
            _callback = callback ?? throw new ArgumentNullException(nameof(callback));
        }

        /// <inheritdoc/>
        public async Task HandleSampleAsync(MonitoredItemMessageModel sample)
        {
            var arguments = new object[] { sample };
            if (!string.IsNullOrEmpty(sample.EndpointId))
            {
                // Send to endpoint listeners
                await _callback.MulticastAsync(sample.EndpointId,
                    EventTargets.PublisherSampleTarget, arguments).ConfigureAwait(false);
            }
        }
        /// <inheritdoc/>
        public async Task HandleMessageAsync(DataSetMessageModel message)
        {
            foreach (var datapoint in message.Payload)
            {
                var arguments = new object[] {
                     new MonitoredItemMessageModel {
                        Timestamp = message.Timestamp,
                        DataSetWriterId = message.DataSetWriterId,
                        PublisherId = message.PublisherId,
                        NodeId = datapoint.Key,
                        DisplayName = datapoint.Key,
                        Value = datapoint.Value?.Value?.Copy(),
                        Status = datapoint.Value?.Status,
                        SourceTimestamp = datapoint.Value?.SourceTimestamp,
                        SourcePicoseconds = datapoint.Value?.SourcePicoseconds,
                        ServerTimestamp = datapoint.Value?.ServerTimestamp,
                        ServerPicoseconds = datapoint.Value?.ServerPicoseconds,
                        DataType = datapoint.Value?.DataType,
                        SequenceNumber = message.SequenceNumber,
                        EndpointId = null // TODO Remove
                    }
                };
                if (!string.IsNullOrEmpty(message.DataSetWriterId))
                {
                    // Send to endpoint listeners
                    await _callback.MulticastAsync(message.DataSetWriterId,
                        EventTargets.PublisherSampleTarget, arguments).ConfigureAwait(false);
                }
            }
        }

        private readonly ICallbackInvoker _callback;
    }
}
