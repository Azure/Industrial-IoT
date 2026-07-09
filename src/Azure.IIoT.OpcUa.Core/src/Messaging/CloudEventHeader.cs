// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Core.Messaging
{
    using System;

    /// <summary>
    /// Cloud events version 1.0 header.
    /// </summary>
    public record class CloudEventHeader
    {
        /// <summary>
        /// Identifies the context in which an event happened. Producers
        /// MUST ensure that source + id is unique for each distinct event.
        /// </summary>
        public required Uri Source { get; init; }

        /// <summary>
        /// Identifies the subject of the event in the context of the
        /// event producer (identified by source).
        /// </summary>
        public string? Subject { get; init; }

        /// <summary>
        /// Describes the type of event related to the originating
        /// occurrence. Often used for routing, observability, policy
        /// enforcement, etc.
        /// </summary>
        public required string Type { get; init; }

        /// <summary>
        /// Identifies the event. Producers MUST ensure that source + id
        /// is unique for each distinct event.
        /// </summary>
        public required string Id { get; init; }

        /// <summary>
        /// Timestamp of when the occurrence happened.
        /// </summary>
        public DateTimeOffset? Time { get; init; }

        /// <summary>
        /// Content type of the data value. Enables data to carry any type
        /// of content, whereby format and encoding might differ from that
        /// of the chosen event format.
        /// </summary>
        public string? DataContentType { get; init; }
    }
}
