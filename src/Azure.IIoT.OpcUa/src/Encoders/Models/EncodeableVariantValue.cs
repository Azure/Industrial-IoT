// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Encoders.Models
{
    using Furly.Extensions.Serializers;
    using Opc.Ua;
    using System;
    using System.Text.Json.Nodes;

    /// <summary>
    /// Encodeable wrapper for Json tokens
    /// </summary>
    public sealed class EncodeableVariantValue : IEncodeable, IJsonEncodeable
    {
        /// <summary>
        /// The encoded object
        /// </summary>
        public JsonNode? Value { get; private set; }

        /// <summary>
        /// Create encodeable token
        /// </summary>
        /// <param name="serializer"></param>
        /// <param name="value"></param>
        public EncodeableVariantValue(IJsonSerializer serializer, JsonNode? value = null)
        {
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            Value = value;
        }

        /// <inheritdoc/>
        public ExpandedNodeId TypeId =>
            "s=" + nameof(EncodeableVariantValue);

        /// <inheritdoc/>
        public ExpandedNodeId BinaryEncodingId =>
            "s=" + nameof(EncodeableVariantValue) + "_Encoding_DefaultBinary";

        /// <inheritdoc/>
        public ExpandedNodeId XmlEncodingId =>
            "s=" + nameof(EncodeableVariantValue) + "_Encoding_DefaultXml";

        /// <inheritdoc/>
        public ExpandedNodeId JsonEncodingId =>
            "s=" + nameof(EncodeableVariantValue) + "_Encoding_DefaultJson";

        /// <inheritdoc/>
        public void Decode(IDecoder decoder)
        {
            Value = JsonNode.Parse(decoder.ReadString(nameof(Value)));
        }

        /// <inheritdoc/>
        public void Encode(IEncoder encoder)
        {
            encoder.WriteString(nameof(Value), Value?.ToJsonString() ?? "null");
        }

        /// <inheritdoc/>
        public bool IsEqual(IEncodeable encodeable)
        {
            if (encodeable is EncodeableVariantValue wrapper)
            {
                return JsonNode.DeepEquals(wrapper.Value, Value);
            }
            return false;
        }

        /// <inheritdoc/>
        public object Clone()
        {
            return new EncodeableVariantValue(_serializer, Value?.DeepClone());
        }

        private readonly IJsonSerializer _serializer;
    }
}
