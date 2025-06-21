// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Module.Tests.Mqtt.TestData
{
    using Autofac;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Azure.IIoT.OpcUa.Publisher.Module.Tests.Fixtures;
    using Azure.IIoT.OpcUa.Publisher.Testing.Fixtures;
    using Azure.IIoT.OpcUa.Publisher.Testing.Tests;
    using System.Threading.Tasks;
    using Xunit;
    using Xunit.Abstractions;

    [Collection(ReadCollection.Name)]
    public class BrowseTests : TwinIntegrationTestBase, IClassFixture<PublisherModuleMqttv5Fixture>
    {
        public BrowseTests(TestDataServer server,
            PublisherModuleMqttv5Fixture module, ITestOutputHelper output) : base(output)
        {
            _server = server;
            _module = module;
        }

        private BrowseServicesTests<ConnectionModel> GetTests()
        {
            return new BrowseServicesTests<ConnectionModel>(
                _module.SdkContainer.Resolve<INodeServices<ConnectionModel>>,
                    _server.GetConnection());
        }

        private readonly TestDataServer _server;
        private readonly PublisherModuleMqttv5Fixture _module;

        [Fact]
        public Task NodeBrowseInRootTest1Async()
        {
            return GetTests().NodeBrowseInRootTest1Async(Ct);
        }

        [Fact]
        public Task NodeBrowseInRootTest2Async()
        {
            return GetTests().NodeBrowseInRootTest2Async(Ct);
        }

        [Fact]
        public Task NodeBrowseFirstInRootTest1Async()
        {
            return GetTests().NodeBrowseFirstInRootTest1Async(Ct);
        }

        [Fact]
        public Task NodeBrowseFirstInRootTest2Async()
        {
            return GetTests().NodeBrowseFirstInRootTest2Async(Ct);
        }

        [Fact]
        public Task NodeBrowseBoilersObjectsTest1Async()
        {
            return GetTests().NodeBrowseBoilersObjectsTest1Async(Ct);
        }

        [Fact]
        public Task NodeBrowseBoilersObjectsTest2Async()
        {
            return GetTests().NodeBrowseBoilersObjectsTest2Async(Ct);
        }

        [Fact]
        public Task NodeBrowseDataAccessObjectsTest1Async()
        {
            return GetTests().NodeBrowseDataAccessObjectsTest1Async(Ct);
        }

        [Fact]
        public Task NodeBrowseDataAccessObjectsTest2Async()
        {
            return GetTests().NodeBrowseDataAccessObjectsTest2Async(Ct);
        }

        [Fact]
        public Task NodeBrowseDataAccessObjectsTest3Async()
        {
            return GetTests().NodeBrowseDataAccessObjectsTest3Async(Ct);
        }

        [Fact]
        public Task NodeBrowseDataAccessObjectsTest4Async()
        {
            return GetTests().NodeBrowseDataAccessObjectsTest4Async(Ct);
        }

        [Fact]
        public Task NodeBrowseDataAccessFC1001Test1Async()
        {
            return GetTests().NodeBrowseDataAccessFC1001Test1Async(Ct);
        }

        [Fact]
        public Task NodeBrowseDataAccessFC1001Test2Async()
        {
            return GetTests().NodeBrowseDataAccessFC1001Test2Async(Ct);
        }

        [Fact]
        public Task NodeBrowseStaticScalarVariablesTestAsync()
        {
            return GetTests().NodeBrowseStaticScalarVariablesTestAsync(Ct);
        }

        [Fact]
        public Task NodeBrowseStaticArrayVariablesTestAsync()
        {
            return GetTests().NodeBrowseStaticArrayVariablesTestAsync(Ct);
        }

        [Fact]
        public Task NodeBrowseStaticScalarVariablesTestWithFilter1Async()
        {
            return GetTests().NodeBrowseStaticScalarVariablesTestWithFilter1Async(Ct);
        }

        [Fact]
        public Task NodeBrowseStaticScalarVariablesTestWithFilter2Async()
        {
            return GetTests().NodeBrowseStaticScalarVariablesTestWithFilter2Async(Ct);
        }

        [Fact]
        public Task NodeBrowseStaticArrayVariablesWithValuesTestAsync()
        {
            return GetTests().NodeBrowseStaticArrayVariablesWithValuesTestAsync(Ct);
        }

        [Fact]
        public Task NodeBrowseStaticArrayVariablesRawModeTestAsync()
        {
            return GetTests().NodeBrowseStaticArrayVariablesRawModeTestAsync(Ct);
        }

        [Fact]
        public Task NodeBrowseContinuationTest1Async()
        {
            return GetTests().NodeBrowseContinuationTest1Async(Ct);
        }

        [Fact]
        public Task NodeBrowseContinuationTest2Async()
        {
            return GetTests().NodeBrowseContinuationTest2Async(Ct);
        }

        [Fact]
        public Task NodeBrowseContinuationTest3Async()
        {
            return GetTests().NodeBrowseContinuationTest3Async(Ct);
        }

        [Fact]
        public Task NodeBrowseContinuationTest4Async()
        {
            return GetTests().NodeBrowseContinuationTest4Async(Ct);
        }

        [Fact]
        public Task NodeBrowseDiagnosticsNoneTestAsync()
        {
            return GetTests().NodeBrowseDiagnosticsNoneTestAsync(Ct);
        }

        [Fact]
        public Task NodeBrowseDiagnosticsStatusTestAsync()
        {
            return GetTests().NodeBrowseDiagnosticsStatusTestAsync(Ct);
        }

        [Fact]
        public Task NodeBrowseDiagnosticsInfoTestAsync()
        {
            return GetTests().NodeBrowseDiagnosticsInfoTestAsync(Ct);
        }

        [Fact]
        public Task NodeBrowseDiagnosticsVerboseTestAsync()
        {
            return GetTests().NodeBrowseDiagnosticsVerboseTestAsync(Ct);
        }
    }
}
