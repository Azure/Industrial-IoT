// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Core.Messaging
{
    using System;
    using System.Buffers;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Event extensions
    /// </summary>
    public static class EventExtensions
    {
        /// <summary>
        /// Send event to a target resource. The data buffers must not be
        /// larger than <see cref="IEventClient.MaxEventPayloadSizeInBytes"/>
        /// </summary>
        /// <param name="client"></param>
        /// <param name="topic"></param>
        /// <param name="buffers"></param>
        /// <param name="contentType"></param>
        /// <param name="contentEncoding"></param>
        /// <param name="configure"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public static async ValueTask SendEventAsync(this IEventClient client,
            string topic, IEnumerable<ReadOnlySequence<byte>> buffers,
            string contentType, string? contentEncoding = null,
            Action<IEvent>? configure = null, CancellationToken ct = default)
        {
            using var msg = client.CreateEvent();
            var sending = msg
                .SetTopic(topic)
                .AddBuffers(buffers)
                .SetContentType(contentType)
                .SetContentEncoding(contentEncoding);
            configure?.Invoke(sending);
            await sending.SendAsync(ct).ConfigureAwait(false);
        }

        /// <summary>
        /// Send event to a target resource. The data buffers must not be
        /// larger than <see cref="IEventClient.MaxEventPayloadSizeInBytes"/>
        /// </summary>
        /// <param name="client"></param>
        /// <param name="topic"></param>
        /// <param name="buffers"></param>
        /// <param name="contentType"></param>
        /// <param name="contentEncoding"></param>
        /// <param name="configure"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public static ValueTask SendEventAsync(this IEventClient client,
            string topic, IEnumerable<ReadOnlyMemory<byte>> buffers,
            string contentType, string? contentEncoding = null,
            Action<IEvent>? configure = null, CancellationToken ct = default)
        {
            return SendEventAsync(client, topic, buffers
                .Select(b => new ReadOnlySequence<byte>(b)), contentType,
                contentEncoding, configure, ct);
        }

        /// <summary>
        /// Send message to a target resource. The data buffer must not be
        /// larger than <see cref="IEventClient.MaxEventPayloadSizeInBytes"/>
        /// </summary>
        /// <param name="client"></param>
        /// <param name="topic"></param>
        /// <param name="buffer"></param>
        /// <param name="contentType"></param>
        /// <param name="contentEncoding"></param>
        /// <param name="configure"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public static ValueTask SendEventAsync(this IEventClient client,
            string topic, ReadOnlyMemory<byte> buffer, string contentType,
            string? contentEncoding = null, Action<IEvent>? configure = null,
            CancellationToken ct = default)
        {
            return client.SendEventAsync(topic, [buffer], contentType,
                contentEncoding, configure, ct);
        }

        /// <summary>
        /// Send message to a target resource. The data buffer must not be
        /// larger than <see cref="IEventClient.MaxEventPayloadSizeInBytes"/>
        /// </summary>
        /// <param name="client"></param>
        /// <param name="topic"></param>
        /// <param name="buffer"></param>
        /// <param name="contentType"></param>
        /// <param name="contentEncoding"></param>
        /// <param name="configure"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public static ValueTask SendEventAsync(this IEventClient client,
            string topic, ReadOnlySequence<byte> buffer, string contentType,
            string? contentEncoding = null, Action<IEvent>? configure = null,
            CancellationToken ct = default)
        {
            return client.SendEventAsync(topic, [buffer], contentType,
                contentEncoding, configure, ct);
        }
    }
}
