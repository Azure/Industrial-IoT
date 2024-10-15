// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Tests.Services.TestData
{
    using Azure.IIoT.OpcUa.Publisher.Services;
    using Azure.IIoT.OpcUa.Publisher.Testing.Fixtures;
    using Azure.IIoT.OpcUa.Publisher.Testing.Tests;
    using Microsoft.Extensions.Configuration;
    using System.Threading.Tasks;
    using Xunit;
    using Xunit.Abstractions;

    [Collection(ReadCollection.Name)]
    public class ExpandTests2
    {
        public ExpandTests2(TestDataServer server, ITestOutputHelper output)
        {
            _server = server;
            _output = output;
        }

        private ConfigurationTests2 GetTests()
        {
            return new ConfigurationTests2(c => new ConfigurationServices(c, _server.Client,
                new PublisherConfig(new ConfigurationBuilder().Build()).ToOptions(),
                _output.BuildLoggerFor<ConfigurationServices>(Logging.Level)),
                _server.GetConnection());
        }

        private readonly TestDataServer _server;
        private readonly ITestOutputHelper _output;

        [Fact]
        public Task ConfigureFromObjectErrorTest1Async()
        {
            return GetTests().ConfigureFromObjectErrorTest1Async();
        }

        [Fact]
        public Task ConfigureFromObjectErrorTest2Async()
        {
            return GetTests().ConfigureFromObjectErrorTest2Async();
        }

        [Fact]
        public Task ConfigureFromObjectErrorTest3Async()
        {
            return GetTests().ConfigureFromObjectErrorTest3Async();
        }

        [Fact]
        public Task ConfigureFromObjectWithBrowsePathTest1Async()
        {
            return GetTests().ConfigureFromObjectWithBrowsePathTest1Async();
        }

        [Fact]
        public Task ConfigureFromObjectWithBrowsePathTest2Async()
        {
            return GetTests().ConfigureFromObjectWithBrowsePathTest2Async();
        }

        [Fact]
        public Task ConfigureFromObjectTest1Async()
        {
            return GetTests().ConfigureFromObjectTest1Async();
        }

        [Fact]
        public Task ConfigureFromObjectTest2Async()
        {
            return GetTests().ConfigureFromObjectTest2Async();
        }

        [Fact]
        public Task ConfigureFromServerObjectTest1Async()
        {
            return GetTests().ConfigureFromServerObjectTest1Async();
        }

        [Fact]
        public Task ConfigureFromServerObjectTest2Async()
        {
            return GetTests().ConfigureFromServerObjectTest2Async();
        }

        [Fact]
        public Task ConfigureFromServerObjectTest3Async()
        {
            return GetTests().ConfigureFromServerObjectTest3Async();
        }

        [Fact]
        public Task ConfigureFromServerObjectTest4Async()
        {
            return GetTests().ConfigureFromServerObjectTest4Async();
        }

        [Fact]
        public Task ConfigureFromServerObjectTest5Async()
        {
            return GetTests().ConfigureFromServerObjectTest5Async();
        }

        [Fact]
        public Task ConfigureFromBaseObjectTypeTest1Async()
        {
            return GetTests().ConfigureFromBaseObjectTypeTest1Async();
        }

        [Fact]
        public Task ConfigureFromBaseObjectTypeTest2Async()
        {
            return GetTests().ConfigureFromBaseObjectTypeTest2Async();
        }

        [Fact]
        public Task ConfigureFromBaseObjectsAndObjectTypesTestAsync()
        {
            return GetTests().ConfigureFromBaseObjectsAndObjectTypesTestAsync();
        }

        [Fact]
        public Task ConfigureFromVariablesTest1Async()
        {
            return GetTests().ConfigureFromVariablesTest1Async();
        }

        [Fact]
        public Task ConfigureFromVariablesAndObjectsTest1Async()
        {
            return GetTests().ConfigureFromVariablesAndObjectsTest1Async();
        }

        [Fact]
        public Task ConfigureFromVariableTypesTest1Async()
        {
            return GetTests().ConfigureFromVariableTypesTest1Async();
        }

        [Fact]
        public Task ConfigureFromVariableTypesTest2Async()
        {
            return GetTests().ConfigureFromVariableTypesTest2Async();
        }

        [Fact]
        public Task ConfigureFromVariableTypesTest3Async()
        {
            return GetTests().ConfigureFromVariableTypesTest3Async();
        }

        [Fact]
        public Task ConfigureFromObjectWithNoObjectsTest1Async()
        {
            return GetTests().ConfigureFromObjectWithNoObjectsTest1Async();
        }

        [Fact]
        public Task ConfigureFromObjectWithNoObjectsTest2Async()
        {
            return GetTests().ConfigureFromObjectWithNoObjectsTest2Async();
        }

        [Fact]
        public Task ConfigureFromEmptyEntryTest1Async()
        {
            return GetTests().ConfigureFromEmptyEntryTest1Async();
        }

        [Fact]
        public Task ConfigureFromEmptyEntryTest2Async()
        {
            return GetTests().ConfigureFromEmptyEntryTest2Async();
        }

        [Fact]
        public Task ConfigureFromBadNodeIdTest1Async()
        {
            return GetTests().ConfigureFromBadNodeIdTest1Async();
        }

        [Fact]
        public Task ConfigureFromBadNodeIdTest2Async()
        {
            return GetTests().ConfigureFromBadNodeIdTest2Async();
        }

        [Fact]
        public Task ConfigureFromBadNodeIdTest3Async()
        {
            return GetTests().ConfigureFromBadNodeIdTest3Async();
        }
    }
}
