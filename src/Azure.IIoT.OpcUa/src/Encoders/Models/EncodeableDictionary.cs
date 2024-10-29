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
    public class EncodeableDictionary : List<KeyDataValuePair>, IEncodeable, IJsonEncodeable
    {
        /// <inheritdoc/>
        public ExpandedNodeId TypeId =>
            "s=" + nameof(EncodeableDictionary);

        /// <inheritdoc/>
        public ExpandedNodeId BinaryEncodingId =>
            "s=" + nameof(EncodeableDictionary) + "_Encoding_DefaultBinary";

        /// <inheritdoc/>
        public ExpandedNodeId XmlEncodingId =>
            "s=" + nameof(EncodeableDictionary) + "_Encoding_DefaultXml";

        /// <inheritdoc/>
        public ExpandedNodeId JsonEncodingId =>
            "s=" + nameof(EncodeableDictionary) + "_Encoding_DefaultJson";

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
                    (x.Value.Value is not LocalizedText lt ||
                      lt.Locale != null || lt.Text != null))
                .ToDictionary(x => x.Key, x => x.Value);

            foreach (var keyValuePair in dictionary)
            {
                encoder.WriteDataValue(keyValuePair.Key, keyValuePair.Value);
            }
        }

        /// <inheritdoc/>
        public virtual void Decode(IDecoder decoder)
        {
            // Only JSON decoder that can decode a dictionary is supported.
            if (decoder is not JsonDecoderEx jsonDecoder)
            {
                throw new FormatException(
                    $"Cannot decode using the decoder: {decoder.GetType()}.");
            }
            var dataSet = jsonDecoder.ReadDataSet(null);
            if (dataSet != null)
            {
                foreach (var field in dataSet.DataSetFields)
                {
                    Add(new KeyDataValuePair
                    {
                        Key = field.Name,
                        Value = field.Value
                    });
                }
            }
        }

        /// <inheritdoc/>
        public virtual bool IsEqual(IEncodeable encodeable)
        {
            if (this == encodeable)
            {
                return true;
            }
            if (encodeable is not EncodeableDictionary encodableDictionary)
            {
                return false;
            }
            if (!Utils.IsEqual(this, encodableDictionary))
            {
                return false;
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
