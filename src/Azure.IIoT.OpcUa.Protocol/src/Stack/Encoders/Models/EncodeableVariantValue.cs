// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Opc.Ua.Encoders {
    using Microsoft.Azure.IIoT.Serializers;
    using System;

    /// <summary>
    /// Encodeable wrapper for Json tokens
    /// </summary>
    public sealed class EncodeableVariantValue : IEncodeable {

        /// <summary>
        /// The encoded object
        /// </summary>
        public VariantValue Value { get; private set; }

        /// <summary>
        /// Create encodeable token
        /// </summary>
        /// <param name="serializer"></param>
        /// <param name="value"></param>
        public EncodeableVariantValue(IJsonSerializer serializer, VariantValue value = null) {
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            Value = value;
        }

        /// <inheritdoc/>
        public ExpandedNodeId TypeId =>
            nameof(EncodeableVariantValue);

        /// <inheritdoc/>
        public ExpandedNodeId BinaryEncodingId =>
            nameof(EncodeableVariantValue) + "_Encoding_DefaultBinary";

        /// <inheritdoc/>
        public ExpandedNodeId XmlEncodingId =>
            nameof(EncodeableVariantValue) + "_Encoding_DefaultXml";

        /// <inheritdoc/>
        public void Decode(IDecoder decoder) {
            Value = _serializer.Parse(decoder.ReadString(nameof(Value)));
        }

        /// <inheritdoc/>
        public void Encode(IEncoder encoder) {
            encoder.WriteString(nameof(Value), _serializer.SerializeToString(Value));
        }

        /// <inheritdoc/>
        public bool IsEqual(IEncodeable encodeable) {
            if (encodeable is EncodeableVariantValue wrapper) {
                return wrapper.Value == Value;
            }
            return false;
        }

        private readonly IJsonSerializer _serializer;
    }
}
