// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Module.Tests.Mqtt.Plc
{
    using Azure.IIoT.OpcUa.Publisher.Module.Tests.Fixtures;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Azure.IIoT.OpcUa.Publisher.Testing.Fixtures;
    using Azure.IIoT.OpcUa.Publisher.Testing.Tests;
    using Autofac;
    using System.Threading.Tasks;
    using Xunit;
    using Xunit.Abstractions;

    public sealed class NodeServicesTests2 : TwinIntegrationTestBase,
        IClassFixture<PlcServer>
    {
        public NodeServicesTests2(PlcServer server, ITestOutputHelper output) : base(output)
        {
            _server = server;
            _module = new PublisherModule(null, testOutputHelper: output,
                version: Furly.Extensions.Mqtt.MqttVersion.v5);
        }

        private PlcModelComplexTypeTests<ConnectionModel> GetTests()
        {
            return new PlcModelComplexTypeTests<ConnectionModel>(_server,
                _module.ClientContainer.Resolve<INodeServices<ConnectionModel>>,
                _server.GetConnection());
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _module.Dispose();
            }
            base.Dispose(disposing);
        }

        private readonly PlcServer _server;
        private readonly PublisherModule _module;

        [Fact]
        public Task PlcModelHeaterTestsAsync()
        {
            return GetTests().PlcModelHeaterTestsAsync(Ct);
        }
    }
}
