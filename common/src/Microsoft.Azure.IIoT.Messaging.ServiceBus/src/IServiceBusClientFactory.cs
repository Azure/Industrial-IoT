// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Messaging.ServiceBus {
    using Microsoft.Azure.ServiceBus;
    using System;
    using System.Threading.Tasks;
    using System.Threading;

    /// <summary>
    /// Create service bus clients
    /// </summary>
    public interface IServiceBusClientFactory {

        /// <summary>
        /// Create subscription client
        /// </summary>
        /// <param name="handler"></param>
        /// <param name="exceptions"></param>
        /// <param name="name"></param>
        /// <param name="topicName"></param>
        /// <returns></returns>
        Task<ISubscriptionClient> CreateOrGetSubscriptionClientAsync(
            Func<Message, CancellationToken, Task> handler,
            Func<ExceptionReceivedEventArgs, Task> exceptions,
            string name, string topicName = null);

        /// <summary>
        /// Create topic client
        /// </summary>
        /// <returns></returns>
        Task<ITopicClient> CreateOrGetTopicClientAsync(string topicName = null);

        /// <summary>
        /// Create queue client
        /// </summary>
        /// <param name="queueName"></param>
        /// <returns></returns>
        Task<IQueueClient> CreateOrGetGetQueueClientAsync(string queueName = null);
    }
}