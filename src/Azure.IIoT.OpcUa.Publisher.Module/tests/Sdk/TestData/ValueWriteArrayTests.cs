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

    [Collection(WriteCollection.Name)]
    public class ValueWriteArrayTests : IClassFixture<PublisherModuleFixture>
    {
        public ValueWriteArrayTests(TestDataServer server, PublisherModuleFixture module)
        {
            _server = server;
            _module = module;
        }

        private WriteArrayValueTests<ConnectionModel> GetTests()
        {
            return new WriteArrayValueTests<ConnectionModel>(
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
        public Task NodeWriteStaticArrayBooleanValueVariableTestAsync()
        {
            return GetTests().NodeWriteStaticArrayBooleanValueVariableTestAsync();
        }

        [Fact]
        public Task NodeWriteStaticArraySByteValueVariableTestAsync()
        {
            return GetTests().NodeWriteStaticArraySByteValueVariableTestAsync();
        }

        [Fact]
        public Task NodeWriteStaticArrayByteValueVariableTestAsync()
        {
            return GetTests().NodeWriteStaticArrayByteValueVariableTestAsync();
        }

        [Fact]
        public Task NodeWriteStaticArrayInt16ValueVariableTestAsync()
        {
            return GetTests().NodeWriteStaticArrayInt16ValueVariableTestAsync();
        }

        [Fact]
        public Task NodeWriteStaticArrayUInt16ValueVariableTestAsync()
        {
            return GetTests().NodeWriteStaticArrayUInt16ValueVariableTestAsync();
        }

        [Fact]
        public Task NodeWriteStaticArrayInt32ValueVariableTestAsync()
        {
            return GetTests().NodeWriteStaticArrayInt32ValueVariableTestAsync();
        }

        [Fact]
        public Task NodeWriteStaticArrayUInt32ValueVariableTestAsync()
        {
            return GetTests().NodeWriteStaticArrayUInt32ValueVariableTestAsync();
        }

        [Fact]
        public Task NodeWriteStaticArrayInt64ValueVariableTestAsync()
        {
            return GetTests().NodeWriteStaticArrayInt64ValueVariableTestAsync();
        }

        [Fact]
        public Task NodeWriteStaticArrayUInt64ValueVariableTestAsync()
        {
            return GetTests().NodeWriteStaticArrayUInt64ValueVariableTestAsync();
        }

        [Fact]
        public Task NodeWriteStaticArrayFloatValueVariableTestAsync()
        {
            return GetTests().NodeWriteStaticArrayFloatValueVariableTestAsync();
        }

        [Fact]
        public Task NodeWriteStaticArrayDoubleValueVariableTestAsync()
        {
            return GetTests().NodeWriteStaticArrayDoubleValueVariableTestAsync();
        }

        [Fact]
        public Task NodeWriteStaticArrayStringValueVariableTest1Async()
        {
            return GetTests().NodeWriteStaticArrayStringValueVariableTest1Async();
        }

        [Fact]
        public Task NodeWriteStaticArrayStringValueVariableTest2Async()
        {
            return GetTests().NodeWriteStaticArrayStringValueVariableTest2Async();
        }

        [Fact]
        public Task NodeWriteStaticArrayDateTimeValueVariableTestAsync()
        {
            return GetTests().NodeWriteStaticArrayDateTimeValueVariableTestAsync();
        }

        [Fact]
        public Task NodeWriteStaticArrayGuidValueVariableTestAsync()
        {
            return GetTests().NodeWriteStaticArrayGuidValueVariableTestAsync();
        }

        [Fact]
        public Task NodeWriteStaticArrayByteStringValueVariableTestAsync()
        {
            return GetTests().NodeWriteStaticArrayByteStringValueVariableTestAsync();
        }

        [Fact]
        public Task NodeWriteStaticArrayXmlElementValueVariableTestAsync()
        {
            return GetTests().NodeWriteStaticArrayXmlElementValueVariableTestAsync();
        }

        [Fact]
        public Task NodeWriteStaticArrayNodeIdValueVariableTestAsync()
        {
            return GetTests().NodeWriteStaticArrayNodeIdValueVariableTestAsync();
        }

        [Fact]
        public Task NodeWriteStaticArrayExpandedNodeIdValueVariableTestAsync()
        {
            return GetTests().NodeWriteStaticArrayExpandedNodeIdValueVariableTestAsync();
        }

        [Fact]
        public Task NodeWriteStaticArrayQualifiedNameValueVariableTestAsync()
        {
            return GetTests().NodeWriteStaticArrayQualifiedNameValueVariableTestAsync();
        }

        [Fact]
        public Task NodeWriteStaticArrayLocalizedTextValueVariableTestAsync()
        {
            return GetTests().NodeWriteStaticArrayLocalizedTextValueVariableTestAsync();
        }

        [Fact]
        public Task NodeWriteStaticArrayStatusCodeValueVariableTestAsync()
        {
            return GetTests().NodeWriteStaticArrayStatusCodeValueVariableTestAsync();
        }

        [Fact]
        public Task NodeWriteStaticArrayVariantValueVariableTest1Async()
        {
            return GetTests().NodeWriteStaticArrayVariantValueVariableTest1Async();
        }

        [Fact]
        public Task NodeWriteStaticArrayEnumerationValueVariableTestAsync()
        {
            return GetTests().NodeWriteStaticArrayEnumerationValueVariableTestAsync();
        }

        [Fact]
        public Task NodeWriteStaticArrayStructureValueVariableTestAsync()
        {
            return GetTests().NodeWriteStaticArrayStructureValueVariableTestAsync();
        }

        [Fact]
        public Task NodeWriteStaticArrayNumberValueVariableTest1Async()
        {
            return GetTests().NodeWriteStaticArrayNumberValueVariableTest1Async();
        }

        [Fact]
        public Task NodeWriteStaticArrayNumberValueVariableTest2Async()
        {
            return GetTests().NodeWriteStaticArrayNumberValueVariableTest2Async();
        }

        [Fact]
        public Task NodeWriteStaticArrayIntegerValueVariableTest1Async()
        {
            return GetTests().NodeWriteStaticArrayIntegerValueVariableTest1Async();
        }

        [Fact]
        public Task NodeWriteStaticArrayIntegerValueVariableTest2Async()
        {
            return GetTests().NodeWriteStaticArrayIntegerValueVariableTest2Async();
        }

        [Fact]
        public Task NodeWriteStaticArrayUIntegerValueVariableTest1Async()
        {
            return GetTests().NodeWriteStaticArrayUIntegerValueVariableTest1Async();
        }

        [Fact]
        public Task NodeWriteStaticArrayUIntegerValueVariableTest2Async()
        {
            return GetTests().NodeWriteStaticArrayUIntegerValueVariableTest2Async();
        }
    }
}
