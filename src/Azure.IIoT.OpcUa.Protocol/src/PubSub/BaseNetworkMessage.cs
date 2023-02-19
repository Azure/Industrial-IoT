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
            if (!Utils.IsEqual(wrapper.DataSetClassId, DataSetClassId) ||
                !Utils.IsEqual(wrapper.Messages, Messages)) {
                return false;
            }
            return true;
        }

        /// <inheritdoc/>
        public override int GetHashCode() {
            var hash = new HashCode();
            hash.Add(base.GetHashCode());
            hash.Add(DataSetClassId);
            hash.Add(Messages);
            return hash.ToHashCode();
        }
    }
}