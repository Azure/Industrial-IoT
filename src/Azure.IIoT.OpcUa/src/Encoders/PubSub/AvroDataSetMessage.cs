// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Encoders.PubSub
{
    using Avro;
    using Azure.IIoT.OpcUa.Encoders;
    using Opc.Ua;
    using System;
    using System.Diagnostics;
    using System.Linq;

    /// <summary>
    /// Avro binary data set message
    /// </summary>
    public class AvroDataSetMessage : BaseDataSetMessage
    {
        /// <summary>
        /// Dataset writer name
        /// </summary>
        public string? DataSetWriterName { get; set; }

        /// <summary>
        /// Dataset name
        /// </summary>
        public string? DataSetName { get; set; }

        /// <summary>
        /// Dataset header
        /// </summary>
        internal bool WithDataSetHeader { get; set; }

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
            if (!Utils.IsEqual(wrapper.DataSetName, DataSetName))
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
        internal virtual void Encode(BaseAvroEncoder encoder, bool withDataSetHeader)
        {
            WithDataSetHeader = withDataSetHeader;

            //
            // Messages can be either written in the context of an array
            // or as a single message or as one of a union of possible
            // messages. Only if we are in a union we need to write the
            // union index. Also, we need to get the type name from the
            // schema so that we can properly validate we are writing the
            // type. This can be optimized in the future.
            //
            var typeName = DataSetName ?? nameof(AvroDataSetMessage);
            if (encoder is AvroEncoder schemas)
            {
                var currentSchema = schemas.Current;
                if (currentSchema is ArraySchema arr)
                {
                    // Writing an array of messages
                    currentSchema = arr.ItemSchema;
                }
                if (currentSchema is UnionSchema unionSchema)
                {
                    encoder.WriteUnion(DataSetWriterId);
                    currentSchema = unionSchema[DataSetWriterId];
                }
                typeName = currentSchema.Name;
            }

            if (!WithDataSetHeader)
            {
                // If no header, write payload record directly
                encoder.WriteDataSet(null, Payload);
                return;
            }

            // If header, write data set message
            encoder.WriteObject(null, typeName, () =>
            {
                WriteDataSetMessageHeader(encoder);
                // Write payload
                encoder.WriteDataSet(nameof(Payload), Payload);
            });
        }

        /// <inheritdoc/>
        internal virtual bool TryDecode(AvroDecoder decoder, bool withDataSetHeader)
        {
            // Reset content
            DataSetMessageContentMask = 0;
            MessageType = MessageType.KeyFrame;
            DataSetWriterId = 0;
            DataSetWriterName = null;
            SequenceNumber = 0;
            MetaDataVersion = null;
            Timestamp = DateTime.MinValue;
            WithDataSetHeader = withDataSetHeader;

            //
            // Messages can be either written in the context of an array
            // or as a single message or as one of a union of possible
            // messages. Only if we are in a union we need to write the
            // union index. Also, we need to get the type name from the
            // schema so that we can properly validate we are writing the
            // type. This can be optimized in the future.
            //
            var current = decoder.Current;
            if (current is ArraySchema arr)
            {
                // Reading in the context of an array schema
                current = arr.ItemSchema;
            }
            if (current is UnionSchema union)
            {
                // Read union: TODO: this does not work, we need to read this when reading the object
                var unionId = decoder.ReadUnion();
            }

            // Save the current offset from which we read
            current = decoder.Current;

            // Try first to read the object with header
            if (withDataSetHeader)
            {
                var result = decoder.ReadObject(null, schema =>
                {
                    if (schema is not RecordSchema recordSchema)
                    {
                        return false;
                    }

                    if (recordSchema.Fields.Count > 0 &&
                        recordSchema.Fields[0].Name == nameof(MessageType))
                    {
                        WithDataSetHeader = true;
                        DataSetName = recordSchema.Name;
                        if (DataSetName == nameof(AvroDataSetMessage))
                        {
                            DataSetName = null;
                        }
                        if (!TryReadDataSetMessageHeader(decoder))
                        {
                            return false;
                        }
                        // Read payload
                        Payload = decoder.ReadDataSet(nameof(Payload));
                        return true;
                    }
                    return (bool?)null;
                });

                if (result.HasValue)
                {
                    return result.Value;
                }
            }

            // Fall back to read the data set
            decoder.Push(current);
            WithDataSetHeader = false;
            Payload = decoder.ReadDataSet(null);
            return true;
        }

        /// <summary>
        /// Write data set message header
        /// </summary>
        /// <param name="encoder"></param>
        private void WriteDataSetMessageHeader(BaseAvroEncoder encoder)
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
        bool TryReadDataSetMessageHeader(AvroDecoder decoder)
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
