// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Testing.Tests
{
    using Azure.IIoT.OpcUa.Models;
    using Furly.Extensions.Serializers;
    using Furly.Extensions.Serializers.Json;
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using System.Xml;
    using Xunit;

    public class ReadScalarValueTests<T>
    {
        /// <summary>
        /// Create node services tests
        /// </summary>
        /// <param name="services"></param>
        /// <param name="connection"></param>
        /// <param name="readExpected"></param>
        public ReadScalarValueTests(Func<INodeServices<T>> services, T connection,
            Func<T, string, IJsonSerializer, Task<VariantValue>> readExpected)
        {
            _services = services;
            _connection = connection;
            _serializer = new DefaultJsonSerializer();
            _readExpected = readExpected;
        }

        public async Task NodeReadAllStaticScalarVariableNodeClassTest1Async()
        {
            var browser = _services();
            const Opc.Ua.NodeClass expected = Opc.Ua.NodeClass.Variable;

            var attributes = new List<AttributeReadRequestModel>();
            for (var i = 10216; i < 10243; i++)
            {
                attributes.Add(new AttributeReadRequestModel
                {
                    Attribute = NodeAttribute.NodeClass,
                    NodeId = "http://test.org/UA/Data/#i=" + i
                });
            }

            // Act
            var result = await browser.ReadAsync(_connection,
                new ReadRequestModel
                {
                    Header = new RequestHeaderModel
                    {
                        Diagnostics = new DiagnosticsModel
                        {
                            AuditId = nameof(NodeReadAllStaticScalarVariableNodeClassTest1Async),
                            TimeStamp = DateTime.Now
                        }
                    },
                    Attributes = attributes
                }).ConfigureAwait(false);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Results);
            Assert.Equal(attributes.Count, result.Results.Count);
            Assert.All(result.Results, r => Assert.Null(r.ErrorInfo));
            Assert.All(result.Results, r => Assert.Equal((int)expected, (int)r.Value));
        }

        public async Task NodeReadAllStaticScalarVariableAccessLevelTest1Async()
        {
            var browser = _services();
            const byte expected = Opc.Ua.AccessLevels.CurrentReadOrWrite;

            var attributes = new List<AttributeReadRequestModel>();
            for (var i = 10216; i < 10243; i++)
            {
                attributes.Add(new AttributeReadRequestModel
                {
                    Attribute = NodeAttribute.AccessLevel,
                    NodeId = "http://test.org/UA/Data/#i=" + i
                });
            }

            // Act
            var result = await browser.ReadAsync(_connection,
                new ReadRequestModel
                {
                    Header = new RequestHeaderModel
                    {
                        Diagnostics = new DiagnosticsModel
                        {
                            AuditId = nameof(NodeReadAllStaticScalarVariableAccessLevelTest1Async),
                            TimeStamp = DateTime.Now
                        }
                    },
                    Attributes = attributes
                }).ConfigureAwait(false);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Results);
            Assert.Equal(attributes.Count, result.Results.Count);
            Assert.All(result.Results, r => Assert.Null(r.ErrorInfo));
            Assert.All(result.Results, r => Assert.Equal(expected, (int)r.Value));
        }

        public async Task NodeReadAllStaticScalarVariableWriteMaskTest1Async()
        {
            var browser = _services();

            var attributes = new List<AttributeReadRequestModel>();
            for (var i = 10216; i < 10243; i++)
            {
                attributes.Add(new AttributeReadRequestModel
                {
                    Attribute = NodeAttribute.WriteMask,
                    NodeId = "http://test.org/UA/Data/#i=" + i
                });
            }

            // Act
            var result = await browser.ReadAsync(_connection,
                new ReadRequestModel
                {
                    Header = new RequestHeaderModel
                    {
                        Diagnostics = new DiagnosticsModel
                        {
                            AuditId = nameof(NodeReadAllStaticScalarVariableWriteMaskTest1Async),
                            TimeStamp = DateTime.Now
                        }
                    },
                    Attributes = attributes
                }).ConfigureAwait(false);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Results);
            Assert.Equal(attributes.Count, result.Results.Count);
            Assert.All(result.Results, r => Assert.Null(r.ErrorInfo));
            Assert.All(result.Results, r => Assert.Equal(0, (int)r.Value));
        }

        public async Task NodeReadAllStaticScalarVariableWriteMaskTest2Async()
        {
            var browser = _services();

            var attributes = new List<AttributeReadRequestModel>();
            for (var i = 10216; i < 10243; i++)
            {
                attributes.Add(new AttributeReadRequestModel
                {
                    Attribute = NodeAttribute.WriteMask,
                    NodeId = "http://test.org/UA/Data/#i=10216"
                });
            }

            // Act
            var result = await browser.ReadAsync(_connection,
                new ReadRequestModel
                {
                    Header = new RequestHeaderModel
                    {
                        Diagnostics = new DiagnosticsModel
                        {
                            AuditId = nameof(NodeReadAllStaticScalarVariableWriteMaskTest2Async),
                            TimeStamp = DateTime.Now
                        }
                    },
                    Attributes = attributes
                }).ConfigureAwait(false);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Results);
            Assert.Equal(attributes.Count, result.Results.Count);
            Assert.All(result.Results, r => Assert.Null(r.ErrorInfo));
            Assert.All(result.Results, r => Assert.Equal(0, (int)r.Value));
        }

        public async Task NodeReadStaticScalarBooleanValueVariableTestAsync()
        {
            var browser = _services();
            const string node = "http://test.org/UA/Data/#i=10216";
            var expected = await _readExpected(_connection, node, _serializer).ConfigureAwait(false);

            // Act
            var result = await browser.ValueReadAsync(_connection,
                new ValueReadRequestModel
                {
                    Header = new RequestHeaderModel
                    {
                        Diagnostics = new DiagnosticsModel
                        {
                            AuditId = nameof(NodeReadStaticScalarBooleanValueVariableTestAsync),
                            TimeStamp = DateTime.Now
                        }
                    },
                    NodeId = node
                }).ConfigureAwait(false);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result);
            Assert.NotNull(result.SourceTimestamp);
            Assert.NotNull(result.ServerTimestamp);
            Assert.True(result.Value.IsBoolean);
            Assert.True(VariantValue.DeepEquals(expected, result.Value),
                $"Expected: {expected} != Actual: {result.Value}");
            Assert.Equal("Boolean", result.DataType);
        }

        public async Task NodeReadStaticScalarBooleanValueVariableWithBrowsePathTest1Async()
        {
            var browser = _services();
            const string node = "http://test.org/UA/Data/#i=10159"; // Scalar
            var path = new[] {
                ".http://test.org/UA/Data/#BooleanValue"
            };
            var expected = await _readExpected(_connection, "http://test.org/UA/Data/#i=10216", _serializer).ConfigureAwait(false);

            // Act
            var result = await browser.ValueReadAsync(_connection,
                new ValueReadRequestModel
                {
                    Header = new RequestHeaderModel
                    {
                        Diagnostics = new DiagnosticsModel
                        {
                            AuditId = nameof(NodeReadStaticScalarBooleanValueVariableTestAsync),
                            TimeStamp = DateTime.Now
                        }
                    },
                    NodeId = node,
                    BrowsePath = path
                }).ConfigureAwait(false);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.SourceTimestamp);
            Assert.NotNull(result.ServerTimestamp);
            Assert.True(result.Value.IsBoolean);
            AssertEqualValue(expected, result.Value);
            Assert.Equal("Boolean", result.DataType);
        }

        public async Task NodeReadStaticScalarBooleanValueVariableWithBrowsePathTest2Async()
        {
            var browser = _services();
            const string node = "http://test.org/UA/Data/#i=10159"; // Scalar
            var path = new[] {
                "http://test.org/UA/Data/#BooleanValue"
            };
            var expected = await _readExpected(_connection, "http://test.org/UA/Data/#i=10216", _serializer).ConfigureAwait(false);

            // Act
            var result = await browser.ValueReadAsync(_connection,
                new ValueReadRequestModel
                {
                    Header = new RequestHeaderModel
                    {
                        Diagnostics = new DiagnosticsModel
                        {
                            AuditId = nameof(NodeReadStaticScalarBooleanValueVariableTestAsync),
                            TimeStamp = DateTime.Now
                        }
                    },
                    NodeId = node,
                    BrowsePath = path
                }).ConfigureAwait(false);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.SourceTimestamp);
            Assert.NotNull(result.ServerTimestamp);
            Assert.True(result.Value.IsBoolean);
            AssertEqualValue(expected, result.Value);
            Assert.Equal("Boolean", result.DataType);
        }

        public async Task NodeReadStaticScalarBooleanValueVariableWithBrowsePathTest3Async()
        {
            var browser = _services();
            var path = new[] {
                "Objects",
                "http://test.org/UA/Data/#Data",
                "http://test.org/UA/Data/#Static",
                "http://test.org/UA/Data/#Scalar",
                "http://test.org/UA/Data/#BooleanValue"
            };
            var expected = await _readExpected(_connection, "http://test.org/UA/Data/#i=10216", _serializer).ConfigureAwait(false);

            // Act
            var result = await browser.ValueReadAsync(_connection,
                new ValueReadRequestModel
                {
                    Header = new RequestHeaderModel
                    {
                        Diagnostics = new DiagnosticsModel
                        {
                            AuditId = nameof(NodeReadStaticScalarBooleanValueVariableTestAsync),
                            TimeStamp = DateTime.Now
                        }
                    },
                    BrowsePath = path
                }).ConfigureAwait(false);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.SourceTimestamp);
            Assert.NotNull(result.ServerTimestamp);
            Assert.True(result.Value.IsBoolean);
            AssertEqualValue(expected, result.Value);
            Assert.Equal("Boolean", result.DataType);
        }

        public async Task NodeReadStaticScalarSByteValueVariableTestAsync()
        {
            var browser = _services();
            const string node = "http://test.org/UA/Data/#i=10217";
            var expected = await _readExpected(_connection, node, _serializer).ConfigureAwait(false);

            // Act
            var result = await browser.ValueReadAsync(_connection,
                new ValueReadRequestModel
                {
                    NodeId = node
                }).ConfigureAwait(false);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.SourceTimestamp);
            Assert.NotNull(result.ServerTimestamp);
            Assert.True(result.Value.IsInteger);
            AssertEqualValue(expected, result.Value);
            Assert.Equal("SByte", result.DataType);
        }

        public async Task NodeReadStaticScalarByteValueVariableTestAsync()
        {
            var browser = _services();
            const string node = "http://test.org/UA/Data/#i=10218";
            var expected = await _readExpected(_connection, node, _serializer).ConfigureAwait(false);

            // Act
            var result = await browser.ValueReadAsync(_connection,
                new ValueReadRequestModel
                {
                    NodeId = node
                }).ConfigureAwait(false);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.SourceTimestamp);
            Assert.NotNull(result.ServerTimestamp);
            Assert.True(result.Value.IsInteger);
            AssertEqualValue(expected, result.Value);
            Assert.Equal("Byte", result.DataType);
        }

        public async Task NodeReadStaticScalarInt16ValueVariableTestAsync()
        {
            var browser = _services();
            const string node = "http://test.org/UA/Data/#i=10219";
            var expected = await _readExpected(_connection, node, _serializer).ConfigureAwait(false);

            // Act
            var result = await browser.ValueReadAsync(_connection,
                new ValueReadRequestModel
                {
                    NodeId = node
                }).ConfigureAwait(false);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.SourceTimestamp);
            Assert.NotNull(result.ServerTimestamp);
            Assert.True(result.Value.IsInteger);
            AssertEqualValue(expected, result.Value);
            Assert.Equal("Int16", result.DataType);
        }

        public async Task NodeReadStaticScalarUInt16ValueVariableTestAsync()
        {
            var browser = _services();
            const string node = "http://test.org/UA/Data/#i=10220";
            var expected = await _readExpected(_connection, node, _serializer).ConfigureAwait(false);

            // Act
            var result = await browser.ValueReadAsync(_connection,
                new ValueReadRequestModel
                {
                    Header = new RequestHeaderModel
                    {
                        Diagnostics = new DiagnosticsModel
                        {
                            AuditId = nameof(NodeReadStaticScalarUInt16ValueVariableTestAsync),
                            TimeStamp = DateTime.Now
                        }
                    },
                    NodeId = node
                }).ConfigureAwait(false);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.SourceTimestamp);
            Assert.NotNull(result.ServerTimestamp);
            Assert.True(result.Value.IsInteger);
            AssertEqualValue(expected, result.Value);
            Assert.Equal("UInt16", result.DataType);
        }

        public async Task NodeReadStaticScalarInt32ValueVariableTestAsync()
        {
            var browser = _services();
            const string node = "http://test.org/UA/Data/#i=10221";
            var expected = await _readExpected(_connection, node, _serializer).ConfigureAwait(false);

            // Act
            var result = await browser.ValueReadAsync(_connection,
                new ValueReadRequestModel
                {
                    NodeId = node
                }).ConfigureAwait(false);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.SourceTimestamp);
            Assert.NotNull(result.ServerTimestamp);
            Assert.True(result.Value.IsInteger);
            AssertEqualValue(expected, result.Value);
            Assert.Equal("Int32", result.DataType);
        }

        public async Task NodeReadStaticScalarUInt32ValueVariableTestAsync()
        {
            var browser = _services();
            const string node = "http://test.org/UA/Data/#i=10222";
            var expected = await _readExpected(_connection, node, _serializer).ConfigureAwait(false);

            // Act
            var result = await browser.ValueReadAsync(_connection,
                new ValueReadRequestModel
                {
                    NodeId = node
                }).ConfigureAwait(false);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.SourceTimestamp);
            Assert.NotNull(result.ServerTimestamp);
            Assert.True(result.Value.IsInteger);
            AssertEqualValue(expected, result.Value);
            Assert.Equal("UInt32", result.DataType);
        }

        public async Task NodeReadStaticScalarInt64ValueVariableTestAsync()
        {
            var browser = _services();
            const string node = "http://test.org/UA/Data/#i=10223";
            var expected = await _readExpected(_connection, node, _serializer).ConfigureAwait(false);

            // Act
            var result = await browser.ValueReadAsync(_connection,
                new ValueReadRequestModel
                {
                    NodeId = node
                }).ConfigureAwait(false);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.SourceTimestamp);
            Assert.NotNull(result.ServerTimestamp);
            Assert.True(result.Value.IsInteger);
            AssertEqualValue(expected, result.Value);
            Assert.Equal("Int64", result.DataType);
        }

        public async Task NodeReadStaticScalarUInt64ValueVariableTestAsync()
        {
            var browser = _services();
            const string node = "http://test.org/UA/Data/#i=10224";
            var expected = await _readExpected(_connection, node, _serializer).ConfigureAwait(false);

            // Act
            var result = await browser.ValueReadAsync(_connection,
                new ValueReadRequestModel
                {
                    NodeId = node
                }).ConfigureAwait(false);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.SourceTimestamp);
            Assert.NotNull(result.ServerTimestamp);
            Assert.True(result.Value.IsInteger);
            AssertEqualValue(expected, result.Value);
            Assert.Equal("UInt64", result.DataType);
        }

        public async Task NodeReadStaticScalarFloatValueVariableTestAsync()
        {
            var browser = _services();
            const string node = "http://test.org/UA/Data/#i=10225";
            var expected = await _readExpected(_connection, node, _serializer).ConfigureAwait(false);

            // Act
            var result = await browser.ValueReadAsync(_connection,
                new ValueReadRequestModel
                {
                    NodeId = node
                }).ConfigureAwait(false);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.SourceTimestamp);
            Assert.NotNull(result.ServerTimestamp);
            Assert.True(result.Value.IsFloat);
            AssertEqualValue(expected, result.Value);
            Assert.Equal("Float", result.DataType);
        }

        public async Task NodeReadStaticScalarDoubleValueVariableTestAsync()
        {
            var browser = _services();
            const string node = "http://test.org/UA/Data/#i=10226";
            var expected = await _readExpected(_connection, node, _serializer).ConfigureAwait(false);

            // Act
            var result = await browser.ValueReadAsync(_connection,
                new ValueReadRequestModel
                {
                    NodeId = node
                }).ConfigureAwait(false);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.SourceTimestamp);
            Assert.NotNull(result.ServerTimestamp);
            Assert.True(result.Value.IsDouble);
            AssertEqualValue(expected, result.Value);
            Assert.Equal("Double", result.DataType);
        }

        public async Task NodeReadStaticScalarStringValueVariableTestAsync()
        {
            var browser = _services();
            const string node = "http://test.org/UA/Data/#i=10227";
            var expected = await _readExpected(_connection, node, _serializer).ConfigureAwait(false);

            // Act
            var result = await browser.ValueReadAsync(_connection,
                new ValueReadRequestModel
                {
                    NodeId = node
                }).ConfigureAwait(false);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.SourceTimestamp);
            Assert.NotNull(result.ServerTimestamp);
            Assert.True(result.Value.IsString);
            AssertEqualValue(expected, result.Value);
            Assert.Equal("String", result.DataType);
        }

        public async Task NodeReadStaticScalarDateTimeValueVariableTestAsync()
        {
            var browser = _services();
            const string node = "http://test.org/UA/Data/#i=10228";
            var expected = await _readExpected(_connection, node, _serializer).ConfigureAwait(false);

            // Act
            var result = await browser.ValueReadAsync(_connection,
                new ValueReadRequestModel
                {
                    NodeId = node
                }).ConfigureAwait(false);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.SourceTimestamp);
            Assert.NotNull(result.ServerTimestamp);
            Assert.True(result.Value.IsDateTime);
            AssertEqualValue(expected, result.Value);
            Assert.Equal("DateTime", result.DataType);
        }

        public async Task NodeReadStaticScalarGuidValueVariableTestAsync()
        {
            var browser = _services();
            const string node = "http://test.org/UA/Data/#i=10229";
            var expected = await _readExpected(_connection, node, _serializer).ConfigureAwait(false);

            // Act
            var result = await browser.ValueReadAsync(_connection,
                new ValueReadRequestModel
                {
                    NodeId = node
                }).ConfigureAwait(false);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.SourceTimestamp);
            Assert.NotNull(result.ServerTimestamp);
            Assert.True(result.Value.IsGuid);
            AssertEqualValue(expected, result.Value);
            Assert.Equal("Guid", result.DataType);
        }

        public async Task NodeReadStaticScalarByteStringValueVariableTestAsync()
        {
            var browser = _services();
            const string node = "http://test.org/UA/Data/#i=10230";
            var expected = await _readExpected(_connection, node, _serializer).ConfigureAwait(false);

            // Act
            var result = await browser.ValueReadAsync(_connection,
                new ValueReadRequestModel
                {
                    NodeId = node
                }).ConfigureAwait(false);

            // Assert
            Assert.NotNull(result.SourceTimestamp);
            Assert.NotNull(result.ServerTimestamp);
            AssertEqualValue(expected, result.Value);
            Assert.Equal("ByteString", result.DataType);
            // TODO : Assert.Equal(VariantValueType.Bytes, result.Value.Type);
            // TODO : Assert.Equal(VariantValueType.String, result.Value.Type);
        }

        public async Task NodeReadStaticScalarXmlElementValueVariableTestAsync()
        {
            var browser = _services();
            const string node = "http://test.org/UA/Data/#i=10231";
            var expected = await _readExpected(_connection, node, _serializer).ConfigureAwait(false);

            // Act
            var result = await browser.ValueReadAsync(_connection,
                new ValueReadRequestModel
                {
                    NodeId = node
                }).ConfigureAwait(false);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.SourceTimestamp);
            Assert.NotNull(result.ServerTimestamp);
            Assert.True(result.Value.IsBytes);
            AssertEqualValue(expected, result.Value);
            Assert.Equal("XmlElement", result.DataType);
            var s = result.Value.ToString(null);
            var xml = result.Value.ConvertTo<XmlElement>();
            Assert.NotNull(xml);
        }

        public async Task NodeReadStaticScalarNodeIdValueVariableTestAsync()
        {
            var browser = _services();
            const string node = "http://test.org/UA/Data/#i=10232";
            var expected = await _readExpected(_connection, node, _serializer).ConfigureAwait(false);

            // Act
            var result = await browser.ValueReadAsync(_connection,
                new ValueReadRequestModel
                {
                    NodeId = node
                }).ConfigureAwait(false);

            // Assert
            // Assert.NotNull(result);
            Assert.NotNull(result.SourceTimestamp);
            Assert.NotNull(result.ServerTimestamp);
            Assert.True(result.Value.IsString);
            AssertEqualValue(expected, result.Value);
            Assert.Equal("NodeId", result.DataType);
        }

        public async Task NodeReadStaticScalarExpandedNodeIdValueVariableTestAsync()
        {
            var browser = _services();
            const string node = "http://test.org/UA/Data/#i=10233";
            var expected = await _readExpected(_connection, node, _serializer).ConfigureAwait(false);

            // Act
            var result = await browser.ValueReadAsync(_connection,
                new ValueReadRequestModel
                {
                    NodeId = node
                }).ConfigureAwait(false);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.SourceTimestamp);
            Assert.NotNull(result.ServerTimestamp);
            Assert.True(result.Value.IsString);
            AssertEqualValue(expected, result.Value);
            Assert.Equal("ExpandedNodeId", result.DataType);
        }

        public async Task NodeReadStaticScalarQualifiedNameValueVariableTestAsync()
        {
            var browser = _services();
            const string node = "http://test.org/UA/Data/#i=10234";
            var expected = await _readExpected(_connection, node, _serializer).ConfigureAwait(false);

            // Act
            var result = await browser.ValueReadAsync(_connection,
                new ValueReadRequestModel
                {
                    NodeId = node
                }).ConfigureAwait(false);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.SourceTimestamp);
            Assert.NotNull(result.ServerTimestamp);
            Assert.True(result.Value.IsString);
            AssertEqualValue(expected, result.Value);
            Assert.Equal("QualifiedName", result.DataType);
        }

        public async Task NodeReadStaticScalarLocalizedTextValueVariableTestAsync()
        {
            var browser = _services();
            const string node = "http://test.org/UA/Data/#i=10235";
            var expected = await _readExpected(_connection, node, _serializer).ConfigureAwait(false);

            // Act
            var result = await browser.ValueReadAsync(_connection,
                new ValueReadRequestModel
                {
                    NodeId = node
                }).ConfigureAwait(false);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.SourceTimestamp);
            Assert.NotNull(result.ServerTimestamp);
            Assert.True(result.Value.IsObject);
            AssertEqualValue(expected, result.Value);
            Assert.Equal("LocalizedText", result.DataType);
        }

        public async Task NodeReadStaticScalarStatusCodeValueVariableTestAsync()
        {
            var browser = _services();
            const string node = "http://test.org/UA/Data/#i=10236";
            var expected = await _readExpected(_connection, node, _serializer).ConfigureAwait(false);

            // Act
            var result = await browser.ValueReadAsync(_connection,
                new ValueReadRequestModel
                {
                    NodeId = node
                }).ConfigureAwait(false);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.SourceTimestamp);
            Assert.NotNull(result.ServerTimestamp);
            Assert.True(
                result.Value.IsObject ||
                result.Value.IsInteger);
            AssertEqualValue(expected, result.Value);
            Assert.Equal("StatusCode", result.DataType);
        }

        public async Task NodeReadStaticScalarVariantValueVariableTestAsync()
        {
            var browser = _services();
            const string node = "http://test.org/UA/Data/#i=10237";
            var expected = await _readExpected(_connection, node, _serializer).ConfigureAwait(false);

            // Act
            var result = await browser.ValueReadAsync(_connection,
                new ValueReadRequestModel
                {
                    NodeId = node
                }).ConfigureAwait(false);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.SourceTimestamp);
            Assert.NotNull(result.ServerTimestamp);
            AssertEqualValue(expected, result.Value);
            // Assert.Equal("BaseDataType", result.DataType);
        }

        public async Task NodeReadStaticScalarEnumerationValueVariableTestAsync()
        {
            var browser = _services();
            const string node = "http://test.org/UA/Data/#i=10238";
            var expected = await _readExpected(_connection, node, _serializer).ConfigureAwait(false);

            // Act
            var result = await browser.ValueReadAsync(_connection,
                new ValueReadRequestModel
                {
                    NodeId = node
                }).ConfigureAwait(false);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.SourceTimestamp);
            Assert.NotNull(result.ServerTimestamp);
            Assert.True(result.Value.IsInteger);
            AssertEqualValue(expected, result.Value);
            // TODO: Assert.Equal("Enumeration", result.DataType);
            Assert.Equal("Int32", result.DataType);
        }

        public async Task NodeReadStaticScalarStructuredValueVariableTestAsync()
        {
            var browser = _services();
            const string node = "http://test.org/UA/Data/#i=10239";
            var expected = await _readExpected(_connection, node, _serializer).ConfigureAwait(false);

            // Act
            var result = await browser.ValueReadAsync(_connection,
                new ValueReadRequestModel
                {
                    NodeId = node
                }).ConfigureAwait(false);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.SourceTimestamp);
            Assert.NotNull(result.ServerTimestamp);
            Assert.True(result.Value.IsObject);
            AssertEqualValue(expected, result.Value);
            Assert.Equal("ExtensionObject", result.DataType);
            // TODO: Assert.Equal("Structure", result.DataType);
        }

        public async Task NodeReadStaticScalarNumberValueVariableTestAsync()
        {
            var browser = _services();
            const string node = "http://test.org/UA/Data/#i=10240";
            var expected = await _readExpected(_connection, node, _serializer).ConfigureAwait(false);

            // Act
            var result = await browser.ValueReadAsync(_connection,
                new ValueReadRequestModel
                {
                    NodeId = node
                }).ConfigureAwait(false);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.SourceTimestamp);
            Assert.NotNull(result.ServerTimestamp);
            AssertEqualValue(expected, result.Value);
            // Assert.Equal("Number", result.DataType);
        }

        public async Task NodeReadStaticScalarIntegerValueVariableTestAsync()
        {
            var browser = _services();
            const string node = "http://test.org/UA/Data/#i=10241";
            var expected = await _readExpected(_connection, node, _serializer).ConfigureAwait(false);

            // Act
            var result = await browser.ValueReadAsync(_connection,
                new ValueReadRequestModel
                {
                    NodeId = node
                }).ConfigureAwait(false);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.SourceTimestamp);
            Assert.NotNull(result.ServerTimestamp);
            AssertEqualValue(expected, result.Value);
            // Assert.Equal("Integer", result.DataType);
        }

        public async Task NodeReadStaticScalarUIntegerValueVariableTestAsync()
        {
            var browser = _services();
            const string node = "http://test.org/UA/Data/#i=10242";
            var expected = await _readExpected(_connection, node, _serializer).ConfigureAwait(false);

            // Act
            var result = await browser.ValueReadAsync(_connection,
                new ValueReadRequestModel
                {
                    NodeId = node
                }).ConfigureAwait(false);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.SourceTimestamp);
            Assert.NotNull(result.ServerTimestamp);
            AssertEqualValue(expected, result.Value);
            // Assert.Equal("UInteger", result.DataType);
        }

        public async Task NodeReadDataAccessMeasurementFloatValueTestAsync()
        {
            var browser = _services();
            const string node = "nsu=DataAccess;s=1:FC1001?Measurement";
            var expected = await _readExpected(_connection, node, _serializer).ConfigureAwait(false);

            // Act
            var result = await browser.ValueReadAsync(_connection,
                new ValueReadRequestModel
                {
                    NodeId = node
                }).ConfigureAwait(false);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.SourceTimestamp);
            Assert.NotNull(result.ServerTimestamp);
            AssertEqualValue(expected, result.Value);
            Assert.Equal("Float", result.DataType);
        }

        public async Task NodeReadDiagnosticsNoneTestAsync()
        {
            var browser = _services();

            // Act
            var results = await browser.ValueReadAsync(_connection,
                new ValueReadRequestModel
                {
                    Header = new RequestHeaderModel
                    {
                        Diagnostics = new DiagnosticsModel
                        {
                            AuditId = nameof(NodeReadDiagnosticsNoneTestAsync),
                            Level = DiagnosticsLevel.None
                        }
                    },
                    NodeId = "http://opcfoundation.org/UA/Boiler/#s=unknown"
                }).ConfigureAwait(false);

            // Assert
            Assert.NotNull(results.ErrorInfo);
            Assert.Null(results.ErrorInfo.NamespaceUri);
            Assert.Null(results.ErrorInfo.Locale);
            Assert.Null(results.ErrorInfo.Inner);
            Assert.Null(results.ErrorInfo.AdditionalInfo);
            Assert.Null(results.ErrorInfo.ErrorMessage);
            Assert.NotNull(results.ErrorInfo.SymbolicId);
            Assert.Equal(Opc.Ua.StatusCodes.BadNodeIdUnknown, results.ErrorInfo.StatusCode);
        }

        public async Task NodeReadDiagnosticsStatusTestAsync()
        {
            var browser = _services();

            // Act
            var results = await browser.ValueReadAsync(_connection,
                new ValueReadRequestModel
                {
                    Header = new RequestHeaderModel
                    {
                        Diagnostics = new DiagnosticsModel
                        {
                            AuditId = nameof(NodeReadDiagnosticsStatusTestAsync),
                            TimeStamp = DateTime.Now
                        }
                    },
                    NodeId = "http://opcfoundation.org/UA/Boiler/#s=unknown"
                }).ConfigureAwait(false);

            // Assert
            Assert.NotNull(results.ErrorInfo);
            Assert.Null(results.ErrorInfo.NamespaceUri);
            Assert.Equal("en-US", results.ErrorInfo.Locale);
            Assert.Null(results.ErrorInfo.Inner);
            Assert.Null(results.ErrorInfo.AdditionalInfo);
            Assert.Equal("BadNodeIdUnknown", results.ErrorInfo.ErrorMessage);
            Assert.NotNull(results.ErrorInfo.SymbolicId);
            Assert.Equal(Opc.Ua.StatusCodes.BadNodeIdUnknown, results.ErrorInfo.StatusCode);
        }

        public async Task NodeReadDiagnosticsDebugTestAsync()
        {
            var browser = _services();

            // Act
            var results = await browser.ValueReadAsync(_connection,
                new ValueReadRequestModel
                {
                    Header = new RequestHeaderModel
                    {
                        Diagnostics = new DiagnosticsModel
                        {
                            AuditId = nameof(NodeReadDiagnosticsDebugTestAsync),
                            Level = DiagnosticsLevel.Information
                        }
                    },
                    NodeId = "http://opcfoundation.org/UA/Boiler/#s=unknown"
                }).ConfigureAwait(false);

            // Assert
            Assert.NotNull(results.ErrorInfo);
            Assert.Null(results.ErrorInfo.NamespaceUri);
            Assert.Equal("en-US", results.ErrorInfo.Locale);
            Assert.Null(results.ErrorInfo.Inner);
            Assert.Null(results.ErrorInfo.AdditionalInfo);
            Assert.Equal("BadNodeIdUnknown", results.ErrorInfo.ErrorMessage);
            Assert.NotNull(results.ErrorInfo.SymbolicId);
            Assert.Equal(Opc.Ua.StatusCodes.BadNodeIdUnknown, results.ErrorInfo.StatusCode);
        }

        public async Task NodeReadDiagnosticsVerboseTestAsync()
        {
            var browser = _services();

            // Act
            var results = await browser.ValueReadAsync(_connection,
                new ValueReadRequestModel
                {
                    Header = new RequestHeaderModel
                    {
                        Diagnostics = new DiagnosticsModel
                        {
                            AuditId = nameof(NodeReadDiagnosticsVerboseTestAsync),
                            Level = DiagnosticsLevel.Verbose
                        }
                    },
                    NodeId = "http://opcfoundation.org/UA/Boiler/#s=unknown"
                }).ConfigureAwait(false);

            // Assert
            Assert.NotNull(results.ErrorInfo);
            Assert.Null(results.ErrorInfo.NamespaceUri);
            Assert.Equal("en-US", results.ErrorInfo.Locale);
            Assert.Null(results.ErrorInfo.Inner);
            Assert.Null(results.ErrorInfo.AdditionalInfo);
            Assert.Equal("BadNodeIdUnknown", results.ErrorInfo.ErrorMessage);
            Assert.NotNull(results.ErrorInfo.SymbolicId);
            Assert.Equal(Opc.Ua.StatusCodes.BadNodeIdUnknown, results.ErrorInfo.StatusCode);
        }

        /// <summary>
        /// Helper to compare equal value
        /// </summary>
        /// <param name="expected"></param>
        /// <param name="value"></param>
        private static void AssertEqualValue(VariantValue expected, VariantValue value)
        {
            Assert.True(VariantValue.DeepEquals(expected, value),
                $"Expected: {expected} != Actual: {value}");
        }

        private readonly T _connection;
        private readonly IJsonSerializer _serializer;
        private readonly Func<T, string, IJsonSerializer, Task<VariantValue>> _readExpected;
        private readonly Func<INodeServices<T>> _services;
    }
}
