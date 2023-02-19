// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Opc.Ua.PubSub {
    using Opc.Ua.Encoders;
    using System;
    using System.Linq;

    /// <summary>
    /// Data set message
    /// </summary>
    public class JsonDataSetMessage : BaseDataSetMessage {

        /// <summary>
        /// Compatibility with 2.8 when encoding and decoding
        /// </summary>
        public bool UseCompatibilityMode { get; set; }

        /// <summary>
        /// Dataset writer name
        /// </summary>
        public string DataSetWriterName { get; set; }

        /// <inheritdoc/>
        public override bool Equals(object value) {
            if (ReferenceEquals(this, value)) {
                return true;
            }
            if (!(value is JsonDataSetMessage wrapper)) {
                return false;
            }
            if (!base.Equals(value)) {
                return false;
            }
            if (!Utils.IsEqual(wrapper.DataSetWriterName, DataSetWriterName)) {
                return false;
            }
            return true;
        }

        /// <inheritdoc/>
        public override int GetHashCode() {
            var hash = new HashCode();
            hash.Add(base.GetHashCode());
            hash.Add(DataSetWriterName);
            return hash.ToHashCode();
        }

        /// <inheritdoc/>
        internal virtual void Encode(JsonEncoderEx encoder, string publisherId, bool withHeader, string property) {
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
                    var status = Status ?? Payload.Values
                        .FirstOrDefault(s => StatusCode.IsNotGood(s.StatusCode))?.StatusCode ?? StatusCodes.Good;
                    if (!UseCompatibilityMode) {
                        encoder.WriteUInt32(nameof(Status), status.Code);
                    }
                    else {
                        // Up to version 2.8 we wrote the full status code
                        encoder.WriteStatusCode(nameof(Status), status);
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
            void WritePayload(JsonEncoderEx jsonEncoder, string propertyName = null) {
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
        internal virtual bool TryDecode(JsonDecoderEx jsonDecoder, string property, ref bool withHeader,
            ref string publisherId) {
            if (TryReadDataSetMessageHeader(jsonDecoder, out var dataSetMessageContentMask)) {
                withHeader |= true;
                DataSetMessageContentMask = dataSetMessageContentMask;
                Payload = jsonDecoder.ReadDataSet(nameof(Payload));
                return true;
            }
            else if (withHeader) {
                // Previously we found a header, not now, we fail here
                return false;
            }
            else {
                // Reset content
                DataSetMessageContentMask = 0;
                MessageType = MessageType.KeyFrame;
                DataSetWriterId = 0;
                DataSetWriterName = null;
                SequenceNumber = 0;
                MetaDataVersion = null;
                Timestamp = DateTime.MinValue;

                if (jsonDecoder.HasField(property)) {
                    // Read payload off of the property name
                    Payload = jsonDecoder.ReadDataSet(property);
                }
                else {
                    // Read the current object as dataset
                    Payload = jsonDecoder.ReadDataSet(null);
                }
                return true;
            }

            // Read the data set message header
            bool TryReadDataSetMessageHeader(JsonDecoderEx jsonDecoder, out uint dataSetMessageContentMask) {
                dataSetMessageContentMask = 0;
                if (jsonDecoder.HasField(nameof(DataSetWriterId))) {
                    DataSetWriterId = jsonDecoder.ReadUInt16(nameof(DataSetWriterId));
                    if (DataSetWriterId == 0) {
                        // Up to version 2.8 we wrote the string id as id which is not per standard
                        DataSetWriterName = jsonDecoder.ReadString(nameof(DataSetWriterId));
                        if (DataSetWriterName != null) {
                            UseCompatibilityMode = true;
                            dataSetMessageContentMask |= (uint)JsonDataSetMessageContentMask.DataSetWriterId;
                            dataSetMessageContentMask |= (uint)JsonDataSetMessageContentMask2.DataSetWriterName;
                        }
                        else {
                            // Continue and treat all of this as payload.
                            return false;
                        }
                    }
                    else {
                        dataSetMessageContentMask |= (uint)JsonDataSetMessageContentMask.DataSetWriterId;
                    }
                }

                if (jsonDecoder.HasField(nameof(MetaDataVersion))) {
                    MetaDataVersion = (ConfigurationVersionDataType)jsonDecoder.ReadEncodeable(
                        nameof(MetaDataVersion), typeof(ConfigurationVersionDataType));
                    if (MetaDataVersion != null) {
                        dataSetMessageContentMask |= (uint)JsonDataSetMessageContentMask.MetaDataVersion;
                    }
                    else {
                        // Continue and treat all of this as payload.
                        return false;
                    }
                }

                if (jsonDecoder.HasField(nameof(SequenceNumber))) {
                    SequenceNumber = jsonDecoder.ReadUInt32(nameof(SequenceNumber));
                    dataSetMessageContentMask |= (uint)JsonDataSetMessageContentMask.SequenceNumber;
                }

                if (jsonDecoder.HasField(nameof(Timestamp))) {
                    Timestamp = jsonDecoder.ReadDateTime(nameof(Timestamp));
                    dataSetMessageContentMask |= (uint)JsonDataSetMessageContentMask.Timestamp;
                }

                if (jsonDecoder.HasField(nameof(Status))) {
                    UseCompatibilityMode = jsonDecoder.IsObject(nameof(Status));
                    dataSetMessageContentMask |= (uint)JsonDataSetMessageContentMask.Status;
                    if (!UseCompatibilityMode) {
                        Status = jsonDecoder.ReadUInt32(nameof(Status));
                    }
                    else {
                        // Up to version 2.8 we wrote the string id as id which is not per standard
                        Status = jsonDecoder.ReadStatusCode(nameof(Status));
                    }
                }

                if (jsonDecoder.HasField(nameof(MessageType))) {
                    var messageType = jsonDecoder.ReadString(nameof(MessageType));
                    dataSetMessageContentMask |= (uint)JsonDataSetMessageContentMask.MessageType;

                    if (messageType != null) {
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
                        else if (messageType.Equals("ua-keyframe")) {
                            MessageType = MessageType.KeyFrame;
                        }
                        else {
                            // Continue and treat this as payload.
                            return false;
                        }
                    }
                }

                if (jsonDecoder.HasField(nameof(DataSetWriterName))) {
                    DataSetWriterName = jsonDecoder.ReadString(nameof(DataSetWriterName));
                    dataSetMessageContentMask |= (uint)JsonDataSetMessageContentMask2.DataSetWriterName;
                }
                return jsonDecoder.HasField(nameof(Payload));
            }
        }
    }
}