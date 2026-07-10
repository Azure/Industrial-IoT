// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Encoders.PubSub
{
    using Azure.IIoT.OpcUa.Encoders;
    using Azure.IIoT.OpcUa.Encoders.Utils;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Opc.Ua;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Samples message
    /// </summary>
    public class MonitoredItemMessage : JsonDataSetMessage
    {
        /// <summary>
        /// Node Id in string format as configured
        /// </summary>
        public string? NodeId { get; set; }

        /// <summary>
        /// Writer group name (dont change then name for backcompat)
        /// </summary>
        public string? WriterGroupId { get; set; }

        /// <summary>
        /// Display name
        /// </summary>
        public string? DisplayName => Payload.DataSetFields.SingleOrDefault().Name;

        /// <summary>
        /// Data value for variable change notification
        /// </summary>
        public Opc.Ua.DataValue? Value => Payload.DataSetFields.SingleOrDefault().Value;

        /// <summary>
        /// Extension fields
        /// </summary>
        public IReadOnlyList<ExtensionFieldModel>? ExtensionFields { get; set; }

        /// <inheritdoc/>
        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(this, obj))
            {
                return true;
            }
            if (obj is not MonitoredItemMessage wrapper)
            {
                return false;
            }
            if (!base.Equals(obj))
            {
                return false;
            }
            if (!Opc.Ua.Utils.IsEqual(wrapper.NodeId, NodeId))
            {
                return false;
            }
            if (!wrapper.ExtensionFields.SetEqualsSafe(ExtensionFields,
                (a, b) => a?.Equals(b) ?? b == null))
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

            hash.Add(NodeId);
            hash.Add(ExtensionFields);
            return hash.ToHashCode();
        }

        /// <inheritdoc/>
        // TODO(Phase 5): Reimplement legacy samples (monitored item) JSON encoding
        // on the UA-.NETStandard 2.0 Opc.Ua.JsonEncoder.
        internal override void Encode(Opc.Ua.IEncoder encoder, string? publisherId, bool withHeader,
            string? property)
        {
            throw new NotSupportedException(
                "Monitored item message encoding is deferred to Phase 5.");
        }

        /// <inheritdoc/>
        // TODO(Phase 5): Reimplement legacy samples (monitored item) JSON decoding
        // on the UA-.NETStandard 2.0 Opc.Ua.JsonDecoder.
        internal override bool TryDecode(Opc.Ua.IDecoder decoder, string? property, ref bool withHeader,
            ref string? publisherId)
        {
            throw new NotSupportedException(
                "Monitored item message decoding is deferred to Phase 5.");
        }
    }
}
