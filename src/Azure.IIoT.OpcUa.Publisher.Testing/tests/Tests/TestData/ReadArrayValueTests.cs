// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Testing.Tests
{
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Furly.Extensions.Serializers;
    using Furly.Extensions.Serializers.Json;
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Xml;
    using Xunit;

    public class ReadArrayValueTests<T>
    {
        /// <summary>
        /// Create node services tests
        /// </summary>
        /// <param name="services"></param>
        /// <param name="connection"></param>
        /// <param name="readExpected"></param>
        public ReadArrayValueTests(Func<INodeServices<T>> services, T connection,
            Func<T, string, IJsonSerializer, Task<VariantValue>> readExpected)
        {
            _services = services;
            _connection = connection;
            _readExpected = readExpected;
            _serializer = new DefaultJsonSerializer();
        }

        public async Task NodeReadAllStaticArrayVariableNodeClassTest1Async(CancellationToken ct = default)
        {
            var browser = _services();
            const Opc.Ua.NodeClass expected = Opc.Ua.NodeClass.Variable;

            var attributes = new List<AttributeReadRequestModel>();
            for (var i = 10300; i < 10326; i++)
            {
                attributes.Add(new AttributeReadRequestModel
                {
                    Attribute = NodeAttribute.NodeClass,
                    NodeId = "http://test.org/UA/Data/#i=" + i
                });
            }

            // Act
            var result = await browser.ReadAsync(_connection, new ReadRequestModel
            {
                Header = new RequestHeaderModel
                {
                    Diagnostics = new DiagnosticsModel
                    {
                        AuditId = nameof(NodeReadAllStaticArrayVariableNodeClassTest1Async),
                        TimeStamp = DateTime.Now
                    }
                },
                Attributes = attributes
            }, ct).ConfigureAwait(false);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Results);
            Assert.Equal(attributes.Count, result.Results.Count);
            Assert.All(result.Results, r => Assert.Null(r.ErrorInfo));
            Assert.All(result.Results, r => Assert.Equal((int)expected, (int)r.Value));
        }

        public async Task NodeReadAllStaticArrayVariableAccessLevelTest1Async(CancellationToken ct = default)
        {
            var browser = _services();
            const int expected = Opc.Ua.AccessLevels.CurrentRead | Opc.Ua.AccessLevels.CurrentWrite;
            var attributes = new List<AttributeReadRequestModel>();
            for (var i = 10300; i < 10326; i++)
            {
                attributes.Add(new AttributeReadRequestModel
                {
                    Attribute = NodeAttribute.AccessLevel,
                    NodeId = "http://test.org/UA/Data/#i=" + i
                });
            }

            // Act
            var result = await browser.ReadAsync(_connection, new ReadRequestModel
            {
                Header = new RequestHeaderModel
                {
                    Diagnostics = new DiagnosticsModel
                    {
                        AuditId = nameof(NodeReadAllStaticArrayVariableAccessLevelTest1Async),
                        TimeStamp = DateTime.Now
                    }
                },
                Attributes = attributes
            }, ct).ConfigureAwait(false);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Results);
            Assert.Equal(attributes.Count, result.Results.Count);
            Assert.All(result.Results, r => Assert.Null(r.ErrorInfo));
            Assert.All(result.Results, r => Assert.Equal(expected, (int)r.Value));
        }

        public async Task NodeReadAllStaticArrayVariableWriteMaskTest1Async(CancellationToken ct = default)
        {
            var browser = _services();

            var attributes = new List<AttributeReadRequestModel>();
            for (var i = 10300; i < 10326; i++)
            {
                attributes.Add(new AttributeReadRequestModel
                {
                    Attribute = NodeAttribute.WriteMask,
                    NodeId = "http://test.org/UA/Data/#i=" + i
                });
            }

            // Act
            var result = await browser.ReadAsync(_connection, new ReadRequestModel
            {
                Header = new RequestHeaderModel
                {
                    Diagnostics = new DiagnosticsModel
                    {
                        AuditId = nameof(NodeReadAllStaticArrayVariableWriteMaskTest1Async),
                        TimeStamp = DateTime.Now
                    }
                },
                Attributes = attributes
            }, ct).ConfigureAwait(false);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Results);
            Assert.Equal(attributes.Count, result.Results.Count);
            Assert.All(result.Results, r => Assert.Null(r.ErrorInfo));
            Assert.All(result.Results, r => Assert.Equal(0, (int)r.Value));
        }

        public async Task NodeReadAllStaticArrayVariableWriteMaskTest2Async(CancellationToken ct = default)
        {
            var browser = _services();

            var attributes = new List<AttributeReadRequestModel>();
            for (var i = 10300; i < 10326; i++)
            {
                attributes.Add(new AttributeReadRequestModel
                {
                    Attribute = NodeAttribute.WriteMask,
                    NodeId = "http://test.org/UA/Data/#i=10300"
                });
            }

            // Act
            var result = await browser.ReadAsync(_connection, new ReadRequestModel
            {
                Header = new RequestHeaderModel
                {
                    Diagnostics = new DiagnosticsModel
                    {
                        AuditId = nameof(NodeReadAllStaticArrayVariableWriteMaskTest2Async),
                        TimeStamp = DateTime.Now
                    }
                },
                Attributes = attributes
            }, ct).ConfigureAwait(false);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Results);
            Assert.Equal(attributes.Count, result.Results.Count);
            Assert.All(result.Results, r => Assert.Null(r.ErrorInfo));
            Assert.All(result.Results, r => Assert.Equal(0, (int)r.Value));
        }

        public async Task NodeReadStaticArrayBooleanValueVariableTestAsync(CancellationToken ct = default)
        {
            var browser = _services();
            const string node = "http://test.org/UA/Data/#i=10300";
            var expected = await _readExpected(_connection, node, _serializer).ConfigureAwait(false);

            // Act
            var result = await browser.ValueReadAsync(_connection, new ValueReadRequestModel
            {
                NodeId = node
            }, ct).ConfigureAwait(false);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Value);
            Assert.NotNull(result.SourceTimestamp);
            Assert.NotNull(result.ServerTimestamp);
            AssertEqualValue(expected, result.Value);

            Assert.True(result.Value.IsListOfValues, $"{result.Value} is not a list.");
            if (result.Value.Count == 0)
            {
                return;
            }

            Assert.True(result.Value[0].IsBoolean, $"{result.Value[0]} is not a boolean.");
            Assert.Equal("Boolean", result.DataType);
        }

        public async Task NodeReadStaticArraySByteValueVariableTestAsync(CancellationToken ct = default)
        {
            var browser = _services();
            const string node = "http://test.org/UA/Data/#i=10301";
            var expected = await _readExpected(_connection, node, _serializer).ConfigureAwait(false);

            // Act
            var result = await browser.ValueReadAsync(_connection, new ValueReadRequestModel
            {
                NodeId = node
            }, ct).ConfigureAwait(false);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Value);
            Assert.NotNull(result.SourceTimestamp);
            Assert.NotNull(result.ServerTimestamp);
            AssertEqualValue(expected, result.Value);

            Assert.True(result.Value.IsListOfValues, $"{result.Value} is not a list.");
            if (result.Value.Count == 0)
            {
                return;
            }

            Assert.True(result.Value[0].IsInteger, $"{result.Value[0]} is not an integer.");
            Assert.Equal("SByte", result.DataType);
        }

        public async Task NodeReadStaticArrayByteValueVariableTestAsync(CancellationToken ct = default)
        {
            var browser = _services();
            const string node = "http://test.org/UA/Data/#i=10302";
            var expected = await _readExpected(_connection, node, _serializer).ConfigureAwait(false);

            // Act
            var result = await browser.ValueReadAsync(_connection, new ValueReadRequestModel
            {
                NodeId = node
            }, ct).ConfigureAwait(false);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.SourceTimestamp);
            Assert.NotNull(result.ServerTimestamp);
            AssertEqualValue(expected, result.Value);

            Assert.Equal("ByteString", result.DataType);
            Assert.True(result.Value.IsNull() || result.Value!.IsBytes);
        }

        public async Task NodeReadStaticArrayInt16ValueVariableTestAsync(CancellationToken ct = default)
        {
            var browser = _services();
            const string node = "http://test.org/UA/Data/#i=10303";
            var expected = await _readExpected(_connection, node, _serializer).ConfigureAwait(false);

            // Act
            var result = await browser.ValueReadAsync(_connection, new ValueReadRequestModel
            {
                NodeId = node
            }, ct).ConfigureAwait(false);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Value);
            Assert.NotNull(result.SourceTimestamp);
            Assert.NotNull(result.ServerTimestamp);
            AssertEqualValue(expected, result.Value);

            Assert.True(result.Value.IsListOfValues, $"{result.Value} is not a list.");
            if (result.Value.Count == 0)
            {
                return;
            }

            Assert.True(result.Value[0].IsInteger, $"{result.Value[0]} is not an integer.");
            Assert.Equal("Int16", result.DataType);
        }

        public async Task NodeReadStaticArrayUInt16ValueVariableTestAsync(CancellationToken ct = default)
        {
            var browser = _services();
            const string node = "http://test.org/UA/Data/#i=10304";
            var expected = await _readExpected(_connection, node, _serializer).ConfigureAwait(false);

            // Act
            var result = await browser.ValueReadAsync(_connection, new ValueReadRequestModel
            {
                NodeId = node
            }, ct).ConfigureAwait(false);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Value);
            Assert.NotNull(result.SourceTimestamp);
            Assert.NotNull(result.ServerTimestamp);
            AssertEqualValue(expected, result.Value);

            Assert.True(result.Value.IsListOfValues, $"{result.Value} is not a list.");
            if (result.Value.Count == 0)
            {
                return;
            }

            Assert.True(result.Value[0].IsInteger, $"{result.Value[0]} is not an integer.");
            Assert.Equal("UInt16", result.DataType);
        }

        public async Task NodeReadStaticArrayInt32ValueVariableTestAsync(CancellationToken ct = default)
        {
            var browser = _services();
            const string node = "http://test.org/UA/Data/#i=10305";
            var expected = await _readExpected(_connection, node, _serializer).ConfigureAwait(false);

            // Act
            var result = await browser.ValueReadAsync(_connection, new ValueReadRequestModel
            {
                NodeId = node
            }, ct).ConfigureAwait(false);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Value);
            Assert.NotNull(result.SourceTimestamp);
            Assert.NotNull(result.ServerTimestamp);
            AssertEqualValue(expected, result.Value);

            Assert.True(result.Value.IsListOfValues, $"{result.Value} is not a list.");
            if (result.Value.Count == 0)
            {
                return;
            }

            Assert.True(result.Value[0].IsInteger, $"{result.Value[0]} is not an integer.");
            Assert.Equal("Int32", result.DataType);
        }

        public async Task NodeReadStaticArrayUInt32ValueVariableTestAsync(CancellationToken ct = default)
        {
            var browser = _services();
            const string node = "http://test.org/UA/Data/#i=10306";
            var expected = await _readExpected(_connection, node, _serializer).ConfigureAwait(false);

            // Act
            var result = await browser.ValueReadAsync(_connection, new ValueReadRequestModel
            {
                NodeId = node
            }, ct).ConfigureAwait(false);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Value);
            Assert.NotNull(result.SourceTimestamp);
            Assert.NotNull(result.ServerTimestamp);
            AssertEqualValue(expected, result.Value);

            Assert.True(result.Value.IsListOfValues, $"{result.Value} is not a list.");
            if (result.Value.Count == 0)
            {
                return;
            }

            Assert.True(result.Value[0].IsInteger, $"{result.Value[0]} is not an integer.");
            Assert.Equal("UInt32", result.DataType);
        }

        public async Task NodeReadStaticArrayInt64ValueVariableTestAsync(CancellationToken ct = default)
        {
            var browser = _services();
            const string node = "http://test.org/UA/Data/#i=10307";
            var expected = await _readExpected(_connection, node, _serializer).ConfigureAwait(false);

            // Act
            var result = await browser.ValueReadAsync(_connection, new ValueReadRequestModel
            {
                NodeId = node
            }, ct).ConfigureAwait(false);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Value);
            Assert.NotNull(result.SourceTimestamp);
            Assert.NotNull(result.ServerTimestamp);
            AssertEqualValue(expected, result.Value);

            Assert.True(result.Value.IsListOfValues, $"{result.Value} is not a list.");
            if (result.Value.Count == 0)
            {
                return;
            }

            Assert.True(result.Value[0].IsInteger, $"{result.Value[0]} is not an integer.");
            Assert.Equal("Int64", result.DataType);
        }

        public async Task NodeReadStaticArrayUInt64ValueVariableTestAsync(CancellationToken ct = default)
        {
            var browser = _services();
            const string node = "http://test.org/UA/Data/#i=10308";
            var expected = await _readExpected(_connection, node, _serializer).ConfigureAwait(false);

            // Act
            var result = await browser.ValueReadAsync(_connection, new ValueReadRequestModel
            {
                NodeId = node
            }, ct).ConfigureAwait(false);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Value);
            Assert.NotNull(result.SourceTimestamp);
            Assert.NotNull(result.ServerTimestamp);
            AssertEqualValue(expected, result.Value);

            Assert.True(result.Value.IsListOfValues, $"{result.Value} is not a list.");
            if (result.Value.Count == 0)
            {
                return;
            }

            Assert.True(result.Value[0].IsInteger, $"{result.Value[0]} is not an integer.");
            Assert.Equal("UInt64", result.DataType);
        }

        public async Task NodeReadStaticArrayFloatValueVariableTestAsync(CancellationToken ct = default)
        {
            var browser = _services();
            const string node = "http://test.org/UA/Data/#i=10309";
            var expected = await _readExpected(_connection, node, _serializer).ConfigureAwait(false);

            // Act
            var result = await browser.ValueReadAsync(_connection, new ValueReadRequestModel
            {
                NodeId = node
            }, ct).ConfigureAwait(false);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Value);
            Assert.NotNull(result.SourceTimestamp);
            Assert.NotNull(result.ServerTimestamp);
            AssertEqualValue(expected, result.Value);

            Assert.True(result.Value.IsListOfValues, $"{result.Value} is not a list.");
            if (result.Value.Count == 0)
            {
                return;
            }

            Assert.True(result.Value[0].IsFloat, $"First is {result.Value}");
            Assert.Equal("Float", result.DataType);
        }

        public async Task NodeReadStaticArrayDoubleValueVariableTestAsync(CancellationToken ct = default)
        {
            var browser = _services();
            const string node = "http://test.org/UA/Data/#i=10310";
            var expected = await _readExpected(_connection, node, _serializer).ConfigureAwait(false);

            // Act
            var result = await browser.ValueReadAsync(_connection, new ValueReadRequestModel
            {
                NodeId = node
            }, ct).ConfigureAwait(false);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Value);
            Assert.NotNull(result.SourceTimestamp);
            Assert.NotNull(result.ServerTimestamp);
            AssertEqualValue(expected, result.Value);

            Assert.True(result.Value.IsListOfValues, $"{result.Value} is not a list.");
            if (result.Value.Count == 0)
            {
                return;
            }

            Assert.True(result.Value[0].IsDouble);
            Assert.Equal("Double", result.DataType);
        }

        public async Task NodeReadStaticArrayStringValueVariableTestAsync(CancellationToken ct = default)
        {
            var browser = _services();
            const string node = "http://test.org/UA/Data/#i=10311";
            var expected = await _readExpected(_connection, node, _serializer).ConfigureAwait(false);

            // Act
            var result = await browser.ValueReadAsync(_connection, new ValueReadRequestModel
            {
                NodeId = node
            }, ct).ConfigureAwait(false);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Value);
            Assert.NotNull(result.SourceTimestamp);
            Assert.NotNull(result.ServerTimestamp);
            AssertEqualValue(expected, result.Value);

            Assert.True(result.Value.IsListOfValues, $"{result.Value} is not a list.");
            if (result.Value.Count == 0)
            {
                return;
            }

            Assert.True(result.Value[0].IsString, $"{result.Value[0]} is not a string.");
            Assert.Equal("String", result.DataType);
        }

        public async Task NodeReadStaticArrayDateTimeValueVariableTestAsync(CancellationToken ct = default)
        {
            var browser = _services();
            const string node = "http://test.org/UA/Data/#i=10312";
            var expected = await _readExpected(_connection, node, _serializer).ConfigureAwait(false);

            // Act
            var result = await browser.ValueReadAsync(_connection, new ValueReadRequestModel
            {
                NodeId = node
            }, ct).ConfigureAwait(false);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Value);
            Assert.NotNull(result.SourceTimestamp);
            Assert.NotNull(result.ServerTimestamp);
            AssertEqualValue(expected, result.Value);

            Assert.True(result.Value.IsListOfValues, $"{result.Value} is not a list.");
            if (result.Value.Count == 0)
            {
                return;
            }

            Assert.True(result.Value[0].IsDateTime);
            Assert.Equal("DateTime", result.DataType);
        }

        public async Task NodeReadStaticArrayGuidValueVariableTestAsync(CancellationToken ct = default)
        {
            var browser = _services();
            const string node = "http://test.org/UA/Data/#i=10313";
            var expected = await _readExpected(_connection, node, _serializer).ConfigureAwait(false);

            // Act
            var result = await browser.ValueReadAsync(_connection, new ValueReadRequestModel
            {
                NodeId = node
            }, ct).ConfigureAwait(false);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Value);
            Assert.NotNull(result.SourceTimestamp);
            Assert.NotNull(result.ServerTimestamp);
            AssertEqualValue(expected, result.Value);

            Assert.True(result.Value.IsListOfValues, $"{result.Value} is not a list.");
            if (result.Value.Count == 0)
            {
                return;
            }

            Assert.True(result.Value[0].IsGuid);
            Assert.Equal("Guid", result.DataType);
        }

        public async Task NodeReadStaticArrayByteStringValueVariableTestAsync(CancellationToken ct = default)
        {
            var browser = _services();
            const string node = "http://test.org/UA/Data/#i=10314";
            var expected = await _readExpected(_connection, node, _serializer).ConfigureAwait(false);

            // Act
            var result = await browser.ValueReadAsync(_connection, new ValueReadRequestModel
            {
                NodeId = node
            }, ct).ConfigureAwait(false);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.SourceTimestamp);
            Assert.NotNull(result.ServerTimestamp);
            AssertEqualValue(expected, result.Value);

            if (result.Value.IsNull())
            {
                return;
            }
            Assert.True(result.Value!.IsListOfValues);
            if (result.Value.Count == 0)
            {
                return;
            }
            // TODO: Can be null.  Assert.Equal(VariantValueType.String, (result.Value)[0].Type);
            // TODO:  Assert.Equal(VariantValueType.Bytes, (result.Value)[0].Type);
            Assert.Equal("ByteString", result.DataType);
        }

        public async Task NodeReadStaticArrayXmlElementValueVariableTestAsync(CancellationToken ct = default)
        {
            var browser = _services();
            const string node = "http://test.org/UA/Data/#i=10315";
            var expected = await _readExpected(_connection, node, _serializer).ConfigureAwait(false);

            // Act
            var result = await browser.ValueReadAsync(_connection, new ValueReadRequestModel
            {
                NodeId = node
            }, ct).ConfigureAwait(false);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Value);
            Assert.NotNull(result.SourceTimestamp);
            Assert.NotNull(result.ServerTimestamp);
            AssertEqualValue(expected, result.Value);

            Assert.True(result.Value.IsListOfValues, $"{result.Value} is not a list.");
            if (result.Value.Count == 0)
            {
                return;
            }

            Assert.True(result.Value[0].IsBytes);
            Assert.Equal("XmlElement", result.DataType);
            var xml = result.Value[0].ConvertTo<XmlElement>();
            Assert.NotNull(xml);
        }

        public async Task NodeReadStaticArrayNodeIdValueVariableTestAsync(CancellationToken ct = default)
        {
            var browser = _services();
            const string node = "http://test.org/UA/Data/#i=10316";
            var expected = await _readExpected(_connection, node, _serializer).ConfigureAwait(false);

            // Act
            var result = await browser.ValueReadAsync(_connection, new ValueReadRequestModel
            {
                NodeId = node
            }, ct).ConfigureAwait(false);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Value);
            Assert.NotNull(result.SourceTimestamp);
            Assert.NotNull(result.ServerTimestamp);
            AssertEqualValue(expected, result.Value);

            Assert.True(result.Value.IsListOfValues, $"{result.Value} is not a list.");
            if (result.Value.Count == 0)
            {
                return;
            }

            Assert.True(result.Value[0].IsString, $"{result.Value[0]} is not a string.");
            Assert.Equal("NodeId", result.DataType);
        }

        public async Task NodeReadStaticArrayExpandedNodeIdValueVariableTestAsync(CancellationToken ct = default)
        {
            var browser = _services();
            const string node = "http://test.org/UA/Data/#i=10317";
            var expected = await _readExpected(_connection, node, _serializer).ConfigureAwait(false);

            // Act
            var result = await browser.ValueReadAsync(_connection, new ValueReadRequestModel
            {
                NodeId = node
            }, ct).ConfigureAwait(false);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Value);
            Assert.NotNull(result.SourceTimestamp);
            Assert.NotNull(result.ServerTimestamp);
            AssertEqualValue(expected, result.Value);

            Assert.True(result.Value.IsListOfValues, $"{result.Value} is not a list.");
            if (result.Value.Count == 0)
            {
                return;
            }

            Assert.True(result.Value[0].IsString, $"{result.Value[0]} is not a string.");
            Assert.Equal("ExpandedNodeId", result.DataType);
        }

        public async Task NodeReadStaticArrayQualifiedNameValueVariableTestAsync(CancellationToken ct = default)
        {
            var browser = _services();
            const string node = "http://test.org/UA/Data/#i=10318";
            var expected = await _readExpected(_connection, node, _serializer).ConfigureAwait(false);

            // Act
            var result = await browser.ValueReadAsync(_connection, new ValueReadRequestModel
            {
                NodeId = node
            }, ct).ConfigureAwait(false);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Value);
            Assert.NotNull(result.SourceTimestamp);
            Assert.NotNull(result.ServerTimestamp);
            AssertEqualValue(expected, result.Value);

            Assert.True(result.Value.IsListOfValues, $"{result.Value} is not a list.");
            if (result.Value.Count == 0)
            {
                return;
            }

            Assert.True(result.Value[0].IsString, $"{result.Value[0]} is not a string.");
            Assert.Equal("QualifiedName", result.DataType);
        }

        public async Task NodeReadStaticArrayLocalizedTextValueVariableTestAsync(CancellationToken ct = default)
        {
            var browser = _services();
            const string node = "http://test.org/UA/Data/#i=10319";
            var expected = await _readExpected(_connection, node, _serializer).ConfigureAwait(false);

            // Act
            var result = await browser.ValueReadAsync(_connection, new ValueReadRequestModel
            {
                NodeId = node
            }, ct).ConfigureAwait(false);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Value);
            Assert.NotNull(result.SourceTimestamp);
            Assert.NotNull(result.ServerTimestamp);
            AssertEqualValue(expected, result.Value);

            Assert.True(result.Value.IsListOfValues, $"{result.Value} is not a list.");
            if (result.Value.Count == 0)
            {
                return;
            }

            Assert.True(result.Value[0].IsObject, $"{result.Value[0]} is not an object.");
            Assert.Equal("LocalizedText", result.DataType);
        }

        public async Task NodeReadStaticArrayStatusCodeValueVariableTestAsync(CancellationToken ct = default)
        {
            var browser = _services();
            const string node = "http://test.org/UA/Data/#i=10320";
            var expected = await _readExpected(_connection, node, _serializer).ConfigureAwait(false);

            // Act
            var result = await browser.ValueReadAsync(_connection, new ValueReadRequestModel
            {
                NodeId = node
            }, ct).ConfigureAwait(false);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Value);
            Assert.NotNull(result.SourceTimestamp);
            Assert.NotNull(result.ServerTimestamp);
            AssertEqualValue(expected, result.Value);

            Assert.True(result.Value.IsListOfValues, $"{result.Value} is not a list.");
            if (result.Value.Count == 0)
            {
                return;
            }
            Assert.True(
               result.Value[0].IsObject ||
               result.Value[0].IsInteger, $"{result.Value[0]} is not a integer or object.");
            Assert.Equal("StatusCode", result.DataType);
        }

        public async Task NodeReadStaticArrayVariantValueVariableTestAsync(CancellationToken ct = default)
        {
            var browser = _services();
            const string node = "http://test.org/UA/Data/#i=10321";
            var expected = await _readExpected(_connection, node, _serializer).ConfigureAwait(false);

            // Act
            var result = await browser.ValueReadAsync(_connection, new ValueReadRequestModel
            {
                NodeId = node
            }, ct).ConfigureAwait(false);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Value);
            Assert.NotNull(result.SourceTimestamp);
            Assert.NotNull(result.ServerTimestamp);
            AssertEqualValue(expected, result.Value);

            Assert.True(result.Value.IsListOfValues, $"{result.Value} is not a list.");
        }

        public async Task NodeReadStaticArrayEnumerationValueVariableTestAsync(CancellationToken ct = default)
        {
            var browser = _services();
            const string node = "http://test.org/UA/Data/#i=10322";
            var expected = await _readExpected(_connection, node, _serializer).ConfigureAwait(false);

            // Act
            var result = await browser.ValueReadAsync(_connection, new ValueReadRequestModel
            {
                NodeId = node
            }, ct).ConfigureAwait(false);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Value);
            Assert.NotNull(result.SourceTimestamp);
            Assert.NotNull(result.ServerTimestamp);
            AssertEqualValue(expected, result.Value);

            Assert.True(result.Value.IsListOfValues, $"{result.Value} is not a list.");
            if (result.Value.Count == 0)
            {
                return;
            }

            Assert.True(result.Value[0].IsInteger, $"{result.Value[0]} is not an integer.");
            Assert.Equal("Int32", result.DataType);
        }

        public async Task NodeReadStaticArrayStructureValueVariableTestAsync(CancellationToken ct = default)
        {
            var browser = _services();
            const string node = "http://test.org/UA/Data/#i=10323";
            var expected = await _readExpected(_connection, node, _serializer).ConfigureAwait(false);

            // Act
            var result = await browser.ValueReadAsync(_connection, new ValueReadRequestModel
            {
                NodeId = node
            }, ct).ConfigureAwait(false);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Value);
            Assert.NotNull(result.SourceTimestamp);
            Assert.NotNull(result.ServerTimestamp);
            AssertEqualValue(expected, result.Value);

            Assert.True(result.Value.IsListOfValues, $"{result.Value} is not a list.");
            if (result.Value.Count == 0)
            {
                return;
            }

            Assert.True(result.Value[0].IsObject, $"{result.Value[0]} is not an object.");
            // TODO: Assert.Equal(VariantValueType.Bytes, (result.Value)[0].Type);
            Assert.Equal("ExtensionObject", result.DataType);
        }

        public async Task NodeReadStaticArrayNumberValueVariableTestAsync(CancellationToken ct = default)
        {
            var browser = _services();
            const string node = "http://test.org/UA/Data/#i=10324";
            var expected = await _readExpected(_connection, node, _serializer).ConfigureAwait(false);

            // Act
            var result = await browser.ValueReadAsync(_connection, new ValueReadRequestModel
            {
                NodeId = node
            }, ct).ConfigureAwait(false);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Value);
            Assert.NotNull(result.SourceTimestamp);
            Assert.NotNull(result.ServerTimestamp);
            AssertEqualValue(expected, result.Value);

            Assert.True(result.Value.IsArray, $"Not an array {result.Value}");
            if (result.Value.Count == 0)
            {
                return;
            }
            Assert.True(result.Value[0].IsDouble || result.Value[0].IsDecimal,
                $"Not a number {result.Value[0]}");
        }

        public async Task NodeReadStaticArrayIntegerValueVariableTestAsync(CancellationToken ct = default)
        {
            var browser = _services();
            const string node = "http://test.org/UA/Data/#i=10325";
            var expected = await _readExpected(_connection, node, _serializer).ConfigureAwait(false);

            // Act
            var result = await browser.ValueReadAsync(_connection, new ValueReadRequestModel
            {
                NodeId = node
            }, ct).ConfigureAwait(false);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Value);
            Assert.NotNull(result.SourceTimestamp);
            Assert.NotNull(result.ServerTimestamp);
            AssertEqualValue(expected, result.Value);

            Assert.True(result.Value.IsListOfValues, $"{result.Value} is not a list.");
            if (result.Value.Count == 0)
            {
                return;
            }

            Assert.True(result.Value[0].IsInteger, $"{result.Value[0]} is not an integer.");
        }

        public async Task NodeReadStaticArrayUIntegerValueVariableTestAsync(CancellationToken ct = default)
        {
            var browser = _services();
            const string node = "http://test.org/UA/Data/#i=10326";
            var expected = await _readExpected(_connection, node, _serializer).ConfigureAwait(false);

            // Act
            var result = await browser.ValueReadAsync(_connection, new ValueReadRequestModel
            {
                NodeId = node
            }, ct).ConfigureAwait(false);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Value);
            Assert.NotNull(result.SourceTimestamp);
            Assert.NotNull(result.ServerTimestamp);
            AssertEqualValue(expected, result.Value);

            Assert.True(result.Value.IsArray, $"Not an array {result.Value}");
            if (result.Value.Count == 0)
            {
                return;
            }

            Assert.True(result.Value[0].IsInteger, $"{result.Value[0]} is not an integer.");
        }

        /// <summary>
        /// Helper to compare equal value
        /// </summary>
        /// <param name="expected"></param>
        /// <param name="value"></param>
        private static void AssertEqualValue(VariantValue? expected, VariantValue? value)
        {
            Assert.True(VariantValue.DeepEquals(expected, value),
                $"Expected: {expected}  != Actual: {value} ");
        }

        private readonly T _connection;
        private readonly Func<T, string, IJsonSerializer, Task<VariantValue>> _readExpected;
        private readonly DefaultJsonSerializer _serializer;
        private readonly Func<INodeServices<T>> _services;
    }
}
