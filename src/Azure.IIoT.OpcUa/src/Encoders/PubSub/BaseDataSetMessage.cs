// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Encoders.PubSub
{
    using Azure.IIoT.OpcUa.Encoders.Models;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using System;

    /// <summary>
    /// Data set message
    /// </summary>
    public abstract class BaseDataSetMessage
    {
        /// <summary>
        /// Content mask
        /// </summary>
        public DataSetMessageContentFlags DataSetMessageContentMask { get; set; }

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
        public Opc.Ua.ConfigurationVersionDataType? MetaDataVersion { get; set; }

        /// <summary>
        /// Sequence number
        /// </summary>
        public uint SequenceNumber { get; set; }

        /// <summary>
        /// Timestamp
        /// </summary>
        public DateTimeOffset? Timestamp { get; set; }

        /// <summary>
        /// Picoseconds
        /// </summary>
        public uint Picoseconds { get; set; }

        /// <summary>
        /// Status
        /// </summary>
        public Opc.Ua.StatusCode? Status { get; set; }

        /// <summary>
        /// Payload
        /// </summary>
        public DataSet Payload { get; set; } = new DataSet();

        /// <summary>
        /// Endpoint url
        /// </summary>
        public string? EndpointUrl { get; set; }

        /// <summary>
        /// Application uri
        /// </summary>
        public string? ApplicationUri { get; set; }

        /// <inheritdoc/>
        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(this, obj))
            {
                return true;
            }
            if (obj is not BaseDataSetMessage wrapper)
            {
                return false;
            }
            if (!Opc.Ua.Utils.IsEqual(wrapper.DataSetWriterId, DataSetWriterId) ||
                !Opc.Ua.Utils.IsEqual(wrapper.SequenceNumber, SequenceNumber) ||
                !Opc.Ua.Utils.IsEqual(wrapper.Status ?? Opc.Ua.StatusCodes.Good, Status ?? Opc.Ua.StatusCodes.Good) ||
                !Opc.Ua.Utils.IsEqual(wrapper.Timestamp, Timestamp) ||
                !Opc.Ua.Utils.IsEqual(wrapper.MessageType, MessageType) ||
                !Opc.Ua.Utils.IsEqual(wrapper.MetaDataVersion, MetaDataVersion) ||
                !Opc.Ua.Utils.IsEqual(wrapper.EndpointUrl, EndpointUrl) ||
                !Opc.Ua.Utils.IsEqual(wrapper.ApplicationUri, ApplicationUri))
            {
                return false;
            }
            if (wrapper.Payload == null || Payload == null)
            {
                return wrapper.Payload == Payload;
            }
            return wrapper.Payload.Equals(Payload);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            var hash = new HashCode();
            hash.Add(MessageType);
            hash.Add(DataSetWriterId);
            hash.Add(SequenceNumber);
            hash.Add(MetaDataVersion);
            hash.Add(Timestamp);
            hash.Add(Picoseconds);
            hash.Add(Status);
            hash.Add(Payload);
            hash.Add(EndpointUrl);
            hash.Add(ApplicationUri);
            return hash.ToHashCode();
        }

        internal const string kMessageTypeName = "DataSetMessage";
    }
}
