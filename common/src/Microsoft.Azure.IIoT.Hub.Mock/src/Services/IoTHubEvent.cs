// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Hub.Mock
{
    using Microsoft.Azure.IIoT.Hub.Models;
    using Microsoft.Azure.IIoT.Messaging;
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// IoT Hub Event messages
    /// </summary>
    public sealed class IoTHubEvent : IEvent
    {
        /// <summary>
        /// Create event
        /// </summary>
        /// <param name="device"></param>
        /// <param name="message"></param>
        internal IoTHubEvent(DeviceModel device,
            IoTHubClientFactory.IoTHubClient.TelemetryMessage message)
        {
            Timestamp = message.GetTimestamp();
            ContentType = message.GetContentType();
            MessageSchema = message.GetMessageSchema();
            RoutingInfo = message.GetRoutingInfo();
            Topic = message.GetTopic();
            Buffers = message.GetBuffers();
            DeviceId = device.Id;
            ModuleId = device.ModuleId;
            Retain = message.GetRetain();
            Ttl = message.GetTtl();
        }

        /// <inheritdoc/>
        public DateTime Timestamp { get; private set; }

        /// <inheritdoc/>
        public IEvent SetTimestamp(DateTime value)
        {
            Timestamp = value;
            return this;
        }

        /// <inheritdoc/>
        public string ContentType { get; private set; }

        /// <inheritdoc/>
        public IEvent SetContentType(string value)
        {
            ContentType = value;
            return this;
        }

        /// <inheritdoc/>
        public string ContentEncoding { get; private set; }

        /// <inheritdoc/>
        public IEvent SetContentEncoding(string value)
        {
            ContentEncoding = value;
            return this;
        }

        /// <inheritdoc/>
        public string MessageSchema { get; private set; }

        /// <inheritdoc/>
        public IEvent SetMessageSchema(string value)
        {
            MessageSchema = value;
            return this;
        }

        /// <inheritdoc/>
        public string DeviceId { get; set; }

        /// <inheritdoc/>
        public string ModuleId { get; set; }

        /// <inheritdoc/>
        public string RoutingInfo { get; private set; }

        /// <inheritdoc/>
        public IEvent SetRoutingInfo(string value)
        {
            RoutingInfo = value;
            return this;
        }

        /// <inheritdoc/>
        public string Topic { get; private set; }

        /// <inheritdoc/>
        public IEvent SetTopic(string value)
        {
            Topic = value;
            return this;
        }

        /// <inheritdoc/>
        public bool Retain { get; private set; }

        /// <inheritdoc/>
        public IEvent SetRetain(bool value)
        {
            Retain = value;
            return this;
        }

        /// <inheritdoc/>
        public TimeSpan Ttl { get; private set; }

        /// <inheritdoc/>
        public IEvent SetTtl(TimeSpan value)
        {
            Ttl = value;
            return this;
        }

        /// <inheritdoc/>
        public IReadOnlyList<ReadOnlyMemory<byte>> Buffers { get; private set; }

        /// <inheritdoc/>
        public IEvent AddBuffers(IReadOnlyList<ReadOnlyMemory<byte>> value)
        {
            Buffers = value;
            return this;
        }

        /// <inheritdoc/>
        public void Dispose()
        {
        }

        /// <inheritdoc/>
        public Task SendAsync(CancellationToken ct = default)
        {
            // Cannot send this
            return Task.CompletedTask;
        }
    }
}
