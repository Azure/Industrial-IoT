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
    public sealed class BrowseStreamTests : TwinIntegrationTestBase, IClassFixture<PublisherModuleMqttv5Fixture>
    {
        public BrowseStreamTests(TestDataServer server,
            PublisherModuleMqttv5Fixture module, ITestOutputHelper output) : base(output)
        {
            _server = server;
            _module = module;
        }

        private BrowseStreamTests<ConnectionModel> GetTests()
        {
            return new BrowseStreamTests<ConnectionModel>(
                _module.SdkContainer.Resolve<INodeServices<ConnectionModel>>,
                _server.GetConnection());
        }

        private readonly TestDataServer _server;
        private readonly PublisherModuleMqttv5Fixture _module;

        [SkippableFact]
        public Task NodeBrowseInRootTest1Async()
        {
            Skip.If(true, "Not yet supported");
            return GetTests().NodeBrowseInRootTest1Async(Ct);
        }

        [SkippableFact]
        public Task NodeBrowseInRootTest2Async()
        {
            Skip.If(true, "Not yet supported");
            return GetTests().NodeBrowseInRootTest2Async(Ct);
        }

        [SkippableFact]
        public Task NodeBrowseBoilersObjectsTest1Async()
        {
            Skip.If(true, "Not yet supported");
            return GetTests().NodeBrowseBoilersObjectsTest1Async(Ct);
        }

        [SkippableFact]
        public Task NodeBrowseDataAccessObjectsTest1Async()
        {
            Skip.If(true, "Not yet supported");
            return GetTests().NodeBrowseDataAccessObjectsTest1Async(Ct);
        }

        [SkippableFact]
        public Task NodeBrowseStaticScalarVariablesTestAsync()
        {
            Skip.If(true, "Not yet supported");
            return GetTests().NodeBrowseStaticScalarVariablesTestAsync(Ct);
        }

        [SkippableFact]
        public Task NodeBrowseStaticArrayVariablesTestAsync()
        {
            Skip.If(true, "Not yet supported");
            return GetTests().NodeBrowseStaticArrayVariablesTestAsync(Ct);
        }

        [SkippableFact]
        public Task NodeBrowseStaticScalarVariablesTestWithFilter1Async()
        {
            Skip.If(true, "Not yet supported");
            return GetTests().NodeBrowseStaticScalarVariablesTestWithFilter1Async(Ct);
        }

        [SkippableFact]
        public Task NodeBrowseStaticScalarVariablesTestWithFilter2Async()
        {
            Skip.If(true, "Not yet supported");
            return GetTests().NodeBrowseStaticScalarVariablesTestWithFilter2Async(Ct);
        }

        [SkippableFact]
        public Task NodeBrowseStaticScalarVariablesTestWithFilter3Async()
        {
            Skip.If(true, "Not yet supported");
            return GetTests().NodeBrowseStaticScalarVariablesTestWithFilter3Async(Ct);
        }

        [SkippableFact]
        public Task NodeBrowseStaticScalarVariablesTestWithFilter4Async()
        {
            Skip.If(true, "Not yet supported");
            return GetTests().NodeBrowseStaticScalarVariablesTestWithFilter4Async(Ct);
        }

        [SkippableFact]
        public Task NodeBrowseStaticScalarVariablesTestWithFilter5Async()
        {
            Skip.If(true, "Not yet supported");
            return GetTests().NodeBrowseStaticScalarVariablesTestWithFilter5Async(Ct);
        }

        [SkippableFact]
        public Task NodeBrowseDiagnosticsNoneTestAsync()
        {
            Skip.If(true, "Not yet supported");
            return GetTests().NodeBrowseDiagnosticsNoneTestAsync(Ct);
        }

        [SkippableFact]
        public Task NodeBrowseDiagnosticsStatusTestAsync()
        {
            Skip.If(true, "Not yet supported");
            return GetTests().NodeBrowseDiagnosticsStatusTestAsync(Ct);
        }

        [SkippableFact]
        public Task NodeBrowseDiagnosticsOperationsTestAsync()
        {
            Skip.If(true, "Not yet supported");
            return GetTests().NodeBrowseDiagnosticsInfoTestAsync(Ct);
        }

        [SkippableFact]
        public Task NodeBrowseDiagnosticsVerboseTestAsync()
        {
            Skip.If(true, "Not yet supported");
            return GetTests().NodeBrowseDiagnosticsVerboseTestAsync(Ct);
        }
    }
}
