// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Module.Tests.Controller.TestData.MsgPack
{
    using Autofac;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Azure.IIoT.OpcUa.Publisher.Module.Tests.Fixtures;
    using Azure.IIoT.OpcUa.Publisher.Testing.Fixtures;
    using Azure.IIoT.OpcUa.Publisher.Testing.Tests;
    using System;
    using System.Threading.Tasks;
    using Xunit;
    using Xunit.Abstractions;

    [Collection(ReadCollection.Name)]
    public sealed class BrowseStreamTests : IClassFixture<PublisherModuleFixture>, IDisposable
    {
        public BrowseStreamTests(TestDataServer server, PublisherModuleFixture module, ITestOutputHelper output)
        {
            _server = server;
            _client = module.CreateRestClientContainer(output, TestSerializerType.MsgPack);
        }

        public void Dispose()
        {
            _client.Dispose();
        }

        private BrowseStreamTests<ConnectionModel> GetTests()
        {
            return new BrowseStreamTests<ConnectionModel>(
                _client.Resolve<INodeServices<ConnectionModel>>,
                _server.GetConnection());
        }

        private readonly TestDataServer _server;
        private readonly IContainer _client;

        [Fact]
        public Task NodeBrowseInRootTest1Async()
        {
            return GetTests().NodeBrowseInRootTest1Async();
        }

        [Fact]
        public Task NodeBrowseInRootTest2Async()
        {
            return GetTests().NodeBrowseInRootTest2Async();
        }

        [Fact]
        public Task NodeBrowseBoilersObjectsTest1Async()
        {
            return GetTests().NodeBrowseBoilersObjectsTest1Async();
        }

        [Fact]
        public Task NodeBrowseDataAccessObjectsTest1Async()
        {
            return GetTests().NodeBrowseDataAccessObjectsTest1Async();
        }

        [Fact]
        public Task NodeBrowseStaticScalarVariablesTestAsync()
        {
            return GetTests().NodeBrowseStaticScalarVariablesTestAsync();
        }

        [Fact]
        public Task NodeBrowseStaticArrayVariablesTestAsync()
        {
            return GetTests().NodeBrowseStaticArrayVariablesTestAsync();
        }

        [Fact]
        public Task NodeBrowseStaticScalarVariablesTestWithFilter1Async()
        {
            return GetTests().NodeBrowseStaticScalarVariablesTestWithFilter1Async();
        }

        [Fact]
        public Task NodeBrowseStaticScalarVariablesTestWithFilter2Async()
        {
            return GetTests().NodeBrowseStaticScalarVariablesTestWithFilter2Async();
        }

        [Fact]
        public Task NodeBrowseStaticScalarVariablesTestWithFilter3Async()
        {
            return GetTests().NodeBrowseStaticScalarVariablesTestWithFilter3Async();
        }

        [Fact]
        public Task NodeBrowseStaticScalarVariablesTestWithFilter4Async()
        {
            return GetTests().NodeBrowseStaticScalarVariablesTestWithFilter4Async();
        }

        [Fact]
        public Task NodeBrowseStaticScalarVariablesTestWithFilter5Async()
        {
            return GetTests().NodeBrowseStaticScalarVariablesTestWithFilter5Async();
        }

        [Fact]
        public Task NodeBrowseDiagnosticsNoneTestAsync()
        {
            return GetTests().NodeBrowseDiagnosticsNoneTestAsync();
        }

        [Fact]
        public Task NodeBrowseDiagnosticsStatusTestAsync()
        {
            return GetTests().NodeBrowseDiagnosticsStatusTestAsync();
        }

        [Fact]
        public Task NodeBrowseDiagnosticsOperationsTestAsync()
        {
            return GetTests().NodeBrowseDiagnosticsInfoTestAsync();
        }

        [Fact]
        public Task NodeBrowseDiagnosticsVerboseTestAsync()
        {
            return GetTests().NodeBrowseDiagnosticsVerboseTestAsync();
        }
    }
}
