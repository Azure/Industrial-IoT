// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Stack.Models
{
    using Opc.Ua;

    /// <summary>
    /// Represents a sampled data value
    /// </summary>
    public class SampledDataValueModel : IEncodeable
    {
        /// <summary>
        /// Value
        /// </summary>
        public DataValue Value { get; }

        /// <summary>
        /// Client handle
        /// </summary>
        public uint ClientHandle { get; }

        /// <summary>
        /// Overflow
        /// </summary>
        public int Overflow { get; }

        /// <summary>
        /// Create change notification
        /// </summary>
        /// <param name="value"></param>
        /// <param name="clientHandle"></param>
        /// <param name="overflow"></param>
        public SampledDataValueModel(DataValue value,
            uint clientHandle, int overflow)
        {
            Value = value;
            ClientHandle = clientHandle;
            Overflow = overflow;
        }

        /// <inheritdoc/>
        public ExpandedNodeId TypeId => ExpandedNodeId.Null;
        /// <inheritdoc/>
        public ExpandedNodeId BinaryEncodingId => ExpandedNodeId.Null;
        /// <inheritdoc/>
        public ExpandedNodeId XmlEncodingId => ExpandedNodeId.Null;

        /// <inheritdoc/>
        public object Clone()
        {
            return new SampledDataValueModel(Value, ClientHandle, Overflow);
        }

        /// <inheritdoc/>
        public void Decode(IDecoder decoder)
        {
            throw new System.NotSupportedException();
        }

        /// <inheritdoc/>
        public void Encode(IEncoder encoder)
        {
            throw new System.NotSupportedException();
        }

        /// <inheritdoc/>
        public bool IsEqual(IEncodeable encodeable)
        {
            if (encodeable is not SampledDataValueModel notification)
            {
                return false;
            }
            return
                Utils.IsEqual(Value, notification.Value) &&
                ClientHandle == notification.ClientHandle &&
                Overflow == notification.Overflow;
        }
    }
}
