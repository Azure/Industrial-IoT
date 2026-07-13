// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Encoders.Models
{
    using Opc.Ua;
    using System.Text.Json;

    /// <summary>
    /// Encodeable wrapper for Json tokens
    /// </summary>
    public sealed class EncodeableJToken : IEncodeable
    {
        /// <summary>
        /// The encoded object
        /// </summary>
        public JsonElement JToken { get; private set; }

        /// <summary>
        /// Create encodeable token
        /// </summary>
        /// <param name="json"></param>
        /// <param name="typeId"></param>
        public EncodeableJToken(JsonElement json, ExpandedNodeId typeId)
        {
            JToken = json.Clone();
            TypeId = typeId;
        }

        /// <inheritdoc/>
        public ExpandedNodeId TypeId { get; private set; }

        /// <inheritdoc/>
        public ExpandedNodeId JsonEncodingId =>
            (ExpandedNodeId)("s=" + nameof(EncodeableJToken) + "_Encoding_DefaultJson");

        /// <inheritdoc/>
        public ExpandedNodeId BinaryEncodingId =>
            (ExpandedNodeId)("s=" + nameof(EncodeableJToken) + "_Encoding_DefaultBinary");

        /// <inheritdoc/>
        public ExpandedNodeId XmlEncodingId =>
            (ExpandedNodeId)("s=" + nameof(EncodeableJToken) + "_Encoding_DefaultXml");

        /// <inheritdoc/>
        public void Decode(IDecoder decoder)
        {
            TypeId = decoder.ReadExpandedNodeId(nameof(TypeId));
            using var document = JsonDocument.Parse(decoder.ReadString(nameof(JToken)));
            JToken = document.RootElement.Clone();
        }

        /// <inheritdoc/>
        public void Encode(IEncoder encoder)
        {
            encoder.WriteExpandedNodeId(nameof(TypeId), TypeId);
            encoder.WriteString(nameof(JToken), JToken.GetRawText());
        }

        /// <inheritdoc/>
        public bool IsEqual(IEncodeable encodeable)
        {
            if (encodeable is EncodeableJToken wrapper)
            {
                return TypeId == wrapper.TypeId &&
                    JsonElement.DeepEquals(wrapper.JToken, JToken);
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
