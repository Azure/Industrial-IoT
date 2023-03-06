// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Messaging
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Telemetry message event to send
    /// </summary>
    public interface IEvent : IDisposable
    {
        /// <summary>
        /// Output path to use
        /// </summary>
        IEvent SetTopic(string value);

        /// <summary>
        /// Processing timestamp
        /// </summary>
        IEvent SetTimestamp(DateTime value);

        /// <summary>
        /// Content type
        /// </summary>
        IEvent SetContentType(string value);

        /// <summary>
        /// Content encoding
        /// </summary>
        IEvent SetContentEncoding(string value);

        /// <summary>
        /// Message schema
        /// </summary>
        IEvent SetMessageSchema(string value);

        /// <summary>
        /// Custom routing info to be added to the header.
        /// </summary>
        IEvent SetRoutingInfo(string value);

        /// <summary>
        /// Whether to retain the message on the receiving end.
        /// </summary>
        IEvent SetRetain(bool value);

        /// <summary>
        /// The time to live for the message
        /// </summary>
        IEvent SetTtl(TimeSpan value);

        /// <summary>
        /// Message payload buffers.
        /// </summary>
        IEvent AddBuffers(IReadOnlyList<ReadOnlyMemory<byte>> value);

        /// <summary>
        /// Sends the message or messages
        /// </summary>
        /// <param name="ct">Send the event</param>
        /// <returns></returns>
        Task SendAsync(CancellationToken ct = default);
    }
}
