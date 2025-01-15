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

    public abstract class OpcUaMonitoredItemTestsBase
    {
        protected virtual Mock<INodeCache> SetupMockedNodeCache(NamespaceTable namespaceTable = null)
        {
            using var mock = Autofac.Extras.Moq.AutoMock.GetLoose();
            var nodeCache = mock.Mock<INodeCache>();
            namespaceTable ??= new NamespaceTable();
            var typeTable = new TypeTable(namespaceTable);
            nodeCache.SetupGet(x => x.TypeTree).Returns(typeTable);
            nodeCache.SetupGet(x => x.NamespaceUris).Returns(namespaceTable);
            return nodeCache;
        }

        protected virtual Mock<IOpcUaSession> SetupMockedSession(NamespaceTable namespaceTable = null)
        {
            namespaceTable ??= new NamespaceTable();

            var nodeCache = SetupMockedNodeCache(namespaceTable).Object;
            var typeTable = nodeCache.TypeTree;

            using var mock = Autofac.Extras.Moq.AutoMock.GetLoose();
            var session = mock.Mock<IOpcUaSession>();
            var messageContext = new ServiceMessageContext
            {
                NamespaceUris = namespaceTable
            };
            var codec = new JsonVariantEncoder(messageContext, new NewtonsoftJsonSerializer());
            session.SetupGet(x => x.Codec).Returns(codec);
            session.SetupGet(x => x.TypeTree).Returns(typeTable);
            session.SetupGet(x => x.NodeCache).Returns(nodeCache);
            session.SetupGet(x => x.MessageContext).Returns(messageContext);
            return session;
        }

        internal async Task<OpcUaMonitoredItem> GetMonitoredItemAsync(BaseMonitoredItemModel template,
            NamespaceTable namespaceUris = null)
        {
            var session = SetupMockedSession(namespaceUris).Object;
            var subscriber = new Mock<ISubscriber>();
            var monitoredItemWrapper = OpcUaMonitoredItem.Create(null!,
                (subscriber.Object, template).YieldReturn(),
                Log.ConsoleFactory(), TimeProvider.System).Single();
            using var subscription = new SimpleSubscription();
            monitoredItemWrapper.AddTo(subscription, session, out _);
            if (monitoredItemWrapper.FinalizeAddTo != null)
            {
                await monitoredItemWrapper.FinalizeAddTo(session, default);
            }
            return monitoredItemWrapper;
        }

        internal sealed class SimpleSubscription : Subscription
        {
            public SimpleSubscription()
            {
            }

            public SimpleSubscription(Subscription template, bool copyEventHandlers)
                : base(template, copyEventHandlers)
            {
            }

            public override Subscription CloneSubscription(bool copyEventHandlers)
            {
                throw new NotImplementedException();
            }
        }
    }
}
