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
    public abstract class PubSubMessage {

        /// <summary>
        /// Message id
        /// </summary>
        public string MessageId { get; set; }

        /// <summary>
        /// Message schema
        /// </summary>
        public abstract string MessageSchema { get; }

        /// <summary>
        /// Content type
        /// </summary>
        public abstract string ContentType { get; }

        /// <summary>
        /// Content encoding
        /// </summary>
        public abstract string ContentEncoding { get; }

        /// <summary>
        /// Publisher identifier
        /// </summary>
        public string PublisherId { get; set; }

        /// <summary>
        /// Dataset writerGroup
        /// </summary>
        public string DataSetWriterGroup { get; set; }

        /// <summary>
        /// Decode the network message from the wire representation
        /// </summary>
        /// <param name="context"></param>
        /// <param name="reader"></param>
        public abstract bool TryDecode(IServiceMessageContext context, IEnumerable<byte[]> reader);

        /// <summary>
        /// Encode the network message into network message chunks
        /// wire representation.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="maxChunkSize"></param>
        /// <returns></returns>
        public abstract IReadOnlyList<byte[]> Encode(IServiceMessageContext context, int maxChunkSize);

        /// <inheritdoc/>
        public override bool Equals(object value) {
            if (ReferenceEquals(this, value)) {
                return true;
            }
            if (!(value is UadpNetworkMessage wrapper)) {
                return false;
            }
            if (!Utils.IsEqual(wrapper.MessageId, MessageId) ||
                !Utils.IsEqual(wrapper.DataSetWriterGroup, DataSetWriterGroup) ||
                !Utils.IsEqual(wrapper.PublisherId, PublisherId)) {
                return false;
            }
            return true;
        }

        /// <inheritdoc/>
        public override int GetHashCode() {
            var hash = new HashCode();
            hash.Add(MessageId);
            hash.Add(PublisherId);
            hash.Add(DataSetWriterGroup);
            return hash.ToHashCode();
        }
    }
}