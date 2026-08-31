// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Encoders.Models
{
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
                PubSubMessageDefaults.DefaultDataSetFieldContentFlags;
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
        /// <remarks>
        /// The field names are folded in one at a time. Passing the
        /// <c>Select</c> result to <see cref="HashCode.Combine{T1}(T1)"/>
        /// hashed the iterator object rather than the names it yields, so the
        /// same instance produced a different hash on every call and two equal
        /// data sets never agreed - which silently breaks any use as a
        /// dictionary key or set member.
        /// <para>
        /// Only the names participate, while <see cref="Equals(object?)"/> also
        /// compares values. That is allowed and deliberate: equal data sets
        /// have equal names and so hash alike, which is the contract; data sets
        /// that differ only by value merely collide.
        /// </para>
        /// </remarks>
        public override int GetHashCode()
        {
            var hash = new HashCode();
            if (DataSetFields != null)
            {
                foreach (var field in DataSetFields)
                {
                    hash.Add(field.Name);
                }
            }
            return hash.ToHashCode();
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
