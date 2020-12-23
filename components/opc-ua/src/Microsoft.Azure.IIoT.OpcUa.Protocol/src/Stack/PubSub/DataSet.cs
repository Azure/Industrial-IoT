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
    }
}