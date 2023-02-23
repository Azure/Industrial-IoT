// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Stack.Tests
{
    using Azure.IIoT.OpcUa.Encoders;
    using Azure.IIoT.OpcUa.Publisher.Stack.Models;
    using Azure.IIoT.OpcUa.Publisher.Stack.Services;
    using Furly.Extensions.Logging;
    using Furly.Extensions.Serializers.Newtonsoft;
    using Moq;
    using Opc.Ua;
    using Opc.Ua.Client;

    public abstract class EventTestsBase
    {
        protected INodeCache GetNodeCache(NamespaceTable namespaceTable = null)
        {
            return SetupMockedNodeCache(namespaceTable).Object;
        }

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

        protected OpcUaMonitoredItem GetMonitoredItem(
            BaseMonitoredItemModel template,
            ServiceMessageContext messageContext = null,
            INodeCache nodeCache = null,
            IVariantEncoder codec = null)
        {
            codec ??= new JsonVariantEncoder(messageContext ?? new ServiceMessageContext(),
                new NewtonsoftJsonSerializer());
            nodeCache ??= GetNodeCache();
            var monitoredItemWrapper = new OpcUaMonitoredItem(template, Log.Console<OpcUaMonitoredItem>());
            monitoredItemWrapper.Create(
                messageContext ?? new ServiceMessageContext(),
                nodeCache,
                nodeCache.TypeTree,
                codec);
            return monitoredItemWrapper;
        }
    }
}
