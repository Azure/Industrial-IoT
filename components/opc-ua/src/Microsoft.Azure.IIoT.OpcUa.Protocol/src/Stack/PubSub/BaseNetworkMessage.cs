// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Opc.Ua.PubSub {
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Encodeable Network message
    /// <see href="https://reference.opcfoundation.org/v104/Core/docs/Part14/7.2.3/"/>
    /// </summary>
    public abstract class BaseNetworkMessage : PubSubMessage {

        /// <summary>
        /// Message content
        /// </summary>
        public uint NetworkMessageContentMask { get; set; }

        /// <summary>
        /// Message type
        /// </summary>
        public override string MessageType => MessageTypeUaData;

        /// <summary>
        /// Dataset class id in case of ua-data message
        /// </summary>
        public Guid DataSetClassId { get; set; }

        /// <summary>
        /// DataSet Messages
        /// </summary>
        public List<BaseDataSetMessage> Messages { get; set; } = new List<BaseDataSetMessage>();

        /// <inheritdoc/>
        public override bool Equals(object value) {
            if (ReferenceEquals(this, value)) {
                return true;
            }
            if (!(value is BaseNetworkMessage wrapper)) {
                return false;
            }
            if (!base.Equals(value)) {
                return false;
            }
            if (!Utils.IsEqual(wrapper.NetworkMessageContentMask, NetworkMessageContentMask) ||
                !Utils.IsEqual(wrapper.DataSetClassId, DataSetClassId) ||
                !Utils.IsEqual(wrapper.Messages, Messages)) {
                return false;
            }
            return true;
        }

        /// <inheritdoc/>
        public override int GetHashCode() {
            var hash = new HashCode();
            hash.Add(base.GetHashCode());
            hash.Add(NetworkMessageContentMask);
            hash.Add(DataSetClassId);
            hash.Add(Messages);
            return hash.ToHashCode();
        }

        /// <inheritdoc/>
        protected sealed class EncodeableAdapter : IEncodeable {
            /// <inheritdoc/>
            public ExpandedNodeId TypeId { get; }
            /// <inheritdoc/>
            public ExpandedNodeId BinaryEncodingId { get; }
            /// <inheritdoc/>
            public ExpandedNodeId XmlEncodingId { get; }

            /// <inheritdoc/>
            public EncodeableAdapter(BaseDataSetMessage message, bool withHeader,
                uint dataSetFieldContentMask = 0xffff) {
                _message = message;
                _dataSetFieldContentMask = dataSetFieldContentMask;
                _withHeader = withHeader;
            }

            /// <inheritdoc/>
            public void Decode(IDecoder decoder) {
                _message.Decode(decoder, _dataSetFieldContentMask, _withHeader);
            }

            /// <inheritdoc/>
            public void Encode(IEncoder encoder) {
                _message.Encode(encoder, _withHeader);
            }

            /// <inheritdoc/>
            public bool IsEqual(IEncodeable encodeable) {
                return _message.Equals(encodeable);
            }
            private readonly BaseDataSetMessage _message;
            private readonly uint _dataSetFieldContentMask;
            private readonly bool _withHeader;
        }
    }
}