// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Opc.Ua.PubSub {
    using Opc.Ua.Encoders;
    using System;

    /// <summary>
    /// Data set message
    /// </summary>
    public class JsonDataSetMessage : BaseDataSetMessage {

        /// <summary>
        /// Use capability mode when encoding and decoding
        /// </summary>
        public bool UseCompatibilityMode { get; set; }

        /// <inheritdoc/>
        public override void Encode(IEncoder encoder, bool withHeader, string property) {
            if (withHeader) {
                if ((DataSetMessageContentMask & (uint)JsonDataSetMessageContentMask.DataSetWriterId) != 0) {
                    if (!UseCompatibilityMode) {
                        encoder.WriteUInt16(nameof(DataSetWriterId), DataSetWriterId);
                    }
                    else {
                        // Up to version 2.8 we wrote the string id as id which is not per standard
                        encoder.WriteString(nameof(DataSetWriterId), DataSetWriterName);
                    }
                }
                if ((DataSetMessageContentMask & (uint)JsonDataSetMessageContentMask.SequenceNumber) != 0) {
                    encoder.WriteUInt32(nameof(SequenceNumber), SequenceNumber);
                }
                if ((DataSetMessageContentMask & (uint)JsonDataSetMessageContentMask.MetaDataVersion) != 0) {
                    encoder.WriteEncodeable(nameof(MetaDataVersion), MetaDataVersion, typeof(ConfigurationVersionDataType));
                }
                if ((DataSetMessageContentMask & (uint)JsonDataSetMessageContentMask.Timestamp) != 0) {
                    encoder.WriteDateTime(nameof(Timestamp), Timestamp);
                }
                if ((DataSetMessageContentMask & (uint)JsonDataSetMessageContentMask.Status) != 0) {
                    if (!UseCompatibilityMode) {
                        encoder.WriteUInt32(nameof(Status), Status.Code);
                    }
                    else {
                        // Up to version 2.8 we wrote the full status code
                        encoder.WriteStatusCode(nameof(Status), Status);
                    }
                }
                if ((DataSetMessageContentMask & (uint)JsonDataSetMessageContentMask.MessageType) != 0) {
                    switch (MessageType) {
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
                    (DataSetMessageContentMask & (uint)JsonDataSetMessageContentMask2.DataSetWriterName) != 0) {
                    encoder.WriteString(nameof(DataSetWriterName), DataSetWriterName);
                }
                WritePayload(encoder, nameof(Payload));
            }
            else {
                WritePayload(encoder, property);
            }
            void WritePayload(IEncoder encoder, string propertyName = null) {
                var jsonEncoder = encoder as JsonEncoderEx;
                if (jsonEncoder == null) {
                    throw new NotSupportedException("Other encoders than JsonEncoderEx are not supported yet.");
                }
                var useReversibleEncoding =
                    (DataSetMessageContentMask & (uint)JsonDataSetMessageContentMask2.ReversibleFieldEncoding) != 0;
                var prevReversibleEncoding = jsonEncoder.UseReversibleEncoding;
                try {
                    // if propertyname is null we are already inside the object
                    jsonEncoder.UseReversibleEncoding = useReversibleEncoding;
                    jsonEncoder.WriteDataSet(propertyName, Payload);
                }
                finally {
                    // Restore original reversible setting
                    jsonEncoder.UseReversibleEncoding = prevReversibleEncoding;
                }
            }
        }

        /// <inheritdoc/>
        public override void Decode(IDecoder decoder, uint dataSetFieldContentMask, bool withHeader, string property) {
            if (withHeader) {
                if ((DataSetMessageContentMask & (uint)JsonDataSetMessageContentMask.DataSetWriterId) != 0) {
                    if (!UseCompatibilityMode) {
                        DataSetWriterId = decoder.ReadUInt16(nameof(DataSetWriterId));
                    }
                    else {
                        // Up to version 2.8 we wrote the string id as id which is not per standard
                        DataSetWriterName = decoder.ReadString(nameof(DataSetWriterId));
                    }
                }
                if ((DataSetMessageContentMask & (uint)JsonDataSetMessageContentMask.SequenceNumber) != 0) {
                    SequenceNumber = decoder.ReadUInt32(nameof(SequenceNumber));
                }
                if ((DataSetMessageContentMask & (uint)JsonDataSetMessageContentMask.MetaDataVersion) != 0) {
                    MetaDataVersion = (ConfigurationVersionDataType)decoder.ReadEncodeable(
                        nameof(MetaDataVersion), typeof(ConfigurationVersionDataType));
                }
                if ((DataSetMessageContentMask & (uint)JsonDataSetMessageContentMask.Timestamp) != 0) {
                    Timestamp = decoder.ReadDateTime(nameof(Timestamp));
                }
                if ((DataSetMessageContentMask & (uint)JsonDataSetMessageContentMask.Status) != 0) {
                    if (!UseCompatibilityMode) {
                        Status = decoder.ReadUInt32(nameof(Status));
                    }
                    else {
                        // Up to version 2.8 we wrote the full status code
                        Status = decoder.ReadStatusCode(nameof(Status));
                    }
                }

                if ((DataSetMessageContentMask & (uint)JsonDataSetMessageContentMask.MessageType) != 0) {
                    var messageType = decoder.ReadString(nameof(MessageType));

                    if (messageType.Equals("ua-deltaframe")) {
                        MessageType = MessageType.DeltaFrame;
                    }
                    else if (messageType.Equals("ua-event")) {
                        MessageType = MessageType.Event;
                    }
                    else if (messageType.Equals("ua-keepalive")) {
                        MessageType = MessageType.KeepAlive;
                    }
                    else if (messageType.Equals("ua-condition")) {
                        MessageType = MessageType.Condition;
                    }
                    else {
                        // Default is key frame
                        MessageType = MessageType.KeyFrame;
                    }
                }
                if ((DataSetMessageContentMask & (uint)JsonDataSetMessageContentMask2.DataSetWriterName) != 0) {
                    DataSetWriterName = decoder.ReadString(nameof(DataSetWriterName));
                }
                ReadPayload(decoder, dataSetFieldContentMask, nameof(Payload));
            }
            else {
                ReadPayload(decoder, dataSetFieldContentMask, property);
            }
            void ReadPayload(IDecoder decoder, uint dataSetFieldContentMask, string propertyName) {
                var jsonDecoder = decoder as JsonDecoderEx;
                if (jsonDecoder == null) {
                    throw new NotSupportedException("Other decoders than JsonDecoderEx are not supported yet.");
                }
                Payload = jsonDecoder.ReadDataSet(propertyName);
                Payload.DataSetFieldContentMask = dataSetFieldContentMask;
            }
        }
    }
}