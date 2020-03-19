// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Opc.Ua.PubSub {
    using System.Collections.Generic;

    /// <summary>
    /// Encodable dataset message payload
    /// </summary>
    public class DataSet : Dictionary<string, DataValue> {

        /// <summary>
        /// Create payload
        /// </summary>
        /// <param name="values"></param>
        /// <param name="fieldContentMask"></param>
        public DataSet(IDictionary<string, DataValue> values, uint fieldContentMask) : this() {
            FieldContentMask = fieldContentMask;
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

        /// <summary>
        /// Encode a field
        /// </summary>
        /// <param name="encoder"></param>
        /// <param name="fieldName"></param>
        /// <param name="value"></param>
        private void EncodeField(IEncoder encoder, string fieldName, DataValue value) {
            if (FieldContentMask != 0) {
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