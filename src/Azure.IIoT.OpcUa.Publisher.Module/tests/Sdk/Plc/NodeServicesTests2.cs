// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Module.Tests.Sdk.Plc
{
    using Autofac;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Azure.IIoT.OpcUa.Publisher.Module.Tests.Fixtures;
    using Azure.IIoT.OpcUa.Publisher.Testing.Fixtures;
    using Azure.IIoT.OpcUa.Publisher.Testing.Tests;
    using System.Threading.Tasks;
    using Xunit;

    public sealed class NodeServicesTests2 : IClassFixture<PlcServer>, IClassFixture<PublisherModuleFixture>
    {
        public NodeServicesTests2(PlcServer server, PublisherModuleFixture module)
        {
            _server = server;
            _module = module;
        }

        private PlcModelComplexTypeTests<ConnectionModel> GetTests()
        {
            return new PlcModelComplexTypeTests<ConnectionModel>(_server,
                _module.SdkContainer.Resolve<INodeServices<ConnectionModel>>,
                _server.GetConnection());
        }

        private readonly PlcServer _server;
        private readonly PublisherModuleFixture _module;

        [Fact]
        public Task PlcModelHeaterTestsAsync()
        {
            return GetTests().PlcModelHeaterTestsAsync();
        }
    }
}
