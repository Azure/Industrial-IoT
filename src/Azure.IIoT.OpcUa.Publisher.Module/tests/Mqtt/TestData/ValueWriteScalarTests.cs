// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Module.Tests.Mqtt.TestData
{
    using Autofac;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Azure.IIoT.OpcUa.Publisher.Module.Tests.Fixtures;
    using Azure.IIoT.OpcUa.Publisher.Stack;
    using Azure.IIoT.OpcUa.Publisher.Testing.Fixtures;
    using Azure.IIoT.OpcUa.Publisher.Testing.Tests;
    using System.Threading.Tasks;
    using Xunit;
    using Xunit.Abstractions;

    [Collection(WriteCollection.Name)]
    public class ValueWriteScalarTests : TwinIntegrationTestBase, IClassFixture<PublisherModuleMqttv311Fixture>
    {
        public ValueWriteScalarTests(TestDataServer server,
            PublisherModuleMqttv311Fixture module, ITestOutputHelper output) : base(output)
        {
            _server = server;
            _module = module;
        }

        private WriteScalarValueTests<ConnectionModel> GetTests()
        {
            return new WriteScalarValueTests<ConnectionModel>(
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
        private readonly PublisherModuleMqttv311Fixture _module;

        [Fact]
        public Task NodeWriteStaticScalarBooleanValueVariableTestAsync()
        {
            return GetTests().NodeWriteStaticScalarBooleanValueVariableTestAsync(Ct);
        }

        [Fact]
        public Task NodeWriteStaticScalarBooleanValueVariableWithBrowsePathTest1Async()
        {
            return GetTests().NodeWriteStaticScalarBooleanValueVariableWithBrowsePathTest1Async(Ct);
        }

        [Fact]
        public Task NodeWriteStaticScalarBooleanValueVariableWithBrowsePathTest2Async()
        {
            return GetTests().NodeWriteStaticScalarBooleanValueVariableWithBrowsePathTest2Async(Ct);
        }

        [Fact]
        public Task NodeWriteStaticScalarBooleanValueVariableWithBrowsePathTest3Async()
        {
            return GetTests().NodeWriteStaticScalarBooleanValueVariableWithBrowsePathTest3Async(Ct);
        }

        [Fact]
        public Task NodeWriteStaticScalarSByteValueVariableTestAsync()
        {
            return GetTests().NodeWriteStaticScalarSByteValueVariableTestAsync(Ct);
        }

        [Fact]
        public Task NodeWriteStaticScalarByteValueVariableTestAsync()
        {
            return GetTests().NodeWriteStaticScalarByteValueVariableTestAsync(Ct);
        }

        [Fact]
        public Task NodeWriteStaticScalarInt16ValueVariableTestAsync()
        {
            return GetTests().NodeWriteStaticScalarInt16ValueVariableTestAsync(Ct);
        }

        [Fact]
        public Task NodeWriteStaticScalarUInt16ValueVariableTestAsync()
        {
            return GetTests().NodeWriteStaticScalarUInt16ValueVariableTestAsync(Ct);
        }

        [Fact]
        public Task NodeWriteStaticScalarInt32ValueVariableTestAsync()
        {
            return GetTests().NodeWriteStaticScalarInt32ValueVariableTestAsync(Ct);
        }

        [Fact]
        public Task NodeWriteStaticScalarUInt32ValueVariableTestAsync()
        {
            return GetTests().NodeWriteStaticScalarUInt32ValueVariableTestAsync(Ct);
        }

        [Fact]
        public Task NodeWriteStaticScalarInt64ValueVariableTestAsync()
        {
            return GetTests().NodeWriteStaticScalarInt64ValueVariableTestAsync(Ct);
        }

        [Fact]
        public Task NodeWriteStaticScalarUInt64ValueVariableTestAsync()
        {
            return GetTests().NodeWriteStaticScalarUInt64ValueVariableTestAsync(Ct);
        }

        [Fact]
        public Task NodeWriteStaticScalarFloatValueVariableTestAsync()
        {
            return GetTests().NodeWriteStaticScalarFloatValueVariableTestAsync(Ct);
        }

        [Fact]
        public Task NodeWriteStaticScalarDoubleValueVariableTestAsync()
        {
            return GetTests().NodeWriteStaticScalarDoubleValueVariableTestAsync(Ct);
        }

        [Fact]
        public Task NodeWriteStaticScalarStringValueVariableTestAsync()
        {
            return GetTests().NodeWriteStaticScalarStringValueVariableTestAsync(Ct);
        }

        [Fact]
        public Task NodeWriteStaticScalarDateTimeValueVariableTestAsync()
        {
            return GetTests().NodeWriteStaticScalarDateTimeValueVariableTestAsync(Ct);
        }

        [Fact]
        public Task NodeWriteStaticScalarGuidValueVariableTestAsync()
        {
            return GetTests().NodeWriteStaticScalarGuidValueVariableTestAsync(Ct);
        }

        [Fact]
        public Task NodeWriteStaticScalarByteStringValueVariableTestAsync()
        {
            return GetTests().NodeWriteStaticScalarByteStringValueVariableTestAsync(Ct);
        }

        [Fact]
        public Task NodeWriteStaticScalarXmlElementValueVariableTestAsync()
        {
            return GetTests().NodeWriteStaticScalarXmlElementValueVariableTestAsync(Ct);
        }

        [Fact]
        public Task NodeWriteStaticScalarNodeIdValueVariableTestAsync()
        {
            return GetTests().NodeWriteStaticScalarNodeIdValueVariableTestAsync(Ct);
        }

        [Fact]
        public Task NodeWriteStaticScalarExpandedNodeIdValueVariableTestAsync()
        {
            return GetTests().NodeWriteStaticScalarExpandedNodeIdValueVariableTestAsync(Ct);
        }

        [Fact]
        public Task NodeWriteStaticScalarQualifiedNameValueVariableTestAsync()
        {
            return GetTests().NodeWriteStaticScalarQualifiedNameValueVariableTestAsync(Ct);
        }

        [Fact]
        public Task NodeWriteStaticScalarLocalizedTextValueVariableTestAsync()
        {
            return GetTests().NodeWriteStaticScalarLocalizedTextValueVariableTestAsync(Ct);
        }

        [Fact]
        public Task NodeWriteStaticScalarStatusCodeValueVariableTestAsync()
        {
            return GetTests().NodeWriteStaticScalarStatusCodeValueVariableTestAsync(Ct);
        }

        [Fact]
        public Task NodeWriteStaticScalarVariantValueVariableTestAsync()
        {
            return GetTests().NodeWriteStaticScalarVariantValueVariableTestAsync(Ct);
        }

        [Fact]
        public Task NodeWriteStaticScalarEnumerationValueVariableTestAsync()
        {
            return GetTests().NodeWriteStaticScalarEnumerationValueVariableTestAsync(Ct);
        }

        [Fact]
        public Task NodeWriteStaticScalarStructuredValueVariableTestAsync()
        {
            return GetTests().NodeWriteStaticScalarStructuredValueVariableTestAsync(Ct);
        }

        [Fact]
        public Task NodeWriteStaticScalarNumberValueVariableTestAsync()
        {
            return GetTests().NodeWriteStaticScalarNumberValueVariableTestAsync(Ct);
        }

        [Fact]
        public Task NodeWriteStaticScalarIntegerValueVariableTestAsync()
        {
            return GetTests().NodeWriteStaticScalarIntegerValueVariableTestAsync(Ct);
        }

        [Fact]
        public Task NodeWriteStaticScalarUIntegerValueVariableTestAsync()
        {
            return GetTests().NodeWriteStaticScalarUIntegerValueVariableTestAsync(Ct);
        }
    }
}
