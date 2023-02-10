// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Protocol.Services {
    using Microsoft.Azure.IIoT.OpcUa.Protocol.Models;
    using Moq;
    using Opc.Ua;
    using Opc.Ua.Client;
    using Serilog;

    public abstract class EventTestsBase {
        protected INodeCache GetNodeCache(NamespaceTable namespaceTable = null) {
            return SetupMockedNodeCache(namespaceTable).Object;
        }

        protected virtual Mock<INodeCache> SetupMockedNodeCache(NamespaceTable namespaceTable = null) {
            using var mock = Autofac.Extras.Moq.AutoMock.GetLoose();
            var nodeCache = mock.Mock<INodeCache>();
            if (namespaceTable == null) {
                namespaceTable = new NamespaceTable();
            }
            var typeTable = new TypeTable(namespaceTable);
            nodeCache.SetupGet(x => x.TypeTree).Returns(typeTable);
            nodeCache.SetupGet(x => x.NamespaceUris).Returns(namespaceTable);
            return nodeCache;
        }

        protected OpcUaMonitoredItem GetMonitoredItem(
            BaseMonitoredItemModel template,
            ServiceMessageContext messageContext = null,
            INodeCache nodeCache = null,
            IVariantEncoder codec = null) {

            if (codec == null) {
                codec = messageContext == null
                    ? new VariantEncoderFactory().Default
                    : new VariantEncoderFactory().Create(messageContext);
            }

            nodeCache ??= GetNodeCache();
            var monitoredItemWrapper = new OpcUaMonitoredItem(template, Log.Logger);
            monitoredItemWrapper.Create(
                messageContext ?? new ServiceMessageContext(),
                nodeCache,
                nodeCache.TypeTree,
                codec);
            return monitoredItemWrapper;
        }
    }
}
