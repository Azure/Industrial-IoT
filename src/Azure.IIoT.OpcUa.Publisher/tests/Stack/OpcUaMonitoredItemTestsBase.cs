// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Tests.Stack
{
    using Azure.IIoT.OpcUa.Encoders;
    using Azure.IIoT.OpcUa.Publisher.Stack;
    using Azure.IIoT.OpcUa.Publisher.Stack.Models;
    using Azure.IIoT.OpcUa.Publisher.Stack.Services;
    using Furly.Extensions.Logging;
    using Furly.Extensions.Serializers.Newtonsoft;
    using Moq;
    using Opc.Ua;
    using Opc.Ua.Client;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    public abstract class OpcUaMonitoredItemTestsBase
    {
        protected virtual MockCache SetupMockedNodeCache()
        {
            return new MockCache();
        }

        protected virtual Mock<IOpcUaSession> SetupMockedSession(NamespaceTable namespaceTable = null)
        {
            namespaceTable ??= new NamespaceTable();

            var nodeCache = SetupMockedNodeCache();

            using var mock = Autofac.Extras.Moq.AutoMock.GetLoose();
            var session = mock.Mock<IOpcUaSession>();
            var messageContext = new ServiceMessageContext
            {
                NamespaceUris = namespaceTable
            };
            var codec = new JsonVariantEncoder(messageContext, new NewtonsoftJsonSerializer());
            session.SetupGet(x => x.Codec).Returns(codec);
            session.SetupGet(x => x.LruNodeCache).Returns(nodeCache);
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

        public sealed class MockCache : ILruNodeCache
        {
            public ISession Session => throw new NotSupportedException();

            public void Clear()
            {
                throw new NotImplementedException();
            }

            public ValueTask<BuiltInType> GetBuiltInTypeAsync(NodeId datatypeId, CancellationToken ct = default)
            {
                throw new NotImplementedException();
            }

            public ValueTask<INode> GetNodeAsync(NodeId nodeId, CancellationToken ct = default)
            {
                throw new NotImplementedException();
            }

            public ValueTask<IReadOnlyList<INode>> GetNodesAsync(IReadOnlyList<NodeId> nodeIds, CancellationToken ct = default)
            {
                throw new NotImplementedException();
            }

            public ValueTask<INode> GetNodeWithBrowsePathAsync(NodeId nodeId, QualifiedNameCollection browsePath, CancellationToken ct = default)
            {
                throw new NotImplementedException();
            }

            public ValueTask<IReadOnlyList<INode>> GetReferencesAsync(IReadOnlyList<NodeId> nodeIds, IReadOnlyList<NodeId> referenceTypeIds, bool isInverse, bool includeSubtypes = true, CancellationToken ct = default)
            {
                throw new NotImplementedException();
            }

            public ValueTask<IReadOnlyList<INode>> GetReferencesAsync(NodeId nodeId, NodeId referenceTypeId, bool isInverse, bool includeSubtypes = true, CancellationToken ct = default)
            {
                throw new NotImplementedException();
            }

            public ValueTask<NodeId> GetSuperTypeAsync(NodeId typeId, CancellationToken ct = default)
            {
                throw new NotImplementedException();
            }

            public ValueTask<DataValue> GetValueAsync(NodeId nodeId, CancellationToken ct = default)
            {
                throw new NotImplementedException();
            }

            public ValueTask<IReadOnlyList<DataValue>> GetValuesAsync(IReadOnlyList<NodeId> nodeIds, CancellationToken ct = default)
            {
                throw new NotImplementedException();
            }

            public bool IsTypeOf(NodeId subTypeId, NodeId superTypeId)
            {
                throw new NotImplementedException();
            }

            public ValueTask LoadTypeHierarchyAsync(IReadOnlyList<NodeId> typeIds, CancellationToken ct = default)
            {
                throw new NotImplementedException();
            }

            internal void Add(Node baseObjectTypeNode)
            {
                throw new NotImplementedException();
            }

            internal void AddReference(NodeId nodeId, NodeId referenceTypeId, NodeId otherNodeId)
            {
                throw new NotImplementedException();
            }
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
