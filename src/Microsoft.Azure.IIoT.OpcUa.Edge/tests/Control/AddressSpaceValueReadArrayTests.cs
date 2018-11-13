// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Edge.Control {
    using Microsoft.Azure.IIoT.OpcUa.Edge.Tests;
    using Microsoft.Azure.IIoT.OpcUa.Registry.Models;
    using Microsoft.Azure.IIoT.OpcUa.Twin.Models;
    using Microsoft.Azure.IIoT.OpcUa.Twin;
    using Microsoft.Azure.IIoT.OpcUa.Protocol.Services;
    using Microsoft.Azure.IIoT.OpcUa.Protocol;
    using Newtonsoft.Json.Linq;
    using System;
    using System.Net;
    using System.Threading.Tasks;
    using Xunit;

    [Collection(ReadCollection.Name)]
    public class AddressSpaceValueReadArrayTests {

        [Fact]
        public async Task NodeReadStaticArrayBooleanValueVariableTest() {

            var browser = GetServices();
            var node = "http://test.org/UA/Data/#i=10300";
            var expected = await _server.Client.ReadValueAsync(GetEndpoint(), node);

            // Act
            var result = await browser.NodeValueReadAsync(GetEndpoint(),
                new ValueReadRequestModel {
                    NodeId = node
                });

            // Assert
            Assert.NotNull(result.Value);
            Assert.NotNull(result.SourceTimestamp);
            Assert.NotNull(result.ServerTimestamp);
            Assert.True(JToken.DeepEquals(expected, result.Value),
                $"Expected: {expected} != Actual: {result.Value}");
            Assert.Equal(JTokenType.Array, result.Value.Type);
            if (((JArray)result.Value).Count == 0) {
                return;
            }

            Assert.Equal(JTokenType.Boolean, ((JArray)result.Value)[0].Type);
            Assert.Equal("Boolean", result.DataType);
        }

        [Fact]
        public async Task NodeReadStaticArraySByteValueVariableTest() {

            var browser = GetServices();
            var node = "http://test.org/UA/Data/#i=10301";
            var expected = await _server.Client.ReadValueAsync(GetEndpoint(), node);

            // Act
            var result = await browser.NodeValueReadAsync(GetEndpoint(),
                new ValueReadRequestModel {
                    NodeId = node
                });

            // Assert
            Assert.NotNull(result.Value);
            Assert.NotNull(result.SourceTimestamp);
            Assert.NotNull(result.ServerTimestamp);
            Assert.True(JToken.DeepEquals(expected, result.Value),
                $"Expected: {expected} != Actual: {result.Value}");
            Assert.Equal(JTokenType.Array, result.Value.Type);
            if (((JArray)result.Value).Count == 0) {
                return;
            }

            Assert.Equal(JTokenType.Integer, ((JArray)result.Value)[0].Type);
            Assert.Equal("SByte", result.DataType);
        }

        [Fact]
        public async Task NodeReadStaticArrayByteValueVariableTest() {

            var browser = GetServices();
            var node = "http://test.org/UA/Data/#i=10302";
            var expected = await _server.Client.ReadValueAsync(GetEndpoint(), node);

            // Act
            var result = await browser.NodeValueReadAsync(GetEndpoint(),
                new ValueReadRequestModel {
                    NodeId = node
                });

            // Assert
            Assert.NotNull(result.Value);
            Assert.NotNull(result.SourceTimestamp);
            Assert.NotNull(result.ServerTimestamp);
            Assert.True(JToken.DeepEquals(expected, result.Value),
                $"Expected: {expected} != Actual: {result.Value}");
            if (result.Value.Type == JTokenType.Null) {
                return;
            }

            Assert.Equal(JTokenType.String, result.Value.Type);
            // TODO: Returns a bytestring, not byte array.  Investigate.
            // Assert.Equal(JTokenType.Bytes, result.Value.Type);
            // Assert.Equal(JTokenType.Array, result.Value.Type);
            // if (((JArray)result.Value).Count == 0) return;
            // Assert.Equal(JTokenType.Integer, ((JArray)result.Value)[0].Type);
            Assert.Equal("ByteString", result.DataType);
            // TODO: Assert.Equal("Byte", result.DataType);
        }


        [Fact]
        public async Task NodeReadStaticArrayInt16ValueVariableTest() {

            var browser = GetServices();
            var node = "http://test.org/UA/Data/#i=10303";
            var expected = await _server.Client.ReadValueAsync(GetEndpoint(), node);

            // Act
            var result = await browser.NodeValueReadAsync(GetEndpoint(),
                new ValueReadRequestModel {
                    NodeId = node
                });

            // Assert
            Assert.NotNull(result.Value);
            Assert.NotNull(result.SourceTimestamp);
            Assert.NotNull(result.ServerTimestamp);
            Assert.True(JToken.DeepEquals(expected, result.Value),
                $"Expected: {expected} != Actual: {result.Value}");
            Assert.Equal(JTokenType.Array, result.Value.Type);
            if (((JArray)result.Value).Count == 0) {
                return;
            }

            Assert.Equal(JTokenType.Integer, ((JArray)result.Value)[0].Type);
            Assert.Equal("Int16", result.DataType);
        }

        [Fact]
        public async Task NodeReadStaticArrayUInt16ValueVariableTest() {

            var browser = GetServices();
            var node = "http://test.org/UA/Data/#i=10304";
            var expected = await _server.Client.ReadValueAsync(GetEndpoint(), node);

            // Act
            var result = await browser.NodeValueReadAsync(GetEndpoint(),
                new ValueReadRequestModel {
                    NodeId = node
                });

            // Assert
            Assert.NotNull(result.Value);
            Assert.NotNull(result.SourceTimestamp);
            Assert.NotNull(result.ServerTimestamp);
            Assert.True(JToken.DeepEquals(expected, result.Value),
                $"Expected: {expected} != Actual: {result.Value}");
            Assert.Equal(JTokenType.Array, result.Value.Type);
            if (((JArray)result.Value).Count == 0) {
                return;
            }

            Assert.Equal(JTokenType.Integer, ((JArray)result.Value)[0].Type);
            Assert.Equal("UInt16", result.DataType);
        }

        [Fact]
        public async Task NodeReadStaticArrayInt32ValueVariableTest() {

            var browser = GetServices();
            var node = "http://test.org/UA/Data/#i=10305";
            var expected = await _server.Client.ReadValueAsync(GetEndpoint(), node);

            // Act
            var result = await browser.NodeValueReadAsync(GetEndpoint(),
                new ValueReadRequestModel {
                    NodeId = node
                });

            // Assert
            Assert.NotNull(result.Value);
            Assert.NotNull(result.SourceTimestamp);
            Assert.NotNull(result.ServerTimestamp);
            Assert.True(JToken.DeepEquals(expected, result.Value),
                $"Expected: {expected} != Actual: {result.Value}");
            Assert.Equal(JTokenType.Array, result.Value.Type);
            if (((JArray)result.Value).Count == 0) {
                return;
            }

            Assert.Equal(JTokenType.Integer, ((JArray)result.Value)[0].Type);
            Assert.Equal("Int32", result.DataType);
        }

        [Fact]
        public async Task NodeReadStaticArrayUInt32ValueVariableTest() {

            var browser = GetServices();
            var node = "http://test.org/UA/Data/#i=10306";
            var expected = await _server.Client.ReadValueAsync(GetEndpoint(), node);

            // Act
            var result = await browser.NodeValueReadAsync(GetEndpoint(),
                new ValueReadRequestModel {
                    NodeId = node
                });

            // Assert
            Assert.NotNull(result.Value);
            Assert.NotNull(result.SourceTimestamp);
            Assert.NotNull(result.ServerTimestamp);
            Assert.True(JToken.DeepEquals(expected, result.Value),
                $"Expected: {expected} != Actual: {result.Value}");
            Assert.Equal(JTokenType.Array, result.Value.Type);
            if (((JArray)result.Value).Count == 0) {
                return;
            }

            Assert.Equal(JTokenType.Integer, ((JArray)result.Value)[0].Type);
            Assert.Equal("UInt32", result.DataType);
        }

        [Fact]
        public async Task NodeReadStaticArrayInt64ValueVariableTest() {

            var browser = GetServices();
            var node = "http://test.org/UA/Data/#i=10307";
            var expected = await _server.Client.ReadValueAsync(GetEndpoint(), node);

            // Act
            var result = await browser.NodeValueReadAsync(GetEndpoint(),
                new ValueReadRequestModel {
                    NodeId = node
                });

            // Assert
            Assert.NotNull(result.Value);
            Assert.NotNull(result.SourceTimestamp);
            Assert.NotNull(result.ServerTimestamp);
            Assert.True(JToken.DeepEquals(expected, result.Value),
                $"Expected: {expected} != Actual: {result.Value}");
            Assert.Equal(JTokenType.Array, result.Value.Type);
            if (((JArray)result.Value).Count == 0) {
                return;
            }

            Assert.Equal(JTokenType.Integer, ((JArray)result.Value)[0].Type);
            Assert.Equal("Int64", result.DataType);
        }

        [Fact]
        public async Task NodeReadStaticArrayUInt64ValueVariableTest() {

            var browser = GetServices();
            var node = "http://test.org/UA/Data/#i=10308";
            var expected = await _server.Client.ReadValueAsync(GetEndpoint(), node);

            // Act
            var result = await browser.NodeValueReadAsync(GetEndpoint(),
                new ValueReadRequestModel {
                    NodeId = node
                });

            // Assert
            Assert.NotNull(result.Value);
            Assert.NotNull(result.SourceTimestamp);
            Assert.NotNull(result.ServerTimestamp);
            Assert.True(JToken.DeepEquals(expected, result.Value),
                $"Expected: {expected} != Actual: {result.Value}");
            Assert.Equal(JTokenType.Array, result.Value.Type);
            if (((JArray)result.Value).Count == 0) {
                return;
            }

            Assert.Equal(JTokenType.Integer, ((JArray)result.Value)[0].Type);
            Assert.Equal("UInt64", result.DataType);
        }

        [Fact]
        public async Task NodeReadStaticArrayFloatValueVariableTest() {

            var browser = GetServices();
            var node = "http://test.org/UA/Data/#i=10309";
            var expected = await _server.Client.ReadValueAsync(GetEndpoint(), node);

            // Act
            var result = await browser.NodeValueReadAsync(GetEndpoint(),
                new ValueReadRequestModel {
                    NodeId = node
                });

            // Assert
            Assert.NotNull(result.Value);
            Assert.NotNull(result.SourceTimestamp);
            Assert.NotNull(result.ServerTimestamp);
            Assert.True(JToken.DeepEquals(expected, result.Value),
                $"Expected: {expected} != Actual: {result.Value}");
            Assert.Equal(JTokenType.Array, result.Value.Type);
            if (((JArray)result.Value).Count == 0) {
                return;
            }

            Assert.Equal(JTokenType.Float, ((JArray)result.Value)[0].Type);
            Assert.Equal("Float", result.DataType);
        }

        [Fact]
        public async Task NodeReadStaticArrayDoubleValueVariableTest() {

            var browser = GetServices();
            var node = "http://test.org/UA/Data/#i=10310";
            var expected = await _server.Client.ReadValueAsync(GetEndpoint(), node);

            // Act
            var result = await browser.NodeValueReadAsync(GetEndpoint(),
                new ValueReadRequestModel {
                    NodeId = node
                });

            // Assert
            Assert.NotNull(result.Value);
            Assert.NotNull(result.SourceTimestamp);
            Assert.NotNull(result.ServerTimestamp);
            Assert.True(JToken.DeepEquals(expected, result.Value),
                $"Expected: {expected} != Actual: {result.Value}");
            Assert.Equal(JTokenType.Array, result.Value.Type);
            if (((JArray)result.Value).Count == 0) {
                return;
            }

            Assert.Equal(JTokenType.Float, ((JArray)result.Value)[0].Type);
            Assert.Equal("Double", result.DataType);
        }

        [Fact]
        public async Task NodeReadStaticArrayStringValueVariableTest() {

            var browser = GetServices();
            var node = "http://test.org/UA/Data/#i=10311";
            var expected = await _server.Client.ReadValueAsync(GetEndpoint(), node);

            // Act
            var result = await browser.NodeValueReadAsync(GetEndpoint(),
                new ValueReadRequestModel {
                    NodeId = node
                });

            // Assert
            Assert.NotNull(result.Value);
            Assert.NotNull(result.SourceTimestamp);
            Assert.NotNull(result.ServerTimestamp);
            Assert.True(JToken.DeepEquals(expected, result.Value),
                $"Expected: {expected} != Actual: {result.Value}");
            Assert.Equal(JTokenType.Array, result.Value.Type);
            if (((JArray)result.Value).Count == 0) {
                return;
            }

            Assert.Equal(JTokenType.String, ((JArray)result.Value)[0].Type);
            Assert.Equal("String", result.DataType);
        }

        [Fact]
        public async Task NodeReadStaticArrayDateTimeValueVariableTest() {

            var browser = GetServices();
            var node = "http://test.org/UA/Data/#i=10312";
            var expected = await _server.Client.ReadValueAsync(GetEndpoint(), node);

            // Act
            var result = await browser.NodeValueReadAsync(GetEndpoint(),
                new ValueReadRequestModel {
                    NodeId = node
                });

            // Assert
            Assert.NotNull(result.Value);
            Assert.NotNull(result.SourceTimestamp);
            Assert.NotNull(result.ServerTimestamp);
            Assert.True(JToken.DeepEquals(expected, result.Value),
                $"Expected: {expected} != Actual: {result.Value}");
            Assert.Equal(JTokenType.Array, result.Value.Type);
            if (((JArray)result.Value).Count == 0) {
                return;
            }

            Assert.Equal(JTokenType.Date, ((JArray)result.Value)[0].Type);
            Assert.Equal("DateTime", result.DataType);
        }

        [Fact]
        public async Task NodeReadStaticArrayGuidValueVariableTest() {

            var browser = GetServices();
            var node = "http://test.org/UA/Data/#i=10313";
            var expected = await _server.Client.ReadValueAsync(GetEndpoint(), node);

            // Act
            var result = await browser.NodeValueReadAsync(GetEndpoint(),
                new ValueReadRequestModel {
                    NodeId = node
                });

            // Assert
            Assert.NotNull(result.Value);
            Assert.NotNull(result.SourceTimestamp);
            Assert.NotNull(result.ServerTimestamp);
            Assert.True(JToken.DeepEquals(expected, result.Value),
                $"Expected: {expected} != Actual: {result.Value}");
            Assert.Equal(JTokenType.Array, result.Value.Type);
            if (((JArray)result.Value).Count == 0) {
                return;
            }

            Assert.Equal(JTokenType.String, ((JArray)result.Value)[0].Type);
            // TODO: Assert.Equal(JTokenType.Guid, ((JArray)result.Value)[0].Type);
            Assert.Equal("Guid", result.DataType);
        }

        [Fact]
        public async Task NodeReadStaticArrayByteStringValueVariableTest() {

            var browser = GetServices();
            var node = "http://test.org/UA/Data/#i=10314";
            var expected = await _server.Client.ReadValueAsync(GetEndpoint(), node);

            // Act
            var result = await browser.NodeValueReadAsync(GetEndpoint(),
                new ValueReadRequestModel {
                    NodeId = node
                });

            // Assert
            Assert.NotNull(result.Value);
            Assert.NotNull(result.SourceTimestamp);
            Assert.NotNull(result.ServerTimestamp);
            Assert.True(JToken.DeepEquals(expected, result.Value),
                $"Expected: {expected} != Actual: {result.Value}");
            if (result.Value.Type == JTokenType.Null) {
                return;
            }
            Assert.Equal(JTokenType.Array, result.Value.Type);
            if (((JArray)result.Value).Count == 0) {
                return;
            }
            Assert.Equal(JTokenType.String, ((JArray)result.Value)[0].Type);
            // TODO:  Assert.Equal(JTokenType.Bytes, ((JArray)result.Value)[0].Type);
            Assert.Equal("ByteString", result.DataType);
        }

        [Fact]
        public async Task NodeReadStaticArrayXmlElementValueVariableTest() {

            var browser = GetServices();
            var node = "http://test.org/UA/Data/#i=10315";
            var expected = await _server.Client.ReadValueAsync(GetEndpoint(), node);

            // Act
            var result = await browser.NodeValueReadAsync(GetEndpoint(),
                new ValueReadRequestModel {
                    NodeId = node
                });

            // Assert
            Assert.NotNull(result.Value);
            Assert.NotNull(result.SourceTimestamp);
            Assert.NotNull(result.ServerTimestamp);
            Assert.True(JToken.DeepEquals(expected, result.Value),
                $"Expected: {expected} != Actual: {result.Value}");
            Assert.Equal(JTokenType.Array, result.Value.Type);
            if (((JArray)result.Value).Count == 0) {
                return;
            }

            Assert.Equal(JTokenType.Object, ((JArray)result.Value)[0].Type);
            Assert.Equal("XmlElement", result.DataType);
        }

        [Fact]
        public async Task NodeReadStaticArrayNodeIdValueVariableTest() {

            var browser = GetServices();
            var node = "http://test.org/UA/Data/#i=10316";
            var expected = await _server.Client.ReadValueAsync(GetEndpoint(), node);

            // Act
            var result = await browser.NodeValueReadAsync(GetEndpoint(),
                new ValueReadRequestModel {
                    NodeId = node
                });

            // Assert
            Assert.NotNull(result.Value);
            Assert.NotNull(result.SourceTimestamp);
            Assert.NotNull(result.ServerTimestamp);
            Assert.True(JToken.DeepEquals(expected, result.Value),
                $"Expected: {expected} != Actual: {result.Value}");
            Assert.Equal(JTokenType.Array, result.Value.Type);
            if (((JArray)result.Value).Count == 0) {
                return;
            }

            Assert.Equal(JTokenType.String, ((JArray)result.Value)[0].Type);
            Assert.Equal("NodeId", result.DataType);
        }

        [Fact]
        public async Task NodeReadStaticArrayExpandedNodeIdValueVariableTest() {

            var browser = GetServices();
            var node = "http://test.org/UA/Data/#i=10317";
            var expected = await _server.Client.ReadValueAsync(GetEndpoint(), node);

            // Act
            var result = await browser.NodeValueReadAsync(GetEndpoint(),
                new ValueReadRequestModel {
                    NodeId = node
                });

            // Assert
            Assert.NotNull(result.Value);
            Assert.NotNull(result.SourceTimestamp);
            Assert.NotNull(result.ServerTimestamp);
            Assert.True(JToken.DeepEquals(expected, result.Value),
                $"Expected: {expected} != Actual: {result.Value}");
            Assert.Equal(JTokenType.Array, result.Value.Type);
            if (((JArray)result.Value).Count == 0) {
                return;
            }

            Assert.Equal(JTokenType.String, ((JArray)result.Value)[0].Type);
            Assert.Equal("ExpandedNodeId", result.DataType);
        }

        [Fact]
        public async Task NodeReadStaticArrayQualifiedNameValueVariableTest() {

            var browser = GetServices();
            var node = "http://test.org/UA/Data/#i=10318";
            var expected = await _server.Client.ReadValueAsync(GetEndpoint(), node);

            // Act
            var result = await browser.NodeValueReadAsync(GetEndpoint(),
                new ValueReadRequestModel {
                    NodeId = node
                });

            // Assert
            Assert.NotNull(result.Value);
            Assert.NotNull(result.SourceTimestamp);
            Assert.NotNull(result.ServerTimestamp);
            Assert.True(JToken.DeepEquals(expected, result.Value),
                $"Expected: {expected} != Actual: {result.Value}");
            Assert.Equal(JTokenType.Array, result.Value.Type);
            if (((JArray)result.Value).Count == 0) {
                return;
            }

            Assert.Equal(JTokenType.String, ((JArray)result.Value)[0].Type);
            Assert.Equal("QualifiedName", result.DataType);
        }

        [Fact]
        public async Task NodeReadStaticArrayLocalizedTextValueVariableTest() {

            var browser = GetServices();
            var node = "http://test.org/UA/Data/#i=10319";
            var expected = await _server.Client.ReadValueAsync(GetEndpoint(), node);

            // Act
            var result = await browser.NodeValueReadAsync(GetEndpoint(),
                new ValueReadRequestModel {
                    NodeId = node
                });

            // Assert
            Assert.NotNull(result.Value);
            Assert.NotNull(result.SourceTimestamp);
            Assert.NotNull(result.ServerTimestamp);
            Assert.True(JToken.DeepEquals(expected, result.Value),
                $"Expected: {expected} != Actual: {result.Value}");
            Assert.Equal(JTokenType.Array, result.Value.Type);
            if (((JArray)result.Value).Count == 0) {
                return;
            }

            Assert.Equal(JTokenType.Object, ((JArray)result.Value)[0].Type);
            Assert.Equal("LocalizedText", result.DataType);
        }

        [Fact]
        public async Task NodeReadStaticArrayStatusCodeValueVariableTest() {

            var browser = GetServices();
            var node = "http://test.org/UA/Data/#i=10320";
            var expected = await _server.Client.ReadValueAsync(GetEndpoint(), node);

            // Act
            var result = await browser.NodeValueReadAsync(GetEndpoint(),
                new ValueReadRequestModel {
                    NodeId = node
                });

            // Assert
            Assert.NotNull(result.Value);
            Assert.NotNull(result.SourceTimestamp);
            Assert.NotNull(result.ServerTimestamp);
            Assert.True(JToken.DeepEquals(expected, result.Value),
                $"Expected: {expected} != Actual: {result.Value}");
            Assert.Equal(JTokenType.Array, result.Value.Type);
            if (((JArray)result.Value).Count == 0) {
                return;
            }
            Assert.True(
               ((JArray)result.Value)[0].Type == JTokenType.Object ||
               ((JArray)result.Value)[0].Type == JTokenType.Integer);
            Assert.Equal("StatusCode", result.DataType);
        }

        [Fact]
        public async Task NodeReadStaticArrayVariantValueVariableTest() {

            var browser = GetServices();
            var node = "http://test.org/UA/Data/#i=10321";
            var expected = await _server.Client.ReadValueAsync(GetEndpoint(), node);

            // Act
            var result = await browser.NodeValueReadAsync(GetEndpoint(),
                new ValueReadRequestModel {
                    NodeId = node
                });

            // Assert
            Assert.NotNull(result.Value);
            Assert.NotNull(result.SourceTimestamp);
            Assert.NotNull(result.ServerTimestamp);
            Assert.True(JToken.DeepEquals(expected, result.Value),
                $"Expected: {expected} != Actual: {result.Value}");
            Assert.Equal(JTokenType.Array, result.Value.Type);
            if (((JArray)result.Value).Count == 0) {
                return;
            }
        }

        [Fact]
        public async Task NodeReadStaticArrayEnumerationValueVariableTest() {

            var browser = GetServices();
            var node = "http://test.org/UA/Data/#i=10322";
            var expected = await _server.Client.ReadValueAsync(GetEndpoint(), node);

            // Act
            var result = await browser.NodeValueReadAsync(GetEndpoint(),
                new ValueReadRequestModel {
                    NodeId = node
                });

            // Assert
            Assert.NotNull(result.Value);
            Assert.NotNull(result.SourceTimestamp);
            Assert.NotNull(result.ServerTimestamp);
            Assert.True(JToken.DeepEquals(expected, result.Value),
                $"Expected: {expected} != Actual: {result.Value}");
            Assert.Equal(JTokenType.Array, result.Value.Type);
            if (((JArray)result.Value).Count == 0) {
                return;
            }

            Assert.Equal(JTokenType.Integer, ((JArray)result.Value)[0].Type);
            Assert.Equal("Int32", result.DataType);
        }

        [Fact]
        public async Task NodeReadStaticArrayStructureValueVariableTest() {

            var browser = GetServices();
            var node = "http://test.org/UA/Data/#i=10323";
            var expected = await _server.Client.ReadValueAsync(GetEndpoint(), node);

            // Act
            var result = await browser.NodeValueReadAsync(GetEndpoint(),
                new ValueReadRequestModel {
                    NodeId = node
                });

            // Assert
            Assert.NotNull(result.Value);
            Assert.NotNull(result.SourceTimestamp);
            Assert.NotNull(result.ServerTimestamp);
            Assert.True(JToken.DeepEquals(expected, result.Value),
                $"Expected: {expected} != Actual: {result.Value}");
            Assert.Equal(JTokenType.Array, result.Value.Type);
            if (((JArray)result.Value).Count == 0) {
                return;
            }

            Assert.Equal(JTokenType.Object, ((JArray)result.Value)[0].Type);
            // TODO: Assert.Equal(JTokenType.Bytes, ((JArray)result.Value)[0].Type);
            Assert.Equal("ExtensionObject", result.DataType);
        }

        [Fact]
        public async Task NodeReadStaticArrayNumberValueVariableTest() {

            var browser = GetServices();
            var node = "http://test.org/UA/Data/#i=10324";
            var expected = await _server.Client.ReadValueAsync(GetEndpoint(), node);

            // Act
            var result = await browser.NodeValueReadAsync(GetEndpoint(),
                new ValueReadRequestModel {
                    NodeId = node
                });

            // Assert
            Assert.NotNull(result.Value);
            Assert.NotNull(result.SourceTimestamp);
            Assert.NotNull(result.ServerTimestamp);
            Assert.True(JToken.DeepEquals(expected, result.Value),
                $"Expected: {expected} != Actual: {result.Value}");

            if (result.Value.Type == JTokenType.String) {
                Assert.NotEmpty(((string)result.Value).DecodeAsBase64());
                return;
            }
            Assert.Equal(JTokenType.Array, result.Value.Type);
            if (((JArray)result.Value).Count == 0) {
                return;
            }
            var type = ((JArray)result.Value)[0].Type;
            Assert.True(type == JTokenType.Integer || type == JTokenType.Float);
        }

        [Fact]
        public async Task NodeReadStaticArrayIntegerValueVariableTest() {

            var browser = GetServices();
            var node = "http://test.org/UA/Data/#i=10325";
            var expected = await _server.Client.ReadValueAsync(GetEndpoint(), node);

            // Act
            var result = await browser.NodeValueReadAsync(GetEndpoint(),
                new ValueReadRequestModel {
                    NodeId = node
                });

            // Assert
            Assert.NotNull(result.Value);
            Assert.NotNull(result.SourceTimestamp);
            Assert.NotNull(result.ServerTimestamp);
            Assert.True(JToken.DeepEquals(expected, result.Value),
                $"Expected: {expected} != Actual: {result.Value}");

            if (result.Value.Type == JTokenType.String) {
                Assert.NotEmpty(((string)result.Value).DecodeAsBase64());
                return;
            }
            Assert.Equal(JTokenType.Array, result.Value.Type);
            if (((JArray)result.Value).Count == 0) {
                return;
            }

            Assert.Equal(JTokenType.Integer, ((JArray)result.Value)[0].Type);
        }

        [Fact]
        public async Task NodeReadStaticArrayUIntegerValueVariableTest() {

            var browser = GetServices();
            var node = "http://test.org/UA/Data/#i=10326";
            var expected = await _server.Client.ReadValueAsync(GetEndpoint(), node);

            // Act
            var result = await browser.NodeValueReadAsync(GetEndpoint(),
                new ValueReadRequestModel {
                    NodeId = node
                });

            // Assert
            Assert.NotNull(result.Value);
            Assert.NotNull(result.SourceTimestamp);
            Assert.NotNull(result.ServerTimestamp);
            Assert.True(JToken.DeepEquals(expected, result.Value),
                $"Expected: {expected} != Actual: {result.Value}");

            if (result.Value.Type == JTokenType.String) {
                Assert.NotEmpty(((string)result.Value).DecodeAsBase64());
                return;
            }
            Assert.Equal(JTokenType.Array, result.Value.Type);
            if (((JArray)result.Value).Count == 0) {
                return;
            }

            Assert.Equal(JTokenType.Integer, ((JArray)result.Value)[0].Type);
        }

        public AddressSpaceValueReadArrayTests(ServerFixture server) {
            _server = server;
        }

        private INodeServices<EndpointModel> GetServices() {
            return new AddressSpaceServices(_server.Client,
                new JsonVariantEncoder(), _server.Logger);
        }

        private EndpointModel GetEndpoint() {
            return new EndpointModel {
                Url = $"opc.tcp://{Dns.GetHostName()}:{_server.Port}/UA/SampleServer"
            };
        }

        private readonly ServerFixture _server;
    }
}
