// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Messaging.ServiceBus.Clients {
    using Microsoft.Azure.IIoT.Utils;
    using Microsoft.Azure.ServiceBus;
    using Microsoft.Azure.ServiceBus.Management;
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Create service bus clients
    /// </summary>
    public class ServiceBusClientFactory : IServiceBusClientFactory {

        /// <summary>
        /// Create factory
        /// </summary>
        /// <param name="config"></param>
        public ServiceBusClientFactory(IServiceBusConfig config) {
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        /// <inheritdoc/>
        public async Task<ISubscriptionClient> CreateOrGetSubscriptionClientAsync(
            Func<Message, CancellationToken, Task> handler,
            Func<ExceptionReceivedEventArgs, Task> exceptionReceivedHandler,
            string name, string topic) {
            topic = GetEntityName(topic);
            if (string.IsNullOrEmpty(name)) {
                name = Dns.GetHostName();
            }
            await _subscriptionLock.WaitAsync();
            try {
                var key = $"{topic}/subscriptions/{name}";
                if (!_subscriptionClients.TryGetValue(key, out var client) ||
                    client.IsClosedOrClosing) {
                    client = await NewSubscriptionClientAsync(GetEntityName(topic), name);
                    _subscriptionClients.Add(key, client);

                    //
                    // TODO: Should also check whether the handlers are different
                    // and close/create new subscription handler...
                    //
                    client.RegisterMessageHandler(handler,
                        new MessageHandlerOptions(exceptionReceivedHandler) {
                            MaxConcurrentCalls = 10,
                            AutoComplete = false
                        });
                }
                return client;
            }
            finally {
                _subscriptionLock.Release();
            }
        }

        /// <inheritdoc/>
        public async Task<ITopicClient> CreateOrGetTopicClientAsync(string topic) {
            topic = GetEntityName(topic);
            await _topicLock.WaitAsync();
            try {
                if (!_topicClients.TryGetValue(topic, out var client) ||
                    client.IsClosedOrClosing) {
                    client = await NewTopicClientAsync(topic);
                    _topicClients.Add(topic, client);
                }
                return client;
            }
            finally {
                _topicLock.Release();
            }
        }

        /// <inheritdoc/>
        public async Task<IQueueClient> CreateOrGetGetQueueClientAsync(string queue) {
            queue = GetEntityName(queue);
            await _queueLock.WaitAsync();
            try {
                if (!_queueClients.TryGetValue(queue, out var client) ||
                    client.IsClosedOrClosing) {
                    client = await NewQueueClientAsync(queue);
                    _queueClients.Add(queue, client);
                }
                return client;
            }
            finally {
                _queueLock.Release();
            }
        }

        /// <inheritdoc/>
        public async Task CloseAsync() {
            await Task.WhenAll(
                CloseAllAsync(_queueLock, _queueClients),
                CloseAllAsync(_topicLock, _topicClients),
                CloseAllAsync(_subscriptionLock, _subscriptionClients));
        }

        /// <summary>
        /// Create subscription client
        /// </summary>
        /// <param name="topic"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        private async Task<ISubscriptionClient> NewSubscriptionClientAsync(
            string topic, string name) {
            var managementClient = new ManagementClient(_config.ServiceBusConnString);
            var exists = await managementClient.TopicExistsAsync(topic);
            if (!exists) {
                await Try.Async(() =>
                    managementClient.CreateTopicAsync(new TopicDescription(topic)));
            }
            exists = await managementClient.SubscriptionExistsAsync(topic, name);
            if (!exists) {
                await managementClient.CreateSubscriptionAsync(
                    new SubscriptionDescription(topic, name));
            }
            return new SubscriptionClient(_config.ServiceBusConnString, topic, name,
                ReceiveMode.PeekLock, RetryPolicy.Default);
        }

        /// <summary>
        /// Create queue client
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        private async Task<IQueueClient> NewQueueClientAsync(string name) {
            var managementClient = new ManagementClient(_config.ServiceBusConnString);
            var exists = await managementClient.QueueExistsAsync(name);
            if (!exists) {
                await managementClient.CreateQueueAsync(new QueueDescription(name));
            }
            return new QueueClient(
                _config.ServiceBusConnString, GetEntityName(name), ReceiveMode.PeekLock,
                RetryPolicy.Default);
        }

        /// <summary>
        /// Create topic client
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        private async Task<TopicClient> NewTopicClientAsync(string name) {
            var managementClient = new ManagementClient(_config.ServiceBusConnString);
            var exists = await managementClient.TopicExistsAsync(name);
            if (!exists) {
                await managementClient.CreateTopicAsync(new TopicDescription(name));
            }
            return new TopicClient(
                _config.ServiceBusConnString, GetEntityName(name), RetryPolicy.Default);
        }

        /// <summary>
        /// Close all clients in list
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="clientLock"></param>
        /// <param name="clients"></param>
        /// <returns></returns>
        private static async Task CloseAllAsync<T>(SemaphoreSlim clientLock,
            Dictionary<string, T> clients) where T : IClientEntity {
            await clientLock.WaitAsync();
            try {
                foreach (var client in clients.Values) {
                    await Try.Async(() => client.CloseAsync());
                }
                clients.Clear();
            }
            finally {
                clientLock.Release();
            }
        }

        /// <summary>
        /// Get entity path
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        private string GetEntityName(string name) {
            if (string.IsNullOrEmpty(name)) {
                var cs = new ServiceBusConnectionStringBuilder(_config.ServiceBusConnString);
                name = cs.EntityPath;
            }
            if (string.IsNullOrEmpty(name)) {
                name = "iiotmessaging";
            }
            return name;
        }

        private readonly IServiceBusConfig _config;
        private readonly SemaphoreSlim _topicLock = new SemaphoreSlim(1);
        private readonly Dictionary<string, ITopicClient> _topicClients =
            new Dictionary<string, ITopicClient>();
        private readonly SemaphoreSlim _queueLock = new SemaphoreSlim(1);
        private readonly Dictionary<string, IQueueClient> _queueClients =
            new Dictionary<string, IQueueClient>();
        private readonly SemaphoreSlim _subscriptionLock = new SemaphoreSlim(1);
        private readonly Dictionary<string, ISubscriptionClient> _subscriptionClients =
            new Dictionary<string, ISubscriptionClient>();
    }
}