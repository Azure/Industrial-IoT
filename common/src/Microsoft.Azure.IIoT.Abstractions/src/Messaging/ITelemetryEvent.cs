// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Messaging {
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Telemetry message event to send
    /// </summary>
    public interface ITelemetryEvent : IDisposable {
        /// <summary>
        /// Processing timestamp
        /// </summary>
        DateTime Timestamp { get; set; }

        /// <summary>
        /// Content type
        /// </summary>
        string ContentType { get; set; }

        /// <summary>
        /// Content encoding
        /// </summary>
        string ContentEncoding { get; set; }

        /// <summary>
        /// Message schema
        /// </summary>
        string MessageSchema { get; set; }

        /// <summary>
        /// Custom routing info to be added to the header.
        /// </summary>
        string RoutingInfo { get; set; }

        /// <summary>
        /// Output path to use
        /// </summary>
        string OutputName { get; set; }

        /// <summary>
        /// Whether to retain the message on the receiving end.
        /// </summary>
        bool Retain { get; set; }

        /// <summary>
        /// The time to live for the message
        /// </summary>
        TimeSpan Ttl { get; set; }

        /// <summary>
        /// Message payload buffers.
        /// </summary>
        IReadOnlyList<byte[]> Buffers { get; set; }
    }
}
