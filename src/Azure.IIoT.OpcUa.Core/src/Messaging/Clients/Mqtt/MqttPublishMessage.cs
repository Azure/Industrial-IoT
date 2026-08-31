// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Core.Messaging.Clients.Mqtt
{
    using System;
    using System.Buffers;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// An outbound mqtt application message assembled by <see cref="MqttEvent"/>
    /// or the rpc layer and handed to the transport for publishing.
    /// </summary>
    internal sealed class MqttPublishMessage
    {
        /// <summary>
        /// Topic to publish on
        /// </summary>
        public string Topic { get; set; } = string.Empty;

        /// <summary>
        /// Payload
        /// </summary>
        public ReadOnlySequence<byte> Payload { get; set; }

        /// <summary>
        /// Quality of service
        /// </summary>
        public QoS QoS { get; set; }

        /// <summary>
        /// Retain flag
        /// </summary>
        public bool Retain { get; set; }

        /// <summary>
        /// Content type (v5 only)
        /// </summary>
        public string? ContentType { get; set; }

        /// <summary>
        /// Response topic (v5 only)
        /// </summary>
        public string? ResponseTopic { get; set; }

        /// <summary>
        /// Correlation data (v5 only)
        /// </summary>
        public byte[]? CorrelationData { get; set; }

        /// <summary>
        /// Message expiry interval (v5 only)
        /// </summary>
        public uint? MessageExpiryIntervalSeconds { get; set; }

        /// <summary>
        /// User properties (v5 only)
        /// </summary>
        public List<KeyValuePair<string, string>>? UserProperties { get; set; }
    }

    /// <summary>
    /// An inbound mqtt application message surfaced to the rpc layer. Wraps the
    /// relevant v5 properties needed for request/response correlation.
    /// </summary>
    internal sealed class MqttInboundMessage
    {
        /// <summary>
        /// Topic the message arrived on
        /// </summary>
        public required string Topic { get; init; }

        /// <summary>
        /// Payload
        /// </summary>
        public ReadOnlySequence<byte> Payload { get; init; }

        /// <summary>
        /// Content type (v5)
        /// </summary>
        public string? ContentType { get; init; }

        /// <summary>
        /// Response topic (v5)
        /// </summary>
        public string? ResponseTopic { get; init; }

        /// <summary>
        /// Correlation data (v5)
        /// </summary>
        public byte[]? CorrelationData { get; init; }

        /// <summary>
        /// User properties (v5)
        /// </summary>
        public IReadOnlyList<KeyValuePair<string, string>>? UserProperties { get; init; }
    }

    /// <summary>
    /// Publisher abstraction used by <see cref="MqttEvent"/> to send assembled
    /// messages through the transport (and optionally through the schema
    /// publisher wrapper).
    /// </summary>
    internal interface IMqttPublisher
    {
        /// <summary>
        /// Publish an assembled message.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="schema"></param>
        /// <param name="ct"></param>
        ValueTask PublishAsync(MqttPublishMessage message, IEventSchema? schema,
            CancellationToken ct);
    }
}
