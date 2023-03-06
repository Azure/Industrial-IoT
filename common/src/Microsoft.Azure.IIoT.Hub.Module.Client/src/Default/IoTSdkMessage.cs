// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Module.Framework.Client
{
    using Microsoft.Azure.IIoT.Hub;
    using Microsoft.Azure.IIoT.Messaging;
    using Microsoft.Azure.Devices.Client;
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Message
    /// </summary>
    internal abstract class IoTSdkMessage : IEvent
    {
        /// <inheritdoc/>
        public IEvent SetContentType(string value)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                _template.ContentType = value;
                _template.Properties.AddOrUpdate(SystemProperties.MessageSchema, value);
            }
            return this;
        }

        /// <inheritdoc/>
        public IEvent SetContentEncoding(string value)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                _template.ContentEncoding = value;
                _template.Properties.AddOrUpdate(CommonProperties.ContentEncoding, value);
            }
            return this;
        }

        /// <inheritdoc/>
        public IEvent SetMessageSchema(string value)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                _template.Properties.AddOrUpdate(CommonProperties.EventSchemaType, value);
            }
            return this;
        }

        /// <inheritdoc/>
        public IEvent SetRoutingInfo(string value)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                _template.Properties.AddOrUpdate(CommonProperties.RoutingInfo, value);
            }
            return this;
        }

        /// <inheritdoc/>
        protected IReadOnlyList<ReadOnlyMemory<byte>> GetBuffers()
        {
            return _buffers;
        }

        /// <inheritdoc/>
        public IEvent AddBuffers(IReadOnlyList<ReadOnlyMemory<byte>> value)
        {
            _buffers.AddRange(value);
            return this;
        }

        /// <inheritdoc/>
        protected string GetTopic()
        {
            return _topic;
        }

        /// <inheritdoc/>
        public IEvent SetTopic(string value)
        {
            _topic = value;
            return this;
        }

        /// <inheritdoc/>
        public IEvent SetRetain(bool value)
        {
            return this;
        }

        /// <inheritdoc/>
        public IEvent SetTtl(TimeSpan value)
        {
            return this;
        }
        /// <inheritdoc/>
        public IEvent SetTimestamp(DateTime value)
        {
            return this;
        }

        /// <inheritdoc />
        public abstract Task SendAsync(CancellationToken ct);

        /// <inheritdoc/>
        public void Dispose()
        {
            // TODO: Return to pool
            _buffers.Clear();
            _template.Dispose();
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Build message
        /// </summary>
        internal IReadOnlyList<Message> AsMessages()
        {
            return _buffers.ConvertAll(m => _template.CloneWithBody(m.ToArray()));
        }

        private readonly List<ReadOnlyMemory<byte>> _buffers = new();
        private readonly Message _template = new();
        private string _topic;
    }
}
