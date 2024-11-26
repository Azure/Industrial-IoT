// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Tests.Stack
{
    using Azure.IIoT.OpcUa.Publisher.Stack;
    using Azure.IIoT.OpcUa.Publisher.Stack.Models;
    using Azure.IIoT.OpcUa.Publisher.Stack.Services;
    using Azure.IIoT.OpcUa.Encoders;
    using Furly.Extensions.Logging;
    using Furly.Extensions.Serializers.Newtonsoft;
    using Moq;
    using Opc.Ua;
    using Opc.Ua.Client;
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Collections.Generic;
    using Microsoft.Extensions.Logging;
    using System.Diagnostics.Metrics;
    using System.Diagnostics;
    using Microsoft.Extensions.Options;

    public abstract class OpcUaMonitoredItemTestsBase : IObservability
    {
        public ILoggerFactory LoggerFactory => Log.ConsoleFactory();
        public IMeterFactory MeterFactory => null!;
        public TimeProvider TimeProvider => TimeProvider.System;
        public ActivitySource ActivitySource { get; }

        protected virtual Mock<INodeCache> SetupMockedNodeCache(NamespaceTable namespaceTable = null)
        {
            using var mock = Autofac.Extras.Moq.AutoMock.GetLoose();
            return mock.Mock<INodeCache>();
        }

        protected virtual Mock<IOpcUaSession> SetupMockedSession(NamespaceTable namespaceTable = null)
        {
            namespaceTable ??= new NamespaceTable();

            var nodeCache = SetupMockedNodeCache(namespaceTable).Object;

            using var mock = Autofac.Extras.Moq.AutoMock.GetLoose();
            var session = mock.Mock<IOpcUaSession>();
            var messageContext = new ServiceMessageContext
            {
                NamespaceUris = namespaceTable
            };
            var codec = new JsonVariantEncoder(messageContext, new NewtonsoftJsonSerializer());
            session.SetupGet(x => x.Codec).Returns(codec);
            session.SetupGet(x => x.NodeCache).Returns(nodeCache);
            session.SetupGet(x => x.MessageContext).Returns(messageContext);
            return session;
        }

        internal async Task<OpcUaMonitoredItem> GetMonitoredItemAsync(BaseMonitoredItemModel template,
            NamespaceTable namespaceUris = null)
        {
            var session = SetupMockedSession(namespaceUris).Object;
            var subscriber = new Mock<ISubscriber>();
            var subscription = new SimpleSubscription(this);
            var monitoredItemWrapper = OpcUaMonitoredItem.Create(null!, session, subscription,
                (subscriber.Object, template).YieldReturn(), this).Single();
            monitoredItemWrapper.Initialize();
            if (monitoredItemWrapper.FinalizeInitialize != null)
            {
                await monitoredItemWrapper.FinalizeInitialize(default);
            }
            return monitoredItemWrapper;
        }

        internal sealed class SimpleSubscription : SubscriptionBase
        {
            public SimpleSubscription(OpcUaMonitoredItemTestsBase outer)
                : base(null!, null!, OptionMonitor.Create<SubscriptionOptions>(), outer)
            {
            }

            protected override MonitoredItem CreateMonitoredItem(
                IOptionsMonitor<MonitoredItemOptions> options)
            {
                return ((OpcUaSubscription.Precreated)options.CurrentValue).Item;
            }

            protected override ValueTask OnDataChangeNotificationAsync(
                uint sequenceNumber, DateTime publishTime, DataChangeNotification notification,
                PublishState publishStateMask, IReadOnlyList<string> stringTable)
            {
                throw new NotImplementedException();
            }

            protected override ValueTask OnEventDataNotificationAsync(uint sequenceNumber,
                DateTime publishTime, EventNotificationList notification,
                PublishState publishStateMask, IReadOnlyList<string> stringTable)
            {
                throw new NotImplementedException();
            }

            protected override ValueTask OnKeepAliveNotificationAsync(uint sequenceNumber,
                DateTime publishTime, PublishState publishStateMask)
            {
                throw new NotImplementedException();
            }

            protected override ValueTask OnStatusChangeNotificationAsync(uint sequenceNumber,
                DateTime publishTime, StatusChangeNotification notification,
                PublishState publishStateMask, IReadOnlyList<string> stringTable)
            {
                throw new NotImplementedException();
            }
        }
    }
}
