// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Encoders.PubSub
{
    using global::Avro;
    using Azure.IIoT.OpcUa.Encoders;
    using Opc.Ua;
    using System;
    using System.Linq;

    /// <summary>
    /// Data set message
    /// </summary>
    public class AvroDataSetMessage : BaseDataSetMessage
    {
        /// <summary>
        /// Message schema
        /// </summary>
        public Schema Schema { get; }

        /// <summary>
        /// Dataset writer name
        /// </summary>
        public string? DataSetWriterName { get; set; }

        /// <summary>
        /// Create message
        /// </summary>
        /// <param name="schema"></param>
        public AvroDataSetMessage(Schema schema)
        {
            Schema = schema;
        }

        /// <inheritdoc/>
        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(this, obj))
            {
                return true;
            }
            if (obj is not AvroDataSetMessage wrapper)
            {
                return false;
            }
            if (!base.Equals(obj))
            {
                return false;
            }
            if (!Utils.IsEqual(wrapper.DataSetWriterName, DataSetWriterName))
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
            hash.Add(DataSetWriterName);
            return hash.ToHashCode();
        }

        /// <inheritdoc/>
        internal virtual void Encode(AvroBinaryEncoder encoder, bool withHeader)
        {
            if (withHeader)
            {
                WriteDataSetMessageHeader(encoder);
            }

            // Write payload
            encoder.WriteDataSet(nameof(Payload), Payload);
        }

        /// <inheritdoc/>
        internal virtual bool TryDecode(AvroBinaryDecoder decoder, bool withHeader)
        {
            // Reset content
            DataSetMessageContentMask = 0;
            MessageType = MessageType.KeyFrame;
            DataSetWriterId = 0;
            DataSetWriterName = null;
            SequenceNumber = 0;
            MetaDataVersion = null;
            Timestamp = DateTime.MinValue;

            if (withHeader && !TryReadDataSetMessageHeader(decoder))
            {
                return false;
            }

            // Read payload
            Payload = decoder.ReadDataSet(nameof(Payload));
            return true;
        }

        /// <summary>
        /// Write data set message header
        /// </summary>
        /// <param name="encoder"></param>
        private void WriteDataSetMessageHeader(AvroBinaryEncoder encoder)
        {
            switch (MessageType)
            {
                case MessageType.KeyFrame:
                    encoder.WriteString(nameof(MessageType), "ua-keyframe");
                    break;
                case MessageType.Event:
                    encoder.WriteString(nameof(MessageType), "ua-event");
                    break;
                case MessageType.KeepAlive:
                    encoder.WriteString(nameof(MessageType), "ua-keepalive");
                    break;
                case MessageType.Condition:
                    encoder.WriteString(nameof(MessageType), "ua-condition");
                    break;
                case MessageType.DeltaFrame:
                    encoder.WriteString(nameof(MessageType), "ua-deltaframe");
                    break;
            }

            encoder.WriteString(nameof(DataSetWriterName), DataSetWriterName);
            encoder.WriteUInt16(nameof(DataSetWriterId), DataSetWriterId); // Do we need this?
            encoder.WriteUInt32(nameof(SequenceNumber), SequenceNumber);
            encoder.WriteEncodeable(nameof(MetaDataVersion), MetaDataVersion,
                typeof(ConfigurationVersionDataType));
            encoder.WriteDateTime(nameof(Timestamp), Timestamp ?? default);

            var status = Status ?? Payload.Values
                .FirstOrDefault(s => StatusCode.IsNotGood(
                    s?.StatusCode ?? StatusCodes.BadNoData))?.StatusCode ?? StatusCodes.Good;
            encoder.WriteStatusCode(nameof(Status), status);
        }

        /// <summary>
        /// Read the data set message header
        /// </summary>
        /// <param name="decoder"></param>
        /// <returns></returns>
        bool TryReadDataSetMessageHeader(AvroBinaryDecoder decoder)
        {
            var messageType = decoder.ReadString(nameof(MessageType));
            if (messageType != null)
            {
                if (messageType.Equals("ua-deltaframe", StringComparison.Ordinal))
                {
                    MessageType = MessageType.DeltaFrame;
                }
                else if (messageType.Equals("ua-event", StringComparison.Ordinal))
                {
                    MessageType = MessageType.Event;
                }
                else if (messageType.Equals("ua-keepalive", StringComparison.Ordinal))
                {
                    MessageType = MessageType.KeepAlive;
                }
                else if (messageType.Equals("ua-condition", StringComparison.Ordinal))
                {
                    MessageType = MessageType.Condition;
                }
                else if (messageType.Equals("ua-keyframe", StringComparison.Ordinal))
                {
                    MessageType = MessageType.KeyFrame;
                }
                else
                {
                    // Continue and treat this as payload.
                    return false;
                }
            }
            DataSetWriterName = decoder.ReadString(nameof(DataSetWriterName));
            DataSetWriterId = decoder.ReadUInt16(nameof(DataSetWriterId));// Do we need this?
            SequenceNumber = decoder.ReadUInt32(nameof(SequenceNumber));
            MetaDataVersion = (ConfigurationVersionDataType?)decoder.ReadEncodeable(
                    nameof(MetaDataVersion), typeof(ConfigurationVersionDataType));
            Timestamp = decoder.ReadDateTime(nameof(Timestamp));
            Status = decoder.ReadStatusCode(nameof(Status));
            return true;
        }
    }
}
