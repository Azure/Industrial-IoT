// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Module.Tests.Controller.TestData.Json
{
    using Autofac;
    using Azure.IIoT.OpcUa.Publisher.Module.Tests.Fixtures;
    using Azure.IIoT.OpcUa.Publisher.Testing.Fixtures;
    using Azure.IIoT.OpcUa.Publisher.Testing.Tests;
    using System;
    using System.Threading.Tasks;
    using Xunit;
    using Xunit.Abstractions;

    [Collection(ReadCollection.Name)]
    public sealed class ExpandTests1 : IClassFixture<PublisherModuleFixture>, IDisposable
    {
        public ExpandTests1(TestDataServer server, PublisherModuleFixture module, ITestOutputHelper output)
        {
            _server = server;
            _client = module.CreateRestClientContainer(output, TestSerializerType.Json);
        }

        public void Dispose()
        {
            _client.Dispose();
        }

        private ConfigurationTests1 GetTests()
        {
            return new ConfigurationTests1(_client.Resolve<IConfigurationServices>(),
                _server.GetConnection());
        }

        private readonly TestDataServer _server;
        private readonly IContainer _client;

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
