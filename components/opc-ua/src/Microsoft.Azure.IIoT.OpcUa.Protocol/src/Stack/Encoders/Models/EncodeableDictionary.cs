// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Opc.Ua.Encoders {
    using Opc.Ua;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Encodeable dictionary carrying field names and values
    /// </summary>
    public class EncodeableDictionary : List<KeyDataValuePair>, IEncodeable {
        /// <inheritdoc/>
        public ExpandedNodeId TypeId =>
            ExpandedNodeId.Parse("nsu=http://microsoft.com/Industrial-IoT/OpcPublisher;i=1");

        /// <inheritdoc/>
        public ExpandedNodeId BinaryEncodingId =>
            nameof(EncodeableDictionary) + "_Encoding_DefaultBinary";

        /// <inheritdoc/>
        public ExpandedNodeId XmlEncodingId =>
            nameof(EncodeableDictionary) + "_Encoding_DefaultXml";

        /// <inheritdoc/>
        public ExpandedNodeId JsonEncodingId =>
            nameof(EncodeableDictionary) + "_Encoding_DefaultJson";

        /// <summary>
        /// Initializes the dictionary with default values.
        /// </summary>
        public EncodeableDictionary() { }

        /// <summary>
        /// Initializes the dictionary with an initial capacity.
        /// </summary>
        public EncodeableDictionary(int capacity) : base(capacity) { }

        /// <summary>
        /// Initializes the dictionary with another collection.
        /// </summary>
        public EncodeableDictionary(IEnumerable<KeyDataValuePair> collection) : base(collection) { }

        /// <inheritdoc/>
        public virtual void Encode(IEncoder encoder) {
            // Get valid dictionary for encoding.
            var dictionary = this
                .Where(x => !string.IsNullOrEmpty(x.Key))
                .Where(x => x.Value != null)
                .Where(x => x.Value.Value != null)
                .Where(x => !(x.Value.Value is LocalizedText lt) || lt.Locale != null || lt.Text != null)
                .ToDictionary(x => x.Key, x => x.Value);

            foreach (var keyValuePair in dictionary) {
                encoder.WriteDataValue(keyValuePair.Key, keyValuePair.Value);
            }
        }

        /// <inheritdoc/>
        public virtual void Decode(IDecoder decoder) {
            // Only JSON decoder that can decode a dictionary is supported.
            if (!(decoder is JsonDecoderEx jsonDecoder)) {
                throw new Exception($"Cannot decode using the decoder: {decoder.GetType()}.");
            }
            var dictionary = jsonDecoder.ReadDataSet(null);
            foreach (var keyValuePair in dictionary) {
                Add(new KeyDataValuePair {
                    Key = keyValuePair.Key,
                    Value = keyValuePair.Value
                });
            }
        }

        /// <inheritdoc/>
        public virtual bool IsEqual(IEncodeable encodeable) {
            if (this == encodeable) {
                return true;
            }
            if (!(encodeable is EncodeableDictionary encodableDictionary)) {
                return false;
            }
            if (!Utils.IsEqual(this, encodableDictionary)) {
                return false;
            }
            return true;
        }
    }
}