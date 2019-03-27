// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Testing.Tests {
    using Microsoft.Azure.IIoT.OpcUa.Twin;
    using Microsoft.Azure.IIoT.OpcUa.Twin.Models;
    using Newtonsoft.Json.Linq;
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Xunit;

    public class ReadScalarValueTests<T> {

        /// <summary>
        /// Create node services tests
        /// </summary>
        /// <param name="services"></param>
        /// <param name="endpoint"></param>
        /// <param name="readExpected"></param>
        public ReadScalarValueTests(Func<INodeServices<T>> services, T endpoint,
            Func<T, string, Task<JToken>> readExpected) {
            _services = services;
            _endpoint = endpoint;
            _readExpected = readExpected;
        }

        public async Task NodeReadAllStaticScalarVariableNodeClassTest1() {

            var browser = _services();
            var expected = Opc.Ua.NodeClass.Variable;

            var attributes = new List<AttributeReadRequestModel>();
            for (var i = 10216; i < 10243; i++) {
                attributes.Add(new AttributeReadRequestModel {
                    Attribute = NodeAttribute.NodeClass,
                    NodeId = "http://test.org/UA/Data/#i=" + i
                });
            }

            // Act
            var result = await browser.NodeReadAsync(_endpoint,
                new ReadRequestModel {
                    Header = new RequestHeaderModel {
                        Diagnostics = new DiagnosticsModel {
                            AuditId = nameof(NodeReadAllStaticScalarVariableNodeClassTest1),
                            TimeStamp = System.DateTime.Now
                        }
                    },
                    Attributes = attributes
                });

            // Assert
            Assert.NotNull(result.Results);
            Assert.Equal(attributes.Count, result.Results.Count);
            Assert.True(result.Results.TrueForAll(r => r.ErrorInfo == null));
            Assert.True(result.Results.TrueForAll(r => (int)r.Value == (int)expected));
        }


        public async Task NodeReadAllStaticScalarVariableAccessLevelTest1() {

            var browser = _services();
            var expected = Opc.Ua.AccessLevels.CurrentRead | Opc.Ua.AccessLevels.CurrentWrite;
            var attributes = new List<AttributeReadRequestModel>();
            for (var i = 10216; i < 10243; i++) {
                attributes.Add(new AttributeReadRequestModel {
                    Attribute = NodeAttribute.AccessLevel,
                    NodeId = "http://test.org/UA/Data/#i=" + i
                });
            }

            // Act
            var result = await browser.NodeReadAsync(_endpoint,
                new ReadRequestModel {
                    Header = new RequestHeaderModel {
                        Diagnostics = new DiagnosticsModel {
                            AuditId = nameof(NodeReadAllStaticScalarVariableAccessLevelTest1),
                            TimeStamp = System.DateTime.Now
                        }
                    },
                    Attributes = attributes
                });

            // Assert
            Assert.NotNull(result.Results);
            Assert.Equal(attributes.Count, result.Results.Count);
            Assert.True(result.Results.TrueForAll(r => r.ErrorInfo == null));
            Assert.True(result.Results.TrueForAll(r => (int)r.Value == expected));
        }


        public async Task NodeReadAllStaticScalarVariableWriteMaskTest1() {

            var browser = _services();

            var attributes = new List<AttributeReadRequestModel>();
            for (var i = 10216; i < 10243; i++) {
                attributes.Add(new AttributeReadRequestModel {
                    Attribute = NodeAttribute.WriteMask,
                    NodeId = "http://test.org/UA/Data/#i=" + i
                });
            }

            // Act
            var result = await browser.NodeReadAsync(_endpoint,
                new ReadRequestModel {
                    Header = new RequestHeaderModel {
                        Diagnostics = new DiagnosticsModel {
                            AuditId = nameof(NodeReadAllStaticScalarVariableWriteMaskTest1),
                            TimeStamp = System.DateTime.Now
                        }
                    },
                    Attributes = attributes
                });

            // Assert
            Assert.NotNull(result.Results);
            Assert.Equal(attributes.Count, result.Results.Count);
            Assert.True(result.Results.TrueForAll(r => r.ErrorInfo == null));
            Assert.True(result.Results.TrueForAll(r => (int)r.Value == 0));
        }


        public async Task NodeReadAllStaticScalarVariableWriteMaskTest2() {

            var browser = _services();

            var attributes = new List<AttributeReadRequestModel>();
            for (var i = 10216; i < 10243; i++) {
                attributes.Add(new AttributeReadRequestModel {
                    Attribute = NodeAttribute.WriteMask,
                    NodeId = "http://test.org/UA/Data/#i=10216"
                });
            }

            // Act
            var result = await browser.NodeReadAsync(_endpoint,
                new ReadRequestModel {
                    Header = new RequestHeaderModel {
                        Diagnostics = new DiagnosticsModel {
                            AuditId = nameof(NodeReadAllStaticScalarVariableWriteMaskTest2),
                            TimeStamp = System.DateTime.Now
                        }
                    },
                    Attributes = attributes
                });

            // Assert
            Assert.NotNull(result.Results);
            Assert.Equal(attributes.Count, result.Results.Count);
            Assert.True(result.Results.TrueForAll(r => r.ErrorInfo == null));
            Assert.True(result.Results.TrueForAll(r => (int)r.Value == 0));
        }


        public async Task NodeReadStaticScalarBooleanValueVariableTest() {

            var browser = _services();
            var node = "http://test.org/UA/Data/#i=10216";
            var expected = await _readExpected(_endpoint, node);

            // Act
            var result = await browser.NodeValueReadAsync(_endpoint,
                new ValueReadRequestModel {
                    Header = new RequestHeaderModel {
                        Diagnostics = new DiagnosticsModel {
                            AuditId = nameof(NodeReadStaticScalarBooleanValueVariableTest),
                            TimeStamp = System.DateTime.Now
                        }
                    },
                    NodeId = node
                });

            // Assert
            Assert.NotNull(result.Value);
            Assert.NotNull(result.SourceTimestamp);
            Assert.NotNull(result.ServerTimestamp);
            Assert.Equal(JTokenType.Boolean, result.Value.Type);
            Assert.True(JToken.DeepEquals(expected, result.Value),
                $"Expected: {expected} ({expected?.Type}) != Actual: {result.Value} ({result?.Value?.Type})");
            Assert.Equal("Boolean", result.DataType);
        }


        public async Task NodeReadStaticScalarBooleanValueVariableWithBrowsePathTest1() {

            var browser = _services();
            var node = "http://test.org/UA/Data/#i=10159"; // Scalar
            var path = new[] {
                ".http://test.org/UA/Data/#BooleanValue"
            };
            var expected = await _readExpected(_endpoint,
                "http://test.org/UA/Data/#i=10216");

            // Act
            var result = await browser.NodeValueReadAsync(_endpoint,
                new ValueReadRequestModel {
                    Header = new RequestHeaderModel {
                        Diagnostics = new DiagnosticsModel {
                            AuditId = nameof(NodeReadStaticScalarBooleanValueVariableTest),
                            TimeStamp = System.DateTime.Now
                        }
                    },
                    NodeId = node,
                    BrowsePath = path
                });

            // Assert
            Assert.NotNull(result.Value);
            Assert.NotNull(result.SourceTimestamp);
            Assert.NotNull(result.ServerTimestamp);
            Assert.Equal(JTokenType.Boolean, result.Value.Type);
            Assert.True(JToken.DeepEquals(expected, result.Value),
                $"Expected: {expected} ({expected?.Type}) != Actual: {result.Value} ({result?.Value?.Type})");
            Assert.Equal("Boolean", result.DataType);
        }


        public async Task NodeReadStaticScalarBooleanValueVariableWithBrowsePathTest2() {

            var browser = _services();
            var node = "http://test.org/UA/Data/#i=10159"; // Scalar
            var path = new[] {
                "http://test.org/UA/Data/#BooleanValue"
            };
            var expected = await _readExpected(_endpoint,
                "http://test.org/UA/Data/#i=10216");

            // Act
            var result = await browser.NodeValueReadAsync(_endpoint,
                new ValueReadRequestModel {
                    Header = new RequestHeaderModel {
                        Diagnostics = new DiagnosticsModel {
                            AuditId = nameof(NodeReadStaticScalarBooleanValueVariableTest),
                            TimeStamp = System.DateTime.Now
                        }
                    },
                    NodeId = node,
                    BrowsePath = path
                });

            // Assert
            Assert.NotNull(result.Value);
            Assert.NotNull(result.SourceTimestamp);
            Assert.NotNull(result.ServerTimestamp);
            Assert.Equal(JTokenType.Boolean, result.Value.Type);
            Assert.True(JToken.DeepEquals(expected, result.Value),
                $"Expected: {expected} ({expected?.Type}) != Actual: {result.Value} ({result?.Value?.Type})");
            Assert.Equal("Boolean", result.DataType);
        }


        public async Task NodeReadStaticScalarBooleanValueVariableWithBrowsePathTest3() {

            var browser = _services();
            var path = new[] {
                "Objects",
                "http://test.org/UA/Data/#Data",
                "http://test.org/UA/Data/#Static",
                "http://test.org/UA/Data/#Scalar",
                "http://test.org/UA/Data/#BooleanValue"
            };
            var expected = await _readExpected(_endpoint,
                "http://test.org/UA/Data/#i=10216");

            // Act
            var result = await browser.NodeValueReadAsync(_endpoint,
                new ValueReadRequestModel {
                    Header = new RequestHeaderModel {
                        Diagnostics = new DiagnosticsModel {
                            AuditId = nameof(NodeReadStaticScalarBooleanValueVariableTest),
                            TimeStamp = System.DateTime.Now
                        }
                    },
                    BrowsePath = path
                });

            // Assert
            Assert.NotNull(result.Value);
            Assert.NotNull(result.SourceTimestamp);
            Assert.NotNull(result.ServerTimestamp);
            Assert.Equal(JTokenType.Boolean, result.Value.Type);
            Assert.True(JToken.DeepEquals(expected, result.Value),
                $"Expected: {expected} ({expected?.Type}) != Actual: {result.Value} ({result?.Value?.Type})");
            Assert.Equal("Boolean", result.DataType);
        }


        public async Task NodeReadStaticScalarSByteValueVariableTest() {

            var browser = _services();
            var node = "http://test.org/UA/Data/#i=10217";
            var expected = await _readExpected(_endpoint, node);

            // Act
            var result = await browser.NodeValueReadAsync(_endpoint,
                new ValueReadRequestModel {
                    NodeId = node
                });

            // Assert
            Assert.NotNull(result.Value);
            Assert.NotNull(result.SourceTimestamp);
            Assert.NotNull(result.ServerTimestamp);
            Assert.Equal(JTokenType.Integer, result.Value.Type);
            Assert.True(JToken.DeepEquals(expected, result.Value),
                $"Expected: {expected} ({expected?.Type}) != Actual: {result.Value} ({result?.Value?.Type})");
            Assert.Equal("SByte", result.DataType);
        }


        public async Task NodeReadStaticScalarByteValueVariableTest() {

            var browser = _services();
            var node = "http://test.org/UA/Data/#i=10218";
            var expected = await _readExpected(_endpoint, node);

            // Act
            var result = await browser.NodeValueReadAsync(_endpoint,
                new ValueReadRequestModel {
                    NodeId = node
                });

            // Assert
            Assert.NotNull(result.Value);
            Assert.NotNull(result.SourceTimestamp);
            Assert.NotNull(result.ServerTimestamp);
            Assert.Equal(JTokenType.Integer, result.Value.Type);
            Assert.True(JToken.DeepEquals(expected, result.Value),
                $"Expected: {expected} ({expected?.Type}) != Actual: {result.Value} ({result?.Value?.Type})");
            Assert.Equal("Byte", result.DataType);
        }


        public async Task NodeReadStaticScalarInt16ValueVariableTest() {

            var browser = _services();
            var node = "http://test.org/UA/Data/#i=10219";
            var expected = await _readExpected(_endpoint, node);

            // Act
            var result = await browser.NodeValueReadAsync(_endpoint,
                new ValueReadRequestModel {
                    NodeId = node
                });

            // Assert
            Assert.NotNull(result.Value);
            Assert.NotNull(result.SourceTimestamp);
            Assert.NotNull(result.ServerTimestamp);
            Assert.Equal(JTokenType.Integer, result.Value.Type);
            Assert.True(JToken.DeepEquals(expected, result.Value),
                $"Expected: {expected} ({expected?.Type}) != Actual: {result.Value} ({result?.Value?.Type})");
            Assert.Equal("Int16", result.DataType);
        }


        public async Task NodeReadStaticScalarUInt16ValueVariableTest() {

            var browser = _services();
            var node = "http://test.org/UA/Data/#i=10220";
            var expected = await _readExpected(_endpoint, node);

            // Act
            var result = await browser.NodeValueReadAsync(_endpoint,
                new ValueReadRequestModel {
                    Header = new RequestHeaderModel {
                        Diagnostics = new DiagnosticsModel {
                            AuditId = nameof(NodeReadStaticScalarUInt16ValueVariableTest),
                            TimeStamp = System.DateTime.Now
                        }
                    },
                    NodeId = node
                });

            // Assert
            Assert.NotNull(result.Value);
            Assert.NotNull(result.SourceTimestamp);
            Assert.NotNull(result.ServerTimestamp);
            Assert.Equal(JTokenType.Integer, result.Value.Type);
            Assert.True(JToken.DeepEquals(expected, result.Value),
                $"Expected: {expected} ({expected?.Type}) != Actual: {result.Value} ({result?.Value?.Type})");
            Assert.Equal("UInt16", result.DataType);
        }


        public async Task NodeReadStaticScalarInt32ValueVariableTest() {

            var browser = _services();
            var node = "http://test.org/UA/Data/#i=10221";
            var expected = await _readExpected(_endpoint, node);

            // Act
            var result = await browser.NodeValueReadAsync(_endpoint,
                new ValueReadRequestModel {
                    NodeId = node
                });

            // Assert
            Assert.NotNull(result.Value);
            Assert.NotNull(result.SourceTimestamp);
            Assert.NotNull(result.ServerTimestamp);
            Assert.Equal(JTokenType.Integer, result.Value.Type);
            Assert.True(JToken.DeepEquals(expected, result.Value),
                $"Expected: {expected} ({expected?.Type}) != Actual: {result.Value} ({result?.Value?.Type})");
            Assert.Equal("Int32", result.DataType);
        }


        public async Task NodeReadStaticScalarUInt32ValueVariableTest() {

            var browser = _services();
            var node = "http://test.org/UA/Data/#i=10222";
            var expected = await _readExpected(_endpoint, node);

            // Act
            var result = await browser.NodeValueReadAsync(_endpoint,
                new ValueReadRequestModel {
                    NodeId = node
                });

            // Assert
            Assert.NotNull(result.Value);
            Assert.NotNull(result.SourceTimestamp);
            Assert.NotNull(result.ServerTimestamp);
            Assert.Equal(JTokenType.Integer, result.Value.Type);
            Assert.True(JToken.DeepEquals(expected, result.Value),
                $"Expected: {expected} ({expected?.Type}) != Actual: {result.Value} ({result?.Value?.Type})");
            Assert.Equal("UInt32", result.DataType);
        }


        public async Task NodeReadStaticScalarInt64ValueVariableTest() {

            var browser = _services();
            var node = "http://test.org/UA/Data/#i=10223";
            var expected = await _readExpected(_endpoint, node);

            // Act
            var result = await browser.NodeValueReadAsync(_endpoint,
                new ValueReadRequestModel {
                    NodeId = node
                });

            // Assert
            Assert.NotNull(result.Value);
            Assert.NotNull(result.SourceTimestamp);
            Assert.NotNull(result.ServerTimestamp);
            Assert.Equal(JTokenType.Integer, result.Value.Type);
            Assert.True(JToken.DeepEquals(expected, result.Value),
                $"Expected: {expected} ({expected?.Type}) != Actual: {result.Value} ({result?.Value?.Type})");
            Assert.Equal("Int64", result.DataType);
        }

        public async Task NodeReadStaticScalarUInt64ValueVariableTest() {

            var browser = _services();
            var node = "http://test.org/UA/Data/#i=10224";
            var expected = await _readExpected(_endpoint, node);

            // Act
            var result = await browser.NodeValueReadAsync(_endpoint,
                new ValueReadRequestModel {
                    NodeId = node
                });

            // Assert
            Assert.NotNull(result.Value);
            Assert.NotNull(result.SourceTimestamp);
            Assert.NotNull(result.ServerTimestamp);
            Assert.Equal(JTokenType.Integer, result.Value.Type);
            Assert.True(JToken.DeepEquals(expected, result.Value),
                $"Expected: {expected} ({expected?.Type}) != Actual: {result.Value} ({result?.Value?.Type})");
            Assert.Equal("UInt64", result.DataType);
        }

        public async Task NodeReadStaticScalarFloatValueVariableTest() {

            var browser = _services();
            var node = "http://test.org/UA/Data/#i=10225";
            var expected = await _readExpected(_endpoint, node);

            // Act
            var result = await browser.NodeValueReadAsync(_endpoint,
                new ValueReadRequestModel {
                    NodeId = node
                });

            // Assert
            Assert.NotNull(result.Value);
            Assert.NotNull(result.SourceTimestamp);
            Assert.NotNull(result.ServerTimestamp);
            Assert.True(result.Value.IsFloatValue());
            Assert.True(JToken.DeepEquals(expected, result.Value),
                $"Expected: {expected} ({expected?.Type}) != Actual: {result.Value} ({result?.Value?.Type})");
            Assert.Equal("Float", result.DataType);
        }

        public async Task NodeReadStaticScalarDoubleValueVariableTest() {

            var browser = _services();
            var node = "http://test.org/UA/Data/#i=10226";
            var expected = await _readExpected(_endpoint, node);

            // Act
            var result = await browser.NodeValueReadAsync(_endpoint,
                new ValueReadRequestModel {
                    NodeId = node
                });

            // Assert
            Assert.NotNull(result.Value);
            Assert.NotNull(result.SourceTimestamp);
            Assert.NotNull(result.ServerTimestamp);
            Assert.True(result.Value.IsFloatValue());
            Assert.True(JToken.DeepEquals(expected, result.Value),
                $"Expected: {expected} ({expected?.Type}) != Actual: {result.Value} ({result?.Value?.Type})");
            Assert.Equal("Double", result.DataType);
        }

        public async Task NodeReadStaticScalarStringValueVariableTest() {

            var browser = _services();
            var node = "http://test.org/UA/Data/#i=10227";
            var expected = await _readExpected(_endpoint, node);

            // Act
            var result = await browser.NodeValueReadAsync(_endpoint,
                new ValueReadRequestModel {
                    NodeId = node
                });

            // Assert
            Assert.NotNull(result.Value);
            Assert.NotNull(result.SourceTimestamp);
            Assert.NotNull(result.ServerTimestamp);
            Assert.Equal(JTokenType.String, result.Value.Type);
            Assert.True(JToken.DeepEquals(expected, result.Value),
                $"Expected: {expected} ({expected?.Type}) != Actual: {result.Value} ({result?.Value?.Type})");
            Assert.Equal("String", result.DataType);
        }


        public async Task NodeReadStaticScalarDateTimeValueVariableTest() {

            var browser = _services();
            var node = "http://test.org/UA/Data/#i=10228";
            var expected = await _readExpected(_endpoint, node);

            // Act
            var result = await browser.NodeValueReadAsync(_endpoint,
                new ValueReadRequestModel {
                    NodeId = node
                });

            // Assert
            Assert.NotNull(result.Value);
            Assert.NotNull(result.SourceTimestamp);
            Assert.NotNull(result.ServerTimestamp);
            Assert.Equal(JTokenType.Date, result.Value.Type);
            Assert.True(JToken.DeepEquals(expected, result.Value),
                $"Expected: {expected} ({expected?.Type}) != Actual: {result.Value} ({result?.Value?.Type})");
            Assert.Equal("DateTime", result.DataType);
        }


        public async Task NodeReadStaticScalarGuidValueVariableTest() {

            var browser = _services();
            var node = "http://test.org/UA/Data/#i=10229";
            var expected = await _readExpected(_endpoint, node);

            // Act
            var result = await browser.NodeValueReadAsync(_endpoint,
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
                $"Expected: {expected} ({expected?.Type}) != Actual: {result.Value} ({result?.Value?.Type})");
            Assert.Equal("Guid", result.DataType);
        }


        public async Task NodeReadStaticScalarByteStringValueVariableTest() {

            var browser = _services();
            var node = "http://test.org/UA/Data/#i=10230";
            var expected = await _readExpected(_endpoint, node);

            // Act
            var result = await browser.NodeValueReadAsync(_endpoint,
                new ValueReadRequestModel {
                    NodeId = node
                });

            // Assert
            Assert.NotNull(result.SourceTimestamp);
            Assert.NotNull(result.ServerTimestamp);
            Assert.True(JToken.DeepEquals(expected, result.Value),
                $"Expected: {expected} ({expected?.Type}) != Actual: {result.Value} ({result?.Value?.Type})");
            Assert.Equal("ByteString", result.DataType);
            // TODO : Assert.Equal(JTokenType.Bytes, result.Value.Type);
            // TODO : Assert.Equal(JTokenType.String, result.Value.Type);
        }


        public async Task NodeReadStaticScalarXmlElementValueVariableTest() {

            var browser = _services();
            var node = "http://test.org/UA/Data/#i=10231";
            var expected = await _readExpected(_endpoint, node);

            // Act
            var result = await browser.NodeValueReadAsync(_endpoint,
                new ValueReadRequestModel {
                    NodeId = node
                });

            // Assert
            Assert.NotNull(result.Value);
            Assert.NotNull(result.SourceTimestamp);
            Assert.NotNull(result.ServerTimestamp);
            Assert.Equal(JTokenType.Object, result.Value.Type);
            Assert.True(JToken.DeepEquals(expected, result.Value),
                $"Expected: {expected} ({expected?.Type}) != Actual: {result.Value} ({result?.Value?.Type})");
            Assert.Equal("XmlElement", result.DataType);
        }


        public async Task NodeReadStaticScalarNodeIdValueVariableTest() {

            var browser = _services();
            var node = "http://test.org/UA/Data/#i=10232";
            var expected = await _readExpected(_endpoint, node);

            // Act
            var result = await browser.NodeValueReadAsync(_endpoint,
                new ValueReadRequestModel {
                    NodeId = node
                });

            // Assert
            // Assert.NotNull(result.Value);
            Assert.NotNull(result.SourceTimestamp);
            Assert.NotNull(result.ServerTimestamp);
            Assert.Equal(JTokenType.String, result.Value.Type);
            Assert.True(JToken.DeepEquals(expected, result.Value),
                $"Expected: {expected} ({expected?.Type}) != Actual: {result.Value} ({result?.Value?.Type})");
            Assert.Equal("NodeId", result.DataType);
        }



        public async Task NodeReadStaticScalarExpandedNodeIdValueVariableTest() {

            var browser = _services();
            var node = "http://test.org/UA/Data/#i=10233";
            var expected = await _readExpected(_endpoint, node);

            // Act
            var result = await browser.NodeValueReadAsync(_endpoint,
                new ValueReadRequestModel {
                    NodeId = node
                });

            // Assert
            Assert.NotNull(result.Value);
            Assert.NotNull(result.SourceTimestamp);
            Assert.NotNull(result.ServerTimestamp);
            Assert.Equal(JTokenType.String, result.Value.Type);
            Assert.True(JToken.DeepEquals(expected, result.Value),
                $"Expected: {expected} ({expected?.Type}) != Actual: {result.Value} ({result?.Value?.Type})");
            Assert.Equal("ExpandedNodeId", result.DataType);
        }



        public async Task NodeReadStaticScalarQualifiedNameValueVariableTest() {

            var browser = _services();
            var node = "http://test.org/UA/Data/#i=10234";
            var expected = await _readExpected(_endpoint, node);

            // Act
            var result = await browser.NodeValueReadAsync(_endpoint,
                new ValueReadRequestModel {
                    NodeId = node
                });

            // Assert
            Assert.NotNull(result.Value);
            Assert.NotNull(result.SourceTimestamp);
            Assert.NotNull(result.ServerTimestamp);
            Assert.Equal(JTokenType.String, result.Value.Type);
            Assert.True(JToken.DeepEquals(expected, result.Value),
                $"Expected: {expected} ({expected?.Type}) != Actual: {result.Value} ({result?.Value?.Type})");
            Assert.Equal("QualifiedName", result.DataType);
        }


        public async Task NodeReadStaticScalarLocalizedTextValueVariableTest() {

            var browser = _services();
            var node = "http://test.org/UA/Data/#i=10235";
            var expected = await _readExpected(_endpoint, node);

            // Act
            var result = await browser.NodeValueReadAsync(_endpoint,
                new ValueReadRequestModel {
                    NodeId = node
                });

            // Assert
            Assert.NotNull(result.Value);
            Assert.NotNull(result.SourceTimestamp);
            Assert.NotNull(result.ServerTimestamp);
            Assert.Equal(JTokenType.Object, result.Value.Type);
            Assert.True(JToken.DeepEquals(expected, result.Value),
                $"Expected: {expected} ({expected?.Type}) != Actual: {result.Value} ({result?.Value?.Type})");
            Assert.Equal("LocalizedText", result.DataType);
        }


        public async Task NodeReadStaticScalarStatusCodeValueVariableTest() {

            var browser = _services();
            var node = "http://test.org/UA/Data/#i=10236";
            var expected = await _readExpected(_endpoint, node);

            // Act
            var result = await browser.NodeValueReadAsync(_endpoint,
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
                $"Expected: {expected} ({expected?.Type}) != Actual: {result.Value} ({result?.Value?.Type})");
            Assert.Equal("StatusCode", result.DataType);
        }


        public async Task NodeReadStaticScalarVariantValueVariableTest() {

            var browser = _services();
            var node = "http://test.org/UA/Data/#i=10237";
            var expected = await _readExpected(_endpoint, node);

            // Act
            var result = await browser.NodeValueReadAsync(_endpoint,
                new ValueReadRequestModel {
                    NodeId = node
                });

            // Assert
            Assert.NotNull(result.SourceTimestamp);
            Assert.NotNull(result.ServerTimestamp);
            Assert.True(JToken.DeepEquals(expected, result.Value),
                $"Expected: {expected} ({expected?.Type}) != Actual: {result.Value} ({result?.Value?.Type})");
            // Assert.Equal("BaseDataType", result.DataType);
        }


        public async Task NodeReadStaticScalarEnumerationValueVariableTest() {

            var browser = _services();
            var node = "http://test.org/UA/Data/#i=10238";
            var expected = await _readExpected(_endpoint, node);

            // Act
            var result = await browser.NodeValueReadAsync(_endpoint,
                new ValueReadRequestModel {
                    NodeId = node
                });

            // Assert
            Assert.NotNull(result.Value);
            Assert.NotNull(result.SourceTimestamp);
            Assert.NotNull(result.ServerTimestamp);
            Assert.Equal(JTokenType.Integer, result.Value.Type);
            Assert.True(JToken.DeepEquals(expected, result.Value),
                $"Expected: {expected} ({expected?.Type}) != Actual: {result.Value} ({result?.Value?.Type})");
            // TODO: Assert.Equal("Enumeration", result.DataType);
            Assert.Equal("Int32", result.DataType);
        }


        public async Task NodeReadStaticScalarStructuredValueVariableTest() {

            var browser = _services();
            var node = "http://test.org/UA/Data/#i=10239";
            var expected = await _readExpected(_endpoint, node);

            // Act
            var result = await browser.NodeValueReadAsync(_endpoint,
                new ValueReadRequestModel {
                    NodeId = node
                });

            // Assert
            Assert.NotNull(result.Value);
            Assert.NotNull(result.SourceTimestamp);
            Assert.NotNull(result.ServerTimestamp);
            Assert.Equal(JTokenType.Object, result.Value.Type);
            Assert.True(JToken.DeepEquals(expected, result.Value),
                $"Expected: {expected} ({expected?.Type}) != Actual: {result.Value} ({result?.Value?.Type})");
            Assert.Equal("ExtensionObject", result.DataType);
            // TODO: Assert.Equal("Structure", result.DataType);
        }


        public async Task NodeReadStaticScalarNumberValueVariableTest() {

            var browser = _services();
            var node = "http://test.org/UA/Data/#i=10240";
            var expected = await _readExpected(_endpoint, node);

            // Act
            var result = await browser.NodeValueReadAsync(_endpoint,
                new ValueReadRequestModel {
                    NodeId = node
                });

            // Assert
            Assert.NotNull(result.Value);
            Assert.NotNull(result.SourceTimestamp);
            Assert.NotNull(result.ServerTimestamp);
            Assert.True(JToken.DeepEquals(expected, result.Value),
                $"Expected: {expected} ({expected?.Type}) != Actual: {result.Value} ({result?.Value?.Type})");
            // Assert.Equal("Number", result.DataType);
        }


        public async Task NodeReadStaticScalarIntegerValueVariableTest() {

            var browser = _services();
            var node = "http://test.org/UA/Data/#i=10241";
            var expected = await _readExpected(_endpoint, node);

            // Act
            var result = await browser.NodeValueReadAsync(_endpoint,
                new ValueReadRequestModel {
                    NodeId = node
                });

            // Assert
            Assert.NotNull(result.Value);
            Assert.NotNull(result.SourceTimestamp);
            Assert.NotNull(result.ServerTimestamp);
            Assert.True(JToken.DeepEquals(expected, result.Value),
                $"Expected: {expected} ({expected?.Type}) != Actual: {result.Value} ({result?.Value?.Type})");
            // Assert.Equal("Integer", result.DataType);
        }


        public async Task NodeReadStaticScalarUIntegerValueVariableTest() {

            var browser = _services();
            var node = "http://test.org/UA/Data/#i=10242";
            var expected = await _readExpected(_endpoint, node);

            // Act
            var result = await browser.NodeValueReadAsync(_endpoint,
                new ValueReadRequestModel {
                    NodeId = node
                });

            // Assert
            Assert.NotNull(result.Value);
            Assert.NotNull(result.SourceTimestamp);
            Assert.NotNull(result.ServerTimestamp);
            Assert.True(JToken.DeepEquals(expected, result.Value),
                $"Expected: {expected} ({expected?.Type}) != Actual: {result.Value} ({result?.Value?.Type})");
            // Assert.Equal("UInteger", result.DataType);
        }


        public async Task NodeReadDiagnosticsNoneTest() {

            var browser = _services();

            // Act
            var results = await browser.NodeValueReadAsync(_endpoint,
                new ValueReadRequestModel {
                    Header = new RequestHeaderModel {
                        Diagnostics = new DiagnosticsModel {
                            AuditId = nameof(NodeReadDiagnosticsNoneTest),
                            Level = DiagnosticsLevel.None
                        }
                    },
                    NodeId = "http://opcfoundation.org/UA/Boiler/#s=unknown"
                });

            // Assert
            Assert.Null(results.ErrorInfo.Diagnostics);
        }


        public async Task NodeReadDiagnosticsStatusTest() {

            var browser = _services();

            // Act
            var results = await browser.NodeValueReadAsync(_endpoint,
                new ValueReadRequestModel {
                    Header = new RequestHeaderModel {
                        Diagnostics = new DiagnosticsModel {
                            AuditId = nameof(NodeReadDiagnosticsStatusTest),
                            TimeStamp = System.DateTime.Now
                        }
                    },
                    NodeId = "http://opcfoundation.org/UA/Boiler/#s=unknown"
                });

            // Assert
            Assert.NotNull(results.ErrorInfo.Diagnostics);
            Assert.Equal(JTokenType.Array, results.ErrorInfo.Diagnostics.Type);
            Assert.Collection(results.ErrorInfo.Diagnostics, j => {
                Assert.Equal(JTokenType.String, j.Type);
                Assert.Equal("BadNodeIdUnknown", (string)j);
            });
        }


        public async Task NodeReadDiagnosticsOperationsTest() {

            var browser = _services();

            // Act
            var results = await browser.NodeValueReadAsync(_endpoint,
                new ValueReadRequestModel {
                    Header = new RequestHeaderModel {
                        Diagnostics = new DiagnosticsModel {
                            AuditId = nameof(NodeReadDiagnosticsOperationsTest),
                            Level = DiagnosticsLevel.Operations
                        }
                    },
                    NodeId = "http://opcfoundation.org/UA/Boiler/#s=unknown"
                });

            // Assert
            Assert.NotNull(results.ErrorInfo.Diagnostics);
            Assert.Equal(JTokenType.Object, results.ErrorInfo.Diagnostics.Type);
            Assert.Collection(results.ErrorInfo.Diagnostics,
                j => {
                    Assert.Equal(JTokenType.Property, j.Type);
                    Assert.Equal("BadNodeIdUnknown", ((JProperty)j).Name);
                    var item = ((JProperty)j).Value as JArray;
                    Assert.NotNull(item);
                    Assert.Equal("ReadValue_ns=4;s=unknown", (string)item[0]);
                });
        }


        public async Task NodeReadDiagnosticsVerboseTest() {

            var browser = _services();

            // Act
            var results = await browser.NodeValueReadAsync(_endpoint,
                new ValueReadRequestModel {
                    Header = new RequestHeaderModel {
                        Diagnostics = new DiagnosticsModel {
                            AuditId = nameof(NodeReadDiagnosticsVerboseTest),
                            Level = DiagnosticsLevel.Verbose
                        }
                    },
                    NodeId = "http://opcfoundation.org/UA/Boiler/#s=unknown"
                });

            // Assert
            Assert.NotNull(results.ErrorInfo.Diagnostics);
            Assert.Equal(JTokenType.Array, results.ErrorInfo.Diagnostics.Type);
        }

        private readonly T _endpoint;
        private readonly Func<T, string, Task<JToken>> _readExpected;
        private readonly Func<INodeServices<T>> _services;
    }
}
