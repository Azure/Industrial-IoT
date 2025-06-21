// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Module.Tests.Sdk.TestData
{
    using Autofac;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Azure.IIoT.OpcUa.Publisher.Module.Tests.Fixtures;
    using Azure.IIoT.OpcUa.Publisher.Stack;
    using Azure.IIoT.OpcUa.Publisher.Testing.Fixtures;
    using Azure.IIoT.OpcUa.Publisher.Testing.Tests;
    using System.Threading.Tasks;
    using Xunit;

    [Collection(WriteCollection.Name)]
    public class ValueWriteScalarTests : IClassFixture<PublisherModuleFixture>
    {
        public ValueWriteScalarTests(TestDataServer server, PublisherModuleFixture module)
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
        private readonly PublisherModuleFixture _module;

        [Fact]
        public Task NodeWriteStaticScalarBooleanValueVariableTestAsync()
        {
            return GetTests().NodeWriteStaticScalarBooleanValueVariableTestAsync();
        }

        [Fact]
        public Task NodeWriteStaticScalarBooleanValueVariableWithBrowsePathTest1Async()
        {
            return GetTests().NodeWriteStaticScalarBooleanValueVariableWithBrowsePathTest1Async();
        }

        [Fact]
        public Task NodeWriteStaticScalarBooleanValueVariableWithBrowsePathTest2Async()
        {
            return GetTests().NodeWriteStaticScalarBooleanValueVariableWithBrowsePathTest2Async();
        }

        [Fact]
        public Task NodeWriteStaticScalarBooleanValueVariableWithBrowsePathTest3Async()
        {
            return GetTests().NodeWriteStaticScalarBooleanValueVariableWithBrowsePathTest3Async();
        }

        [Fact]
        public Task NodeWriteStaticScalarSByteValueVariableTestAsync()
        {
            return GetTests().NodeWriteStaticScalarSByteValueVariableTestAsync();
        }

        [Fact]
        public Task NodeWriteStaticScalarByteValueVariableTestAsync()
        {
            return GetTests().NodeWriteStaticScalarByteValueVariableTestAsync();
        }

        [Fact]
        public Task NodeWriteStaticScalarInt16ValueVariableTestAsync()
        {
            return GetTests().NodeWriteStaticScalarInt16ValueVariableTestAsync();
        }

        [Fact]
        public Task NodeWriteStaticScalarUInt16ValueVariableTestAsync()
        {
            return GetTests().NodeWriteStaticScalarUInt16ValueVariableTestAsync();
        }

        [Fact]
        public Task NodeWriteStaticScalarInt32ValueVariableTestAsync()
        {
            return GetTests().NodeWriteStaticScalarInt32ValueVariableTestAsync();
        }

        [Fact]
        public Task NodeWriteStaticScalarUInt32ValueVariableTestAsync()
        {
            return GetTests().NodeWriteStaticScalarUInt32ValueVariableTestAsync();
        }

        [Fact]
        public Task NodeWriteStaticScalarInt64ValueVariableTestAsync()
        {
            return GetTests().NodeWriteStaticScalarInt64ValueVariableTestAsync();
        }

        [Fact]
        public Task NodeWriteStaticScalarUInt64ValueVariableTestAsync()
        {
            return GetTests().NodeWriteStaticScalarUInt64ValueVariableTestAsync();
        }

        [Fact]
        public Task NodeWriteStaticScalarFloatValueVariableTestAsync()
        {
            return GetTests().NodeWriteStaticScalarFloatValueVariableTestAsync();
        }

        [Fact]
        public Task NodeWriteStaticScalarDoubleValueVariableTestAsync()
        {
            return GetTests().NodeWriteStaticScalarDoubleValueVariableTestAsync();
        }

        [Fact]
        public Task NodeWriteStaticScalarStringValueVariableTestAsync()
        {
            return GetTests().NodeWriteStaticScalarStringValueVariableTestAsync();
        }

        [Fact]
        public Task NodeWriteStaticScalarDateTimeValueVariableTestAsync()
        {
            return GetTests().NodeWriteStaticScalarDateTimeValueVariableTestAsync();
        }

        [Fact]
        public Task NodeWriteStaticScalarGuidValueVariableTestAsync()
        {
            return GetTests().NodeWriteStaticScalarGuidValueVariableTestAsync();
        }

        [Fact]
        public Task NodeWriteStaticScalarByteStringValueVariableTestAsync()
        {
            return GetTests().NodeWriteStaticScalarByteStringValueVariableTestAsync();
        }

        [Fact]
        public Task NodeWriteStaticScalarXmlElementValueVariableTestAsync()
        {
            return GetTests().NodeWriteStaticScalarXmlElementValueVariableTestAsync();
        }

        [Fact]
        public Task NodeWriteStaticScalarNodeIdValueVariableTestAsync()
        {
            return GetTests().NodeWriteStaticScalarNodeIdValueVariableTestAsync();
        }

        [Fact]
        public Task NodeWriteStaticScalarExpandedNodeIdValueVariableTestAsync()
        {
            return GetTests().NodeWriteStaticScalarExpandedNodeIdValueVariableTestAsync();
        }

        [Fact]
        public Task NodeWriteStaticScalarQualifiedNameValueVariableTestAsync()
        {
            return GetTests().NodeWriteStaticScalarQualifiedNameValueVariableTestAsync();
        }

        [Fact]
        public Task NodeWriteStaticScalarLocalizedTextValueVariableTestAsync()
        {
            return GetTests().NodeWriteStaticScalarLocalizedTextValueVariableTestAsync();
        }

        [Fact]
        public Task NodeWriteStaticScalarStatusCodeValueVariableTestAsync()
        {
            return GetTests().NodeWriteStaticScalarStatusCodeValueVariableTestAsync();
        }

        [Fact]
        public Task NodeWriteStaticScalarVariantValueVariableTestAsync()
        {
            return GetTests().NodeWriteStaticScalarVariantValueVariableTestAsync();
        }

        [Fact]
        public Task NodeWriteStaticScalarEnumerationValueVariableTestAsync()
        {
            return GetTests().NodeWriteStaticScalarEnumerationValueVariableTestAsync();
        }

        [Fact]
        public Task NodeWriteStaticScalarStructuredValueVariableTestAsync()
        {
            return GetTests().NodeWriteStaticScalarStructuredValueVariableTestAsync();
        }

        [Fact]
        public Task NodeWriteStaticScalarNumberValueVariableTestAsync()
        {
            return GetTests().NodeWriteStaticScalarNumberValueVariableTestAsync();
        }

        [Fact]
        public Task NodeWriteStaticScalarIntegerValueVariableTestAsync()
        {
            return GetTests().NodeWriteStaticScalarIntegerValueVariableTestAsync();
        }

        [Fact]
        public Task NodeWriteStaticScalarUIntegerValueVariableTestAsync()
        {
            return GetTests().NodeWriteStaticScalarUIntegerValueVariableTestAsync();
        }
    }
}
