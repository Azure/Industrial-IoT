﻿// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Tests.Services
{
    using Azure.IIoT.OpcUa.Publisher.Stack;
    using Azure.IIoT.OpcUa.Encoders;
    using Furly.Extensions.Serializers.Newtonsoft;
    using Moq;
    using Opc.Ua;
    using Opc.Ua.Client;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using System.Threading;

    public abstract class DataSetWriterResolverTestBase
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

            session.Setup(x => x.GetOperationLimitsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new OperationLimitsModel());
            return session;
        }
    }
}
