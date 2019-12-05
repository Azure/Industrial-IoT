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

    public class ReadArrayValueTests<T> {

        /// <summary>
        /// Create node services tests
        /// </summary>
        /// <param name="services"></param>
        /// <param name="endpoint"></param>
        /// <param name="readExpected"></param>
        public ReadArrayValueTests(Func<INodeServices<T>> services, T endpoint,
            Func<T, string, Task<JToken>> readExpected) {
            _services = services;
            _endpoint = endpoint;
            _readExpected = readExpected;
        }

        public async Task NodeReadAllStaticArrayVariableNodeClassTest1Async() {

            var browser = _services();
            var expected = Opc.Ua.NodeClass.Variable;

            var attributes = new List<AttributeReadRequestModel>();
            for (var i = 10300; i < 10326; i++) {
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
                            AuditId = nameof(NodeReadAllStaticArrayVariableNodeClassTest1Async),
                            TimeStamp = DateTime.Now
                        }
                    },
                    Attributes = attributes
                });

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Results);
            Assert.Equal(attributes.Count, result.Results.Count);
            Assert.True(result.Results.TrueForAll(r => r.ErrorInfo == null));
            Assert.True(result.Results.TrueForAll(r => (int)r.Value == (int)expected));
        }


        public async Task NodeReadAllStaticArrayVariableAccessLevelTest1Async() {

            var browser = _services();
            var expected = Opc.Ua.AccessLevels.CurrentRead | Opc.Ua.AccessLevels.CurrentWrite;
            var attributes = new List<AttributeReadRequestModel>();
            for (var i = 10300; i < 10326; i++) {
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
                            AuditId = nameof(NodeReadAllStaticArrayVariableAccessLevelTest1Async),
                            TimeStamp = DateTime.Now
                        }
                    },
                    Attributes = attributes
                });

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Results);
            Assert.Equal(attributes.Count, result.Results.Count);
            Assert.True(result.Results.TrueForAll(r => r.ErrorInfo == null));
            Assert.True(result.Results.TrueForAll(r => (int)r.Value == expected));
        }


        public async Task NodeReadAllStaticArrayVariableWriteMaskTest1Async() {

            var browser = _services();

            var attributes = new List<AttributeReadRequestModel>();
            for (var i = 10300; i < 10326; i++) {
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
                            AuditId = nameof(NodeReadAllStaticArrayVariableWriteMaskTest1Async),
                            TimeStamp = DateTime.Now
                        }
                    },
                    Attributes = attributes
                });

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Results);
            Assert.Equal(attributes.Count, result.Results.Count);
            Assert.True(result.Results.TrueForAll(r => r.ErrorInfo == null));
            Assert.True(result.Results.TrueForAll(r => (int)r.Value == 0));
        }


        public async Task NodeReadAllStaticArrayVariableWriteMaskTest2Async() {

            var browser = _services();

            var attributes = new List<AttributeReadRequestModel>();
            for (var i = 10300; i < 10326; i++) {
                attributes.Add(new AttributeReadRequestModel {
                    Attribute = NodeAttribute.WriteMask,
                    NodeId = "http://test.org/UA/Data/#i=10300"
                });
            }

            // Act
            var result = await browser.NodeReadAsync(_endpoint,
                new ReadRequestModel {
                    Header = new RequestHeaderModel {
                        Diagnostics = new DiagnosticsModel {
                            AuditId = nameof(NodeReadAllStaticArrayVariableWriteMaskTest2Async),
                            TimeStamp = DateTime.Now
                        }
                    },
                    Attributes = attributes
                });

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Results);
            Assert.Equal(attributes.Count, result.Results.Count);
            Assert.True(result.Results.TrueForAll(r => r.ErrorInfo == null));
            Assert.True(result.Results.TrueForAll(r => (int)r.Value == 0));
        }


        public async Task NodeReadStaticArrayBooleanValueVariableTestAsync() {

            var browser = _services();
            var node = "http://test.org/UA/Data/#i=10300";
            var expected = await _readExpected(_endpoint, node);

            // Act
            var result = await browser.NodeValueReadAsync(_endpoint,
                new ValueReadRequestModel {
                    NodeId = node
                });

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Value);
            Assert.NotNull(result.SourceTimestamp);
            Assert.NotNull(result.ServerTimestamp);
            AssertEqualValue(expected, result.Value);

            Assert.Equal(JTokenType.Array, result.Value.Type);
            if (((JArray)result.Value).Count == 0) {
                return;
            }

            Assert.Equal(JTokenType.Boolean, ((JArray)result.Value)[0].Type);
            Assert.Equal("Boolean", result.DataType);
        }


        public async Task NodeReadStaticArraySByteValueVariableTestAsync() {

            var browser = _services();
            var node = "http://test.org/UA/Data/#i=10301";
            var expected = await _readExpected(_endpoint, node);

            // Act
            var result = await browser.NodeValueReadAsync(_endpoint,
                new ValueReadRequestModel {
                    NodeId = node
                });

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Value);
            Assert.NotNull(result.SourceTimestamp);
            Assert.NotNull(result.ServerTimestamp);
            AssertEqualValue(expected, result.Value);

            Assert.Equal(JTokenType.Array, result.Value.Type);
            if (((JArray)result.Value).Count == 0) {
                return;
            }

            Assert.Equal(JTokenType.Integer, ((JArray)result.Value)[0].Type);
            Assert.Equal("SByte", result.DataType);
        }


        public async Task NodeReadStaticArrayByteValueVariableTestAsync() {

            var browser = _services();
            var node = "http://test.org/UA/Data/#i=10302";
            var expected = await _readExpected(_endpoint, node);

            // Act
            var result = await browser.NodeValueReadAsync(_endpoint,
                new ValueReadRequestModel {
                    NodeId = node
                });

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Value);
            Assert.NotNull(result.SourceTimestamp);
            Assert.NotNull(result.ServerTimestamp);
            AssertEqualValue(expected, result.Value);

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



        public async Task NodeReadStaticArrayInt16ValueVariableTestAsync() {

            var browser = _services();
            var node = "http://test.org/UA/Data/#i=10303";
            var expected = await _readExpected(_endpoint, node);

            // Act
            var result = await browser.NodeValueReadAsync(_endpoint,
                new ValueReadRequestModel {
                    NodeId = node
                });

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Value);
            Assert.NotNull(result.SourceTimestamp);
            Assert.NotNull(result.ServerTimestamp);
            AssertEqualValue(expected, result.Value);

            Assert.Equal(JTokenType.Array, result.Value.Type);
            if (((JArray)result.Value).Count == 0) {
                return;
            }

            Assert.Equal(JTokenType.Integer, ((JArray)result.Value)[0].Type);
            Assert.Equal("Int16", result.DataType);
        }


        public async Task NodeReadStaticArrayUInt16ValueVariableTestAsync() {

            var browser = _services();
            var node = "http://test.org/UA/Data/#i=10304";
            var expected = await _readExpected(_endpoint, node);

            // Act
            var result = await browser.NodeValueReadAsync(_endpoint,
                new ValueReadRequestModel {
                    NodeId = node
                });

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Value);
            Assert.NotNull(result.SourceTimestamp);
            Assert.NotNull(result.ServerTimestamp);
            AssertEqualValue(expected, result.Value);

            Assert.Equal(JTokenType.Array, result.Value.Type);
            if (((JArray)result.Value).Count == 0) {
                return;
            }

            Assert.Equal(JTokenType.Integer, ((JArray)result.Value)[0].Type);
            Assert.Equal("UInt16", result.DataType);
        }


        public async Task NodeReadStaticArrayInt32ValueVariableTestAsync() {

            var browser = _services();
            var node = "http://test.org/UA/Data/#i=10305";
            var expected = await _readExpected(_endpoint, node);

            // Act
            var result = await browser.NodeValueReadAsync(_endpoint,
                new ValueReadRequestModel {
                    NodeId = node
                });

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Value);
            Assert.NotNull(result.SourceTimestamp);
            Assert.NotNull(result.ServerTimestamp);
            AssertEqualValue(expected, result.Value);

            Assert.Equal(JTokenType.Array, result.Value.Type);
            if (((JArray)result.Value).Count == 0) {
                return;
            }

            Assert.Equal(JTokenType.Integer, ((JArray)result.Value)[0].Type);
            Assert.Equal("Int32", result.DataType);
        }


        public async Task NodeReadStaticArrayUInt32ValueVariableTestAsync() {

            var browser = _services();
            var node = "http://test.org/UA/Data/#i=10306";
            var expected = await _readExpected(_endpoint, node);

            // Act
            var result = await browser.NodeValueReadAsync(_endpoint,
                new ValueReadRequestModel {
                    NodeId = node
                });

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Value);
            Assert.NotNull(result.SourceTimestamp);
            Assert.NotNull(result.ServerTimestamp);
            AssertEqualValue(expected, result.Value);

            Assert.Equal(JTokenType.Array, result.Value.Type);
            if (((JArray)result.Value).Count == 0) {
                return;
            }

            Assert.Equal(JTokenType.Integer, ((JArray)result.Value)[0].Type);
            Assert.Equal("UInt32", result.DataType);
        }


        public async Task NodeReadStaticArrayInt64ValueVariableTestAsync() {

            var browser = _services();
            var node = "http://test.org/UA/Data/#i=10307";
            var expected = await _readExpected(_endpoint, node);

            // Act
            var result = await browser.NodeValueReadAsync(_endpoint,
                new ValueReadRequestModel {
                    NodeId = node
                });

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Value);
            Assert.NotNull(result.SourceTimestamp);
            Assert.NotNull(result.ServerTimestamp);
            AssertEqualValue(expected, result.Value);

            Assert.Equal(JTokenType.Array, result.Value.Type);
            if (((JArray)result.Value).Count == 0) {
                return;
            }

            Assert.Equal(JTokenType.Integer, ((JArray)result.Value)[0].Type);
            Assert.Equal("Int64", result.DataType);
        }


        public async Task NodeReadStaticArrayUInt64ValueVariableTestAsync() {

            var browser = _services();
            var node = "http://test.org/UA/Data/#i=10308";
            var expected = await _readExpected(_endpoint, node);

            // Act
            var result = await browser.NodeValueReadAsync(_endpoint,
                new ValueReadRequestModel {
                    NodeId = node
                });

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Value);
            Assert.NotNull(result.SourceTimestamp);
            Assert.NotNull(result.ServerTimestamp);
            AssertEqualValue(expected, result.Value);

            Assert.Equal(JTokenType.Array, result.Value.Type);
            if (((JArray)result.Value).Count == 0) {
                return;
            }

            Assert.Equal(JTokenType.Integer, ((JArray)result.Value)[0].Type);
            Assert.Equal("UInt64", result.DataType);
        }


        public async Task NodeReadStaticArrayFloatValueVariableTestAsync() {

            var browser = _services();
            var node = "http://test.org/UA/Data/#i=10309";
            var expected = await _readExpected(_endpoint, node);

            // Act
            var result = await browser.NodeValueReadAsync(_endpoint,
                new ValueReadRequestModel {
                    NodeId = node
                });

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Value);
            Assert.NotNull(result.SourceTimestamp);
            Assert.NotNull(result.ServerTimestamp);
            AssertEqualValue(expected, result.Value);

            Assert.Equal(JTokenType.Array, result.Value.Type);
            if (((JArray)result.Value).Count == 0) {
                return;
            }

            Assert.True(((JArray)result.Value)[0].IsFloatValue(), $"First is {result.Value}");
            Assert.Equal("Float", result.DataType);
        }


        public async Task NodeReadStaticArrayDoubleValueVariableTestAsync() {

            var browser = _services();
            var node = "http://test.org/UA/Data/#i=10310";
            var expected = await _readExpected(_endpoint, node);

            // Act
            var result = await browser.NodeValueReadAsync(_endpoint,
                new ValueReadRequestModel {
                    NodeId = node
                });

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Value);
            Assert.NotNull(result.SourceTimestamp);
            Assert.NotNull(result.ServerTimestamp);
            AssertEqualValue(expected, result.Value);

            Assert.Equal(JTokenType.Array, result.Value.Type);
            if (((JArray)result.Value).Count == 0) {
                return;
            }

            Assert.True(((JArray)result.Value)[0].IsFloatValue());
            Assert.Equal("Double", result.DataType);
        }


        public async Task NodeReadStaticArrayStringValueVariableTestAsync() {

            var browser = _services();
            var node = "http://test.org/UA/Data/#i=10311";
            var expected = await _readExpected(_endpoint, node);

            // Act
            var result = await browser.NodeValueReadAsync(_endpoint,
                new ValueReadRequestModel {
                    NodeId = node
                });

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Value);
            Assert.NotNull(result.SourceTimestamp);
            Assert.NotNull(result.ServerTimestamp);
            AssertEqualValue(expected, result.Value);

            Assert.Equal(JTokenType.Array, result.Value.Type);
            if (((JArray)result.Value).Count == 0) {
                return;
            }

            Assert.Equal(JTokenType.String, ((JArray)result.Value)[0].Type);
            Assert.Equal("String", result.DataType);
        }


        public async Task NodeReadStaticArrayDateTimeValueVariableTestAsync() {

            var browser = _services();
            var node = "http://test.org/UA/Data/#i=10312";
            var expected = await _readExpected(_endpoint, node);

            // Act
            var result = await browser.NodeValueReadAsync(_endpoint,
                new ValueReadRequestModel {
                    NodeId = node
                });

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Value);
            Assert.NotNull(result.SourceTimestamp);
            Assert.NotNull(result.ServerTimestamp);
            AssertEqualValue(expected, result.Value);

            Assert.Equal(JTokenType.Array, result.Value.Type);
            if (((JArray)result.Value).Count == 0) {
                return;
            }

            Assert.Equal(JTokenType.Date, ((JArray)result.Value)[0].Type);
            Assert.Equal("DateTime", result.DataType);
        }


        public async Task NodeReadStaticArrayGuidValueVariableTestAsync() {

            var browser = _services();
            var node = "http://test.org/UA/Data/#i=10313";
            var expected = await _readExpected(_endpoint, node);

            // Act
            var result = await browser.NodeValueReadAsync(_endpoint,
                new ValueReadRequestModel {
                    NodeId = node
                });

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Value);
            Assert.NotNull(result.SourceTimestamp);
            Assert.NotNull(result.ServerTimestamp);
            AssertEqualValue(expected, result.Value);

            Assert.Equal(JTokenType.Array, result.Value.Type);
            if (((JArray)result.Value).Count == 0) {
                return;
            }

            Assert.Equal(JTokenType.String, ((JArray)result.Value)[0].Type);
            // TODO: Assert.Equal(JTokenType.Guid, ((JArray)result.Value)[0].Type);
            Assert.Equal("Guid", result.DataType);
        }


        public async Task NodeReadStaticArrayByteStringValueVariableTestAsync() {

            var browser = _services();
            var node = "http://test.org/UA/Data/#i=10314";
            var expected = await _readExpected(_endpoint, node);

            // Act
            var result = await browser.NodeValueReadAsync(_endpoint,
                new ValueReadRequestModel {
                    NodeId = node
                });

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Value);
            Assert.NotNull(result.SourceTimestamp);
            Assert.NotNull(result.ServerTimestamp);
            AssertEqualValue(expected, result.Value);

            if (result.Value.Type == JTokenType.Null) {
                return;
            }
            Assert.Equal(JTokenType.Array, result.Value.Type);
            if (((JArray)result.Value).Count == 0) {
                return;
            }
            // TODO: Can be null.  Assert.Equal(JTokenType.String, ((JArray)result.Value)[0].Type);
            // TODO:  Assert.Equal(JTokenType.Bytes, ((JArray)result.Value)[0].Type);
            Assert.Equal("ByteString", result.DataType);
        }


        public async Task NodeReadStaticArrayXmlElementValueVariableTestAsync() {

            var browser = _services();
            var node = "http://test.org/UA/Data/#i=10315";
            var expected = await _readExpected(_endpoint, node);

            // Act
            var result = await browser.NodeValueReadAsync(_endpoint,
                new ValueReadRequestModel {
                    NodeId = node
                });

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Value);
            Assert.NotNull(result.SourceTimestamp);
            Assert.NotNull(result.ServerTimestamp);
            AssertEqualValue(expected, result.Value);

            Assert.Equal(JTokenType.Array, result.Value.Type);
            if (((JArray)result.Value).Count == 0) {
                return;
            }

            Assert.Equal(JTokenType.Object, ((JArray)result.Value)[0].Type);
            Assert.Equal("XmlElement", result.DataType);
        }


        public async Task NodeReadStaticArrayNodeIdValueVariableTestAsync() {

            var browser = _services();
            var node = "http://test.org/UA/Data/#i=10316";
            var expected = await _readExpected(_endpoint, node);

            // Act
            var result = await browser.NodeValueReadAsync(_endpoint,
                new ValueReadRequestModel {
                    NodeId = node
                });

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Value);
            Assert.NotNull(result.SourceTimestamp);
            Assert.NotNull(result.ServerTimestamp);
            AssertEqualValue(expected, result.Value);

            Assert.Equal(JTokenType.Array, result.Value.Type);
            if (((JArray)result.Value).Count == 0) {
                return;
            }

            Assert.Equal(JTokenType.String, ((JArray)result.Value)[0].Type);
            Assert.Equal("NodeId", result.DataType);
        }


        public async Task NodeReadStaticArrayExpandedNodeIdValueVariableTestAsync() {

            var browser = _services();
            var node = "http://test.org/UA/Data/#i=10317";
            var expected = await _readExpected(_endpoint, node);

            // Act
            var result = await browser.NodeValueReadAsync(_endpoint,
                new ValueReadRequestModel {
                    NodeId = node
                });

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Value);
            Assert.NotNull(result.SourceTimestamp);
            Assert.NotNull(result.ServerTimestamp);
            AssertEqualValue(expected, result.Value);

            Assert.Equal(JTokenType.Array, result.Value.Type);
            if (((JArray)result.Value).Count == 0) {
                return;
            }

            Assert.Equal(JTokenType.String, ((JArray)result.Value)[0].Type);
            Assert.Equal("ExpandedNodeId", result.DataType);
        }


        public async Task NodeReadStaticArrayQualifiedNameValueVariableTestAsync() {

            var browser = _services();
            var node = "http://test.org/UA/Data/#i=10318";
            var expected = await _readExpected(_endpoint, node);

            // Act
            var result = await browser.NodeValueReadAsync(_endpoint,
                new ValueReadRequestModel {
                    NodeId = node
                });

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Value);
            Assert.NotNull(result.SourceTimestamp);
            Assert.NotNull(result.ServerTimestamp);
            AssertEqualValue(expected, result.Value);

            Assert.Equal(JTokenType.Array, result.Value.Type);
            if (((JArray)result.Value).Count == 0) {
                return;
            }

            Assert.Equal(JTokenType.String, ((JArray)result.Value)[0].Type);
            Assert.Equal("QualifiedName", result.DataType);
        }


        public async Task NodeReadStaticArrayLocalizedTextValueVariableTestAsync() {

            var browser = _services();
            var node = "http://test.org/UA/Data/#i=10319";
            var expected = await _readExpected(_endpoint, node);

            // Act
            var result = await browser.NodeValueReadAsync(_endpoint,
                new ValueReadRequestModel {
                    NodeId = node
                });

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Value);
            Assert.NotNull(result.SourceTimestamp);
            Assert.NotNull(result.ServerTimestamp);
            AssertEqualValue(expected, result.Value);

            Assert.Equal(JTokenType.Array, result.Value.Type);
            if (((JArray)result.Value).Count == 0) {
                return;
            }

            Assert.Equal(JTokenType.Object, ((JArray)result.Value)[0].Type);
            Assert.Equal("LocalizedText", result.DataType);
        }


        public async Task NodeReadStaticArrayStatusCodeValueVariableTestAsync() {

            var browser = _services();
            var node = "http://test.org/UA/Data/#i=10320";
            var expected = await _readExpected(_endpoint, node);

            // Act
            var result = await browser.NodeValueReadAsync(_endpoint,
                new ValueReadRequestModel {
                    NodeId = node
                });

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Value);
            Assert.NotNull(result.SourceTimestamp);
            Assert.NotNull(result.ServerTimestamp);
            AssertEqualValue(expected, result.Value);

            Assert.Equal(JTokenType.Array, result.Value.Type);
            if (((JArray)result.Value).Count == 0) {
                return;
            }
            Assert.True(
               ((JArray)result.Value)[0].Type == JTokenType.Object ||
               ((JArray)result.Value)[0].Type == JTokenType.Integer);
            Assert.Equal("StatusCode", result.DataType);
        }


        public async Task NodeReadStaticArrayVariantValueVariableTestAsync() {

            var browser = _services();
            var node = "http://test.org/UA/Data/#i=10321";
            var expected = await _readExpected(_endpoint, node);

            // Act
            var result = await browser.NodeValueReadAsync(_endpoint,
                new ValueReadRequestModel {
                    NodeId = node
                });

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Value);
            Assert.NotNull(result.SourceTimestamp);
            Assert.NotNull(result.ServerTimestamp);
            AssertEqualValue(expected, result.Value);

            Assert.Equal(JTokenType.Array, result.Value.Type);
            if (((JArray)result.Value).Count == 0) {
                return;
            }
        }


        public async Task NodeReadStaticArrayEnumerationValueVariableTestAsync() {

            var browser = _services();
            var node = "http://test.org/UA/Data/#i=10322";
            var expected = await _readExpected(_endpoint, node);

            // Act
            var result = await browser.NodeValueReadAsync(_endpoint,
                new ValueReadRequestModel {
                    NodeId = node
                });

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Value);
            Assert.NotNull(result.SourceTimestamp);
            Assert.NotNull(result.ServerTimestamp);
            AssertEqualValue(expected, result.Value);

            Assert.Equal(JTokenType.Array, result.Value.Type);
            if (((JArray)result.Value).Count == 0) {
                return;
            }

            Assert.Equal(JTokenType.Integer, ((JArray)result.Value)[0].Type);
            Assert.Equal("Int32", result.DataType);
        }


        public async Task NodeReadStaticArrayStructureValueVariableTestAsync() {

            var browser = _services();
            var node = "http://test.org/UA/Data/#i=10323";
            var expected = await _readExpected(_endpoint, node);

            // Act
            var result = await browser.NodeValueReadAsync(_endpoint,
                new ValueReadRequestModel {
                    NodeId = node
                });

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Value);
            Assert.NotNull(result.SourceTimestamp);
            Assert.NotNull(result.ServerTimestamp);
            AssertEqualValue(expected, result.Value);

            Assert.Equal(JTokenType.Array, result.Value.Type);
            if (((JArray)result.Value).Count == 0) {
                return;
            }

            Assert.Equal(JTokenType.Object, ((JArray)result.Value)[0].Type);
            // TODO: Assert.Equal(JTokenType.Bytes, ((JArray)result.Value)[0].Type);
            Assert.Equal("ExtensionObject", result.DataType);
        }


        public async Task NodeReadStaticArrayNumberValueVariableTestAsync() {

            var browser = _services();
            var node = "http://test.org/UA/Data/#i=10324";
            var expected = await _readExpected(_endpoint, node);

            // Act
            var result = await browser.NodeValueReadAsync(_endpoint,
                new ValueReadRequestModel {
                    NodeId = node
                });

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Value);
            Assert.NotNull(result.SourceTimestamp);
            Assert.NotNull(result.ServerTimestamp);
            AssertEqualValue(expected, result.Value);

            if (result.Value.Type == JTokenType.String) {
                Assert.NotEmpty(((string)result.Value).DecodeAsBase64());
                return;
            }
            Assert.Equal(JTokenType.Array, result.Value.Type);
            if (((JArray)result.Value).Count == 0) {
                return;
            }
            var type = ((JArray)result.Value)[0].Type;
            Assert.True(type == JTokenType.Integer ||
                ((JArray)result.Value)[0].IsFloatValue(), $"Got bad type {type}");
        }


        public async Task NodeReadStaticArrayIntegerValueVariableTestAsync() {

            var browser = _services();
            var node = "http://test.org/UA/Data/#i=10325";
            var expected = await _readExpected(_endpoint, node);

            // Act
            var result = await browser.NodeValueReadAsync(_endpoint,
                new ValueReadRequestModel {
                    NodeId = node
                });

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Value);
            Assert.NotNull(result.SourceTimestamp);
            Assert.NotNull(result.ServerTimestamp);
            AssertEqualValue(expected, result.Value);

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


        public async Task NodeReadStaticArrayUIntegerValueVariableTestAsync() {

            var browser = _services();
            var node = "http://test.org/UA/Data/#i=10326";
            var expected = await _readExpected(_endpoint, node);

            // Act
            var result = await browser.NodeValueReadAsync(_endpoint,
                new ValueReadRequestModel {
                    NodeId = node
                });

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Value);
            Assert.NotNull(result.SourceTimestamp);
            Assert.NotNull(result.ServerTimestamp);
            AssertEqualValue(expected, result.Value);

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


        /// <summary>
        /// Helper to compare equal value
        /// </summary>
        /// <param name="expected"></param>
        /// <param name="value"></param>
        private static void AssertEqualValue(JToken expected, JToken value) {
            value = value ?? JValue.CreateNull();
            expected = expected ?? JValue.CreateNull();
            Assert.True(JToken.DeepEquals(expected, value),
                $"Expected: {expected} ({expected?.Type}) != Actual: {value} ({value?.Type})");
        }

        private readonly T _endpoint;
        private readonly Func<T, string, Task<JToken>> _readExpected;
        private readonly Func<INodeServices<T>> _services;
    }
}
