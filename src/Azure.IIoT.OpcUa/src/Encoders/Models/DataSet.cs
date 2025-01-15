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
    using System.Linq;

    /// <summary>
    /// Encodable dataset message payload
    /// </summary>
    public class DataSet
    {
        /// <summary>
        /// Field mask
        /// </summary>
        public DataSetFieldContentFlags DataSetFieldContentMask { get; set; }

        /// <summary>
        /// Entries
        /// </summary>
        public IReadOnlyList<(string Name, DataValue? Value)> DataSetFields { get; }

        /// <summary>
        /// Create payload
        /// </summary>
        /// <param name="values"></param>
        /// <param name="fieldContentMask"></param>
        public DataSet(IDictionary<string, DataValue?> values,
            DataSetFieldContentFlags? fieldContentMask = null)
            : this(fieldContentMask)
        {
            DataSetFields = values.Select(kv => (kv.Key, kv.Value)).ToList();
        }

        /// <summary>
        /// Create payload
        /// </summary>
        /// <param name="values"></param>
        /// <param name="fieldContentMask"></param>
        public DataSet(IReadOnlyList<(string, DataValue?)> values,
            DataSetFieldContentFlags? fieldContentMask)
            : this(fieldContentMask)
        {
            DataSetFields = values;
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
            DataSetFields = new[] { (field, value) };
        }

        /// <summary>
        /// Create default dataset
        /// </summary>
        /// <param name="fieldContentMask"></param>
        public DataSet(DataSetFieldContentFlags? fieldContentMask = null)
        {
            DataSetFieldContentMask = fieldContentMask ??
                PubSubMessage.DefaultDataSetFieldContentFlags;
            DataSetFields = Array.Empty<(string, DataValue?)>();
        }

        /// <inheritdoc/>
        public override bool Equals(object? obj)
        {
            if (obj is not DataSet set)
            {
                return false;
            }
            if (!DataSetFields.SequenceEqualsSafe(set.DataSetFields,
                (x, y) => x.Name == y.Name &&
                    Utils.IsEqual(x.Value?.Value, y.Value?.Value)))
            {
                return false;
            }
            return true;
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return HashCode.Combine(DataSetFields.Select(s => s.Name));
        }

        /// <summary>
        /// Remove field from dataset
        /// </summary>
        /// <param name="field"></param>
        /// <returns></returns>
        internal DataSet Remove(string field)
        {
            return new DataSet(DataSetFields
                .Where(b => b.Name != field)
                .ToList(), DataSetFieldContentMask);
        }

        /// <summary>
        /// Set field from dataset to different value
        /// </summary>
        /// <param name="field"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        internal DataSet Set(string field, DataValue? value)
        {
            return new DataSet(DataSetFields
                .Select(b => (b.Name, b.Name == field ? value : b.Value))
                .ToList(), DataSetFieldContentMask);
        }

        /// <summary>
        /// Set field from dataset to different value
        /// </summary>
        /// <param name="field"></param>
        /// <param name="value"></param>
        /// <param name="additionalFlags"></param>
        /// <returns></returns>
        internal DataSet Add(string field, DataValue? value,
            DataSetFieldContentFlags? additionalFlags = null)
        {
            var fieldContentMask = DataSetFieldContentMask;
            if (additionalFlags.HasValue)
            {
                fieldContentMask |= additionalFlags.Value;
            }
            return new DataSet(DataSetFields
                .Append((field, value))
                .ToList(), fieldContentMask);
        }
    }
}
