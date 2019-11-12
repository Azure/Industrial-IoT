// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Opc.Ua.PubSub {
    using System;
    using System.Collections.Generic;
    using Opc.Ua;

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
            // TODO
            throw new NotImplementedException();
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
        public bool IsEqual(IEncodeable encodeable) {
            // TODO
            throw new NotImplementedException();
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

        /// <summary>
        /// Encode as binary
        /// </summary>
        /// <param name="encoder"></param>
        private void EncodeBinary(IEncoder encoder) {
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
    }
}