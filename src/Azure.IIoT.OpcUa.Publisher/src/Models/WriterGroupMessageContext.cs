// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    using System;

    /// <summary>
    /// Data set message emitted by writer in a writer group.
    /// </summary>
    public record class WriterGroupMessageContext
    {
        /// <summary>
        /// Topic for the message if not metadata message
        /// </summary>
        public required string Topic { get; init; }

        /// <summary>
        /// Metadata topic for metadata messages
        /// </summary>
        public required string MetaDataTopic { get; init; }

        /// <summary>
        /// Publisher id
        /// </summary>
        public required string PublisherId { get; init; }

        /// <summary>
        /// Dataset writer model reference
        /// </summary>
        public required DataSetWriterModel Writer { get; init; }

        /// <summary>
        /// Sequence number inside the writer
        /// </summary>
        public required Func<uint> NextWriterSequenceNumber { get; init; }

        /// <summary>
        /// Writer group model reference
        /// </summary>
        public required WriterGroupModel WriterGroup { get; init; }
    }
}
