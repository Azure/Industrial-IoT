// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Core.Messaging.Clients
{
    using Azure.IIoT.OpcUa.Core.Messaging;
    using System;
    using System.Buffers;
    using System.Collections.Generic;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Nil output client
    /// </summary>
    public sealed class NullEventClient : IEvent, IEventClient, IEventClientCapabilities
    {
        /// <inheritdoc/>
        public string Name => "NULL";
        /// <inheritdoc/>
        public int MaxEventPayloadSizeInBytes => int.MaxValue;
        /// <inheritdoc/>
        public string Identity => Dns.GetHostName();

        /// <inheritdoc/>
        public EventClientCapabilities Capabilities => 0;

        /// <inheritdoc/>
        public IEvent CreateEvent()
        {
            return this;
        }

        /// <inheritdoc/>
        public ValueTask SendAsync(CancellationToken ct)
        {
            return ValueTask.CompletedTask;
        }

        /// <inheritdoc/>
        public void Dispose()
        {
        }

        /// <inheritdoc/>
        public IEvent AsCloudEvent(CloudEventHeader header)
        {
            return this;
        }

        /// <inheritdoc/>
        public IEvent SetQoS(QoS value)
        {
            return this;
        }

        /// <inheritdoc/>
        public IEvent AddBuffers(IEnumerable<ReadOnlySequence<byte>> value)
        {
            return this;
        }

        /// <inheritdoc/>
        public IEvent AddProperty(string name, string? value)
        {
            return this;
        }

        /// <inheritdoc/>
        public IEvent SetContentEncoding(string? value)
        {
            return this;
        }

        /// <inheritdoc/>
        public IEvent SetSchema(IEventSchema schema)
        {
            return this;
        }

        /// <inheritdoc/>
        public IEvent SetContentType(string? value)
        {
            return this;
        }

        /// <inheritdoc/>
        public IEvent SetRetain(bool value)
        {
            return this;
        }

        /// <inheritdoc/>
        public IEvent SetTimestamp(DateTimeOffset value)
        {
            return this;
        }

        /// <inheritdoc/>
        public IEvent SetTopic(string? value)
        {
            return this;
        }

        /// <inheritdoc/>
        public IEvent SetTtl(TimeSpan value)
        {
            return this;
        }
    }
}
