// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Encoders.PubSub
{
    using Opc.Ua;
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Encodeable Network message
    /// <see href="https://reference.opcfoundation.org/v104/Core/docs/Part14/7.2.3/"/>
    /// </summary>
    public abstract class BaseNetworkMessage : PubSubMessage
    {
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
#pragma warning disable CA2227 // Collection properties should be read only
        public IList<BaseDataSetMessage> Messages { get; set; } = new List<BaseDataSetMessage>();
#pragma warning restore CA2227 // Collection properties should be read only

        /// <inheritdoc/>
        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(this, obj))
            {
                return true;
            }
            if (!(obj is BaseNetworkMessage wrapper))
            {
                return false;
            }
            if (!base.Equals(obj))
            {
                return false;
            }
            if (!Utils.IsEqual(wrapper.DataSetClassId, DataSetClassId) ||
                !Utils.IsEqual(wrapper.Messages, Messages))
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
            hash.Add(DataSetClassId);
            hash.Add(Messages);
            return hash.ToHashCode();
        }
    }
}
