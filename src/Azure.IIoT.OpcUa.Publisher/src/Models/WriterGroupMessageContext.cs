// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models {
    using Azure.IIoT.OpcUa.Api.Models;

    /// <summary>
    /// Data set message emitted by writer in a writer group.
    /// </summary>
    public class WriterGroupMessageContext {

        /// <summary>
        /// Sequence number inside the writer group and based
        /// on message type
        /// </summary>
        public uint SequenceNumber { get; set; }

        /// <summary>
        /// Publisher id
        /// </summary>
        public string PublisherId { get; set; }

        /// <summary>
        /// Dataset writer model reference
        /// </summary>
        public DataSetWriterModel Writer { get; set; }

        /// <summary>
        /// Writer group model reference
        /// </summary>
        public WriterGroupModel WriterGroup { get; set; }
    }
}