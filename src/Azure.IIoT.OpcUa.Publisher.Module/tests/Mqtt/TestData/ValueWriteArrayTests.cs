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

    [Collection(WriteCollection.Name)]
    public class ValueWriteArrayTests : TwinIntegrationTestBase, IClassFixture<PublisherModuleMqttv5Fixture>
    {
        public ValueWriteArrayTests(TestDataServer server,
            PublisherModuleMqttv5Fixture module, ITestOutputHelper output) : base(output)
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
        private readonly PublisherModuleMqttv5Fixture _module;

        [Fact]
        public Task NodeWriteStaticArrayBooleanValueVariableTestAsync()
        {
            return GetTests().NodeWriteStaticArrayBooleanValueVariableTestAsync(Ct);
        }

        [Fact]
        public Task NodeWriteStaticArraySByteValueVariableTestAsync()
        {
            return GetTests().NodeWriteStaticArraySByteValueVariableTestAsync(Ct);
        }

        [Fact]
        public Task NodeWriteStaticArrayByteValueVariableTestAsync()
        {
            return GetTests().NodeWriteStaticArrayByteValueVariableTestAsync(Ct);
        }

        [Fact]
        public Task NodeWriteStaticArrayInt16ValueVariableTestAsync()
        {
            return GetTests().NodeWriteStaticArrayInt16ValueVariableTestAsync(Ct);
        }

        [Fact]
        public Task NodeWriteStaticArrayUInt16ValueVariableTestAsync()
        {
            return GetTests().NodeWriteStaticArrayUInt16ValueVariableTestAsync(Ct);
        }

        [Fact]
        public Task NodeWriteStaticArrayInt32ValueVariableTestAsync()
        {
            return GetTests().NodeWriteStaticArrayInt32ValueVariableTestAsync(Ct);
        }

        [Fact]
        public Task NodeWriteStaticArrayUInt32ValueVariableTestAsync()
        {
            return GetTests().NodeWriteStaticArrayUInt32ValueVariableTestAsync(Ct);
        }

        [Fact]
        public Task NodeWriteStaticArrayInt64ValueVariableTestAsync()
        {
            return GetTests().NodeWriteStaticArrayInt64ValueVariableTestAsync(Ct);
        }

        [Fact]
        public Task NodeWriteStaticArrayUInt64ValueVariableTestAsync()
        {
            return GetTests().NodeWriteStaticArrayUInt64ValueVariableTestAsync(Ct);
        }

        [Fact]
        public Task NodeWriteStaticArrayFloatValueVariableTestAsync()
        {
            return GetTests().NodeWriteStaticArrayFloatValueVariableTestAsync(Ct);
        }

        [Fact]
        public Task NodeWriteStaticArrayDoubleValueVariableTestAsync()
        {
            return GetTests().NodeWriteStaticArrayDoubleValueVariableTestAsync(Ct);
        }

        [Fact]
        public Task NodeWriteStaticArrayStringValueVariableTest1Async()
        {
            return GetTests().NodeWriteStaticArrayStringValueVariableTest1Async(Ct);
        }

        [Fact]
        public Task NodeWriteStaticArrayStringValueVariableTest2Async()
        {
            return GetTests().NodeWriteStaticArrayStringValueVariableTest2Async(Ct);
        }

        [Fact]
        public Task NodeWriteStaticArrayDateTimeValueVariableTestAsync()
        {
            return GetTests().NodeWriteStaticArrayDateTimeValueVariableTestAsync(Ct);
        }

        [Fact]
        public Task NodeWriteStaticArrayGuidValueVariableTestAsync()
        {
            return GetTests().NodeWriteStaticArrayGuidValueVariableTestAsync(Ct);
        }

        [Fact]
        public Task NodeWriteStaticArrayByteStringValueVariableTestAsync()
        {
            return GetTests().NodeWriteStaticArrayByteStringValueVariableTestAsync(Ct);
        }

        [Fact]
        public Task NodeWriteStaticArrayXmlElementValueVariableTestAsync()
        {
            return GetTests().NodeWriteStaticArrayXmlElementValueVariableTestAsync(Ct);
        }

        [Fact]
        public Task NodeWriteStaticArrayNodeIdValueVariableTestAsync()
        {
            return GetTests().NodeWriteStaticArrayNodeIdValueVariableTestAsync(Ct);
        }

        [Fact]
        public Task NodeWriteStaticArrayExpandedNodeIdValueVariableTestAsync()
        {
            return GetTests().NodeWriteStaticArrayExpandedNodeIdValueVariableTestAsync(Ct);
        }

        [Fact]
        public Task NodeWriteStaticArrayQualifiedNameValueVariableTestAsync()
        {
            return GetTests().NodeWriteStaticArrayQualifiedNameValueVariableTestAsync(Ct);
        }

        [Fact]
        public Task NodeWriteStaticArrayLocalizedTextValueVariableTestAsync()
        {
            return GetTests().NodeWriteStaticArrayLocalizedTextValueVariableTestAsync(Ct);
        }

        [Fact]
        public Task NodeWriteStaticArrayStatusCodeValueVariableTestAsync()
        {
            return GetTests().NodeWriteStaticArrayStatusCodeValueVariableTestAsync(Ct);
        }

        [Fact]
        public Task NodeWriteStaticArrayVariantValueVariableTest1Async()
        {
            return GetTests().NodeWriteStaticArrayVariantValueVariableTest1Async(Ct);
        }

        [Fact]
        public Task NodeWriteStaticArrayEnumerationValueVariableTestAsync()
        {
            return GetTests().NodeWriteStaticArrayEnumerationValueVariableTestAsync(Ct);
        }

        [Fact]
        public Task NodeWriteStaticArrayStructureValueVariableTestAsync()
        {
            return GetTests().NodeWriteStaticArrayStructureValueVariableTestAsync(Ct);
        }

        [Fact]
        public Task NodeWriteStaticArrayNumberValueVariableTest1Async()
        {
            return GetTests().NodeWriteStaticArrayNumberValueVariableTest1Async(Ct);
        }

        [Fact]
        public Task NodeWriteStaticArrayNumberValueVariableTest2Async()
        {
            return GetTests().NodeWriteStaticArrayNumberValueVariableTest2Async(Ct);
        }

        [Fact]
        public Task NodeWriteStaticArrayIntegerValueVariableTest1Async()
        {
            return GetTests().NodeWriteStaticArrayIntegerValueVariableTest1Async(Ct);
        }

        [Fact]
        public Task NodeWriteStaticArrayIntegerValueVariableTest2Async()
        {
            return GetTests().NodeWriteStaticArrayIntegerValueVariableTest2Async(Ct);
        }

        [Fact]
        public Task NodeWriteStaticArrayUIntegerValueVariableTest1Async()
        {
            return GetTests().NodeWriteStaticArrayUIntegerValueVariableTest1Async(Ct);
        }

        [Fact]
        public Task NodeWriteStaticArrayUIntegerValueVariableTest2Async()
        {
            return GetTests().NodeWriteStaticArrayUIntegerValueVariableTest2Async(Ct);
        }
    }
}
