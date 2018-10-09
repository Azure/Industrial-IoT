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
    using Newtonsoft.Json.Linq;
    using System.Net;
    using System.Threading.Tasks;
    using Xunit;

    [Collection(ReadCollection.Name)]
    public class AddressSpaceValueReadScalarTests {

        [Fact]
        public async Task NodeReadStaticScalarBooleanValueVariableTest() {

            var browser = GetServices();
            var node = "http://test.org/UA/Data/#i=10216";
            var expected = await _server.Client.ReadValueAsync(GetEndpoint(), node);

            // Act
            var result = await browser.NodeValueReadAsync(GetEndpoint(),
                new ValueReadRequestModel {
                    Diagnostics = new DiagnosticsModel {
                        AuditId = nameof(NodeReadStaticScalarBooleanValueVariableTest),
                        TimeStamp = System.DateTime.Now
                    },
                    NodeId = node
                });

            // Assert
            Assert.NotNull(result.Value);
            Assert.NotNull(result.SourceTimestamp);
            Assert.NotNull(result.ServerTimestamp);
            Assert.Equal(JTokenType.Boolean, result.Value.Type);
            Assert.True(JToken.DeepEquals(expected, result.Value),
                $"Expected: {expected} != Actual: {result.Value}");
            Assert.Equal("Boolean", result.DataType);
        }

        [Fact]
        public async Task NodeReadStaticScalarSByteValueVariableTest() {

            var browser = GetServices();
            var node = "http://test.org/UA/Data/#i=10217";
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
            Assert.Equal(JTokenType.Integer, result.Value.Type);
            Assert.True(JToken.DeepEquals(expected, result.Value),
                $"Expected: {expected} != Actual: {result.Value}");
            Assert.Equal("SByte", result.DataType);
        }

        [Fact]
        public async Task NodeReadStaticScalarByteValueVariableTest() {

            var browser = GetServices();
            var node = "http://test.org/UA/Data/#i=10218";
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
            Assert.Equal(JTokenType.Integer, result.Value.Type);
            Assert.True(JToken.DeepEquals(expected, result.Value),
                $"Expected: {expected} != Actual: {result.Value}");
            Assert.Equal("Byte", result.DataType);
        }

        [Fact]
        public async Task NodeReadStaticScalarInt16ValueVariableTest() {

            var browser = GetServices();
            var node = "http://test.org/UA/Data/#i=10219";
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
            Assert.Equal(JTokenType.Integer, result.Value.Type);
            Assert.True(JToken.DeepEquals(expected, result.Value),
                $"Expected: {expected} != Actual: {result.Value}");
            Assert.Equal("Int16", result.DataType);
        }

        [Fact]
        public async Task NodeReadStaticScalarUInt16ValueVariableTest() {

            var browser = GetServices();
            var node = "http://test.org/UA/Data/#i=10220";
            var expected = await _server.Client.ReadValueAsync(GetEndpoint(), node);

            // Act
            var result = await browser.NodeValueReadAsync(GetEndpoint(),
                new ValueReadRequestModel {
                    Diagnostics = new DiagnosticsModel {
                        AuditId = nameof(NodeReadStaticScalarUInt16ValueVariableTest),
                        TimeStamp = System.DateTime.Now
                    },
                    NodeId = node
                });

            // Assert
            Assert.NotNull(result.Value);
            Assert.NotNull(result.SourceTimestamp);
            Assert.NotNull(result.ServerTimestamp);
            Assert.Equal(JTokenType.Integer, result.Value.Type);
            Assert.True(JToken.DeepEquals(expected, result.Value),
                $"Expected: {expected} != Actual: {result.Value}");
            Assert.Equal("UInt16", result.DataType);
        }


        [Fact]
        public async Task NodeReadStaticScalarInt32ValueVariableTest() {

            var browser = GetServices();
            var node = "http://test.org/UA/Data/#i=10221";
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
            Assert.Equal(JTokenType.Integer, result.Value.Type);
            Assert.True(JToken.DeepEquals(expected, result.Value),
                $"Expected: {expected} != Actual: {result.Value}");
            Assert.Equal("Int32", result.DataType);
        }


        [Fact]
        public async Task NodeReadStaticScalarUInt32ValueVariableTest() {

            var browser = GetServices();
            var node = "http://test.org/UA/Data/#i=10222";
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
            Assert.Equal(JTokenType.Integer, result.Value.Type);
            Assert.True(JToken.DeepEquals(expected, result.Value),
                $"Expected: {expected} != Actual: {result.Value}");
            Assert.Equal("UInt32", result.DataType);
        }


        [Fact]
        public async Task NodeReadStaticScalarInt64ValueVariableTest() {

            var browser = GetServices();
            var node = "http://test.org/UA/Data/#i=10223";
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
            Assert.Equal(JTokenType.Integer, result.Value.Type);
            Assert.True(JToken.DeepEquals(expected, result.Value),
                $"Expected: {expected} != Actual: {result.Value}");
            Assert.Equal("Int64", result.DataType);
        }


        [Fact]
        public async Task NodeReadStaticScalarUInt64ValueVariableTest() {

            var browser = GetServices();
            var node = "http://test.org/UA/Data/#i=10224";
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
            Assert.Equal(JTokenType.Integer, result.Value.Type);
            Assert.True(JToken.DeepEquals(expected, result.Value),
                $"Expected: {expected} != Actual: {result.Value}");
            Assert.Equal("UInt64", result.DataType);
        }


        [Fact]
        public async Task NodeReadStaticScalarFloatValueVariableTest() {

            var browser = GetServices();
            var node = "http://test.org/UA/Data/#i=10225";
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
            Assert.Equal(JTokenType.Float, result.Value.Type);
            Assert.True(JToken.DeepEquals(expected, result.Value),
                $"Expected: {expected} != Actual: {result.Value}");
            Assert.Equal("Float", result.DataType);
        }


        [Fact]
        public async Task NodeReadStaticScalarDoubleValueVariableTest() {

            var browser = GetServices();
            var node = "http://test.org/UA/Data/#i=10226";
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
            Assert.Equal(JTokenType.Float, result.Value.Type);
            Assert.True(JToken.DeepEquals(expected, result.Value),
                $"Expected: {expected} != Actual: {result.Value}");
            Assert.Equal("Double", result.DataType);
        }


        [Fact]
        public async Task NodeReadStaticScalarStringValueVariableTest() {

            var browser = GetServices();
            var node = "http://test.org/UA/Data/#i=10227";
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
            Assert.Equal(JTokenType.String, result.Value.Type);
            Assert.True(JToken.DeepEquals(expected, result.Value),
                $"Expected: {expected} != Actual: {result.Value}");
            Assert.Equal("String", result.DataType);
        }


        [Fact]
        public async Task NodeReadStaticScalarDateTimeValueVariableTest() {

            var browser = GetServices();
            var node = "http://test.org/UA/Data/#i=10228";
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
            Assert.Equal(JTokenType.Date, result.Value.Type);
            Assert.True(JToken.DeepEquals(expected, result.Value),
                $"Expected: {expected} != Actual: {result.Value}");
            Assert.Equal("DateTime", result.DataType);
        }


        [Fact]
        public async Task NodeReadStaticScalarGuidValueVariableTest() {

            var browser = GetServices();
            var node = "http://test.org/UA/Data/#i=10229";
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
            // Assert.Equal(JTokenType.Guid, result.Value.Type);
            Assert.Equal(JTokenType.String, result.Value.Type);
            Assert.True(JToken.DeepEquals(expected, result.Value),
                $"Expected: {expected} != Actual: {result.Value}");
            Assert.Equal("Guid", result.DataType);
        }


        [Fact]
        public async Task NodeReadStaticScalarByteStringValueVariableTest() {

            var browser = GetServices();
            var node = "http://test.org/UA/Data/#i=10230";
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
            Assert.Equal("ByteString", result.DataType);
            if (JTokenType.Null == result.Value.Type) {
                return; // Can happen
            }
            // TODO : Assert.Equal(JTokenType.Bytes, result.Value.Type);
            Assert.Equal(JTokenType.String, result.Value.Type);
        }

        [Fact]
        public async Task NodeReadStaticScalarXmlElementValueVariableTest() {

            var browser = GetServices();
            var node = "http://test.org/UA/Data/#i=10231";
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
            Assert.Equal(JTokenType.Object, result.Value.Type);
            Assert.True(JToken.DeepEquals(expected, result.Value),
                $"Expected: {expected} != Actual: {result.Value}");
            Assert.Equal("XmlElement", result.DataType);
        }

        [Fact]
        public async Task NodeReadStaticScalarNodeIdValueVariableTest() {

            var browser = GetServices();
            var node = "http://test.org/UA/Data/#i=10232";
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
            Assert.Equal(JTokenType.String, result.Value.Type);
            Assert.True(JToken.DeepEquals(expected, result.Value),
                $"Expected: {expected} != Actual: {result.Value}");
            Assert.Equal("NodeId", result.DataType);
        }


        [Fact]
        public async Task NodeReadStaticScalarExpandedNodeIdValueVariableTest() {

            var browser = GetServices();
            var node = "http://test.org/UA/Data/#i=10233";
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
            Assert.Equal(JTokenType.String, result.Value.Type);
            Assert.True(JToken.DeepEquals(expected, result.Value),
                $"Expected: {expected} != Actual: {result.Value}");
            Assert.Equal("ExpandedNodeId", result.DataType);
        }


        [Fact]
        public async Task NodeReadStaticScalarQualifiedNameValueVariableTest() {

            var browser = GetServices();
            var node = "http://test.org/UA/Data/#i=10234";
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
            Assert.Equal(JTokenType.String, result.Value.Type);
            Assert.True(JToken.DeepEquals(expected, result.Value),
                $"Expected: {expected} != Actual: {result.Value}");
            Assert.Equal("QualifiedName", result.DataType);
        }

        [Fact]
        public async Task NodeReadStaticScalarLocalizedTextValueVariableTest() {

            var browser = GetServices();
            var node = "http://test.org/UA/Data/#i=10235";
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
            Assert.Equal(JTokenType.Object, result.Value.Type);
            Assert.True(JToken.DeepEquals(expected, result.Value),
                $"Expected: {expected} != Actual: {result.Value}");
            Assert.Equal("LocalizedText", result.DataType);
        }

        [Fact]
        public async Task NodeReadStaticScalarStatusCodeValueVariableTest() {

            var browser = GetServices();
            var node = "http://test.org/UA/Data/#i=10236";
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
            Assert.True(
                result.Value.Type == JTokenType.Object ||
                result.Value.Type == JTokenType.Integer);
            Assert.True(JToken.DeepEquals(expected, result.Value),
                $"Expected: {expected} != Actual: {result.Value}");
            Assert.Equal("StatusCode", result.DataType);
        }

        [Fact]
        public async Task NodeReadStaticScalarVariantValueVariableTest() {

            var browser = GetServices();
            var node = "http://test.org/UA/Data/#i=10237";
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
            // Assert.Equal("BaseDataType", result.DataType);
        }

        [Fact]
        public async Task NodeReadStaticScalarEnumerationValueVariableTest() {

            var browser = GetServices();
            var node = "http://test.org/UA/Data/#i=10238";
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
            Assert.Equal(JTokenType.Integer, result.Value.Type);
            Assert.True(JToken.DeepEquals(expected, result.Value),
                $"Expected: {expected} != Actual: {result.Value}");
            // TODO: Assert.Equal("Enumeration", result.DataType);
            Assert.Equal("Int32", result.DataType);
        }


        [Fact]
        public async Task NodeReadStaticScalarStructuredValueVariableTest() {

            var browser = GetServices();
            var node = "http://test.org/UA/Data/#i=10239";
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
            Assert.Equal(JTokenType.Object, result.Value.Type);
            Assert.True(JToken.DeepEquals(expected, result.Value),
                $"Expected: {expected} != Actual: {result.Value}");
            Assert.Equal("ExtensionObject", result.DataType);
            // TODO: Assert.Equal("Structure", result.DataType);
        }

        [Fact]
        public async Task NodeReadStaticScalarNumberValueVariableTest() {

            var browser = GetServices();
            var node = "http://test.org/UA/Data/#i=10240";
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
            // Assert.Equal("Number", result.DataType);
        }

        [Fact]
        public async Task NodeReadStaticScalarIntegerValueVariableTest() {

            var browser = GetServices();
            var node = "http://test.org/UA/Data/#i=10241";
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
            // Assert.Equal("Integer", result.DataType);
        }

        [Fact]
        public async Task NodeReadStaticScalarUIntegerValueVariableTest() {

            var browser = GetServices();
            var node = "http://test.org/UA/Data/#i=10242";
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
            // Assert.Equal("UInteger", result.DataType);
        }

        [Fact]
        public async Task NodeReadDiagnosticsNoneTest() {

            var browser = GetServices();

            // Act
            var results = await browser.NodeValueReadAsync(GetEndpoint(),
                new ValueReadRequestModel {
                    Diagnostics = new DiagnosticsModel {
                        AuditId = nameof(NodeReadDiagnosticsNoneTest),
                        Level = DiagnosticsLevel.None
                    },
                    NodeId = "http://opcfoundation.org/UA/Boiler/#s=unknown"
                });

            // Assert
            Assert.Null(results.Diagnostics);
        }

        [Fact]
        public async Task NodeReadDiagnosticsStatusTest() {

            var browser = GetServices();

            // Act
            var results = await browser.NodeValueReadAsync(GetEndpoint(),
                new ValueReadRequestModel {
                    Diagnostics = new DiagnosticsModel {
                        AuditId = nameof(NodeReadDiagnosticsStatusTest),
                        TimeStamp = System.DateTime.Now
                    },
                    NodeId = "http://opcfoundation.org/UA/Boiler/#s=unknown"
                });

            // Assert
            Assert.NotNull(results.Diagnostics);
            Assert.Equal(JTokenType.Array, results.Diagnostics.Type);
            Assert.Collection(results.Diagnostics, j => {
                Assert.Equal(JTokenType.String, j.Type);
                Assert.Equal("BadNodeIdUnknown", (string)j);
            });
        }

        [Fact]
        public async Task NodeReadDiagnosticsOperationsTest() {

            var browser = GetServices();

            // Act
            var results = await browser.NodeValueReadAsync(GetEndpoint(),
                new ValueReadRequestModel {
                    Diagnostics = new DiagnosticsModel {
                        AuditId = nameof(NodeReadDiagnosticsOperationsTest),
                        Level = DiagnosticsLevel.Operations
                    },
                    NodeId = "http://opcfoundation.org/UA/Boiler/#s=unknown"
                });

            // Assert
            Assert.NotNull(results.Diagnostics);
            Assert.Equal(JTokenType.Object, results.Diagnostics.Type);
            Assert.Collection(results.Diagnostics,
                j => {
                    Assert.Equal(JTokenType.Property, j.Type);
                    Assert.Equal("BadNodeIdUnknown", ((JProperty)j).Name);
                    var item = ((JProperty)j).Value as JArray;
                    Assert.NotNull(item);
                    Assert.Equal("ReadValue_ns=4;s=unknown", (string)item[0]);
                });
        }

        [Fact]
        public async Task NodeReadDiagnosticsVerboseTest() {

            var browser = GetServices();

            // Act
            var results = await browser.NodeValueReadAsync(GetEndpoint(),
                new ValueReadRequestModel {
                    Diagnostics = new DiagnosticsModel {
                        AuditId = nameof(NodeReadDiagnosticsVerboseTest),
                        Level = DiagnosticsLevel.Verbose
                    },
                    NodeId = "http://opcfoundation.org/UA/Boiler/#s=unknown"
                });

            // Assert
            Assert.NotNull(results.Diagnostics);
            Assert.Equal(JTokenType.Array, results.Diagnostics.Type);
        }

        public AddressSpaceValueReadScalarTests(ServerFixture server) {
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
