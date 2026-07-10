// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Encoders.PubSub
{
    using Azure.IIoT.OpcUa.Encoders;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using System;
    using System.Linq;

    /// <summary>
    /// Data set message
    /// </summary>
    public class JsonDataSetMessage : BaseDataSetMessage
    {
        /// <summary>
        /// Compatibility with 2.8 when encoding and decoding
        /// </summary>
        public bool UseCompatibilityMode { get; set; }

        /// <summary>
        /// Dataset writer name
        /// </summary>
        public string? DataSetWriterName { get; set; }

        /// <inheritdoc/>
        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(this, obj))
            {
                return true;
            }
            if (obj is not JsonDataSetMessage wrapper)
            {
                return false;
            }
            if (!base.Equals(obj))
            {
                return false;
            }
            if (!Opc.Ua.Utils.IsEqual(wrapper.DataSetWriterName, DataSetWriterName))
            {
                return false;
            }
            return true;
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            var hash = new HashCode();
            hash.Add(base.GetHashCode());

            hash.Add(DataSetWriterName);
            return hash.ToHashCode();
        }

        /// <inheritdoc/>
        // TODO(Phase 5): Reimplement JSON PubSub dataset encoding on top of the
        // UA-.NETStandard 2.0 Opc.Ua.JsonEncoder. The fork-specific JsonEncoderEx
        // (WriteDataSet / reversible-encoding toggling) was removed in the 2.0 migration.
        internal virtual void Encode(Opc.Ua.IEncoder encoder, string? publisherId, bool withHeader,
            string? property)
        {
            throw new NotSupportedException(
                "JSON PubSub dataset message encoding is deferred to Phase 5.");
        }

        /// <inheritdoc/>
        // TODO(Phase 5): Reimplement JSON PubSub dataset decoding on top of the
        // UA-.NETStandard 2.0 Opc.Ua.JsonDecoder.
        internal virtual bool TryDecode(Opc.Ua.IDecoder jsonDecoder, string? property, ref bool withHeader,
            ref string? publisherId)
        {
            throw new NotSupportedException(
                "JSON PubSub dataset message decoding is deferred to Phase 5.");
        }
    }
}
