// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Opc.Ua.PubSub {
    using System;
    using System.Collections.Generic;
    using Opc.Ua.Encoders;

    /// <summary>
    /// Encodable dataset message payload
    /// </summary>
    public class DataSet : Dictionary<string, DataValue>, IEncodeable {

        /// <summary>
        /// Create payload
        /// </summary>
        /// <param name="values"></param>
        public DataSet(IDictionary<string, DataValue> values) : this() {
            foreach (var value in values) {
                this[value.Key] = value.Value;
            }
        }

        /// <summary>
        /// Create default
        /// </summary>
        public DataSet() {
            FieldContentMask = 0;
        }

        /// <summary>
        /// Field content mask
        /// </summary>
        public uint FieldContentMask { get; set; }

        /// <inheritdoc/>
        public ExpandedNodeId TypeId => ExpandedNodeId.Null;

        /// <inheritdoc/>
        public ExpandedNodeId BinaryEncodingId => ExpandedNodeId.Null;

        /// <inheritdoc/>
        public ExpandedNodeId XmlEncodingId => ExpandedNodeId.Null;

        /// <inheritdoc/>
        public void Decode(IDecoder decoder) {
            switch (decoder.EncodingType) {
                case EncodingType.Binary:
                    DecodeBinary(decoder);
                    break;
                case EncodingType.Json:
                    DecodeJson(decoder);
                    break;
                case EncodingType.Xml:
                    throw new NotImplementedException("XML decoding is not implemented.");
                default:
                    throw new NotImplementedException($"Unknown encoding: {decoder.EncodingType}");
            }
        }

        /// <inheritdoc/>
        public void Encode(IEncoder encoder) {
            switch (encoder.EncodingType) {
                case EncodingType.Binary:
                    EncodeBinary(encoder);
                    break;
                case EncodingType.Json:
                    EncodeJson(encoder);
                    break;
                case EncodingType.Xml:
                    throw new NotImplementedException("XML encoding is not implemented.");
                default:
                    throw new NotImplementedException($"Unknown encoding: {encoder.EncodingType}");
            }
        }

        /// <inheritdoc/>
        public override bool Equals(object value) {
            return IsEqual(value as IEncodeable);
        }

        /// <inheritdoc/>
        public override int GetHashCode() {
            return base.GetHashCode();
        }

        /// <inheritdoc/>
        public bool IsEqual(IEncodeable encodeable) {
            if (ReferenceEquals(this, encodeable)) {
                return true;
            }

            if (!(encodeable is DataSet wrapper)) {
                return false;
            }
            if (wrapper.Count != Count) {
                return false;
            }

            foreach (var value in wrapper) {
                if (!Utils.IsEqual(wrapper[value.Key], this[value.Key])) {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Encode as json
        /// </summary>
        /// <param name="encoder"></param>
        private void EncodeJson(IEncoder encoder) {
            foreach (var item in this) {
                EncodeField(encoder, item.Key, item.Value);
            }
        }

#pragma warning disable IDE0060 // Remove unused parameter
        /// <summary>
        /// Encode as binary
        /// </summary>
        /// <param name="encoder"></param>
        private void EncodeBinary(IEncoder encoder) {
#pragma warning restore IDE0060 // Remove unused parameter
            throw new NotImplementedException();
        }

        /// <summary>
        /// Encode a field
        /// </summary>
        /// <param name="encoder"></param>
        /// <param name="fieldName"></param>
        /// <param name="value"></param>
        private void EncodeField(IEncoder encoder, string fieldName, DataValue value) {
            if (FieldContentMask > 0) {
                if ((FieldContentMask & (uint)DataSetFieldContentMask.RawData) != 0) {
                    encoder.WriteRaw(fieldName, value);
                }
                else {
                    var dv = new DataValue {
                        WrappedValue = value.WrappedValue
                    };
                    if ((FieldContentMask & (uint)DataSetFieldContentMask.StatusCode) != 0) {
                        dv.StatusCode = value.StatusCode;
                    }
                    if ((FieldContentMask & (uint)DataSetFieldContentMask.SourceTimestamp) != 0) {
                        dv.SourceTimestamp = value.SourceTimestamp;
                    }
                    if ((FieldContentMask & (uint)DataSetFieldContentMask.SourcePicoSeconds) != 0) {
                        dv.SourcePicoseconds = value.SourcePicoseconds;
                    }
                    if ((FieldContentMask & (uint)DataSetFieldContentMask.ServerTimestamp) != 0) {
                        dv.ServerTimestamp = value.ServerTimestamp;
                    }
                    if ((FieldContentMask & (uint)DataSetFieldContentMask.ServerPicoSeconds) != 0) {
                        dv.ServerPicoseconds = value.ServerPicoseconds;
                    }
                    encoder.WriteDataValue(fieldName, dv);
                }
            }
        }

        /// <summary>
        /// Decode as json
        /// </summary>
        /// <param name="decoder"></param>
        private void DecodeJson(IDecoder decoder) {
            if (!(decoder is JsonDecoderEx jsonDecoder)) {
                // report failure
                return;
            }

            var payload = jsonDecoder.ReadDataValueDictionary("Payload");

            if (payload == null) {
                return;
            }

            foreach (var value in payload) {
                Add(value.Key, value.Value);
                if (value.Value != null) {
                    if (value.Value.StatusCode != null) {
                        FieldContentMask |= (uint)DataSetFieldContentMask.StatusCode;
                    }
                    if (value.Value.SourceTimestamp != null) {
                        FieldContentMask |= (uint)DataSetFieldContentMask.SourceTimestamp;
                    }
                    if (value.Value.SourcePicoseconds != 0) {
                        FieldContentMask |= (uint)DataSetFieldContentMask.SourcePicoSeconds;
                    }
                    if (value.Value.ServerTimestamp != null) {
                        FieldContentMask |= (uint)DataSetFieldContentMask.ServerTimestamp;
                    }
                    if (value.Value.ServerPicoseconds != 0) {
                        FieldContentMask |= (uint)DataSetFieldContentMask.ServerPicoSeconds;
                    }
                }
            }
        }

#pragma warning disable IDE0060 // Remove unused parameter
        /// <summary>
        /// Decode as binary
        /// </summary>
        /// <param name="decoder"></param>
        private void DecodeBinary(IDecoder decoder) {
#pragma warning restore IDE0060 // Remove unused parameter
            throw new NotImplementedException("Binary decoding is not implemented.");
        }
    }
}