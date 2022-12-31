// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Opc.Ua.PubSub {
    using System;
    using System.Linq;
    using System.Xml;

    /// <summary>
    /// Data set message
    /// </summary>
    public class UadpDataSetMessage : BaseDataSetMessage {

        /// <summary>
        /// Get and set the configured size of the message
        /// </summary>
        public ushort ConfiguredSize { get; set; }

        /// <summary>
        /// Get and set the DataSetOffset of the message
        /// </summary>
        public ushort DataSetOffset { get; set; }

        /// <summary>
        /// Get or set decoded payload size (hold it here for now)
        /// </summary>
        internal ushort PayloadSizeInStream { get; set; }

        /// <summary>
        /// Get and Set the startPosition in decoder
        /// </summary>
        internal int StartPositionInStream { get; set; }

        /// <summary>
        /// Sets metadata for the dataset
        /// </summary>
        public DataSetMetaDataType MetaData { get; set; }

        /// <summary>
        /// Get or set timestamp pico seconds
        /// </summary>
        public ushort PicoSeconds { get; set; }

        /// <summary>
        /// The possible values for the DataSetFlags1 encoding byte.
        /// </summary>
        [Flags]
        internal enum DataSetFlags1EncodingMask : byte {
            None = 0,
            MessageIsValid = 1,
            FieldTypeVariant = 1,
            FieldTypeRawData = 2,
            FieldTypeDataValue = 4,
            FieldTypeReserved = 6,
            DataSetMessageSequenceNumber = 8,
            Status = 16,
            ConfigurationVersionMajorVersion = 32,
            ConfigurationVersionMinorVersion = 64,
            DataSetFlags2 = 128,

            DataSetFlags1UsedBits = 0xF9,
            FieldTypeUsedBits = 0x7
        }

        /// <summary>
        /// The possible values for the DataSetFlags2 encoding byte.
        /// </summary>
        [Flags]
        internal enum DataSetFlags2EncodingMask : byte {
            DataKeyFrame = 0,
            DataDeltaFrame = 1,
            Event = 2,
            KeepAlive = 3,
            Timestamp = 16,
            PicoSeconds = 32,
            Reserved = 64,
            ReservedForExtendedFlags = 128,

            MessageTypeBits = 0x7
        }

        /// <summary>
        /// Get DataSetFlags1
        /// </summary>
        internal DataSetFlags1EncodingMask DataSetFlags1 {
            get {
                if (_dataSetFlags1 == null) {
                    _dataSetFlags1 = DataSetFlags1EncodingMask.MessageIsValid;

                    // DataSetFlags1: Bit range 1-2: Field Encoding
                    if (Payload.DataSetFieldContentMask == 0) {
                        _dataSetFlags1 |= DataSetFlags1EncodingMask.FieldTypeVariant;
                    }
                    else if ((Payload.DataSetFieldContentMask & (uint)DataSetFieldContentMask.RawData) != 0) {
                        _dataSetFlags1 |= DataSetFlags1EncodingMask.FieldTypeRawData;
                    }
                    else {
                        _dataSetFlags1 |= DataSetFlags1EncodingMask.FieldTypeDataValue;
                    }

                    if ((DataSetMessageContentMask & (uint)UadpDataSetMessageContentMask.SequenceNumber) != 0) {
                        // DataSetFlags1: Bit range 3: sequence
                        _dataSetFlags1 |= DataSetFlags1EncodingMask.DataSetMessageSequenceNumber;
                    }
                    if ((DataSetMessageContentMask & (uint)UadpDataSetMessageContentMask.Status) != 0) {
                        // DataSetFlags1: Bit range 4: status
                        _dataSetFlags1 |= DataSetFlags1EncodingMask.Status;
                    }
                    if ((DataSetMessageContentMask & (uint)UadpDataSetMessageContentMask.MajorVersion) != 0) {
                        // DataSetFlags1: Bit range 5: major version
                        _dataSetFlags1 |= DataSetFlags1EncodingMask.ConfigurationVersionMajorVersion;
                    }
                    if ((DataSetMessageContentMask & (uint)UadpDataSetMessageContentMask.MinorVersion) != 0) {
                        // DataSetFlags1: Bit range 6: minor version
                        _dataSetFlags1 |= DataSetFlags1EncodingMask.ConfigurationVersionMinorVersion;
                    }

                    // DataSetFlags1: Bit 7 if needed
                    if ((DataSetMessageContentMask & (uint)(UadpDataSetMessageContentMask.Timestamp |
                                                            UadpDataSetMessageContentMask.PicoSeconds)) != 0) {
                        _dataSetFlags1 |= DataSetFlags1EncodingMask.DataSetFlags2;
                    }
                    if (MessageType != MessageType.KeyFrame) {
                        _dataSetFlags1 |= DataSetFlags1EncodingMask.DataSetFlags2;
                    }
                }
                return _dataSetFlags1.Value;
            }
            private set {
                _dataSetFlags1 = value;
                if ((value & DataSetFlags1EncodingMask.MessageIsValid) != 0) {

                    // DataSetFlags1: Bit range 1-2: Field Encoding
                    if ((value & DataSetFlags1EncodingMask.FieldTypeRawData) != 0) {
                        Payload.DataSetFieldContentMask = (uint)DataSetFieldContentMask.RawData;
                    }
                    else if ((value & DataSetFlags1EncodingMask.FieldTypeDataValue) != 0) {
                        Payload.DataSetFieldContentMask = (uint)(DataSetFieldContentMask.StatusCode
                                                          | DataSetFieldContentMask.SourceTimestamp
                                                          | DataSetFieldContentMask.ServerTimestamp
                                                          | DataSetFieldContentMask.SourcePicoSeconds
                                                          | DataSetFieldContentMask.ServerPicoSeconds);
                    }
                    else {
                        Payload.DataSetFieldContentMask = 0;
                    }

                    // DataSetFlags1: Bit range 3: sequence
                    if ((value & DataSetFlags1EncodingMask.DataSetMessageSequenceNumber) != 0) {
                        DataSetMessageContentMask |= (uint)UadpDataSetMessageContentMask.SequenceNumber;
                    }
                    if ((value & DataSetFlags1EncodingMask.Status) != 0) {
                        // DataSetFlags1: Bit range 4: status
                        DataSetMessageContentMask |= (uint)UadpDataSetMessageContentMask.Status;
                    }
                    if ((value & DataSetFlags1EncodingMask.ConfigurationVersionMajorVersion) != 0) {
                        // DataSetFlags1: Bit range 5: major version
                        DataSetMessageContentMask |= (uint)UadpDataSetMessageContentMask.MajorVersion;
                    }
                    if ((value & DataSetFlags1EncodingMask.ConfigurationVersionMinorVersion) != 0) {
                        // DataSetFlags1: Bit range 6: minor version
                        DataSetMessageContentMask |= (uint)UadpDataSetMessageContentMask.MinorVersion;
                    }
                }
            }
        }

        /// <summary>
        /// Get DataSetFlags2
        /// </summary>
        internal DataSetFlags2EncodingMask DataSetFlags2 {
            get {
                if (_dataSetFlags2 == null) {
                    _dataSetFlags2 = 0;

                    // Bit range 0-3: DataSetMessage type
                    switch (MessageType) {
                        case MessageType.DeltaFrame:
                            _dataSetFlags2 |= DataSetFlags2EncodingMask.DataDeltaFrame;
                            break;
                        case MessageType.Event:
                            _dataSetFlags2 |= DataSetFlags2EncodingMask.Event;
                            break;
                        case MessageType.Condition:
                            _dataSetFlags2 |= (DataSetFlags2EncodingMask.Event | (DataSetFlags2EncodingMask)0x4);
                            break;
                        case MessageType.KeyFrame:
                            // Default is key frame
                            break;
                        default:
                            throw ServiceResultException.Create(StatusCodes.BadEncodingError,
                                "Message type {0} not valid for data set messages.", MessageType);
                    }

                    // Bit range 4-5: timestamp
                    if ((DataSetMessageContentMask & (uint)UadpDataSetMessageContentMask.Timestamp) != 0) {
                        _dataSetFlags2 |= DataSetFlags2EncodingMask.Timestamp;
                    }
                    if ((DataSetMessageContentMask & (uint)UadpDataSetMessageContentMask.PicoSeconds) != 0) {
                        _dataSetFlags2 |= DataSetFlags2EncodingMask.PicoSeconds;
                    }
                }
                return _dataSetFlags2.Value;
            }
            private set {
                _dataSetFlags2 = value;

                // Bit range 0-3: DataSetMessage type
                switch (value & DataSetFlags2EncodingMask.MessageTypeBits) {
                    case DataSetFlags2EncodingMask.DataDeltaFrame:
                        MessageType = MessageType.DeltaFrame;
                        break;
                    case DataSetFlags2EncodingMask.Event:
                        MessageType = MessageType.Event;
                        break;
                    case DataSetFlags2EncodingMask.KeepAlive:
                        MessageType = MessageType.KeepAlive;
                        break;
                    default:
                        // Default is key frame
                        MessageType = MessageType.KeyFrame;
                        break;
                }

                // Bit range 4-5: timestamp
                if ((value & DataSetFlags2EncodingMask.Timestamp) != 0) {
                    DataSetMessageContentMask |= (uint)UadpDataSetMessageContentMask.Timestamp;
                }
                if ((value & DataSetFlags2EncodingMask.PicoSeconds) != 0) {
                    DataSetMessageContentMask |= (uint)UadpDataSetMessageContentMask.PicoSeconds;
                }
            }
        }

        /// <inheritdoc/>
        public override void Decode(IDecoder decoder, uint dataSetFieldContentMask,
            bool withHeader, string property) {
            if (decoder is not BinaryDecoder binaryDecoder) {
                throw ServiceResultException.Create(StatusCodes.BadDecodingError, "Must use Binary decoder here");
            }
            if (withHeader) {
                ReadDataSetMessageHeader(binaryDecoder);
            }
            else {
                Payload.DataSetFieldContentMask = dataSetFieldContentMask;
            }
            if ((DataSetFlags2 & DataSetFlags2EncodingMask.DataDeltaFrame) != 0) {
                ReadMessageDataDeltaFrame(binaryDecoder);
            }
            else {
                ReadMessageDataKeyFrame(binaryDecoder);
            }
        }

        /// <inheritdoc/>
        public override void Encode(IEncoder encoder, bool withHeader, string property) {

            if (encoder is not BinaryEncoder binaryEncoder) {
                throw ServiceResultException.Create(StatusCodes.BadEncodingError, "Must use Binary encoder here");
            }

            StartPositionInStream = binaryEncoder.Position;
            if (DataSetOffset > 0 && StartPositionInStream < DataSetOffset) {
                StartPositionInStream = DataSetOffset;
                binaryEncoder.Position = DataSetOffset;
            }

            if (withHeader) {
                WriteDataSetMessageHeader(binaryEncoder);
            }

            if ((DataSetFlags2 & DataSetFlags2EncodingMask.DataDeltaFrame) != 0) {
                WritePayloadDeltaFrame(binaryEncoder);
            }
            else {
                //
                // Every other type is encoded as key frame. Technically we should also encode
                // as keyframe if delta frame would be larger, but we skip this for now.
                //
                WritePayloadKeyFrame(binaryEncoder);
            }

            PayloadSizeInStream = (ushort)(binaryEncoder.Position - StartPositionInStream);
            if (ConfiguredSize > 0 && PayloadSizeInStream < ConfiguredSize) {
                PayloadSizeInStream = ConfiguredSize;
                binaryEncoder.Position = StartPositionInStream + PayloadSizeInStream;
            }
        }

        /// <summary>
        /// Write DataSet message header
        /// </summary>
        /// <param name="encoder"></param>
        private void WriteDataSetMessageHeader(IEncoder encoder) {
            if ((DataSetFlags1 & DataSetFlags1EncodingMask.MessageIsValid) != 0) {
                encoder.WriteByte("DataSetFlags1", (byte)DataSetFlags1);
            }
            if ((DataSetFlags1 & DataSetFlags1EncodingMask.DataSetFlags2) != 0) {
                encoder.WriteByte("DataSetFlags2", (byte)DataSetFlags2);
            }
            if ((DataSetFlags1 & DataSetFlags1EncodingMask.DataSetMessageSequenceNumber) != 0) {
                encoder.WriteUInt16("SequenceNumber", (ushort)SequenceNumber);
            }
            if ((DataSetFlags2 & DataSetFlags2EncodingMask.Timestamp) != 0) {
                encoder.WriteDateTime("Timestamp", Timestamp);
            }
            if ((DataSetFlags2 & DataSetFlags2EncodingMask.PicoSeconds) != 0) {
                encoder.WriteUInt16("Picoseconds", PicoSeconds);
            }
            if ((DataSetFlags1 & DataSetFlags1EncodingMask.Status) != 0) {
                // This is the high order 16 bits of the StatusCode DataType representing
                // the numeric value of the Severity and SubCode of the StatusCode DataType.
                encoder.WriteUInt16("Status", (ushort)(Status.Code >> 16));
            }
            if ((DataSetFlags1 & DataSetFlags1EncodingMask.ConfigurationVersionMajorVersion) != 0) {
                encoder.WriteUInt32("ConfigurationMajorVersion", MetaDataVersion.MajorVersion);
            }
            if ((DataSetFlags1 & DataSetFlags1EncodingMask.ConfigurationVersionMinorVersion) != 0) {
                encoder.WriteUInt32("ConfigurationMinorVersion", MetaDataVersion.MinorVersion);
            }
        }

        /// <summary>
        /// Write payload data
        /// </summary>
        /// <param name="binaryEncoder"></param>
        private void WritePayloadKeyFrame(BinaryEncoder binaryEncoder) {

            var fieldType = DataSetFlags1 & DataSetFlags1EncodingMask.FieldTypeUsedBits;
            switch (fieldType) {
                case DataSetFlags1EncodingMask.FieldTypeVariant:
                    binaryEncoder.WriteUInt16("DataSetFieldCount", (ushort)Payload.Count);
                    foreach (var value in Payload) {
                        binaryEncoder.WriteVariant("Variant", value.Value.WrappedValue);
                    }
                    break;
                case DataSetFlags1EncodingMask.FieldTypeDataValue:
                    binaryEncoder.WriteUInt16("DataSetFieldCount", (ushort)Payload.Count);
                    foreach (var value in Payload) {
                        binaryEncoder.WriteDataValue("DataValue", value.Value);
                    }
                    break;
                case DataSetFlags1EncodingMask.FieldTypeRawData:
                    // DataSetFieldCount is not written for RawData
                    foreach (var value in Payload) {
                        WriteFieldAsRawData(binaryEncoder, value.Key, value.Value);
                    }
                    break;
            }
        }

        /// <summary>
        /// Write payload data delta frame
        /// </summary>
        /// <param name="binaryEncoder"></param>
        private void WritePayloadDeltaFrame(BinaryEncoder binaryEncoder) {

            // ignore null fields
            var fieldCount = Payload.Count(value => value.Value?.Value != null);
            binaryEncoder.WriteUInt16("FieldCount", (ushort)fieldCount);

            var fieldType = DataSetFlags1 & DataSetFlags1EncodingMask.FieldTypeUsedBits;
            var values = Payload.ToList();
            for (var i = 0; i < values.Count; i++) {
                var value = values[i];
                if (value.Value?.Value == null) {
                    continue;
                }

                // write field index corresponding to metadata
                binaryEncoder.WriteUInt16("FieldIndex", GetFieldIndex(value.Key, i));
                switch (fieldType) {
                    case DataSetFlags1EncodingMask.FieldTypeVariant:
                        binaryEncoder.WriteVariant("FieldValue", value.Value.WrappedValue);
                        break;
                    case DataSetFlags1EncodingMask.FieldTypeDataValue:
                        binaryEncoder.WriteDataValue("FieldValue", value.Value);
                        break;
                    case DataSetFlags1EncodingMask.FieldTypeRawData:
                        WriteFieldAsRawData(binaryEncoder, value.Key, value.Value.WrappedValue);
                        break;
                }
            }

            // Get field index in metadata if metadata was provided.
            ushort GetFieldIndex(string key, int pos) {
                if (MetaData?.Fields != null) {
                    for (var i = 0; i< MetaData.Fields.Count; i++) {
                        if (MetaData.Fields[i].Name == key) {
                            return (ushort)i;
                        }
                    }
                    // Assign a unique new one after the fields in metadata
                    return (ushort)(MetaData.Fields.Count + pos);
                }
                return (ushort)pos;
            }
        }

        /// <summary>
        /// Read DataSet message header
        /// </summary>
        /// <param name="decoder"></param>
        private void ReadDataSetMessageHeader(IDecoder decoder) {
            if ((DataSetFlags1 & DataSetFlags1EncodingMask.MessageIsValid) != 0) {
                DataSetFlags1 = (DataSetFlags1EncodingMask)decoder.ReadByte("DataSetFlags1");
            }
            if ((DataSetFlags1 & DataSetFlags1EncodingMask.DataSetFlags2) != 0) {
                DataSetFlags2 = (DataSetFlags2EncodingMask)decoder.ReadByte("DataSetFlags2");
            }
            if ((DataSetFlags1 & DataSetFlags1EncodingMask.DataSetMessageSequenceNumber) != 0) {
                SequenceNumber = decoder.ReadUInt16("SequenceNumber");
            }
            if ((DataSetFlags2 & DataSetFlags2EncodingMask.Timestamp) != 0) {
                Timestamp = decoder.ReadDateTime("Timestamp");
            }
            if ((DataSetFlags2 & DataSetFlags2EncodingMask.PicoSeconds) != 0) {
                PicoSeconds = decoder.ReadUInt16("Picoseconds");
            }

            if ((DataSetFlags1 & DataSetFlags1EncodingMask.Status) != 0) {
                // This is the high order 16 bits of the StatusCode DataType representing
                // the numeric value of the Severity and SubCode of the StatusCode DataType.
                var code = decoder.ReadUInt16("Status");
                Status = (uint)code << 16;
            }

            uint minorVersion = 1;
            uint majorVersion = 0;
            if ((DataSetFlags1 & DataSetFlags1EncodingMask.ConfigurationVersionMajorVersion) != 0) {
                majorVersion = decoder.ReadUInt32("ConfigurationMajorVersion");
            }
            if ((DataSetFlags1 & DataSetFlags1EncodingMask.ConfigurationVersionMinorVersion) != 0) {
                minorVersion = decoder.ReadUInt32("ConfigurationMinorVersion");
            }
            MetaDataVersion = new ConfigurationVersionDataType() {
                MinorVersion = minorVersion,
                MajorVersion = majorVersion
            };
        }

        /// <summary>
        /// Read message data key frame from decoder
        /// </summary>
        /// <param name="binaryDecoder"></param>
        /// <returns></returns>
        private void ReadMessageDataKeyFrame(BinaryDecoder binaryDecoder) {
            try {
                ushort fieldCount = 0;
                var fieldType = DataSetFlags1 & DataSetFlags1EncodingMask.FieldTypeUsedBits;
                if (fieldType == DataSetFlags1EncodingMask.FieldTypeRawData) {
                    if (MetaData != null) {
                        // metadata should provide field count
                        fieldCount = (ushort)MetaData.Fields.Count;
                    }
                    else {
                        throw ServiceResultException.Create(StatusCodes.BadDecodingError,
                            "Requires metadata to decode");
                    }
                }
                else {
                    fieldCount = binaryDecoder.ReadUInt16("DataSetFieldCount");
                }

                // check configuration version
                switch (fieldType) {
                    case DataSetFlags1EncodingMask.FieldTypeVariant:
                        for (var i = 0; i < fieldCount; i++) {
                            var fieldMetaData = GetFieldMetadata(i);
                            Payload.Add(fieldMetaData?.Name ?? i.ToString(),
                                new DataValue(binaryDecoder.ReadVariant("Variant")));
                        }
                        break;
                    case DataSetFlags1EncodingMask.FieldTypeDataValue:
                        for (var i = 0; i < fieldCount; i++) {
                            var fieldMetaData = GetFieldMetadata(i);
                            Payload.Add(fieldMetaData?.Name ?? i.ToString(),
                                binaryDecoder.ReadDataValue("DataValue"));
                        }
                        break;
                    case DataSetFlags1EncodingMask.FieldTypeRawData:
                        for (var i = 0; i < fieldCount; i++) {
                            var fieldMetaData = GetFieldMetadata(i);
                            if (fieldMetaData != null) {
                                var decodedValue = ReadRawData(binaryDecoder, fieldMetaData);
                                Payload.Add(fieldMetaData.Name, new DataValue(new Variant(decodedValue)));
                            }
                        }
                        break;
                }
            }
            catch (Exception ex) {
                throw ServiceResultException.Create(StatusCodes.BadDecodingError,
                    ex, "Failed to decode key frame.");
            }
        }

        /// <summary>
        /// Read message data delta frame from decoder
        /// </summary>
        /// <param name="binaryDecoder"></param>
        /// <returns></returns>
        private void ReadMessageDataDeltaFrame(BinaryDecoder binaryDecoder) {
            try {
                var fieldType = DataSetFlags1 & DataSetFlags1EncodingMask.FieldTypeUsedBits;
                ushort fieldCount = fieldCount = binaryDecoder.ReadUInt16("FieldCount");

                for (var i = 0; i < fieldCount; i++) {
                    var fieldIndex = binaryDecoder.ReadUInt16("FieldIndex");
                    var fieldMetaData = GetFieldMetadata(fieldIndex);
                    switch (fieldType) {
                        case DataSetFlags1EncodingMask.FieldTypeVariant:
                            Payload.Add(fieldMetaData?.Name ?? fieldIndex.ToString(),
                                new DataValue(binaryDecoder.ReadVariant("FieldValue")));
                            break;
                        case DataSetFlags1EncodingMask.FieldTypeDataValue:
                            Payload.Add(fieldMetaData?.Name ?? fieldIndex.ToString(),
                                binaryDecoder.ReadDataValue("FieldValue"));
                            break;
                        case DataSetFlags1EncodingMask.FieldTypeRawData:
                            if (fieldMetaData != null) {
                                var decodedValue = ReadRawData(binaryDecoder, fieldMetaData);
                                Payload.Add(fieldMetaData.Name,
                                    new DataValue(new Variant(decodedValue)));
                            }
                            break;
                    }
                }
            }
            catch (Exception ex) {
                throw ServiceResultException.Create(StatusCodes.BadDecodingError,
                    ex, "Failed to decode delta frame.");
            }
        }

        /// <summary>
        /// Get field metadata for the index
        /// </summary>
        /// <param name="fieldIndex"></param>
        /// <returns></returns>
        private FieldMetaData GetFieldMetadata(int fieldIndex) {
            if (MetaData?.Fields == null) {
                return null;
            }
            if (fieldIndex < 0 || fieldIndex >= MetaData.Fields.Count) {
                return null;
            }
            return MetaData.Fields[fieldIndex];
        }

        /// <summary>
        /// Encodes field value as RawData
        /// </summary>
        /// <param name="binaryEncoder"></param>
        /// <param name="propertyName"></param>
        /// <param name="variant"></param>
        private static void WriteFieldAsRawData(BinaryEncoder binaryEncoder, string propertyName, Variant variant) {
            try {
                if (variant.TypeInfo == null || variant.TypeInfo.BuiltInType == BuiltInType.Null) {
                    return;
                }
                object valueToEncode = variant.Value;
                if (variant.TypeInfo.ValueRank == ValueRanks.Scalar) {
                    switch (variant.TypeInfo.BuiltInType) {
                        case BuiltInType.Boolean:
                            binaryEncoder.WriteBoolean("Bool", Convert.ToBoolean(valueToEncode));
                            break;
                        case BuiltInType.SByte:
                            binaryEncoder.WriteSByte("SByte", Convert.ToSByte(valueToEncode));
                            break;
                        case BuiltInType.Byte:
                            binaryEncoder.WriteByte("Byte", Convert.ToByte(valueToEncode));
                            break;
                        case BuiltInType.Int16:
                            binaryEncoder.WriteInt16("Int16", Convert.ToInt16(valueToEncode));
                            break;
                        case BuiltInType.UInt16:
                            binaryEncoder.WriteUInt16("UInt16", Convert.ToUInt16(valueToEncode));
                            break;
                        case BuiltInType.Int32:
                            binaryEncoder.WriteInt32("Int32", Convert.ToInt32(valueToEncode));
                            break;
                        case BuiltInType.UInt32:
                            binaryEncoder.WriteUInt32("UInt32", Convert.ToUInt32(valueToEncode));
                            break;
                        case BuiltInType.Int64:
                            binaryEncoder.WriteInt64("Int64", Convert.ToInt64(valueToEncode));
                            break;
                        case BuiltInType.UInt64:
                            binaryEncoder.WriteUInt64("UInt64", Convert.ToUInt64(valueToEncode));
                            break;
                        case BuiltInType.Float:
                            binaryEncoder.WriteFloat("Float", Convert.ToSingle(valueToEncode));
                            break;
                        case BuiltInType.Double:
                            binaryEncoder.WriteDouble("Double", Convert.ToDouble(valueToEncode));
                            break;
                        case BuiltInType.DateTime:
                            binaryEncoder.WriteDateTime("DateTime", Convert.ToDateTime(valueToEncode));
                            break;
                        case BuiltInType.Guid:
                            binaryEncoder.WriteGuid("GUID", (Uuid)valueToEncode);
                            break;
                        case BuiltInType.String:
                            binaryEncoder.WriteString("String", valueToEncode as string);
                            break;
                        case BuiltInType.ByteString:
                            binaryEncoder.WriteByteString("ByteString", (byte[])valueToEncode);
                            break;
                        case BuiltInType.QualifiedName:
                            binaryEncoder.WriteQualifiedName("QualifiedName", valueToEncode as QualifiedName);
                            break;
                        case BuiltInType.LocalizedText:
                            binaryEncoder.WriteLocalizedText("LocalizedText", valueToEncode as LocalizedText);
                            break;
                        case BuiltInType.NodeId:
                            binaryEncoder.WriteNodeId("NodeId", valueToEncode as NodeId);
                            break;
                        case BuiltInType.ExpandedNodeId:
                            binaryEncoder.WriteExpandedNodeId("ExpandedNodeId", valueToEncode as ExpandedNodeId);
                            break;
                        case BuiltInType.StatusCode:
                            binaryEncoder.WriteStatusCode("StatusCode", (StatusCode)valueToEncode);
                            break;
                        case BuiltInType.XmlElement:
                            binaryEncoder.WriteXmlElement("XmlElement", valueToEncode as XmlElement);
                            break;
                        case BuiltInType.Enumeration:
                            binaryEncoder.WriteInt32("Enumeration", Convert.ToInt32(valueToEncode));
                            break;
                        case BuiltInType.ExtensionObject:
                            binaryEncoder.WriteExtensionObject("ExtensionObject", valueToEncode as ExtensionObject);
                            break;
                    }
                }
                else if (variant.TypeInfo.ValueRank >= ValueRanks.OneDimension) {
                    binaryEncoder.WriteArray(null, valueToEncode, variant.TypeInfo.ValueRank,
                        variant.TypeInfo.BuiltInType);
                }
            }
            catch (Exception ex) {
                throw ServiceResultException.Create(StatusCodes.BadEncodingError,
                    ex, "Error encoding field {0}.", propertyName);
            }
        }

        /// <summary>
        /// Decode RawData type (for SimpleTypeDescription!?)
        /// </summary>
        /// <param name="binaryDecoder"></param>
        /// <param name="fieldMetaData"></param>
        /// <returns></returns>
        private static object ReadRawData(BinaryDecoder binaryDecoder, FieldMetaData fieldMetaData) {
            if (fieldMetaData.BuiltInType != 0) {
                try {
                    switch (fieldMetaData.ValueRank) {
                        case ValueRanks.Scalar:
                            return ReadRawScalar(binaryDecoder, fieldMetaData.BuiltInType);
                        case ValueRanks.OneDimension:
                        case ValueRanks.TwoDimensions:
                            return binaryDecoder.ReadArray(null, fieldMetaData.ValueRank, (BuiltInType)fieldMetaData.BuiltInType);
                        case ValueRanks.OneOrMoreDimensions:
                        case ValueRanks.Any:// Scalar or Array with any number of dimensions
                        case ValueRanks.ScalarOrOneDimension:
                        // not implemented
                        default:
                            Utils.Trace("Decoding ValueRank = {0} not supported yet !!!", fieldMetaData.ValueRank);
                            break;
                    }
                }
                catch (Exception ex) {
                    Utils.Trace(ex, "Error reading element for RawData.");
                    return StatusCodes.BadDecodingError;
                }
            }
            return null;
        }

        /// <summary>
        /// Read a scalar type
        /// </summary>
        /// <param name="binaryDecoder"></param>
        /// <param name="builtInType"></param>
        /// <returns>The decoded object</returns>
        private static object ReadRawScalar(BinaryDecoder binaryDecoder, byte builtInType) {
            switch ((BuiltInType)builtInType) {
                case BuiltInType.Boolean:
                    return binaryDecoder.ReadBoolean(null);
                case BuiltInType.SByte:
                    return binaryDecoder.ReadSByte(null);
                case BuiltInType.Byte:
                    return binaryDecoder.ReadByte(null);
                case BuiltInType.Int16:
                    return binaryDecoder.ReadInt16(null);
                case BuiltInType.UInt16:
                    return binaryDecoder.ReadUInt16(null);
                case BuiltInType.Int32:
                    return binaryDecoder.ReadInt32(null);
                case BuiltInType.UInt32:
                    return binaryDecoder.ReadUInt32(null);
                case BuiltInType.Int64:
                    return binaryDecoder.ReadInt64(null);
                case BuiltInType.UInt64:
                    return binaryDecoder.ReadUInt64(null);
                case BuiltInType.Float:
                    return binaryDecoder.ReadFloat(null);
                case BuiltInType.Double:
                    return binaryDecoder.ReadDouble(null);
                case BuiltInType.String:
                    return binaryDecoder.ReadString(null);
                case BuiltInType.DateTime:
                    return binaryDecoder.ReadDateTime(null);
                case BuiltInType.Guid:
                    return binaryDecoder.ReadGuid(null);
                case BuiltInType.ByteString:
                    return binaryDecoder.ReadByteString(null);
                case BuiltInType.XmlElement:
                    return binaryDecoder.ReadXmlElement(null);
                case BuiltInType.NodeId:
                    return binaryDecoder.ReadNodeId(null);
                case BuiltInType.ExpandedNodeId:
                    return binaryDecoder.ReadExpandedNodeId(null);
                case BuiltInType.StatusCode:
                    return binaryDecoder.ReadStatusCode(null);
                case BuiltInType.QualifiedName:
                    return binaryDecoder.ReadQualifiedName(null);
                case BuiltInType.LocalizedText:
                    return binaryDecoder.ReadLocalizedText(null);
                case BuiltInType.DataValue:
                    return binaryDecoder.ReadDataValue(null);
                case BuiltInType.Enumeration:
                    return binaryDecoder.ReadInt32(null);
                case BuiltInType.Variant:
                    return binaryDecoder.ReadVariant(null);
                case BuiltInType.ExtensionObject:
                    return binaryDecoder.ReadExtensionObject(null);
                default:
                    return null;
            }
        }

        private DataSetFlags1EncodingMask? _dataSetFlags1;
        private DataSetFlags2EncodingMask? _dataSetFlags2;
    }
}
