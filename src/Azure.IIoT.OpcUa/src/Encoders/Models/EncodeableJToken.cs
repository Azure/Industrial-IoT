// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Encoders.Models
{
    using Newtonsoft.Json.Linq;
    using Opc.Ua;
    using System;

    /// <summary>
    /// Encodeable wrapper for Json tokens
    /// </summary>
    public sealed class EncodeableJToken : IEncodeable, IJsonEncodeable
    {
        /// <summary>
        /// The encoded object
        /// </summary>
        public JToken JToken { get; private set; }

        /// <summary>
        /// Create encodeable token
        /// </summary>
        /// <param name="jToken"></param>
        /// <param name="typeId"></param>
        public EncodeableJToken(JToken jToken, ExpandedNodeId typeId)
        {
            JToken = jToken ?? throw new ArgumentNullException(nameof(jToken));
            TypeId = typeId;
        }

        /// <inheritdoc/>
        public ExpandedNodeId TypeId { get; private set; }

        /// <inheritdoc/>
        public ExpandedNodeId JsonEncodingId =>
            nameof(EncodeableJToken) + "_Encoding_DefaultJson";

        /// <inheritdoc/>
        public ExpandedNodeId BinaryEncodingId =>
            nameof(EncodeableJToken) + "_Encoding_DefaultBinary";

        /// <inheritdoc/>
        public ExpandedNodeId XmlEncodingId =>
            nameof(EncodeableJToken) + "_Encoding_DefaultXml";

        /// <inheritdoc/>
        public void Decode(IDecoder decoder)
        {
            TypeId = decoder.ReadExpandedNodeId(nameof(TypeId));
            JToken = JToken.Parse(decoder.ReadString(nameof(JToken)));
        }

        /// <inheritdoc/>
        public void Encode(IEncoder encoder)
        {
            encoder.WriteExpandedNodeId(nameof(TypeId), TypeId);
            encoder.WriteString(nameof(JToken), JToken.ToString());
        }

        /// <inheritdoc/>
        public bool IsEqual(IEncodeable encodeable)
        {
            if (encodeable is EncodeableJToken wrapper)
            {
                return TypeId == wrapper.TypeId &&
                    JToken.EqualityComparer.Equals(wrapper.JToken, JToken);
            }
            return false;
        }

        /// <inheritdoc/>
        public object Clone()
        {
            return new EncodeableJToken(JToken, TypeId);
        }
    }
}
