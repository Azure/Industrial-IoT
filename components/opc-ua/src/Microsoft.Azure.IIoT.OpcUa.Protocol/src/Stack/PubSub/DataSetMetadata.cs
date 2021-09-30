// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Opc.Ua.PubSub {
    using System;
    using System.IO;

    /// <summary>
    /// Encodeable dataset metadata
    /// </summary>
    public class DataSetMetadata : IEncodeable {

        /// <summary>
        /// Message id
        /// </summary>
        public string MessageId { get; set; }

        /// <summary>
        /// Message type
        /// </summary>
        public string MessageType { get; set; }

        /// <summary>
        /// Publisher id
        /// </summary>
        public string PublisherId { get; set; }

        /// <summary>
        /// Data set class
        /// </summary>
        public string DataSetClassId { get; set; }

        /// <summary>
        /// Metadata
        /// </summary>
        public DataSetMetaDataType MetaData { get; set; }

        /// <inheritdoc/>
        public ExpandedNodeId TypeId { get; }

        /// <inheritdoc/>
        public ExpandedNodeId BinaryEncodingId { get; }

        /// <inheritdoc/>
        public ExpandedNodeId XmlEncodingId { get; }

        /// <inheritdoc/>
        public void Decode(IDecoder decoder) {
            MessageId = decoder.ReadString("MessageId");
            MessageType = decoder.ReadString("MessageType");
            PublisherId = decoder.ReadString("PublisherId");
            DataSetClassId = decoder.ReadString("DataSetClassId");
            MetaData = (DataSetMetaDataType)decoder.ReadEncodeable("MetaData", typeof(DataSetMetaDataType));
        }

        /// <inheritdoc/>
        public void Encode(IEncoder encoder) {
            encoder.WriteString("MessageId", MessageId);
            encoder.WriteString("MessageType", "ua-metadata");
            encoder.WriteString("PublisherId", PublisherId);
            encoder.WriteString("DataSetClassId", DataSetClassId);
            encoder.WriteEncodeable("MetaData", MetaData, typeof(DataSetMetaDataType));
        }

        /// <inheritdoc/>
        public bool IsEqual(IEncodeable encodeable) {
            // TODO
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public static DataSetMetadata Decode(IServiceMessageContext context, StreamReader reader) {
            var json = reader.ReadToEnd();

            var output = new DataSetMetadata();

            using (var decoder = new JsonDecoder(json, context)) {
                output.MessageId = decoder.ReadString("MessageId");
                output.MessageType = decoder.ReadString("MessageType");
                output.PublisherId = decoder.ReadString("PublisherId");
                output.DataSetClassId = decoder.ReadString("DataSetClassId");
                output.MetaData = (DataSetMetaDataType)decoder.ReadEncodeable("MetaData", typeof(DataSetMetaDataType));
                decoder.Close();
            }

            return output;
        }
    }
}