// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Core.Messaging.Clients.IoTEdge
{
    using Azure.IIoT.OpcUa.Core.IoTEdge;
    using Microsoft.Extensions.Options;
    using System;
    using Xunit;

    public sealed class IoTEdgeModuleClientTests
    {
        [Fact]
        public void ConstructorRejectsNullDependenciesBeforeCreatingSdkClient()
        {
            Assert.Throws<ArgumentNullException>(() => new IoTEdgeModuleClient(
                null!, new TestIdentity(), []));
            Assert.Throws<ArgumentNullException>(() => new IoTEdgeModuleClient(
                Options.Create(new IoTEdgeClientOptions()), null!, []));
        }

        private sealed class TestIdentity : IIoTEdgeDeviceIdentity
        {
            public string? Hub => "hub";
            public string DeviceId => "device";
            public string? ModuleId => "module";
            public string? Gateway => null;
        }
    }
}
