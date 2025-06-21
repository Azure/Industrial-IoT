// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Module.Tests.Sdk.TestData
{
    using Autofac;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Azure.IIoT.OpcUa.Publisher.Module.Tests.Fixtures;
    using Azure.IIoT.OpcUa.Publisher.Testing.Fixtures;
    using Azure.IIoT.OpcUa.Publisher.Testing.Tests;
    using System.Threading.Tasks;
    using Xunit;

    [Collection(ReadCollection.Name)]
    public sealed class BrowseStreamTests : IClassFixture<PublisherModuleFixture>
    {
        public BrowseStreamTests(TestDataServer server, PublisherModuleFixture module)
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
        private readonly PublisherModuleFixture _module;

        [SkippableFact]
        public Task NodeBrowseInRootTest1Async()
        {
            Skip.If(true, "Not yet supported");
            return GetTests().NodeBrowseInRootTest1Async();
        }

        [SkippableFact]
        public Task NodeBrowseInRootTest2Async()
        {
            Skip.If(true, "Not yet supported");
            return GetTests().NodeBrowseInRootTest2Async();
        }

        [SkippableFact]
        public Task NodeBrowseBoilersObjectsTest1Async()
        {
            Skip.If(true, "Not yet supported");
            return GetTests().NodeBrowseBoilersObjectsTest1Async();
        }

        [SkippableFact]
        public Task NodeBrowseDataAccessObjectsTest1Async()
        {
            Skip.If(true, "Not yet supported");
            return GetTests().NodeBrowseDataAccessObjectsTest1Async();
        }

        [SkippableFact]
        public Task NodeBrowseStaticScalarVariablesTestAsync()
        {
            Skip.If(true, "Not yet supported");
            return GetTests().NodeBrowseStaticScalarVariablesTestAsync();
        }

        [SkippableFact]
        public Task NodeBrowseStaticArrayVariablesTestAsync()
        {
            Skip.If(true, "Not yet supported");
            return GetTests().NodeBrowseStaticArrayVariablesTestAsync();
        }

        [SkippableFact]
        public Task NodeBrowseStaticScalarVariablesTestWithFilter1Async()
        {
            Skip.If(true, "Not yet supported");
            return GetTests().NodeBrowseStaticScalarVariablesTestWithFilter1Async();
        }

        [SkippableFact]
        public Task NodeBrowseStaticScalarVariablesTestWithFilter2Async()
        {
            Skip.If(true, "Not yet supported");
            return GetTests().NodeBrowseStaticScalarVariablesTestWithFilter2Async();
        }

        [SkippableFact]
        public Task NodeBrowseStaticScalarVariablesTestWithFilter3Async()
        {
            Skip.If(true, "Not yet supported");
            return GetTests().NodeBrowseStaticScalarVariablesTestWithFilter3Async();
        }

        [SkippableFact]
        public Task NodeBrowseStaticScalarVariablesTestWithFilter4Async()
        {
            Skip.If(true, "Not yet supported");
            return GetTests().NodeBrowseStaticScalarVariablesTestWithFilter4Async();
        }

        [SkippableFact]
        public Task NodeBrowseStaticScalarVariablesTestWithFilter5Async()
        {
            Skip.If(true, "Not yet supported");
            return GetTests().NodeBrowseStaticScalarVariablesTestWithFilter5Async();
        }

        [SkippableFact]
        public Task NodeBrowseDiagnosticsNoneTestAsync()
        {
            Skip.If(true, "Not yet supported");
            return GetTests().NodeBrowseDiagnosticsNoneTestAsync();
        }

        [SkippableFact]
        public Task NodeBrowseDiagnosticsStatusTestAsync()
        {
            Skip.If(true, "Not yet supported");
            return GetTests().NodeBrowseDiagnosticsStatusTestAsync();
        }

        [SkippableFact]
        public Task NodeBrowseDiagnosticsOperationsTestAsync()
        {
            Skip.If(true, "Not yet supported");
            return GetTests().NodeBrowseDiagnosticsInfoTestAsync();
        }

        [SkippableFact]
        public Task NodeBrowseDiagnosticsVerboseTestAsync()
        {
            Skip.If(true, "Not yet supported");
            return GetTests().NodeBrowseDiagnosticsVerboseTestAsync();
        }
    }
}
