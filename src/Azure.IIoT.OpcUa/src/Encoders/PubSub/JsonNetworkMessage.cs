// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Encoders.PubSub
{
    using Azure.IIoT.OpcUa.Encoders;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Azure.IIoT.OpcUa.Core;
    using System;
    using System.Buffers;
    using System.Collections.Generic;
    using System.IO;
    using System.IO.Compression;
    using System.Linq;
    using System.Text;

    /// <summary>
    /// Json Network message
    /// <see href="https://reference.opcfoundation.org/v104/Core/docs/Part14/7.2.3/"/>
    /// </summary>
    public class JsonNetworkMessage : BaseNetworkMessage
    {
        /// <inheritdoc/>
        public override string MessageSchema => HasSamplesPayload ?
            MessageSchemaTypes.MonitoredItemMessageJson : MessageSchemaTypes.NetworkMessageJson;

        /// <inheritdoc/>
        public override string ContentType => UseGzipCompression ?
            Encoders.ContentType.JsonGzip : ContentMimeType.Json;

        /// <inheritdoc/>
        public override string ContentEncoding => Encoding.UTF8.WebName;

        /// <summary>
        /// Ua data message type
        /// </summary>
        public const string MessageTypeUaData = "ua-data";

        /// <summary>
        /// Message id
        /// </summary>
        public Func<string> MessageId { get; set; } = () => Guid.NewGuid().ToString();

        /// <summary>
        /// Message type
        /// </summary>
        internal string MessageType { get; set; } = MessageTypeUaData;

        /// <summary>
        /// Get flag that indicates if message has network message header
        /// </summary>
        public bool HasNetworkMessageHeader
            => (NetworkMessageContentMask & NetworkMessageContentFlags.NetworkMessageHeader) != 0;

        /// <summary>
        /// Flag that indicates if the Network message contains a single dataset message
        /// </summary>
        public bool HasSingleDataSetMessage
            => (NetworkMessageContentMask & NetworkMessageContentFlags.SingleDataSetMessage) != 0;

        /// <summary>
        /// Flag that indicates if the Network message dataSets have header
        /// </summary>
        public bool HasDataSetMessageHeader
            => (NetworkMessageContentMask & NetworkMessageContentFlags.DataSetMessageHeader) != 0;

        /// <summary>
        /// Flag that indicates if the Network message payload is monitored item samples
        /// </summary>
        public bool HasSamplesPayload
        {
            get
            {
                if (_hasSamplesPayload == null)
                {
                    if (Messages.Count > 0)
                    {
                        _hasSamplesPayload = Messages.Any(m => m is MonitoredItemMessage);
                    }
                    else
                    {
                        return false;
                    }
                }
                return _hasSamplesPayload.Value;
            }
            set => _hasSamplesPayload = value;
        }

        /// <summary>
        /// Sets the message schema to use
        /// </summary>
        internal string? MessageSchemaToUse
        {
            get => MessageSchema;
            set
            {
                HasSamplesPayload = value?.Equals(
                    MessageSchemaTypes.MonitoredItemMessageJson, StringComparison.OrdinalIgnoreCase) == true;
            }
        }

        /// <summary>
        /// Flag that indicates if advanced encoding should be used
        /// </summary>
        public bool UseAdvancedEncoding { get; set; }

        /// <summary>
        /// Namespace format to use
        /// </summary>
        public NamespaceFormat NamespaceFormat { get; set; }

        /// <summary>
        /// Wrap the resulting message into an array. This is for legacy compatiblity
        /// where we used to encode a set of network messages in arrays. This is the
        /// default in OPC Publisher 2.+ if strict compliance with standard is not
        /// enabled.
        /// </summary>
        public bool UseArrayEnvelope { get; set; }

        /// <summary>
        /// Use gzip compression
        /// </summary>
        public bool UseGzipCompression { get; set; }

        /// <inheritdoc/>
        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(this, obj))
            {
                return true;
            }
            if (obj is not JsonNetworkMessage wrapper)
            {
                return false;
            }
            if (!base.Equals(obj))
            {
                return false;
            }
            if (!Opc.Ua.Utils.IsEqual(wrapper.MessageId(), MessageId()) ||
                !Opc.Ua.Utils.IsEqual(wrapper.DataSetWriterGroup, DataSetWriterGroup))
            {
                return false;
            }
            return true;
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            var hash = new HashCode();
            hash.Add(base.GetHashCode());
            hash.Add(MessageId);
            hash.Add(DataSetWriterGroup);
            return hash.ToHashCode();
        }


        // TODO(Phase 5): The JSON PubSub network message encode/decode paths were
        // built on the removed fork-specific JsonEncoderEx/JsonDecoderEx. They will be
        // reimplemented on the UA-.NETStandard 2.0 Opc.Ua stack (Opc.Ua.PubSub) in Phase 5.

        /// <inheritdoc/>
        public override bool TryDecode(Opc.Ua.IServiceMessageContext context, Stream stream,
            IDataSetMetaDataResolver? resolver)
        {
            throw new NotSupportedException(
                "JSON PubSub network message decoding is deferred to Phase 5.");
        }

        /// <inheritdoc/>
        public override bool TryDecode(Opc.Ua.IServiceMessageContext context,
            Queue<ReadOnlySequence<byte>> reader, IDataSetMetaDataResolver? resolver = null)
        {
            throw new NotSupportedException(
                "JSON PubSub network message decoding is deferred to Phase 5.");
        }

        /// <inheritdoc/>
        public override IReadOnlyList<ReadOnlySequence<byte>> Encode(Opc.Ua.IServiceMessageContext context,
            int maxChunkSize, IDataSetMetaDataResolver? resolver = null)
        {
            throw new NotSupportedException(
                "JSON PubSub network message encoding is deferred to Phase 5.");
        }
        private bool? _hasSamplesPayload;
    }
}
