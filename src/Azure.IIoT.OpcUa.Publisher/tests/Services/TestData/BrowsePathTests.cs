// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Tests.Services.TestData
{
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Azure.IIoT.OpcUa.Publisher.Services;
    using Azure.IIoT.OpcUa.Publisher.Testing.Fixtures;
    using Azure.IIoT.OpcUa.Publisher.Testing.Tests;
    using System.Threading.Tasks;
    using Xunit;
    using Xunit.Abstractions;

    [Collection(ReadCollection.Name)]
    public class BrowsePathTests
    {
        public BrowsePathTests(TestDataServer server, ITestOutputHelper output)
        {
            _server = server;
            _output = output;
        }

        private BrowsePathTests<ConnectionModel> GetTests()
        {
            return new BrowsePathTests<ConnectionModel>(
                () => new NodeServices<ConnectionModel>(_server.Client, _server.Parser,
                    _output.BuildLoggerFor<NodeServices<ConnectionModel>>(Logging.Level)),
                _server.GetConnection());
        }

        private readonly TestDataServer _server;
        private readonly ITestOutputHelper _output;

        [Fact]
        public Task NodeBrowsePathStaticScalarMethod3Test1Async()
        {
            return GetTests().NodeBrowsePathStaticScalarMethod3Test1Async();
        }

        [Fact]
        public Task NodeBrowsePathStaticScalarMethod3Test2Async()
        {
            return GetTests().NodeBrowsePathStaticScalarMethod3Test2Async();
        }

        [Fact]
        public Task NodeBrowsePathStaticScalarMethod3Test3Async()
        {
            return GetTests().NodeBrowsePathStaticScalarMethod3Test3Async();
        }

        [Fact]
        public Task NodeBrowsePathStaticScalarMethodsTestAsync()
        {
            return GetTests().NodeBrowsePathStaticScalarMethodsTestAsync();
        }
    }
}
