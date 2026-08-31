// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Core.Messaging
{
    using System.Buffers;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Handles events
    /// </summary>
    public interface IEventConsumer
    {
        /// <summary>
        /// Handle event sent to a target topic. The responder can be
        /// used to send messages back directly on a topic of choice to
        /// the client that sent the message.
        /// </summary>
        /// <param name="topic"></param>
        /// <param name="data"></param>
        /// <param name="contentType"></param>
        /// <param name="properties"></param>
        /// <param name="responder"></param>
        /// <param name="ct"></param>
        Task HandleAsync(string topic,
            ReadOnlySequence<byte> data, string contentType,
            IReadOnlyDictionary<string, string?> properties,
            IEventClient? responder, CancellationToken ct = default);

        /// <summary>
        /// Null instance
        /// </summary>
        static IEventConsumer Null { get; } = new NullConsumer();

        /// <inheritdoc/>
        private sealed class NullConsumer : IEventConsumer
        {
            /// <inheritdoc/>
            public Task HandleAsync(string topic, ReadOnlySequence<byte> data,
                string contentType, IReadOnlyDictionary<string, string?> properties,
                IEventClient? responder, CancellationToken ct)
            {
                return Task.CompletedTask;
            }
        }
    }
}
