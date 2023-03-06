// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Module.Framework.Client
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Event client extensions
    /// </summary>
    public static class EventClientEx
    {
        /// <summary>
        /// Send event
        /// </summary>
        /// <param name="client"></param>
        /// <param name="topic"></param>
        /// <param name="buffers"></param>
        /// <param name="contentEncoding"></param>
        /// <param name="contentType"></param>
        /// <param name="messageSchema"></param>
        /// <param name="routingInfo"></param>
        /// <returns></returns>
        /// <param name="ct"></param>
        public static async Task SendEventsAsync(this IEventClient client,
            string topic, IReadOnlyList<ReadOnlyMemory<byte>> buffers,
            string contentEncoding, string contentType,
            string messageSchema = null, string routingInfo = null,
            CancellationToken ct = default)
        {
            using var msg = client.CreateEvent();
            await msg
                .SetTopic(topic)
                .AddBuffers(buffers)
                .SetContentType(contentType)
                .SetContentEncoding(contentEncoding)
                .SetMessageSchema(messageSchema)
                .SetRoutingInfo(routingInfo)
                .SendAsync(ct).ConfigureAwait(false);
        }

        /// <summary>
        /// Send event
        /// </summary>
        /// <param name="client"></param>
        /// <param name="topic"></param>
        /// <param name="buffer"></param>
        /// <param name="contentEncoding"></param>
        /// <param name="contentType"></param>
        /// <param name="messageSchema"></param>
        /// <param name="routingInfo"></param>
        /// <returns></returns>
        /// <param name="ct"></param>
        public static async Task SendEventAsync(this IEventClient client,
            string topic, ReadOnlyMemory<byte> buffer,
            string contentEncoding, string contentType,
            string messageSchema = null, string routingInfo = null,
            CancellationToken ct = default)
        {
            using var msg = client.CreateEvent();
            await msg
                .SetTopic(topic)
                .AddBuffers(new[] { buffer })
                .SetContentType(contentType)
                .SetContentEncoding(contentEncoding)
                .SetMessageSchema(messageSchema)
                .SetRoutingInfo(routingInfo)
                .SendAsync(ct).ConfigureAwait(false);
        }
    }
}
