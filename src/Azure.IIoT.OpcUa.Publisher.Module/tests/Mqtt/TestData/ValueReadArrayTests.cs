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

    [Collection(ReadCollection.Name)]
    public class ValueReadArrayTests : TwinIntegrationTestBase, IClassFixture<PublisherModuleMqttv311Fixture>
    {
        public ValueReadArrayTests(TestDataServer server,
            PublisherModuleMqttv311Fixture module, ITestOutputHelper output) : base(output)
        {
            _server = server;
            _module = module;
        }

        private ReadArrayValueTests<ConnectionModel> GetTests()
        {
            return new ReadArrayValueTests<ConnectionModel>(
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
        public Task NodeReadAllStaticArrayVariableNodeClassTest1Async()
        {
            return GetTests().NodeReadAllStaticArrayVariableNodeClassTest1Async(Ct);
        }

        [Fact]
        public Task NodeReadAllStaticArrayVariableAccessLevelTest1Async()
        {
            return GetTests().NodeReadAllStaticArrayVariableAccessLevelTest1Async(Ct);
        }

        [Fact]
        public Task NodeReadAllStaticArrayVariableWriteMaskTest1Async()
        {
            return GetTests().NodeReadAllStaticArrayVariableWriteMaskTest1Async(Ct);
        }

        [Fact]
        public Task NodeReadAllStaticArrayVariableWriteMaskTest2Async()
        {
            return GetTests().NodeReadAllStaticArrayVariableWriteMaskTest2Async(Ct);
        }

        [Fact]
        public Task NodeReadStaticArrayBooleanValueVariableTestAsync()
        {
            return GetTests().NodeReadStaticArrayBooleanValueVariableTestAsync(Ct);
        }

        [Fact]
        public Task NodeReadStaticArraySByteValueVariableTestAsync()
        {
            return GetTests().NodeReadStaticArraySByteValueVariableTestAsync(Ct);
        }

        [Fact]
        public Task NodeReadStaticArrayByteValueVariableTestAsync()
        {
            return GetTests().NodeReadStaticArrayByteValueVariableTestAsync(Ct);
        }

        [Fact]
        public Task NodeReadStaticArrayInt16ValueVariableTestAsync()
        {
            return GetTests().NodeReadStaticArrayInt16ValueVariableTestAsync(Ct);
        }

        [Fact]
        public Task NodeReadStaticArrayUInt16ValueVariableTestAsync()
        {
            return GetTests().NodeReadStaticArrayUInt16ValueVariableTestAsync(Ct);
        }

        [Fact]
        public Task NodeReadStaticArrayInt32ValueVariableTestAsync()
        {
            return GetTests().NodeReadStaticArrayInt32ValueVariableTestAsync(Ct);
        }

        [Fact]
        public Task NodeReadStaticArrayUInt32ValueVariableTestAsync()
        {
            return GetTests().NodeReadStaticArrayUInt32ValueVariableTestAsync(Ct);
        }

        [Fact]
        public Task NodeReadStaticArrayInt64ValueVariableTestAsync()
        {
            return GetTests().NodeReadStaticArrayInt64ValueVariableTestAsync(Ct);
        }

        [Fact]
        public Task NodeReadStaticArrayUInt64ValueVariableTestAsync()
        {
            return GetTests().NodeReadStaticArrayUInt64ValueVariableTestAsync(Ct);
        }

        [Fact]
        public Task NodeReadStaticArrayFloatValueVariableTestAsync()
        {
            return GetTests().NodeReadStaticArrayFloatValueVariableTestAsync(Ct);
        }

        [Fact]
        public Task NodeReadStaticArrayDoubleValueVariableTestAsync()
        {
            return GetTests().NodeReadStaticArrayDoubleValueVariableTestAsync(Ct);
        }

        [Fact]
        public Task NodeReadStaticArrayStringValueVariableTestAsync()
        {
            return GetTests().NodeReadStaticArrayStringValueVariableTestAsync(Ct);
        }

        [Fact]
        public Task NodeReadStaticArrayDateTimeValueVariableTestAsync()
        {
            return GetTests().NodeReadStaticArrayDateTimeValueVariableTestAsync(Ct);
        }

        [Fact]
        public Task NodeReadStaticArrayGuidValueVariableTestAsync()
        {
            return GetTests().NodeReadStaticArrayGuidValueVariableTestAsync(Ct);
        }

        [Fact]
        public Task NodeReadStaticArrayByteStringValueVariableTestAsync()
        {
            return GetTests().NodeReadStaticArrayByteStringValueVariableTestAsync(Ct);
        }

        [Fact]
        public Task NodeReadStaticArrayXmlElementValueVariableTestAsync()
        {
            return GetTests().NodeReadStaticArrayXmlElementValueVariableTestAsync(Ct);
        }

        [Fact]
        public Task NodeReadStaticArrayNodeIdValueVariableTestAsync()
        {
            return GetTests().NodeReadStaticArrayNodeIdValueVariableTestAsync(Ct);
        }

        [Fact]
        public Task NodeReadStaticArrayExpandedNodeIdValueVariableTestAsync()
        {
            return GetTests().NodeReadStaticArrayExpandedNodeIdValueVariableTestAsync(Ct);
        }

        [Fact]
        public Task NodeReadStaticArrayQualifiedNameValueVariableTestAsync()
        {
            return GetTests().NodeReadStaticArrayQualifiedNameValueVariableTestAsync(Ct);
        }

        [Fact]
        public Task NodeReadStaticArrayLocalizedTextValueVariableTestAsync()
        {
            return GetTests().NodeReadStaticArrayLocalizedTextValueVariableTestAsync(Ct);
        }

        [Fact]
        public Task NodeReadStaticArrayStatusCodeValueVariableTestAsync()
        {
            return GetTests().NodeReadStaticArrayStatusCodeValueVariableTestAsync(Ct);
        }

        [Fact]
        public Task NodeReadStaticArrayVariantValueVariableTestAsync()
        {
            return GetTests().NodeReadStaticArrayVariantValueVariableTestAsync(Ct);
        }

        [Fact]
        public Task NodeReadStaticArrayEnumerationValueVariableTestAsync()
        {
            return GetTests().NodeReadStaticArrayEnumerationValueVariableTestAsync(Ct);
        }

        [Fact]
        public Task NodeReadStaticArrayStructureValueVariableTestAsync()
        {
            return GetTests().NodeReadStaticArrayStructureValueVariableTestAsync(Ct);
        }

        [Fact]
        public Task NodeReadStaticArrayNumberValueVariableTestAsync()
        {
            return GetTests().NodeReadStaticArrayNumberValueVariableTestAsync(Ct);
        }

        [Fact]
        public Task NodeReadStaticArrayIntegerValueVariableTestAsync()
        {
            return GetTests().NodeReadStaticArrayIntegerValueVariableTestAsync(Ct);
        }

        [Fact]
        public Task NodeReadStaticArrayUIntegerValueVariableTestAsync()
        {
            return GetTests().NodeReadStaticArrayUIntegerValueVariableTestAsync(Ct);
        }
    }
}
