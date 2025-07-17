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
    public class ExpandTests1
    {
        public ExpandTests1(TestDataServer server, ITestOutputHelper output)
        {
            _server = server;
            _output = output;
        }

        private ConfigurationTests1 GetTests()
        {
#pragma warning disable CA2000 // Dispose objects before losing scope
            return new ConfigurationTests1(new ConfigurationServices(null!, _server.Client,
                new PublisherConfig(new ConfigurationBuilder().Build()).ToOptions(),
                _output.BuildLoggerFor<ConfigurationServices>(Logging.Level)),
                _server.GetConnection());
#pragma warning restore CA2000 // Dispose objects before losing scope
        }

        private readonly TestDataServer _server;
        private readonly ITestOutputHelper _output;

        [Fact]
        public Task ExpandObjectWithBrowsePathTest1Async()
        {
            return GetTests().ExpandObjectWithBrowsePathTest1Async();
        }

        [Fact]
        public Task ExpandObjectWithBrowsePathTest2Async()
        {
            return GetTests().ExpandObjectWithBrowsePathTest2Async();
        }

        [Fact]
        public Task ExpandObjectTest1Async()
        {
            return GetTests().ExpandObjectTest1Async();
        }

        [Fact]
        public Task ExpandObjectTest2Async()
        {
            return GetTests().ExpandObjectTest2Async();
        }

        [Fact]
        public Task ExpandServerObjectTest1Async()
        {
            return GetTests().ExpandServerObjectTest1Async();
        }

        [Fact]
        public Task ExpandServerObjectTest2Async()
        {
            return GetTests().ExpandServerObjectTest2Async();
        }

        [Fact]
        public Task ExpandServerObjectTest3Async()
        {
            return GetTests().ExpandServerObjectTest3Async();
        }

        [Fact]
        public Task ExpandServerObjectTest4Async()
        {
            return GetTests().ExpandServerObjectTest4Async();
        }

        [Fact]
        public Task ExpandServerObjectTest5Async()
        {
            return GetTests().ExpandServerObjectTest5Async();
        }

        [Fact]
        public Task ExpandBaseObjectTypeTest1Async()
        {
            return GetTests().ExpandBaseObjectTypeTest1Async();
        }

        [Fact]
        public Task ExpandBaseObjectTypeTest2Async()
        {
            return GetTests().ExpandBaseObjectTypeTest2Async();
        }

        [Fact]
        public Task ExpandBaseObjectsAndObjectTypesTestAsync()
        {
            return GetTests().ExpandBaseObjectsAndObjectTypesTestAsync();
        }

        [Fact]
        public Task ExpandBoilerTypeTestAsync()
        {
            return GetTests().ExpandBoilerTypeTestAsync();
        }

        [Fact]
        public Task ExpandBoilerDrumTypeTestAsync()
        {
            return GetTests().ExpandBoilerDrumTypeTestAsync();
        }

        [Fact]
        public Task ExpandBoilerDrumAndValveTypeTestAsync()
        {
            return GetTests().ExpandBoilerDrumAndValveTypeTestAsync();
        }

        [Fact]
        public Task ExpandValveTypeTestAsync()
        {
            return GetTests().ExpandValveTypeTestAsync();
        }

        [Fact]
        public Task ExpandTestDataObjectTypeTestAsync()
        {
            return GetTests().ExpandTestDataObjectTypeTestAsync();
        }

        [Fact]
        public Task ExpandServerTypeTestAsync()
        {
            return GetTests().ExpandServerTypeTestAsync();
        }

        [Fact]
        public Task ExpandPublishSubscribeTestAsync()
        {
            return GetTests().ExpandPublishSubscribeTestAsync();
        }

        [Fact]
        public Task ExpandVariablesTest1Async()
        {
            return GetTests().ExpandVariablesTest1Async();
        }

        [Fact]
        public Task ExpandVariablesAndObjectsTest1Async()
        {
            return GetTests().ExpandVariablesAndObjectsTest1Async();
        }

        [Fact]
        public Task ExpandVariableTypesTest1Async()
        {
            return GetTests().ExpandVariableTypesTest1Async();
        }

        [Fact]
        public Task ExpandVariableTypesTest2Async()
        {
            return GetTests().ExpandVariableTypesTest2Async();
        }

        [Fact]
        public Task ExpandVariableTypesTest3Async()
        {
            return GetTests().ExpandVariableTypesTest3Async();
        }

        [Fact]
        public Task ExpandObjectWithNoObjectsTest1Async()
        {
            return GetTests().ExpandObjectWithNoObjectsTest1Async();
        }

        [Fact]
        public Task ExpandObjectWithNoObjectsTest2Async()
        {
            return GetTests().ExpandObjectWithNoObjectsTest2Async();
        }

        [Fact]
        public Task ExpandEmptyEntryTest1Async()
        {
            return GetTests().ExpandEmptyEntryTest1Async();
        }

        [Fact]
        public Task ExpandEmptyEntryTest2Async()
        {
            return GetTests().ExpandEmptyEntryTest2Async();
        }

        [Fact]
        public Task ExpandBadNodeIdTest1Async()
        {
            return GetTests().ExpandBadNodeIdTest1Async();
        }

        [Fact]
        public Task ExpandBadNodeIdTest2Async()
        {
            return GetTests().ExpandBadNodeIdTest2Async();
        }

        [Fact]
        public Task ExpandBadNodeIdTest3Async()
        {
            return GetTests().ExpandBadNodeIdTest3Async();
        }
    }
}
