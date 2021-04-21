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
                // Write keys for decoding. Check first if the keys property is already used
                // as it would overwrite the values.
                if (this.Any(x => x.Key == kKeysIdentifier)) {
                    throw new ServiceResultException(StatusCodes.BadEncodingError,
                        $"The key '{kKeysIdentifier}' is already in use.");
                }
                encoder.WriteStringArray(kKeysIdentifier, this.Select(x => x.Key).ToArray());
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
            StringCollection keys = null;
            try {
                // May throw in some decoders if an empty array was encoded.
                keys = decoder.ReadStringArray(kKeysIdentifier);
            }
            catch {
            }

            if (keys != null) {
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