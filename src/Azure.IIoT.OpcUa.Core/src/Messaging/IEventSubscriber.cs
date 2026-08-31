// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Core.Messaging
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Subscribe and receive events
    /// </summary>
    public interface IEventSubscriber
    {
        /// <summary>
        /// Name of the technology implementing the event subscriber
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Subscribe to a topic and consume. Subscriptions are
        /// transient and exist only as long as the process.
        /// The topic string allows '*' wildcards, but not '#'.
        /// </summary>
        /// <param name="topic"></param>
        /// <param name="consumer"></param>
        /// <param name="ct"></param>
        ValueTask<IAsyncDisposable> SubscribeAsync(string topic,
            IEventConsumer consumer, CancellationToken ct = default);
    }
}
