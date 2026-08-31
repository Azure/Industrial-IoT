// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Encoders.Models
{
    using Azure.IIoT.OpcUa.Encoders;
    using Opc.Ua;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Encodeable dictionary carrying field names and values
    /// </summary>
    public class EncodeableDictionary : List<KeyDataValuePair>, IEncodeable
    {
        /// <inheritdoc/>
        public ExpandedNodeId TypeId =>
            (ExpandedNodeId)("s=" + nameof(EncodeableDictionary));

        /// <inheritdoc/>
        public ExpandedNodeId BinaryEncodingId =>
            (ExpandedNodeId)("s=" + nameof(EncodeableDictionary) + "_Encoding_DefaultBinary");

        /// <inheritdoc/>
        public ExpandedNodeId XmlEncodingId =>
            (ExpandedNodeId)("s=" + nameof(EncodeableDictionary) + "_Encoding_DefaultXml");

        /// <inheritdoc/>
        public ExpandedNodeId JsonEncodingId =>
            (ExpandedNodeId)("s=" + nameof(EncodeableDictionary) + "_Encoding_DefaultJson");

        /// <summary>
        /// Initializes the dictionary with default values.
        /// </summary>
        public EncodeableDictionary() { }

        /// <summary>
        /// Initializes the dictionary with an initial capacity.
        /// </summary>
        /// <param name="capacity"></param>
        public EncodeableDictionary(int capacity)
            : base(capacity)
        {
        }

        /// <summary>
        /// Initializes the dictionary with another collection.
        /// </summary>
        /// <param name="collection"></param>
        public EncodeableDictionary(IEnumerable<KeyDataValuePair> collection)
            : base(collection)
        {
        }

        /// <inheritdoc/>
        public virtual void Encode(IEncoder encoder)
        {
            // Get valid dictionary for encoding.
            var dictionary = this
                .Where(x => !string.IsNullOrEmpty(x.Key) &&
                    x.Value?.Value != null &&
                    (x.Value?.Value is not LocalizedText lt ||
                      lt.Locale != null || lt.Text != null))
                .ToDictionary(x => x.Key, x => x.Value);

            foreach (var keyValuePair in dictionary)
            {
                encoder.WriteDataValue(keyValuePair.Key, keyValuePair.Value.GetValueOrDefault());
            }
        }

        /// <inheritdoc/>
        public virtual void Decode(IDecoder decoder)
        {
            // TODO(Phase 5): Reimplement dataset decoding on the 2.0 stack
            // Opc.Ua.JsonDecoder once the PubSub codecs are migrated.
            throw new NotSupportedException(
                "EncodeableDictionary decoding is deferred to Phase 5.");
        }

        /// <inheritdoc/>
        /// <remarks>
        /// The entries are compared one by one rather than by handing this
        /// instance to <see cref="Utils.IsEqual(object, object)"/>. That helper
        /// dispatches an <see cref="IEncodeable"/> back to its own
        /// <c>IsEqual</c>, so passing <c>this</c> to it called straight back
        /// into here and any comparison of two distinct dictionaries recursed
        /// until the stack ran out. <see cref="KeyDataValuePair"/> avoids this
        /// by comparing its fields, and so does this now.
        /// </remarks>
        public virtual bool IsEqual(IEncodeable encodeable)
        {
            if (ReferenceEquals(this, encodeable))
            {
                return true;
            }
            if (encodeable is not EncodeableDictionary encodableDictionary)
            {
                return false;
            }
            if (Count != encodableDictionary.Count)
            {
                return false;
            }
            for (var index = 0; index < Count; index++)
            {
                var entry = this[index];
                var other = encodableDictionary[index];
                if (entry is null || other is null)
                {
                    if (!ReferenceEquals(entry, other))
                    {
                        return false;
                    }
                    continue;
                }
                if (!entry.IsEqual(other))
                {
                    return false;
                }
            }
            return true;
        }

        /// <inheritdoc/>
        public object Clone()
        {
            return new EncodeableDictionary(this);
        }
    }
}
