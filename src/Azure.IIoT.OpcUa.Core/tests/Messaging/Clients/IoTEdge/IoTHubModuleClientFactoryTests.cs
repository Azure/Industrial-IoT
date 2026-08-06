// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Core.Messaging.Clients.IoTEdge
{
    using Azure.IIoT.OpcUa.Core.IoTEdge;
    using System;
    using Xunit;

    public sealed class IoTHubModuleClientFactoryTests
    {
        [Fact]
        public void InstanceIsSingleton()
        {
            var a = IoTHubModuleClientFactory.Instance;
            var b = IoTHubModuleClientFactory.Instance;
            Assert.Same(a, b);
        }

        [Fact]
        public void InstanceImplementsInterface()
        {
            Assert.IsAssignableFrom<IIoTHubModuleClientFactory>(
                IoTHubModuleClientFactory.Instance);
        }

        [Fact]
        public void CreateThrowsWhenOptionsIsNull()
        {
            Assert.Throws<ArgumentNullException>(() =>
                IoTHubModuleClientFactory.Instance.Create(null!, _ => { }));
        }

        [Fact]
        public void CreateThrowsWhenConfigureIsNull()
        {
            Assert.Throws<ArgumentNullException>(() =>
                IoTHubModuleClientFactory.Instance.Create(
                    new IoTEdgeClientOptions(), null!));
        }
    }
}
