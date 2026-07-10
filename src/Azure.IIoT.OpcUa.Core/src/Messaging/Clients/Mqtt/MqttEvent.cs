// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Core.Messaging.Clients.Mqtt
{
    using System;
    using System.Buffers;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Mqtt event. Assembles an application message and publishes one message per
    /// added buffer. Behavior (user property names, v3.11 restrictions, topic
    /// length check) matches the former Legacy.Extensions.Mqtt implementation.
    /// </summary>
    internal sealed class MqttEvent : IEvent
    {
        /// <summary>
        /// Create message
        /// </summary>
        /// <param name="version"></param>
        /// <param name="defaultQoS"></param>
        /// <param name="publisher"></param>
        public MqttEvent(MqttVersion version, QoS defaultQoS, IMqttPublisher publisher)
        {
            _version = version;
            _publisher = publisher;
            _message.QoS = defaultQoS;
        }

        /// <inheritdoc/>
        public IEvent AsCloudEvent(CloudEventHeader header)
        {
            if (_version != MqttVersion.v311)
            {
                AddUserProperty("specversion", "1.0");
                AddUserProperty("id", header.Id);
                AddUserProperty("source", header.Source.ToString());
                AddUserProperty("type", header.Type);
                if (header.Time != null)
                {
                    AddUserProperty("time", header.Time.ToString()!);
                }
                if (header.DataContentType != null)
                {
                    AddUserProperty("datacontenttype", header.DataContentType);
                }
                if (header.Subject != null)
                {
                    AddUserProperty("subject", header.Subject);
                }
            }
            return this;
        }

        /// <inheritdoc/>
        public IEvent SetContentEncoding(string? value)
        {
            if (_version != MqttVersion.v311 && !string.IsNullOrWhiteSpace(value))
            {
                AddUserProperty("ContentEncoding", value);
            }
            return this;
        }

        /// <inheritdoc/>
        public IEvent SetSchema(IEventSchema schema)
        {
            _schema = schema;
            return this;
        }

        /// <inheritdoc/>
        public IEvent SetContentType(string? value)
        {
            if (_version != MqttVersion.v311 && !string.IsNullOrWhiteSpace(value))
            {
                _message.ContentType = value;
            }
            return this;
        }

        /// <inheritdoc/>
        public IEvent SetQoS(QoS value)
        {
            _message.QoS = value;
            return this;
        }

        /// <inheritdoc/>
        public IEvent AddProperty(string name, string? value)
        {
            if (_version != MqttVersion.v311 && !string.IsNullOrWhiteSpace(value))
            {
                AddUserProperty(name, value);
            }
            return this;
        }

        /// <inheritdoc/>
        public IEvent SetTtl(TimeSpan value)
        {
            if (_version != MqttVersion.v311)
            {
                _message.MessageExpiryIntervalSeconds = (uint)value.TotalSeconds;
            }
            return this;
        }

        /// <inheritdoc/>
        public IEvent SetTopic(string? value)
        {
            if (value != null)
            {
                if (value.Length > 4096)
                {
                    var topicLength = Encoding.UTF8.GetByteCount(value);
                    const int kMaxTopicLength = 0xffff;
                    if (topicLength > kMaxTopicLength)
                    {
                        throw new ArgumentException(
                            "Topic for MQTT message cannot be larger than " +
                            $"{kMaxTopicLength} bytes, but current length " +
                            $"is {topicLength}.", nameof(value));
                    }
                }
                _message.Topic = value;
            }
            return this;
        }

        /// <inheritdoc/>
        public IEvent SetRetain(bool value)
        {
            _message.Retain = value;
            return this;
        }

        /// <inheritdoc/>
        public IEvent SetTimestamp(DateTimeOffset value)
        {
            if (_version != MqttVersion.v311)
            {
                AddUserProperty("TimeStamp",
                    value.ToString(CultureInfo.InvariantCulture));
            }
            return this;
        }

        /// <inheritdoc/>
        public IEvent AddBuffers(IEnumerable<ReadOnlySequence<byte>> value)
        {
            _buffers.AddRange(value);
            return this;
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            _buffers.Clear();
        }

        /// <inheritdoc/>
        public async ValueTask SendAsync(CancellationToken ct = default)
        {
            if (_buffers.Count == 0)
            {
                _message.Payload = ReadOnlySequence<byte>.Empty;
                await _publisher.PublishAsync(_message, _schema, ct).ConfigureAwait(false);
                return;
            }
            foreach (var buffer in _buffers)
            {
                _message.Payload = buffer;
                await _publisher.PublishAsync(_message, _schema, ct).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Add a user property
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        private void AddUserProperty(string name, string value)
        {
            (_message.UserProperties ??= []).Add(
                new KeyValuePair<string, string>(name, value));
        }

        private IEventSchema? _schema;
        private readonly List<ReadOnlySequence<byte>> _buffers = [];
        private readonly MqttPublishMessage _message = new();
        private readonly IMqttPublisher _publisher;
        private readonly MqttVersion _version;
    }
}
