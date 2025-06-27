// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Module.Tests.Sdk.TestData
{
    using Azure.IIoT.OpcUa.Publisher.Module.Tests.Fixtures;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Azure.IIoT.OpcUa.Publisher.Stack;
    using Azure.IIoT.OpcUa.Publisher.Testing.Fixtures;
    using Azure.IIoT.OpcUa.Publisher.Testing.Tests;
    using Autofac;
    using System.Threading.Tasks;
    using Xunit;

    [Collection(ReadCollection.Name)]
    public class ValueReadScalarTests : IClassFixture<PublisherModuleFixture>
    {
        public ValueReadScalarTests(TestDataServer server, PublisherModuleFixture module)
        {
            _server = server;
            _module = module;
        }

        private ReadScalarValueTests<ConnectionModel> GetTests()
        {
            return new ReadScalarValueTests<ConnectionModel>(
                _module.SdkContainer.Resolve<INodeServices<ConnectionModel>>,
                _server.GetConnection(), (ep, n, s) => _server.Client.ReadValueAsync(new ConnectionModel
                {
                    Endpoint = new EndpointModel
                    {
                        Url = ep.Endpoint.Url,
                        Certificate = _server.Certificate?.RawData?.ToThumbprint()
                    }
                }, n, s));
        }

        private readonly TestDataServer _server;
        private readonly PublisherModuleFixture _module;

        [Fact]
        public Task NodeReadAllStaticScalarVariableNodeClassTest1Async()
        {
            return GetTests().NodeReadAllStaticScalarVariableNodeClassTest1Async();
        }

        [Fact]
        public Task NodeReadAllStaticScalarVariableAccessLevelTest1Async()
        {
            return GetTests().NodeReadAllStaticScalarVariableAccessLevelTest1Async();
        }

        [Fact]
        public Task NodeReadAllStaticScalarVariableWriteMaskTest1Async()
        {
            return GetTests().NodeReadAllStaticScalarVariableWriteMaskTest1Async();
        }

        [Fact]
        public Task NodeReadAllStaticScalarVariableWriteMaskTest2Async()
        {
            return GetTests().NodeReadAllStaticScalarVariableWriteMaskTest2Async();
        }

        [Fact]
        public Task NodeReadStaticScalarBooleanValueVariableTestAsync()
        {
            return GetTests().NodeReadStaticScalarBooleanValueVariableTestAsync();
        }

        [Fact]
        public Task NodeReadStaticScalarBooleanValueVariableWithBrowsePathTest1Async()
        {
            return GetTests().NodeReadStaticScalarBooleanValueVariableWithBrowsePathTest1Async();
        }

        [Fact]
        public Task NodeReadStaticScalarBooleanValueVariableWithBrowsePathTest2Async()
        {
            return GetTests().NodeReadStaticScalarBooleanValueVariableWithBrowsePathTest2Async();
        }

        [Fact]
        public Task NodeReadStaticScalarBooleanValueVariableWithBrowsePathTest3Async()
        {
            return GetTests().NodeReadStaticScalarBooleanValueVariableWithBrowsePathTest3Async();
        }

        [Fact]
        public Task NodeReadStaticScalarSByteValueVariableTestAsync()
        {
            return GetTests().NodeReadStaticScalarSByteValueVariableTestAsync();
        }

        [Fact]
        public Task NodeReadStaticScalarByteValueVariableTestAsync()
        {
            return GetTests().NodeReadStaticScalarByteValueVariableTestAsync();
        }

        [Fact]
        public Task NodeReadStaticScalarInt16ValueVariableTestAsync()
        {
            return GetTests().NodeReadStaticScalarInt16ValueVariableTestAsync();
        }

        [Fact]
        public Task NodeReadStaticScalarUInt16ValueVariableTestAsync()
        {
            return GetTests().NodeReadStaticScalarUInt16ValueVariableTestAsync();
        }

        [Fact]
        public Task NodeReadStaticScalarInt32ValueVariableTestAsync()
        {
            return GetTests().NodeReadStaticScalarInt32ValueVariableTestAsync();
        }

        [Fact]
        public Task NodeReadStaticScalarUInt32ValueVariableTestAsync()
        {
            return GetTests().NodeReadStaticScalarUInt32ValueVariableTestAsync();
        }

        [Fact]
        public Task NodeReadStaticScalarInt64ValueVariableTestAsync()
        {
            return GetTests().NodeReadStaticScalarInt64ValueVariableTestAsync();
        }

        [Fact]
        public Task NodeReadStaticScalarUInt64ValueVariableTestAsync()
        {
            return GetTests().NodeReadStaticScalarUInt64ValueVariableTestAsync();
        }

        [Fact]
        public Task NodeReadStaticScalarFloatValueVariableTestAsync()
        {
            return GetTests().NodeReadStaticScalarFloatValueVariableTestAsync();
        }

        [Fact]
        public Task NodeReadStaticScalarDoubleValueVariableTestAsync()
        {
            return GetTests().NodeReadStaticScalarDoubleValueVariableTestAsync();
        }

        [Fact]
        public Task NodeReadStaticScalarStringValueVariableTestAsync()
        {
            return GetTests().NodeReadStaticScalarStringValueVariableTestAsync();
        }

        [Fact]
        public Task NodeReadStaticScalarDateTimeValueVariableTestAsync()
        {
            return GetTests().NodeReadStaticScalarDateTimeValueVariableTestAsync();
        }

        [Fact]
        public Task NodeReadStaticScalarGuidValueVariableTestAsync()
        {
            return GetTests().NodeReadStaticScalarGuidValueVariableTestAsync();
        }

        [Fact]
        public Task NodeReadStaticScalarByteStringValueVariableTestAsync()
        {
            return GetTests().NodeReadStaticScalarByteStringValueVariableTestAsync();
        }

        [Fact]
        public Task NodeReadStaticScalarXmlElementValueVariableTestAsync()
        {
            return GetTests().NodeReadStaticScalarXmlElementValueVariableTestAsync();
        }

        [Fact]
        public Task NodeReadStaticScalarNodeIdValueVariableTestAsync()
        {
            return GetTests().NodeReadStaticScalarNodeIdValueVariableTestAsync();
        }

        [Fact]
        public Task NodeReadStaticScalarExpandedNodeIdValueVariableTestAsync()
        {
            return GetTests().NodeReadStaticScalarExpandedNodeIdValueVariableTestAsync();
        }

        [Fact]
        public Task NodeReadStaticScalarQualifiedNameValueVariableTestAsync()
        {
            return GetTests().NodeReadStaticScalarQualifiedNameValueVariableTestAsync();
        }

        [Fact]
        public Task NodeReadStaticScalarLocalizedTextValueVariableTestAsync()
        {
            return GetTests().NodeReadStaticScalarLocalizedTextValueVariableTestAsync();
        }

        [Fact]
        public Task NodeReadStaticScalarStatusCodeValueVariableTestAsync()
        {
            return GetTests().NodeReadStaticScalarStatusCodeValueVariableTestAsync();
        }

        [Fact]
        public Task NodeReadStaticScalarVariantValueVariableTestAsync()
        {
            return GetTests().NodeReadStaticScalarVariantValueVariableTestAsync();
        }

        [Fact]
        public Task NodeReadStaticScalarEnumerationValueVariableTestAsync()
        {
            return GetTests().NodeReadStaticScalarEnumerationValueVariableTestAsync();
        }

        [Fact]
        public Task NodeReadStaticScalarStructuredValueVariableTestAsync()
        {
            return GetTests().NodeReadStaticScalarStructuredValueVariableTestAsync();
        }

        [Fact]
        public Task NodeReadStaticScalarNumberValueVariableTestAsync()
        {
            return GetTests().NodeReadStaticScalarNumberValueVariableTestAsync();
        }

        [Fact]
        public Task NodeReadStaticScalarIntegerValueVariableTestAsync()
        {
            return GetTests().NodeReadStaticScalarIntegerValueVariableTestAsync();
        }

        [Fact]
        public Task NodeReadStaticScalarUIntegerValueVariableTestAsync()
        {
            return GetTests().NodeReadStaticScalarUIntegerValueVariableTestAsync();
        }

        [Fact]
        public Task NodeReadDataAccessMeasurementFloatValueTestAsync()
        {
            return GetTests().NodeReadDataAccessMeasurementFloatValueTestAsync();
        }

        [Fact]
        public Task NodeReadDiagnosticsNoneTestAsync()
        {
            return GetTests().NodeReadDiagnosticsNoneTestAsync();
        }

        [Fact]
        public Task NodeReadDiagnosticsStatusTestAsync()
        {
            return GetTests().NodeReadDiagnosticsStatusTestAsync();
        }

        [Fact]
        public Task NodeReadDiagnosticsDebugTestAsync()
        {
            return GetTests().NodeReadDiagnosticsDebugTestAsync();
        }

        [Fact]
        public Task NodeReadDiagnosticsVerboseTestAsync()
        {
            return GetTests().NodeReadDiagnosticsVerboseTestAsync();
        }
    }
}
