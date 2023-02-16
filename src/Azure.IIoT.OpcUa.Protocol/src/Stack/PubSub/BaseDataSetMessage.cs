// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Opc.Ua.PubSub {
    using Opc.Ua.Encoders;
    using System;

    /// <summary>
    /// Data set message
    /// </summary>
    public abstract class BaseDataSetMessage {

        /// <summary>
        /// Content mask
        /// </summary>
        public uint DataSetMessageContentMask { get; set; }

        /// <summary>
        /// Dataset message type
        /// </summary>
        public MessageType MessageType { get; set; }

        /// <summary>
        /// Dataset writer id
        /// </summary>
        public ushort DataSetWriterId { get; set; }

        /// <summary>
        /// Metadata version
        /// </summary>
        public ConfigurationVersionDataType MetaDataVersion { get; set; }

        /// <summary>
        /// Sequence number
        /// </summary>
        public uint SequenceNumber { get; set; }

        /// <summary>
        /// Timestamp
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Picoseconds
        /// </summary>
        public uint Picoseconds { get; set; }

        /// <summary>
        /// Status
        /// </summary>
        public StatusCode? Status { get; set; }

        /// <summary>
        /// Payload
        /// </summary>
        public DataSet Payload { get; set; } = new DataSet();

        /// <inheritdoc/>
        public override bool Equals(object value) {
            if (ReferenceEquals(this, value)) {
                return true;
            }
            if (!(value is BaseDataSetMessage wrapper)) {
                return false;
            }
            if (!Utils.IsEqual(wrapper.DataSetWriterId, DataSetWriterId) ||
                !Utils.IsEqual(wrapper.SequenceNumber, SequenceNumber) ||
                !Utils.IsEqual(wrapper.Status, Status) ||
                !Utils.IsEqual(wrapper.Timestamp, Timestamp) ||
                !Utils.IsEqual(wrapper.MessageType, MessageType) ||
                !Utils.IsEqual(wrapper.MetaDataVersion, MetaDataVersion)) {
                return false;
            }
            if (wrapper.Payload == null || Payload == null) {
                return wrapper.Payload == Payload;
            }
            return wrapper.Payload.Equals(Payload);
        }

        /// <inheritdoc/>
        public override int GetHashCode() {
            var hash = new HashCode();
            hash.Add(MessageType);
            hash.Add(DataSetWriterId);
            hash.Add(SequenceNumber);
            hash.Add(MetaDataVersion);
            hash.Add(Timestamp);
            hash.Add(Picoseconds);
            hash.Add(Status);
            hash.Add(Payload);
            return hash.ToHashCode();
        }
    }
}