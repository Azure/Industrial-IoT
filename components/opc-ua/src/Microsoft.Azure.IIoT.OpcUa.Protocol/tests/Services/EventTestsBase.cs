namespace Microsoft.Azure.IIoT.OpcUa.Protocol.Services {
    using Microsoft.Azure.IIoT.OpcUa.Protocol.Models;
    using Moq;
    using Opc.Ua;
    using Opc.Ua.Client;
    using Serilog;
    using static Microsoft.Azure.IIoT.OpcUa.Protocol.Services.SubscriptionServices;

    public class EventTestsBase {
        protected INodeCache GetNodeCache() {
            return SetupMockedNodeCache().Object;
        }

        protected virtual Mock<INodeCache> SetupMockedNodeCache() {
            using var mock = Autofac.Extras.Moq.AutoMock.GetLoose();
            var nodeCache = mock.Mock<INodeCache>();
            var typeTable = new TypeTable(new NamespaceTable());
            nodeCache.SetupGet(x => x.TypeTree).Returns(typeTable);
            return nodeCache;
        }

        protected MonitoredItemWrapper GetMonitoredItemWrapper(BaseMonitoredItemModel template, ServiceMessageContext messageContext = null, INodeCache nodeCache = null, IVariantEncoder codec = null, bool activate = true) {
            var monitoredItemWrapper = new MonitoredItemWrapper(template, Log.Logger);
            monitoredItemWrapper.Create(
                messageContext ?? new ServiceMessageContext(),
                nodeCache ?? GetNodeCache(),
                codec ?? new VariantEncoderFactory().Default,
                activate);
            return monitoredItemWrapper;
        }
    }
}
