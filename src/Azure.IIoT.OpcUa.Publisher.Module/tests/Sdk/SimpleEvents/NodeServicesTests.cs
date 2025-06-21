// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Module.Tests.Sdk.SimpleEvents
{
    using Autofac;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Azure.IIoT.OpcUa.Publisher.Module.Tests.Fixtures;
    using Azure.IIoT.OpcUa.Publisher.Testing.Fixtures;
    using Azure.IIoT.OpcUa.Publisher.Testing.Tests;
    using System.Threading.Tasks;
    using Xunit;

    public sealed class NodeServicesTests : IClassFixture<SimpleEventsServer>, IClassFixture<PublisherModuleFixture>
    {
        public NodeServicesTests(SimpleEventsServer server, PublisherModuleFixture module)
        {
            _server = server;
            _module = module;
        }

        private SimpleEventsServerTests<ConnectionModel> GetTests()
        {
            return new SimpleEventsServerTests<ConnectionModel>(
                _module.SdkContainer.Resolve<INodeServices<ConnectionModel>>,
                _server.GetConnection());
        }

        private readonly SimpleEventsServer _server;
        private readonly PublisherModuleFixture _module;

        [Fact]
        public Task CompileSimpleBaseEventQueryTestAsync()
        {
            return GetTests().CompileSimpleBaseEventQueryTestAsync();
        }

        [Fact]
        public Task CompileSimpleEventsQueryTestAsync()
        {
            return GetTests().CompileSimpleEventsQueryTestAsync();
        }
    }
}
