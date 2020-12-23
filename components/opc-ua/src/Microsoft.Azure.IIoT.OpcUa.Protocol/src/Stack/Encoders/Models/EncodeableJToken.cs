// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Opc.Ua.Encoders {
    using Newtonsoft.Json.Linq;
    using System;

    /// <summary>
    /// Encodeable wrapper for Json tokens
    /// </summary>
    public sealed class EncodeableJToken : IEncodeable {

        /// <summary>
        /// The encoded object
        /// </summary>
        public JToken JToken { get; private set; }

        /// <summary>
        /// Create encodeable token
        /// </summary>
        /// <param name="jToken"></param>
        public EncodeableJToken(JToken jToken) {
            JToken = jToken ?? throw new ArgumentNullException(nameof(jToken));
        }

        /// <inheritdoc/>
        public ExpandedNodeId TypeId =>
            nameof(EncodeableJToken);

        /// <inheritdoc/>
        public ExpandedNodeId BinaryEncodingId =>
            nameof(EncodeableJToken) + "_Encoding_DefaultBinary";

        /// <inheritdoc/>
        public ExpandedNodeId XmlEncodingId =>
            nameof(EncodeableJToken) + "_Encoding_DefaultXml";

        /// <inheritdoc/>
        public void Decode(IDecoder decoder) {
            JToken = JToken.Parse(decoder.ReadString(nameof(JToken)));
        }

        /// <inheritdoc/>
        public void Encode(IEncoder encoder) {
            encoder.WriteString(nameof(JToken), JToken.ToString());
        }

        /// <inheritdoc/>
        public bool IsEqual(IEncodeable encodeable) {
            if (encodeable is EncodeableJToken wrapper) {
                return JToken.EqualityComparer.Equals(wrapper.JToken, JToken);
            }
            return false;
        }
    }
}
