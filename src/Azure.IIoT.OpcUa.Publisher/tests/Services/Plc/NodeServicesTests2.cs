// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Tests.Services.Plc
{
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Azure.IIoT.OpcUa.Publisher.Services;
    using Azure.IIoT.OpcUa.Publisher.Testing.Fixtures;
    using Azure.IIoT.OpcUa.Publisher.Testing.Tests;
    using System.Threading.Tasks;
    using Xunit;
    using Xunit.Abstractions;

    public sealed class NodeServicesTests2 : IClassFixture<PlcServer>
    {
        public NodeServicesTests2(PlcServer server, ITestOutputHelper output)
        {
            _output = output;
            _server = server;
        }

        private PlcModelComplexTypeTests<ConnectionModel> GetTests()
        {
            return new PlcModelComplexTypeTests<ConnectionModel>(_server,
                () => new NodeServices<ConnectionModel>(_server.Client, _server.Parser,
                    _output.BuildLoggerFor<NodeServices<ConnectionModel>>(Logging.Level)),
                _server.GetConnection());
        }

        private readonly ITestOutputHelper _output;
        private readonly PlcServer _server;

        [Fact]
        public Task PlcModelHeaterTestsAsync()
        {
            return GetTests().PlcModelHeaterTestsAsync();
        }
    }
}
