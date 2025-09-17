// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Tests.Stack
{
    using Azure.IIoT.OpcUa.Encoders;
    using Azure.IIoT.OpcUa.Publisher.Stack;
    using Azure.IIoT.OpcUa.Publisher.Stack.Extensions;
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
        protected virtual Mock<IOpcUaSession> SetupMockedSession(NamespaceTable namespaceTable = null)
        {
            using var mock = Autofac.Extras.Moq.AutoMock.GetLoose();
            namespaceTable ??= new NamespaceTable();

            var s = new Mock<ISession>();
            s.Setup(x => x.ReadNodeAsync(It.IsAny<NodeId>(), It.IsAny<CancellationToken>()))
                .Returns((NodeId x, CancellationToken _)
                => Task.FromResult(GetNode(x)));
            s.Setup(x => x.ReadNodesAsync(It.IsAny<IList<NodeId>>(), It.IsAny<NodeClass>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .Returns((IList<NodeId> nodeIds, NodeClass nodeClass, bool includeReferences, CancellationToken cancellationToken)
                => Task.FromResult(GetNodes(nodeIds, nodeClass, includeReferences)));
            s.Setup(x => x.FetchReferencesAsync(It.IsAny<NodeId>(), It.IsAny<CancellationToken>()))
                .Returns((NodeId x, CancellationToken _)
                => Task.FromResult(new ReferenceDescriptionCollection(GetReferences(x))));

            var nodeCache = new LruNodeCache(s.Object);

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

        protected virtual IEnumerable<ReferenceDescription> GetReferences(NodeId x)
        {
            var node = GetNode(x);
            return node == null ? Array.Empty<ReferenceDescription>() :
                node.ReferenceTable.Select(r => new ReferenceDescription
                {
                    ReferenceTypeId = new NodeId(r.ReferenceTypeId),
                    IsForward = !r.IsInverse,
                    NodeId = new ExpandedNodeId(r.TargetId)
                });
        }

        protected virtual IEnumerable<ReferenceDescription> GetReferences(uint id)
        {
            return Array.Empty<ReferenceDescription>();
        }

        protected virtual (IList<Node>, IList<ServiceResult>) GetNodes(
            IList<NodeId> nodeIds, NodeClass nodeClass, bool includeReferences)
        {
            var nodes = new List<Node>();
            var results = new List<ServiceResult>();
            foreach (var id in nodeIds)
            {
                var node = GetNode(id);
                if (node != null && (nodeClass == NodeClass.Unspecified || node.NodeClass == nodeClass))
                {
                    nodes.Add(node);
                    results.Add(ServiceResult.Good);
                }
                else
                {
                    results.Add(new ServiceResult(StatusCodes.BadNodeIdUnknown));
                }
            }
            return (nodes, results);
        }

        protected virtual Node GetNode(NodeId x)
        {
            if (x.IdType == IdType.Numeric && x.Identifier is uint id)
            {
                return GetNode(id);
            }
            return null;
        }

        protected virtual Node GetNode(uint id)
        {
            return null;
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
