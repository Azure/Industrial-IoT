// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Encoders.PubSub
{
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Opc.Ua;
    using System;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Xml;

    /// <summary>
    /// Data set message
    /// </summary>
    public class UadpDataSetMessage : BaseDataSetMessage
    {
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
        /// Get or set timestamp pico seconds
        /// </summary>
        public ushort PicoSeconds { get; set; }

        /// <summary>
        /// The possible values for the DataSetFlags1 encoding byte.
        /// </summary>
        [Flags]
        internal enum DataSetFlags1EncodingMask : byte
        {
            None = 0,
            MessageIsValid = 1,
            FieldTypeRawData = 2,
            FieldTypeDataValue = 4,
            FieldTypeUsedBits = FieldTypeRawData | FieldTypeDataValue,
            DataSetMessageSequenceNumber = 8,
            Status = 16,
            ConfigurationVersionMajorVersion = 32,
            ConfigurationVersionMinorVersion = 64,
            DataSetFlags2 = 128,

            DataSetFlags1UsedBits =
                MessageIsValid |
                DataSetMessageSequenceNumber |
                Status |
                ConfigurationVersionMajorVersion |
                ConfigurationVersionMinorVersion |
                DataSetFlags2
        }

        /// <summary>
        /// The possible values for the DataSetFlags2 encoding byte.
        /// </summary>
        [Flags]
        internal enum DataSetFlags2EncodingMask : byte
        {
            DataKeyFrame = 0,
            DataDeltaFrame = 1,
            Event = 2,
            KeepAlive = DataDeltaFrame | Event,
            Reserved1 = 4,
            MessageTypeBits = KeepAlive | Reserved1,
            Timestamp = 16,
            PicoSeconds = 32,
            Reserved2 = 64,
            ReservedForExtendedFlags = 128
        }

        /// <summary>
        /// Get DataSetFlags1
        /// </summary>
        internal DataSetFlags1EncodingMask DataSetFlags1
        {
            get
            {
                if (_dataSetFlags1 == null)
                {
                    _dataSetFlags1 = DataSetFlags1EncodingMask.MessageIsValid;

                    // DataSetFlags1: Bit range 1-2: Field Encoding
                    _dataSetFlags1 &= ~DataSetFlags1EncodingMask.FieldTypeUsedBits;
                    if ((Payload.DataSetFieldContentMask & DataSetFieldContentFlags.RawData) != 0)
                    {
                        _dataSetFlags1 |= DataSetFlags1EncodingMask.FieldTypeRawData;
                    }
                    else if (Payload.DataSetFieldContentMask != 0)
                    {
                        _dataSetFlags1 |= DataSetFlags1EncodingMask.FieldTypeDataValue;
                    }

                    if ((DataSetMessageContentMask & DataSetMessageContentFlags.SequenceNumber) != 0)
                    {
                        // DataSetFlags1: Bit range 3: sequence
                        _dataSetFlags1 |= DataSetFlags1EncodingMask.DataSetMessageSequenceNumber;
                    }
                    if ((DataSetMessageContentMask & DataSetMessageContentFlags.Status) != 0)
                    {
                        // DataSetFlags1: Bit range 4: status
                        _dataSetFlags1 |= DataSetFlags1EncodingMask.Status;
                    }
                    if ((DataSetMessageContentMask & DataSetMessageContentFlags.MajorVersion) != 0)
                    {
                        // DataSetFlags1: Bit range 5: major version
                        _dataSetFlags1 |= DataSetFlags1EncodingMask.ConfigurationVersionMajorVersion;
                    }
                    if ((DataSetMessageContentMask & DataSetMessageContentFlags.MinorVersion) != 0)
                    {
                        // DataSetFlags1: Bit range 6: minor version
                        _dataSetFlags1 |= DataSetFlags1EncodingMask.ConfigurationVersionMinorVersion;
                    }

                    // DataSetFlags1: Bit 7 if needed
                    if ((DataSetMessageContentMask & (DataSetMessageContentFlags.Timestamp |
                                                      DataSetMessageContentFlags.PicoSeconds)) != 0)
                    {
                        _dataSetFlags1 |= DataSetFlags1EncodingMask.DataSetFlags2;
                    }
                    if (MessageType != MessageType.KeyFrame)
                    {
                        _dataSetFlags1 |= DataSetFlags1EncodingMask.DataSetFlags2;
                    }
                }
                return _dataSetFlags1.Value;
            }
            private set
            {
                _dataSetFlags1 = value;
                if ((value & DataSetFlags1EncodingMask.MessageIsValid) != 0)
                {
                    // DataSetFlags1: Bit range 1-2: Field Encoding
                    if ((value & DataSetFlags1EncodingMask.FieldTypeRawData) != 0)
                    {
                        Payload.DataSetFieldContentMask = DataSetFieldContentFlags.RawData;
                    }
                    else if ((value & DataSetFlags1EncodingMask.FieldTypeDataValue) != 0)
                    {
                        Payload.DataSetFieldContentMask = DataSetFieldContentFlags.StatusCode
                                                        | DataSetFieldContentFlags.SourceTimestamp
                                                        | DataSetFieldContentFlags.ServerTimestamp
                                                        | DataSetFieldContentFlags.SourcePicoSeconds
                                                        | DataSetFieldContentFlags.ServerPicoSeconds;
                    }
                    else
                    {
                        Payload.DataSetFieldContentMask = 0;
                    }

                    // DataSetFlags1: Bit range 3: sequence
                    if ((value & DataSetFlags1EncodingMask.DataSetMessageSequenceNumber) != 0)
                    {
                        DataSetMessageContentMask |= DataSetMessageContentFlags.SequenceNumber;
                    }
                    if ((value & DataSetFlags1EncodingMask.Status) != 0)
                    {
                        // DataSetFlags1: Bit range 4: status
                        DataSetMessageContentMask |= DataSetMessageContentFlags.Status;
                    }
                    if ((value & DataSetFlags1EncodingMask.ConfigurationVersionMajorVersion) != 0)
                    {
                        // DataSetFlags1: Bit range 5: major version
                        DataSetMessageContentMask |= DataSetMessageContentFlags.MajorVersion;
                    }
                    if ((value & DataSetFlags1EncodingMask.ConfigurationVersionMinorVersion) != 0)
                    {
                        // DataSetFlags1: Bit range 6: minor version
                        DataSetMessageContentMask |= DataSetMessageContentFlags.MinorVersion;
                    }
                }
            }
        }

        /// <summary>
        /// Get DataSetFlags2
        /// </summary>
        /// <exception cref="EncodingException"></exception>
        internal DataSetFlags2EncodingMask DataSetFlags2
        {
            get
            {
                if (_dataSetFlags2 == null)
                {
                    _dataSetFlags2 = 0;

                    // Bit range 0-3: DataSetMessage type
                    switch (MessageType)
                    {
                        case MessageType.DeltaFrame:
                            _dataSetFlags2 |= DataSetFlags2EncodingMask.DataDeltaFrame;
                            break;
                        case MessageType.Event:
                            _dataSetFlags2 |= DataSetFlags2EncodingMask.Event;
                            break;
                        case MessageType.KeepAlive:
                            _dataSetFlags2 |= DataSetFlags2EncodingMask.KeepAlive;
                            break;
                        case MessageType.Condition:
                            _dataSetFlags2 |= DataSetFlags2EncodingMask.Event | DataSetFlags2EncodingMask.Reserved1;
                            break;
                        case MessageType.KeyFrame:
                            // Default is key frame
                            break;
                        default:
                            throw new EncodingException(
                                $"Message type {MessageType} not valid for data set messages.");
                    }

                    // Bit range 4-5: timestamp
                    if ((DataSetMessageContentMask & DataSetMessageContentFlags.Timestamp) != 0)
                    {
                        _dataSetFlags2 |= DataSetFlags2EncodingMask.Timestamp;
                    }
                    if ((DataSetMessageContentMask & DataSetMessageContentFlags.PicoSeconds) != 0)
                    {
                        _dataSetFlags2 |= DataSetFlags2EncodingMask.PicoSeconds;
                    }
                }
                return _dataSetFlags2.Value;
            }
            private set
            {
                _dataSetFlags2 = value;

                // Bit range 0-3: DataSetMessage type
                switch (value & DataSetFlags2EncodingMask.MessageTypeBits)
                {
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
                if ((value & DataSetFlags2EncodingMask.Timestamp) != 0)
                {
                    DataSetMessageContentMask |= DataSetMessageContentFlags.Timestamp;
                }
                if ((value & DataSetFlags2EncodingMask.PicoSeconds) != 0)
                {
                    DataSetMessageContentMask |= DataSetMessageContentFlags.PicoSeconds;
                }
            }
        }

        /// <inheritdoc/>
        internal void Encode(BinaryEncoder binaryEncoder, IDataSetMetaDataResolver? resolver)
        {
            StartPositionInStream = binaryEncoder.Position;
            if (DataSetOffset > 0 && StartPositionInStream < DataSetOffset)
            {
                StartPositionInStream = DataSetOffset;
                binaryEncoder.Position = DataSetOffset;
            }

            WriteDataSetMessageHeader(binaryEncoder);

            var metadata = resolver?.Find(DataSetWriterId,
                MetaDataVersion?.MajorVersion ?? 1, MetaDataVersion?.MinorVersion ?? 0);

            if ((DataSetFlags2 & DataSetFlags2EncodingMask.DataDeltaFrame) != 0)
            {
                WritePayloadDeltaFrame(binaryEncoder, metadata);
            }
            else
            {
                //
                // Every other type is encoded as key frame. Technically we should also encode
                // as keyframe if delta frame would be larger, but we skip this for now.
                //
                WritePayloadKeyFrame(binaryEncoder, metadata);
            }

            PayloadSizeInStream = (ushort)(binaryEncoder.Position - StartPositionInStream);
            if (ConfiguredSize > 0 && PayloadSizeInStream < ConfiguredSize)
            {
                PayloadSizeInStream = ConfiguredSize;
                binaryEncoder.Position = StartPositionInStream + PayloadSizeInStream;
            }
        }

        /// <summary>
        /// Decode data set message
        /// </summary>
        /// <param name="decoder"></param>
        /// <param name="resolver"></param>
        /// <exception cref="DecodingException"></exception>
        internal bool TryDecode(BinaryDecoder decoder, IDataSetMetaDataResolver? resolver)
        {
            if (decoder is not BinaryDecoder binaryDecoder)
            {
                throw new DecodingException("Must use Binary decoder here");
            }
            try
            {
                if (!TryReadDataSetMessageHeader(binaryDecoder))
                {
                    return false;
                }

                var metadata = resolver?.Find(DataSetWriterId,
                    MetaDataVersion?.MajorVersion ?? 1, MetaDataVersion?.MinorVersion ?? 0);
                if ((DataSetFlags2 & DataSetFlags2EncodingMask.DataDeltaFrame) != 0)
                {
                    ReadPayloadDeltaFrame(binaryDecoder, metadata);
                }
                else
                {
                    ReadPayloadKeyFrame(binaryDecoder, metadata);
                }
                return true;
            }
            catch (EndOfStreamException)
            {
                return false;
            }
        }

        /// <summary>
        /// Read DataSet message header
        /// </summary>
        /// <param name="decoder"></param>
        private bool TryReadDataSetMessageHeader(BinaryDecoder decoder)
        {
            DataSetFlags1 = (DataSetFlags1EncodingMask)decoder.ReadByte(null);
            if ((DataSetFlags1 & DataSetFlags1EncodingMask.MessageIsValid) == 0)
            {
                // Invalid message
                return false;
            }
            if ((DataSetFlags1 & DataSetFlags1EncodingMask.DataSetFlags2) != 0)
            {
                DataSetFlags2 = (DataSetFlags2EncodingMask)decoder.ReadByte(null);
            }
            if ((DataSetFlags1 & DataSetFlags1EncodingMask.DataSetMessageSequenceNumber) != 0)
            {
                SequenceNumber = decoder.ReadUInt16(null);
            }
            if ((DataSetFlags2 & DataSetFlags2EncodingMask.Timestamp) != 0)
            {
                Timestamp = decoder.ReadDateTime(null);
            }
            if ((DataSetFlags2 & DataSetFlags2EncodingMask.PicoSeconds) != 0)
            {
                PicoSeconds = decoder.ReadUInt16(null);
            }

            if ((DataSetFlags1 & DataSetFlags1EncodingMask.Status) != 0)
            {
                // This is the high order 16 bits of the StatusCode DataType representing
                // the numeric value of the Severity and SubCode of the StatusCode DataType.
                var code = decoder.ReadUInt16(null);
                Status = (uint)code << 16;
            }

            uint minorVersion = 1;
            uint majorVersion = 0;
            if ((DataSetFlags1 & DataSetFlags1EncodingMask.ConfigurationVersionMajorVersion) != 0)
            {
                majorVersion = decoder.ReadUInt32(null);
            }
            if ((DataSetFlags1 & DataSetFlags1EncodingMask.ConfigurationVersionMinorVersion) != 0)
            {
                minorVersion = decoder.ReadUInt32(null);
            }
            MetaDataVersion = new ConfigurationVersionDataType()
            {
                MinorVersion = minorVersion,
                MajorVersion = majorVersion
            };
            return true;
        }

        /// <summary>
        /// Write DataSet message header
        /// </summary>
        /// <param name="encoder"></param>
        private void WriteDataSetMessageHeader(BinaryEncoder encoder)
        {
            if ((DataSetFlags1 & DataSetFlags1EncodingMask.MessageIsValid) != 0)
            {
                encoder.WriteByte(null, (byte)DataSetFlags1);
            }
            if ((DataSetFlags1 & DataSetFlags1EncodingMask.DataSetFlags2) != 0)
            {
                encoder.WriteByte(null, (byte)DataSetFlags2);
            }
            if ((DataSetFlags1 & DataSetFlags1EncodingMask.DataSetMessageSequenceNumber) != 0)
            {
                encoder.WriteUInt16(null, (ushort)SequenceNumber);
            }
            if ((DataSetFlags2 & DataSetFlags2EncodingMask.Timestamp) != 0)
            {
                encoder.WriteDateTime(null, Timestamp?.UtcDateTime ?? default);
            }
            if ((DataSetFlags2 & DataSetFlags2EncodingMask.PicoSeconds) != 0)
            {
                encoder.WriteUInt16(null, PicoSeconds);
            }
            if ((DataSetFlags1 & DataSetFlags1EncodingMask.Status) != 0)
            {
                // This is the high order 16 bits of the StatusCode DataType representing
                // the numeric value of the Severity and SubCode of the StatusCode DataType.
                var status = Status ?? Payload.Values
                    .FirstOrDefault(s => StatusCode.IsNotGood(s?.StatusCode ?? StatusCodes.BadNoData))?
                        .StatusCode ?? StatusCodes.Good;
                encoder.WriteUInt16(null, (ushort)(status.Code >> 16));
            }
            if ((DataSetFlags1 & DataSetFlags1EncodingMask.ConfigurationVersionMajorVersion) != 0)
            {
                encoder.WriteUInt32(null, MetaDataVersion?.MajorVersion ?? 1);
            }
            if ((DataSetFlags1 & DataSetFlags1EncodingMask.ConfigurationVersionMinorVersion) != 0)
            {
                encoder.WriteUInt32(null, MetaDataVersion?.MinorVersion ?? 0);
            }
        }

        /// <summary>
        /// Read message data key frame from decoder
        /// </summary>
        /// <param name="binaryDecoder"></param>
        /// <param name="metadata"></param>
        /// <returns></returns>
        /// <exception cref="DecodingException"></exception>
        private void ReadPayloadKeyFrame(BinaryDecoder binaryDecoder, PublishedDataSetMetaDataModel? metadata)
        {
            var fieldType = DataSetFlags1 & DataSetFlags1EncodingMask.FieldTypeUsedBits;
            ushort dataSetFieldCount;
            if (fieldType == DataSetFlags1EncodingMask.FieldTypeRawData)
            {
                if (metadata != null)
                {
                    // metadata should provide field count
                    dataSetFieldCount = (ushort)metadata.Fields.Count;
                }
                else
                {
                    throw new DecodingException("Requires metadata to decode");
                }
            }
            else
            {
                dataSetFieldCount = binaryDecoder.ReadUInt16(null);
            }

            // check configuration version
            switch (fieldType)
            {
                case 0:
                    for (var i = 0; i < dataSetFieldCount; i++)
                    {
                        var fieldMetaData = GetFieldMetadata(metadata, i);
                        Payload.Add(fieldMetaData?.Name ?? i.ToString(CultureInfo.InvariantCulture),
                            new DataValue(binaryDecoder.ReadVariant(null)));
                    }
                    break;
                case DataSetFlags1EncodingMask.FieldTypeDataValue:
                    for (var i = 0; i < dataSetFieldCount; i++)
                    {
                        var fieldMetaData = GetFieldMetadata(metadata, i);
                        Payload.Add(fieldMetaData?.Name ?? i.ToString(CultureInfo.InvariantCulture),
                            binaryDecoder.ReadDataValue(null));
                    }
                    break;
                case DataSetFlags1EncodingMask.FieldTypeRawData:
                    for (var i = 0; i < dataSetFieldCount; i++)
                    {
                        var fieldMetaData = GetFieldMetadata(metadata, i);
                        if (fieldMetaData != null)
                        {
                            var decodedValue = ReadRawData(binaryDecoder, fieldMetaData);
                            Payload.Add(fieldMetaData.Name, new DataValue(new Variant(decodedValue)));
                        }
                    }
                    break;
                default:
                    throw new DecodingException($"Reserved field type {fieldType} not allowed.");
            }
        }

        /// <summary>
        /// Write payload data
        /// </summary>
        /// <param name="binaryEncoder"></param>
        /// <param name="metadata"></param>
        /// <exception cref="EncodingException"></exception>
        private void WritePayloadKeyFrame(BinaryEncoder binaryEncoder, PublishedDataSetMetaDataModel? metadata)
        {
            var fieldType = DataSetFlags1 & DataSetFlags1EncodingMask.FieldTypeUsedBits;
            switch (fieldType)
            {
                case 0:
                    Debug.Assert(Payload.Count <= ushort.MaxValue);
                    binaryEncoder.WriteUInt16(null, (ushort)Payload.Count);
                    foreach (var value in Payload)
                    {
                        binaryEncoder.WriteVariant(null, value.Value?.WrappedValue ?? default);
                    }
                    break;
                case DataSetFlags1EncodingMask.FieldTypeDataValue:
                    Debug.Assert(Payload.Count <= ushort.MaxValue);
                    binaryEncoder.WriteUInt16(null, (ushort)Payload.Count);
                    foreach (var value in Payload)
                    {
                        binaryEncoder.WriteDataValue(null, value.Value);
                    }
                    break;
                case DataSetFlags1EncodingMask.FieldTypeRawData:
                    // DataSetFieldCount is not written for RawData
                    var values = Payload.ToList();
                    for (var i = 0; i < values.Count; i++)
                    {
                        var fieldMetaData = GetFieldMetadata(metadata, i);
                        WriteFieldAsRawData(binaryEncoder,
                            values[i].Value?.WrappedValue ?? default, fieldMetaData);
                    }
                    break;
                default:
                    throw new EncodingException(
                        $"Reserved field type {fieldType} not allowed.");
            }
        }

        /// <summary>
        /// Read message data delta frame from decoder
        /// </summary>
        /// <param name="binaryDecoder"></param>
        /// <param name="metadata"></param>
        /// <returns></returns>
        /// <exception cref="DecodingException"></exception>
        private void ReadPayloadDeltaFrame(BinaryDecoder binaryDecoder, PublishedDataSetMetaDataModel? metadata)
        {
            var fieldType = DataSetFlags1 & DataSetFlags1EncodingMask.FieldTypeUsedBits;
            var fieldCount = binaryDecoder.ReadUInt16(null);

            for (var i = 0; i < fieldCount; i++)
            {
                var fieldIndex = binaryDecoder.ReadUInt16(null);
                var fieldMetaData = GetFieldMetadata(metadata, fieldIndex);
                switch (fieldType)
                {
                    case 0:
                        Payload.Add(fieldMetaData?.Name ?? fieldIndex.ToString(CultureInfo.InvariantCulture),
                            new DataValue(binaryDecoder.ReadVariant(null)));
                        break;
                    case DataSetFlags1EncodingMask.FieldTypeDataValue:
                        Payload.Add(fieldMetaData?.Name ?? fieldIndex.ToString(CultureInfo.InvariantCulture),
                            binaryDecoder.ReadDataValue(null));
                        break;
                    case DataSetFlags1EncodingMask.FieldTypeRawData:
                        if (fieldMetaData != null)
                        {
                            var decodedValue = ReadRawData(binaryDecoder, fieldMetaData);
                            Payload.Add(fieldMetaData.Name,
                                new DataValue(new Variant(decodedValue)));
                        }
                        break;
                    default:
                        throw new DecodingException($"Reserved field type {fieldType} not allowed.");
                }
            }
        }

        /// <summary>
        /// Write payload data delta frame
        /// </summary>
        /// <param name="binaryEncoder"></param>
        /// <param name="metadata"></param>
        /// <exception cref="EncodingException"></exception>
        private void WritePayloadDeltaFrame(BinaryEncoder binaryEncoder, PublishedDataSetMetaDataModel? metadata)
        {
            // ignore null fields
            var fieldCount = Payload.Count(value => value.Value?.Value != null);
            Debug.Assert(fieldCount <= ushort.MaxValue);
            binaryEncoder.WriteUInt16(null, (ushort)fieldCount);

            var fieldType = DataSetFlags1 & DataSetFlags1EncodingMask.FieldTypeUsedBits;
            var values = Payload.ToList();
            for (var i = 0; i < values.Count; i++)
            {
                var value = values[i];
                if (value.Value?.Value == null)
                {
                    continue;
                }

                // write field index corresponding to metadata
                var fieldIndex = GetFieldIndex(metadata, value.Key, i);
                binaryEncoder.WriteUInt16(null, fieldIndex);
                switch (fieldType)
                {
                    case 0:
                        binaryEncoder.WriteVariant(null, value.Value.WrappedValue);
                        break;
                    case DataSetFlags1EncodingMask.FieldTypeDataValue:
                        binaryEncoder.WriteDataValue(null, value.Value);
                        break;
                    case DataSetFlags1EncodingMask.FieldTypeRawData:
                        var fieldMetadata = GetFieldMetadata(metadata, fieldIndex);
                        WriteFieldAsRawData(binaryEncoder, value.Value.WrappedValue, fieldMetadata);
                        break;
                    default:
                        throw new EncodingException($"Reserved field type {fieldType} not allowed.");
                }
            }
        }

        /// <summary>
        /// Get field index in metadata if metadata was provided.
        /// </summary>
        /// <param name="metadata"></param>
        /// <param name="key"></param>
        /// <param name="pos"></param>
        /// <returns></returns>
        private static ushort GetFieldIndex(PublishedDataSetMetaDataModel? metadata, string key, int pos)
        {
            if (metadata?.Fields != null)
            {
                for (var i = 0; i < metadata.Fields.Count; i++)
                {
                    if (metadata.Fields[i].Name == key)
                    {
                        return (ushort)i;
                    }
                }
                // Assign a unique new one after the fields in metadata
                var idx = metadata.Fields.Count + pos;
                Debug.Assert(idx <= ushort.MaxValue);
                return (ushort)idx;
            }
            return (ushort)pos;
        }

        /// <summary>
        /// Get field metadata for the index
        /// </summary>
        /// <param name="metadata"></param>
        /// <param name="fieldIndex"></param>
        /// <returns></returns>
        private static PublishedFieldMetaDataModel? GetFieldMetadata(PublishedDataSetMetaDataModel? metadata,
            int fieldIndex)
        {
            if (metadata?.Fields == null)
            {
                return null;
            }
            if (fieldIndex < 0 || fieldIndex >= metadata.Fields.Count)
            {
                return null;
            }
            return metadata.Fields[fieldIndex];
        }

        /// <summary>
        /// Encodes field value as RawData
        /// </summary>
        /// <param name="binaryEncoder"></param>
        /// <param name="variant"></param>
        /// <param name="fieldMetaData"></param>
        private static void WriteFieldAsRawData(BinaryEncoder binaryEncoder, Variant variant,
            PublishedFieldMetaDataModel? fieldMetaData)
        {
            var builtInType = (BuiltInType?)fieldMetaData?.BuiltInType
                ?? variant.TypeInfo?.BuiltInType ?? BuiltInType.Null;
            var valueRank = fieldMetaData?.ValueRank
                ?? variant.TypeInfo?.ValueRank ?? 0;
            if (builtInType == BuiltInType.Null)
            {
                return;
            }
            var valueToEncode = variant.Value;
            if (valueRank == ValueRanks.Scalar)
            {
                switch (builtInType)
                {
                    case BuiltInType.Boolean:
                        binaryEncoder.WriteBoolean(null,
                            Convert.ToBoolean(valueToEncode, CultureInfo.InvariantCulture));
                        break;
                    case BuiltInType.SByte:
                        binaryEncoder.WriteSByte(null,
                            Convert.ToSByte(valueToEncode, CultureInfo.InvariantCulture));
                        break;
                    case BuiltInType.Byte:
                        binaryEncoder.WriteByte(null,
                            Convert.ToByte(valueToEncode, CultureInfo.InvariantCulture));
                        break;
                    case BuiltInType.Int16:
                        binaryEncoder.WriteInt16(null,
                            Convert.ToInt16(valueToEncode, CultureInfo.InvariantCulture));
                        break;
                    case BuiltInType.UInt16:
                        binaryEncoder.WriteUInt16(null,
                            Convert.ToUInt16(valueToEncode, CultureInfo.InvariantCulture));
                        break;
                    case BuiltInType.Int32:
                        binaryEncoder.WriteInt32(null,
                            Convert.ToInt32(valueToEncode, CultureInfo.InvariantCulture));
                        break;
                    case BuiltInType.UInt32:
                        binaryEncoder.WriteUInt32(null,
                            Convert.ToUInt32(valueToEncode, CultureInfo.InvariantCulture));
                        break;
                    case BuiltInType.Int64:
                        binaryEncoder.WriteInt64(null,
                            Convert.ToInt64(valueToEncode, CultureInfo.InvariantCulture));
                        break;
                    case BuiltInType.UInt64:
                        binaryEncoder.WriteUInt64(null,
                            Convert.ToUInt64(valueToEncode, CultureInfo.InvariantCulture));
                        break;
                    case BuiltInType.Float:
                        binaryEncoder.WriteFloat(null,
                            Convert.ToSingle(valueToEncode, CultureInfo.InvariantCulture));
                        break;
                    case BuiltInType.Double:
                        binaryEncoder.WriteDouble(null,
                            Convert.ToDouble(valueToEncode, CultureInfo.InvariantCulture));
                        break;
                    case BuiltInType.DateTime:
                        binaryEncoder.WriteDateTime(null,
                            Convert.ToDateTime(valueToEncode, CultureInfo.InvariantCulture));
                        break;
                    case BuiltInType.Enumeration:
                        binaryEncoder.WriteInt32(null,
                            Convert.ToInt32(valueToEncode, CultureInfo.InvariantCulture));
                        break;
                    case BuiltInType.Guid:
                        binaryEncoder.WriteGuid(null, (Uuid)valueToEncode);
                        break;
                    case BuiltInType.String:
                        binaryEncoder.WriteString(null, valueToEncode as string);
                        break;
                    case BuiltInType.ByteString:
                        binaryEncoder.WriteByteString(null, (byte[])valueToEncode);
                        break;
                    case BuiltInType.QualifiedName:
                        binaryEncoder.WriteQualifiedName(null, valueToEncode as QualifiedName);
                        break;
                    case BuiltInType.LocalizedText:
                        binaryEncoder.WriteLocalizedText(null, valueToEncode as LocalizedText);
                        break;
                    case BuiltInType.NodeId:
                        binaryEncoder.WriteNodeId(null, valueToEncode as NodeId);
                        break;
                    case BuiltInType.ExpandedNodeId:
                        binaryEncoder.WriteExpandedNodeId(null, valueToEncode as ExpandedNodeId);
                        break;
                    case BuiltInType.StatusCode:
                        binaryEncoder.WriteStatusCode(null, (StatusCode)valueToEncode);
                        break;
                    case BuiltInType.XmlElement:
                        binaryEncoder.WriteXmlElement(null, valueToEncode as XmlElement);
                        break;
                    case BuiltInType.ExtensionObject:
                        binaryEncoder.WriteExtensionObject(null, valueToEncode as ExtensionObject);
                        break;
                }
            }
            else if (valueRank >= ValueRanks.OneDimension)
            {
                binaryEncoder.WriteArray(null, valueToEncode, valueRank, builtInType);
            }
        }

        /// <summary>
        /// Decode RawData type (for SimpleTypeDescription!?)
        /// </summary>
        /// <param name="binaryDecoder"></param>
        /// <param name="fieldMetaData"></param>
        /// <returns></returns>
        private static object? ReadRawData(BinaryDecoder binaryDecoder, PublishedFieldMetaDataModel fieldMetaData)
        {
            if (fieldMetaData.BuiltInType != (byte)BuiltInType.Null)
            {
                try
                {
                    switch (fieldMetaData.ValueRank)
                    {
                        case ValueRanks.Scalar:
                            return ReadRawScalar(binaryDecoder, fieldMetaData.BuiltInType);
                        case ValueRanks.OneDimension:
                        case ValueRanks.TwoDimensions:
                            return binaryDecoder.ReadArray(null, fieldMetaData.ValueRank,
                                (BuiltInType)fieldMetaData.BuiltInType);

                        case ValueRanks.OneOrMoreDimensions:
                        case ValueRanks.Any:// Scalar or Array with any number of dimensions
                        case ValueRanks.ScalarOrOneDimension:
                            // not implemented
                            return StatusCodes.BadNotSupported;
                        default:
                            // not implemented
                            return StatusCodes.BadNotSupported;
                    }
                }
                catch (ServiceResultException sre)
                {
                    return sre.StatusCode;
                }
                catch
                {
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
        private static object? ReadRawScalar(BinaryDecoder binaryDecoder, byte builtInType)
        {
            switch ((BuiltInType)builtInType)
            {
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
