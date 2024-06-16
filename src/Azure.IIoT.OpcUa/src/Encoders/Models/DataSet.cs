// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Encoders.Models
{
    using Azure.IIoT.OpcUa.Encoders.PubSub;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Opc.Ua;
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Encodable dataset message payload
    /// </summary>
    public class DataSet : Dictionary<string, DataValue?>
    {
        /// <summary>
        /// Field mask
        /// </summary>
        public DataSetFieldContentFlags DataSetFieldContentMask { get; set; }

        /// <summary>
        /// Create payload
        /// </summary>
        /// <param name="values"></param>
        /// <param name="fieldContentMask"></param>
        public DataSet(IDictionary<string, DataValue?> values,
            DataSetFieldContentFlags? fieldContentMask)
            : this(fieldContentMask)
        {
            foreach (var value in values)
            {
                this[value.Key] = value.Value;
            }
        }

        /// <summary>
        /// Create payload
        /// </summary>
        /// <param name="field"></param>
        /// <param name="value"></param>
        /// <param name="fieldContentMask"></param>
        public DataSet(string field, DataValue? value,
            DataSetFieldContentFlags? fieldContentMask)
            : this(fieldContentMask)
        {
            this[field] = value;
        }

        /// <summary>
        /// Create default dataset
        /// </summary>
        /// <param name="fieldContentMask"></param>
        public DataSet(DataSetFieldContentFlags? fieldContentMask = null)
        {
            DataSetFieldContentMask = fieldContentMask ??
                PubSubMessage.DefaultDataSetFieldContentFlags;
        }

        /// <inheritdoc/>
        public override bool Equals(object? obj)
        {
            if (obj is not Dictionary<string, DataValue> set)
            {
                return false;
            }
            if (!Keys.SequenceEqualsSafe(set.Keys))
            {
                return false;
            }
            if (!Values.SequenceEqualsSafe(set.Values,
                (x, y) => Utils.IsEqual(x?.Value, y?.Value)))
            {
                return false;
            }
            return true;
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return HashCode.Combine(Keys);
        }
    }
}
