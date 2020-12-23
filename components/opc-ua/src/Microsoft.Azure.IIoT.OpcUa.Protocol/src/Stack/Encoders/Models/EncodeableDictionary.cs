// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Opc.Ua.Encoders {
    using Opc.Ua;

    /// <summary>
    /// Encodeable dictionary carrying field names and values
    /// </summary>
    public class EncodeableDictionary : IEncodeable {

        /// <summary>
        /// Event fields
        /// </summary>
        public KeyValuePairCollection Fields { get; set; }

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
        /// Create
        /// </summary>
        public EncodeableDictionary() {
            Fields = new KeyValuePairCollection();
        }

        /// <inheritdoc/>
        public virtual void Encode(IEncoder encoder) {
            //  todo: check if "EventFields" is appropriate
            encoder.WriteEncodeableArray("EventFields", Fields.ToArray(), typeof(KeyValuePair));
        }

        /// <inheritdoc/>
        public virtual void Decode(IDecoder decoder) {
            Fields = (KeyValuePairCollection)decoder.ReadEncodeableArray(
                "EventFields", typeof(KeyValuePair));
        }

        /// <inheritdoc/>
        public virtual bool IsEqual(IEncodeable encodeable) {
            if (this == encodeable) {
                return true;
            }
            if (!(encodeable is EncodeableDictionary eventFieldList)) {
                return false;
            }
            if (!Utils.IsEqual(Fields, eventFieldList.Fields)) {
                return false;
            }
            return true;
        }
    }
}