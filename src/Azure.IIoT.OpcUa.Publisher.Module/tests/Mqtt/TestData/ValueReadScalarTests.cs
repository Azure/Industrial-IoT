// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Module.Tests.Mqtt.TestData
{
    using Azure.IIoT.OpcUa.Publisher.Module.Tests.Fixtures;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Azure.IIoT.OpcUa.Publisher.Stack;
    using Azure.IIoT.OpcUa.Publisher.Testing.Fixtures;
    using Azure.IIoT.OpcUa.Publisher.Testing.Tests;
    using Autofac;
    using System.Threading.Tasks;
    using Xunit;
    using Xunit.Abstractions;

    [Collection(ReadCollection.Name)]
    public class ValueReadScalarTests : TwinIntegrationTestBase, IClassFixture<PublisherModuleMqttv5Fixture>
    {
        public ValueReadScalarTests(TestDataServer server,
            PublisherModuleMqttv5Fixture module, ITestOutputHelper output) : base(output)
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
        private readonly PublisherModuleMqttv5Fixture _module;

        [Fact]
        public Task NodeReadAllStaticScalarVariableNodeClassTest1Async()
        {
            return GetTests().NodeReadAllStaticScalarVariableNodeClassTest1Async(Ct);
        }

        [Fact]
        public Task NodeReadAllStaticScalarVariableAccessLevelTest1Async()
        {
            return GetTests().NodeReadAllStaticScalarVariableAccessLevelTest1Async(Ct);
        }

        [Fact]
        public Task NodeReadAllStaticScalarVariableWriteMaskTest1Async()
        {
            return GetTests().NodeReadAllStaticScalarVariableWriteMaskTest1Async(Ct);
        }

        [Fact]
        public Task NodeReadAllStaticScalarVariableWriteMaskTest2Async()
        {
            return GetTests().NodeReadAllStaticScalarVariableWriteMaskTest2Async(Ct);
        }

        [Fact]
        public Task NodeReadStaticScalarBooleanValueVariableTestAsync()
        {
            return GetTests().NodeReadStaticScalarBooleanValueVariableTestAsync(Ct);
        }

        [Fact]
        public Task NodeReadStaticScalarBooleanValueVariableWithBrowsePathTest1Async()
        {
            return GetTests().NodeReadStaticScalarBooleanValueVariableWithBrowsePathTest1Async(Ct);
        }

        [Fact]
        public Task NodeReadStaticScalarBooleanValueVariableWithBrowsePathTest2Async()
        {
            return GetTests().NodeReadStaticScalarBooleanValueVariableWithBrowsePathTest2Async(Ct);
        }

        [Fact]
        public Task NodeReadStaticScalarBooleanValueVariableWithBrowsePathTest3Async()
        {
            return GetTests().NodeReadStaticScalarBooleanValueVariableWithBrowsePathTest3Async(Ct);
        }

        [Fact]
        public Task NodeReadStaticScalarSByteValueVariableTestAsync()
        {
            return GetTests().NodeReadStaticScalarSByteValueVariableTestAsync(Ct);
        }

        [Fact]
        public Task NodeReadStaticScalarByteValueVariableTestAsync()
        {
            return GetTests().NodeReadStaticScalarByteValueVariableTestAsync(Ct);
        }

        [Fact]
        public Task NodeReadStaticScalarInt16ValueVariableTestAsync()
        {
            return GetTests().NodeReadStaticScalarInt16ValueVariableTestAsync(Ct);
        }

        [Fact]
        public Task NodeReadStaticScalarUInt16ValueVariableTestAsync()
        {
            return GetTests().NodeReadStaticScalarUInt16ValueVariableTestAsync(Ct);
        }

        [Fact]
        public Task NodeReadStaticScalarInt32ValueVariableTestAsync()
        {
            return GetTests().NodeReadStaticScalarInt32ValueVariableTestAsync(Ct);
        }

        [Fact]
        public Task NodeReadStaticScalarUInt32ValueVariableTestAsync()
        {
            return GetTests().NodeReadStaticScalarUInt32ValueVariableTestAsync(Ct);
        }

        [Fact]
        public Task NodeReadStaticScalarInt64ValueVariableTestAsync()
        {
            return GetTests().NodeReadStaticScalarInt64ValueVariableTestAsync(Ct);
        }

        [Fact]
        public Task NodeReadStaticScalarUInt64ValueVariableTestAsync()
        {
            return GetTests().NodeReadStaticScalarUInt64ValueVariableTestAsync(Ct);
        }

        [Fact]
        public Task NodeReadStaticScalarFloatValueVariableTestAsync()
        {
            return GetTests().NodeReadStaticScalarFloatValueVariableTestAsync(Ct);
        }

        [Fact]
        public Task NodeReadStaticScalarDoubleValueVariableTestAsync()
        {
            return GetTests().NodeReadStaticScalarDoubleValueVariableTestAsync(Ct);
        }

        [Fact]
        public Task NodeReadStaticScalarStringValueVariableTestAsync()
        {
            return GetTests().NodeReadStaticScalarStringValueVariableTestAsync(Ct);
        }

        [Fact]
        public Task NodeReadStaticScalarDateTimeValueVariableTestAsync()
        {
            return GetTests().NodeReadStaticScalarDateTimeValueVariableTestAsync(Ct);
        }

        [Fact]
        public Task NodeReadStaticScalarGuidValueVariableTestAsync()
        {
            return GetTests().NodeReadStaticScalarGuidValueVariableTestAsync(Ct);
        }

        [Fact]
        public Task NodeReadStaticScalarByteStringValueVariableTestAsync()
        {
            return GetTests().NodeReadStaticScalarByteStringValueVariableTestAsync(Ct);
        }

        [Fact]
        public Task NodeReadStaticScalarXmlElementValueVariableTestAsync()
        {
            return GetTests().NodeReadStaticScalarXmlElementValueVariableTestAsync(Ct);
        }

        [Fact]
        public Task NodeReadStaticScalarNodeIdValueVariableTestAsync()
        {
            return GetTests().NodeReadStaticScalarNodeIdValueVariableTestAsync(Ct);
        }

        [Fact]
        public Task NodeReadStaticScalarExpandedNodeIdValueVariableTestAsync()
        {
            return GetTests().NodeReadStaticScalarExpandedNodeIdValueVariableTestAsync(Ct);
        }

        [Fact]
        public Task NodeReadStaticScalarQualifiedNameValueVariableTestAsync()
        {
            return GetTests().NodeReadStaticScalarQualifiedNameValueVariableTestAsync(Ct);
        }

        [Fact]
        public Task NodeReadStaticScalarLocalizedTextValueVariableTestAsync()
        {
            return GetTests().NodeReadStaticScalarLocalizedTextValueVariableTestAsync(Ct);
        }

        [Fact]
        public Task NodeReadStaticScalarStatusCodeValueVariableTestAsync()
        {
            return GetTests().NodeReadStaticScalarStatusCodeValueVariableTestAsync(Ct);
        }

        [Fact]
        public Task NodeReadStaticScalarVariantValueVariableTestAsync()
        {
            return GetTests().NodeReadStaticScalarVariantValueVariableTestAsync(Ct);
        }

        [Fact]
        public Task NodeReadStaticScalarEnumerationValueVariableTestAsync()
        {
            return GetTests().NodeReadStaticScalarEnumerationValueVariableTestAsync(Ct);
        }

        [Fact]
        public Task NodeReadStaticScalarStructuredValueVariableTestAsync()
        {
            return GetTests().NodeReadStaticScalarStructuredValueVariableTestAsync(Ct);
        }

        [Fact]
        public Task NodeReadStaticScalarNumberValueVariableTestAsync()
        {
            return GetTests().NodeReadStaticScalarNumberValueVariableTestAsync(Ct);
        }

        [Fact]
        public Task NodeReadStaticScalarIntegerValueVariableTestAsync()
        {
            return GetTests().NodeReadStaticScalarIntegerValueVariableTestAsync(Ct);
        }

        [Fact]
        public Task NodeReadStaticScalarUIntegerValueVariableTestAsync()
        {
            return GetTests().NodeReadStaticScalarUIntegerValueVariableTestAsync(Ct);
        }

        [Fact]
        public Task NodeReadDataAccessMeasurementFloatValueTestAsync()
        {
            return GetTests().NodeReadDataAccessMeasurementFloatValueTestAsync(Ct);
        }

        [Fact]
        public Task NodeReadDiagnosticsNoneTestAsync()
        {
            return GetTests().NodeReadDiagnosticsNoneTestAsync(Ct);
        }

        [Fact]
        public Task NodeReadDiagnosticsStatusTestAsync()
        {
            return GetTests().NodeReadDiagnosticsStatusTestAsync(Ct);
        }

        [Fact]
        public Task NodeReadDiagnosticsDebugTestAsync()
        {
            return GetTests().NodeReadDiagnosticsDebugTestAsync(Ct);
        }

        [Fact]
        public Task NodeReadDiagnosticsVerboseTestAsync()
        {
            return GetTests().NodeReadDiagnosticsVerboseTestAsync(Ct);
        }
    }
}
