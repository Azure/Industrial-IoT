// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Encoders.PubSub
{
    using Azure.IIoT.OpcUa.Encoders;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Data set message
    /// </summary>
    public class JsonDataSetMessage : BaseDataSetMessage
    {
        /// <summary>
        /// Compatibility with 2.8 when encoding and decoding
        /// </summary>
        public bool UseCompatibilityMode { get; set; }

        /// <summary>
        /// Dataset writer name
        /// </summary>
        public string? DataSetWriterName { get; set; }

        /// <inheritdoc/>
        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(this, obj))
            {
                return true;
            }
            if (obj is not JsonDataSetMessage wrapper)
            {
                return false;
            }
            if (!base.Equals(obj))
            {
                return false;
            }
            if (!Opc.Ua.Utils.IsEqual(wrapper.DataSetWriterName, DataSetWriterName))
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
        internal virtual void Encode(JsonEncoderEx encoder, string? publisherId, bool withHeader,
            string? property)
        {
            if (withHeader)
            {
                if ((DataSetMessageContentMask & DataSetMessageContentFlags.DataSetWriterId) != 0)
                {
                    if (!UseCompatibilityMode)
                    {
                        encoder.WriteUInt16(nameof(DataSetWriterId), DataSetWriterId);
                    }
                    else
                    {
                        // Up to version 2.8 we wrote the string id as id which is not per standard
                        encoder.WriteString(nameof(DataSetWriterId), DataSetWriterName);
                    }
                }
                if ((DataSetMessageContentMask & DataSetMessageContentFlags.SequenceNumber) != 0)
                {
                    encoder.WriteUInt32(nameof(SequenceNumber), SequenceNumber);
                }
                if ((DataSetMessageContentMask & DataSetMessageContentFlags.MetaDataVersion) != 0)
                {
                    encoder.WriteEncodeable(nameof(MetaDataVersion), MetaDataVersion,
                        typeof(Opc.Ua.ConfigurationVersionDataType));
                }
                if ((DataSetMessageContentMask & DataSetMessageContentFlags.Timestamp) != 0)
                {
                    encoder.WriteDateTime(nameof(Timestamp), Timestamp?.UtcDateTime ?? default);
                }
                if ((DataSetMessageContentMask & DataSetMessageContentFlags.Status) != 0)
                {
                    var status = Status ?? Payload.DataSetFields
                        .FirstOrDefault(s => Opc.Ua.StatusCode.IsNotGood(s.Value?.StatusCode ??
                            Opc.Ua.StatusCodes.BadNoData)).Value?.StatusCode ?? Opc.Ua.StatusCodes.Good;
                    if (!UseCompatibilityMode)
                    {
                        encoder.WriteUInt32(nameof(Status), status.Code);
                    }
                    else
                    {
                        // Up to version 2.8 we wrote the full status code
                        encoder.WriteStatusCode(nameof(Status), status);
                    }
                }
                if ((DataSetMessageContentMask & DataSetMessageContentFlags.MessageType) != 0)
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
                }
                if (!UseCompatibilityMode &&
                    (DataSetMessageContentMask & DataSetMessageContentFlags.DataSetWriterName) != 0)
                {
                    encoder.WriteString(nameof(DataSetWriterName), DataSetWriterName);
                }
                WritePayload(encoder, nameof(Payload));
            }
            else
            {
                WritePayload(encoder, property);
            }

            void WritePayload(JsonEncoderEx jsonEncoder, string? propertyName = null)
            {
                var useReversibleEncoding =
                    (DataSetMessageContentMask & DataSetMessageContentFlags.ReversibleFieldEncoding) != 0;
                var prevReversibleEncoding = jsonEncoder.UseReversibleEncoding;
                try
                {
                    jsonEncoder.UseReversibleEncoding = useReversibleEncoding;

                    // if propertyname is null we are already inside the object
                    jsonEncoder.WriteDataSet(propertyName, Payload);
                }
                finally
                {
                    // Restore original reversible setting
                    jsonEncoder.UseReversibleEncoding = prevReversibleEncoding;
                }
            }
        }

        /// <inheritdoc/>
        internal virtual bool TryDecode(JsonDecoderEx jsonDecoder, string? property, ref bool withHeader,
            ref string? publisherId)
        {
            if (TryReadDataSetMessageHeader(jsonDecoder, out var dataSetMessageContentMask))
            {
                withHeader |= true;
                DataSetMessageContentMask = dataSetMessageContentMask;
                var payload = jsonDecoder.ReadDataSet(nameof(Payload));
                if (payload != null)
                {
                    Payload = payload;
                }
                return true;
            }
            else if (withHeader)
            {
                // Previously we found a header, not now, we fail here
                return false;
            }
            else
            {
                // Reset content
                DataSetMessageContentMask = 0;
                MessageType = MessageType.KeyFrame;
                DataSetWriterId = 0;
                DataSetWriterName = null;
                SequenceNumber = 0;
                MetaDataVersion = null;
                Timestamp = DateTimeOffset.MinValue;

                var payload = property != null && jsonDecoder.HasField(property) ?
                    // Read payload off of the property name
                    jsonDecoder.ReadDataSet(property) :
                    // Read the current object as dataset
                    jsonDecoder.ReadDataSet(null);

                if (payload != null)
                {
                    Payload = payload;
                }
                return true;
            }

            // Read the data set message header
            bool TryReadDataSetMessageHeader(JsonDecoderEx jsonDecoder, out DataSetMessageContentFlags dataSetMessageContentMask)
            {
                dataSetMessageContentMask = 0;
                if (jsonDecoder.HasField(nameof(DataSetWriterId)))
                {
                    DataSetWriterId = jsonDecoder.ReadUInt16(nameof(DataSetWriterId));
                    if (DataSetWriterId == 0)
                    {
                        // Up to version 2.8 we wrote the string id as id which is not per standard
                        DataSetWriterName = jsonDecoder.ReadString(nameof(DataSetWriterId));
                        if (DataSetWriterName != null)
                        {
                            UseCompatibilityMode = true;
                            dataSetMessageContentMask |= DataSetMessageContentFlags.DataSetWriterId;
                            dataSetMessageContentMask |= DataSetMessageContentFlags.DataSetWriterName;
                        }
                        else
                        {
                            // Continue and treat all of this as payload.
                            return false;
                        }
                    }
                    else
                    {
                        dataSetMessageContentMask |= DataSetMessageContentFlags.DataSetWriterId;
                    }
                }

                if (jsonDecoder.HasField(nameof(MetaDataVersion)))
                {
                    MetaDataVersion = (Opc.Ua.ConfigurationVersionDataType?)jsonDecoder.ReadEncodeable(
                        nameof(MetaDataVersion), typeof(Opc.Ua.ConfigurationVersionDataType));
                    if (MetaDataVersion != null)
                    {
                        dataSetMessageContentMask |= DataSetMessageContentFlags.MetaDataVersion;
                    }
                    else
                    {
                        // Continue and treat all of this as payload.
                        return false;
                    }
                }

                if (jsonDecoder.HasField(nameof(SequenceNumber)))
                {
                    SequenceNumber = jsonDecoder.ReadUInt32(nameof(SequenceNumber));
                    dataSetMessageContentMask |= DataSetMessageContentFlags.SequenceNumber;
                }

                if (jsonDecoder.HasField(nameof(Timestamp)))
                {
                    Timestamp = jsonDecoder.ReadDateTime(nameof(Timestamp));
                    dataSetMessageContentMask |= DataSetMessageContentFlags.Timestamp;
                }

                if (jsonDecoder.HasField(nameof(Status)))
                {
                    UseCompatibilityMode = jsonDecoder.IsObject(nameof(Status));
                    dataSetMessageContentMask |= DataSetMessageContentFlags.Status;
                    if (!UseCompatibilityMode)
                    {
                        Status = jsonDecoder.ReadUInt32(nameof(Status));
                    }
                    else
                    {
                        // Up to version 2.8 we wrote the string id as id which is not per standard
                        Status = jsonDecoder.ReadStatusCode(nameof(Status));
                    }
                }

                if (jsonDecoder.HasField(nameof(MessageType)))
                {
                    var messageType = jsonDecoder.ReadString(nameof(MessageType));
                    dataSetMessageContentMask |= DataSetMessageContentFlags.MessageType;

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
                }

                if (jsonDecoder.HasField(nameof(DataSetWriterName)))
                {
                    DataSetWriterName = jsonDecoder.ReadString(nameof(DataSetWriterName));
                    dataSetMessageContentMask |= DataSetMessageContentFlags.DataSetWriterName;
                }
                return jsonDecoder.HasField(nameof(Payload));
            }
        }
    }
}
