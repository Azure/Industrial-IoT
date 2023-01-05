// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Edge.Publisher.Models {
    using Microsoft.Azure.IIoT.OpcUa.Publisher.Models;
    using System;
    using Opc.Ua;

    /// <summary>
    /// Data set message emitted by writer in a writer group.
    /// </summary>
    public class WriterGroupMessageContext {

        /// <summary>
        /// Sequence number
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