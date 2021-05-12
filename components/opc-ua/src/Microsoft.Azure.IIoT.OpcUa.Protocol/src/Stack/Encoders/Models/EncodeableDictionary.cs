// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Opc.Ua.Encoders {
    using Opc.Ua;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Encodeable dictionary carrying field names and values
    /// </summary>
    public class EncodeableDictionary : List<KeyDataValuePair>, IEncodeable {

        /// <summary>
        /// Identifier for the keys in the reversible format.
        /// </summary>
        private const string kKeysIdentifier = "_Keys";

        /// <summary>
        /// Identifier for the number of key-value pairs in the reversible format.
        /// </summary>
        private const string kCountIdentifier = "_Count";

        /// <inheritdoc/>
        public ExpandedNodeId TypeId =>
            nameof(EncodeableDictionary);

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
            if (encoder.UseReversibleEncoding) {
                if (this.Any(x => x.Key == kKeysIdentifier)) {
                    throw new ServiceResultException(StatusCodes.BadEncodingError,
                        $"The key '{kKeysIdentifier}' is already in use.");
                }

                if (this.Any(x => x.Key == kCountIdentifier)) {
                    throw new ServiceResultException(StatusCodes.BadEncodingError,
                        $"The key '{kCountIdentifier}' is already in use.");
                }

                // Write keys for decoding.
                var keys = this
                    .Where(x => !string.IsNullOrEmpty(x.Key) && x.Value != null)
                    .Select(x => x.Key)
                    .ToArray();
                encoder.WriteUInt32(kCountIdentifier, (uint)keys.Length);
                if (Count > 0) {
                    encoder.WriteStringArray(kKeysIdentifier, keys);
                }
            }

            foreach (var entry in this) {
                if (!string.IsNullOrEmpty(entry.Key)) {
                    encoder.WriteDataValue(entry.Key, entry.Value);
                }
            }
        }

        /// <inheritdoc/>
        public virtual void Decode(IDecoder decoder) {
            // Read keys for decoding.
            var count = decoder.ReadUInt32(kCountIdentifier);
            if (count > 0) {
                var keys = decoder.ReadStringArray(kKeysIdentifier);
                foreach (var key in keys) {
                    Add(new KeyDataValuePair {
                        Key = key,
                        Value = decoder.ReadDataValue(key)
                    });
                }
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