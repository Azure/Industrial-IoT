// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    using Furly.Extensions.Messaging;
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Context to add to notification to convey data required for
    /// data set messages emitted by writer in a writer group.
    /// </summary>
    public record class DataSetWriterContext
    {
        /// <summary>
        /// The allocated identifier of the writer
        /// </summary>
        public required ushort DataSetWriterId { get; init; }

        /// <summary>
        /// Topic for the message
        /// </summary>
        public required string Topic { get; init; }

        /// <summary>
        /// Requested qos
        /// </summary>
        public required QoS? Qos { get; init; }

        /// <summary>
        /// Requested Retain
        /// </summary>
        public bool? Retain { get; init; }

        /// <summary>
        /// Requested Time to live
        /// </summary>
        public TimeSpan? Ttl { get; init; }

        /// <summary>
        /// Publisher id
        /// </summary>
        public required string PublisherId { get; init; }

        /// <summary>
        /// Dataset writer model reference
        /// </summary>
        public required DataSetWriterModel Writer { get; init; }

        /// <summary>
        /// Dataset writer name unique in the context of the group
        /// </summary>
        public required string WriterName { get; init; }

        /// <summary>
        /// Metadata for the dataset
        /// </summary>
        public required PublishedDataSetMessageSchemaModel? MetaData { get; init; }

        /// <summary>
        /// Extension fields
        /// </summary>
        public required IReadOnlyList<(string, Opc.Ua.DataValue?)> ExtensionFields { get; init; }

        /// <summary>
        /// Sequence number inside the writer
        /// </summary>
        public required Func<uint> NextWriterSequenceNumber { get; init; }

        /// <summary>
        /// Writer group model reference
        /// </summary>
        public required WriterGroupModel WriterGroup { get; init; }

        /// <summary>
        /// The applicable network message schema
        /// </summary>
        public required IEventSchema? Schema { get; init; }

        /// <summary>
        /// The cloud event header
        /// </summary>
        public required CloudEventHeader? CloudEvent { get; init; }
    }
}
