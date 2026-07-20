// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Testing.Tests
{
    using Azure.IIoT.OpcUa.Core.Serialization;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using System.Text.Json.Nodes;
    using System;
    using System.Collections.Generic;
    using System.Text.Json;
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
            Func<T, string, Task<JsonNode?>> readExpected)
        {
            _services = services;
            _connection = connection;
            _readExpected = readExpected;
        }

        public async Task NodeReadAllStaticArrayVariableNodeClassTest1Async(CancellationToken ct = default)
        {
            var browser = _services();
            const Opc.Ua.NodeClass expected = Opc.Ua.NodeClass.Variable;

            var attributes = new List<AttributeReadRequestModel>();
            for (var i = 2228; i < 2254; i++)
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
            for (var i = 2228; i < 2254; i++)
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
            for (var i = 2228; i < 2254; i++)
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
            Assert.All(result.Results, r => Assert.Equal(0, (int)r.Value!));
        }

        public async Task NodeReadAllStaticArrayVariableWriteMaskTest2Async(CancellationToken ct = default)
        {
            var browser = _services();

            var attributes = new List<AttributeReadRequestModel>();
            for (var i = 2228; i < 2254; i++)
            {
                attributes.Add(new AttributeReadRequestModel
                {
                    Attribute = NodeAttribute.WriteMask,
                    NodeId = "http://test.org/UA/Data/#i=2228"
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
            Assert.All(result.Results, r => Assert.Equal(0, (int)r.Value!));
        }

        public async Task NodeReadStaticArrayBooleanValueVariableTestAsync(CancellationToken ct = default)
        {
            var browser = _services();
            const string node = "http://test.org/UA/Data/#i=2228";
            var expected = await _readExpected(_connection, node).ConfigureAwait(false);

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

            Assert.True(result.Value.IsListOfValues(), $"{result.Value} is not a list.");
            if (result.Value.Count() == 0)
            {
                return;
            }

            Assert.True(result.Value[0].IsBoolean(), $"{result.Value[0]} is not a boolean.");
            Assert.Equal("Boolean", result.DataType);
        }

        public async Task NodeReadStaticArraySByteValueVariableTestAsync(CancellationToken ct = default)
        {
            var browser = _services();
            const string node = "http://test.org/UA/Data/#i=2229";
            var expected = await _readExpected(_connection, node).ConfigureAwait(false);

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

            Assert.True(result.Value.IsListOfValues(), $"{result.Value} is not a list.");
            if (result.Value.Count() == 0)
            {
                return;
            }

            Assert.True(result.Value[0].IsInteger(), $"{result.Value[0]} is not an integer.");
            Assert.Equal("SByte", result.DataType);
        }

        public async Task NodeReadStaticArrayByteValueVariableTestAsync(CancellationToken ct = default)
        {
            var browser = _services();
            const string node = "http://test.org/UA/Data/#i=2230";
            var expected = await _readExpected(_connection, node).ConfigureAwait(false);

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

            Assert.Equal("Byte", result.DataType);
            Assert.True(result.Value!.IsListOfValues());
            Assert.All(result.Value.Values(), value =>
                Assert.True(value!.IsInteger(), $"{value} is not an integer."));
        }

        public async Task NodeReadStaticArrayInt16ValueVariableTestAsync(CancellationToken ct = default)
        {
            var browser = _services();
            const string node = "http://test.org/UA/Data/#i=2231";
            var expected = await _readExpected(_connection, node).ConfigureAwait(false);

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

            Assert.True(result.Value.IsListOfValues(), $"{result.Value} is not a list.");
            if (result.Value.Count() == 0)
            {
                return;
            }

            Assert.True(result.Value[0].IsInteger(), $"{result.Value[0]} is not an integer.");
            Assert.Equal("Int16", result.DataType);
        }

        public async Task NodeReadStaticArrayUInt16ValueVariableTestAsync(CancellationToken ct = default)
        {
            var browser = _services();
            const string node = "http://test.org/UA/Data/#i=2232";
            var expected = await _readExpected(_connection, node).ConfigureAwait(false);

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

            Assert.True(result.Value.IsListOfValues(), $"{result.Value} is not a list.");
            if (result.Value.Count() == 0)
            {
                return;
            }

            Assert.True(result.Value[0].IsInteger(), $"{result.Value[0]} is not an integer.");
            Assert.Equal("UInt16", result.DataType);
        }

        public async Task NodeReadStaticArrayInt32ValueVariableTestAsync(CancellationToken ct = default)
        {
            var browser = _services();
            const string node = "http://test.org/UA/Data/#i=2233";
            var expected = await _readExpected(_connection, node).ConfigureAwait(false);

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

            Assert.True(result.Value.IsListOfValues(), $"{result.Value} is not a list.");
            if (result.Value.Count() == 0)
            {
                return;
            }

            Assert.True(result.Value[0].IsInteger(), $"{result.Value[0]} is not an integer.");
            Assert.Equal("Int32", result.DataType);
        }

        public async Task NodeReadStaticArrayUInt32ValueVariableTestAsync(CancellationToken ct = default)
        {
            var browser = _services();
            const string node = "http://test.org/UA/Data/#i=2234";
            var expected = await _readExpected(_connection, node).ConfigureAwait(false);

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

            Assert.True(result.Value.IsListOfValues(), $"{result.Value} is not a list.");
            if (result.Value.Count() == 0)
            {
                return;
            }

            Assert.True(result.Value[0].IsInteger(), $"{result.Value[0]} is not an integer.");
            Assert.Equal("UInt32", result.DataType);
        }

        public async Task NodeReadStaticArrayInt64ValueVariableTestAsync(CancellationToken ct = default)
        {
            var browser = _services();
            const string node = "http://test.org/UA/Data/#i=2235";
            var expected = await _readExpected(_connection, node).ConfigureAwait(false);

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

            Assert.True(result.Value.IsListOfValues(), $"{result.Value} is not a list.");
            if (result.Value.Count() == 0)
            {
                return;
            }

            Assert.True(result.Value[0].IsInteger(), $"{result.Value[0]} is not an integer.");
            Assert.Equal("Int64", result.DataType);
        }

        public async Task NodeReadStaticArrayUInt64ValueVariableTestAsync(CancellationToken ct = default)
        {
            var browser = _services();
            const string node = "http://test.org/UA/Data/#i=2236";
            var expected = await _readExpected(_connection, node).ConfigureAwait(false);

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

            Assert.True(result.Value.IsListOfValues(), $"{result.Value} is not a list.");
            if (result.Value.Count() == 0)
            {
                return;
            }

            Assert.True(result.Value[0].IsInteger(), $"{result.Value[0]} is not an integer.");
            Assert.Equal("UInt64", result.DataType);
        }

        public async Task NodeReadStaticArrayFloatValueVariableTestAsync(CancellationToken ct = default)
        {
            var browser = _services();
            const string node = "http://test.org/UA/Data/#i=2237";
            var expected = await _readExpected(_connection, node).ConfigureAwait(false);

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

            Assert.True(result.Value.IsListOfValues(), $"{result.Value} is not a list.");
            if (result.Value.Count() == 0)
            {
                return;
            }

            Assert.True(result.Value[0].IsFloat(), $"First is {result.Value}");
            Assert.Equal("Float", result.DataType);
        }

        public async Task NodeReadStaticArrayDoubleValueVariableTestAsync(CancellationToken ct = default)
        {
            var browser = _services();
            const string node = "http://test.org/UA/Data/#i=2238";
            var expected = await _readExpected(_connection, node).ConfigureAwait(false);

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

            Assert.True(result.Value.IsListOfValues(), $"{result.Value} is not a list.");
            if (result.Value.Count() == 0)
            {
                return;
            }

            Assert.True(result.Value[0].IsDouble());
            Assert.Equal("Double", result.DataType);
        }

        public async Task NodeReadStaticArrayStringValueVariableTestAsync(CancellationToken ct = default)
        {
            var browser = _services();
            const string node = "http://test.org/UA/Data/#i=2239";
            var expected = await _readExpected(_connection, node).ConfigureAwait(false);

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

            Assert.True(result.Value.IsListOfValues(), $"{result.Value} is not a list.");
            if (result.Value.Count() == 0)
            {
                return;
            }

            Assert.True(result.Value[0].IsString(), $"{result.Value[0]} is not a string.");
            Assert.Equal("String", result.DataType);
        }

        public async Task NodeReadStaticArrayDateTimeValueVariableTestAsync(CancellationToken ct = default)
        {
            var browser = _services();
            const string node = "http://test.org/UA/Data/#i=2240";
            var expected = await _readExpected(_connection, node).ConfigureAwait(false);

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

            Assert.True(result.Value.IsListOfValues(), $"{result.Value} is not a list.");
            if (result.Value.Count() == 0)
            {
                return;
            }

            Assert.True(result.Value[0].IsDateTime());
            Assert.Equal("DateTime", result.DataType);
        }

        public async Task NodeReadStaticArrayGuidValueVariableTestAsync(CancellationToken ct = default)
        {
            var browser = _services();
            const string node = "http://test.org/UA/Data/#i=2241";
            var expected = await _readExpected(_connection, node).ConfigureAwait(false);

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

            Assert.True(result.Value.IsListOfValues(), $"{result.Value} is not a list.");
            if (result.Value.Count() == 0)
            {
                return;
            }

            Assert.True(result.Value[0].IsGuid());
            Assert.Equal("Guid", result.DataType);
        }

        public async Task NodeReadStaticArrayByteStringValueVariableTestAsync(CancellationToken ct = default)
        {
            var browser = _services();
            const string node = "http://test.org/UA/Data/#i=2242";
            var expected = await _readExpected(_connection, node).ConfigureAwait(false);

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
            Assert.True(result.Value!.IsListOfValues());
            if (result.Value.Count() == 0)
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
            const string node = "http://test.org/UA/Data/#i=2243";
            var expected = await _readExpected(_connection, node).ConfigureAwait(false);

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

            Assert.True(result.Value.IsListOfValues(), $"{result.Value} is not a list.");
            if (result.Value.Count() == 0)
            {
                return;
            }

            Assert.True(result.Value[0].IsBytes());
            Assert.Equal("XmlElement", result.DataType);
            var xml = result.Value[0].ConvertTo<XmlElement>();
            Assert.NotNull(xml);
        }

        public async Task NodeReadStaticArrayNodeIdValueVariableTestAsync(CancellationToken ct = default)
        {
            var browser = _services();
            const string node = "http://test.org/UA/Data/#i=2244";
            var expected = await _readExpected(_connection, node).ConfigureAwait(false);

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

            Assert.True(result.Value.IsListOfValues(), $"{result.Value} is not a list.");
            if (result.Value.Count() == 0)
            {
                return;
            }

            AssertNodeIdElement(result.Value[0]);
            Assert.Equal("NodeId", result.DataType);
        }

        public async Task NodeReadStaticArrayExpandedNodeIdValueVariableTestAsync(CancellationToken ct = default)
        {
            var browser = _services();
            const string node = "http://test.org/UA/Data/#i=2245";
            var expected = await _readExpected(_connection, node).ConfigureAwait(false);

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

            Assert.True(result.Value.IsListOfValues(), $"{result.Value} is not a list.");
            if (result.Value.Count() == 0)
            {
                return;
            }

            AssertNodeIdElement(result.Value[0]);
            Assert.Equal("ExpandedNodeId", result.DataType);
        }

        private static void AssertNodeIdElement(JsonNode? value)
        {
            if (value is null)
            {
                return;
            }
            if (value.IsString())
            {
                return;
            }
            Assert.True(value.IsObject(), $"{value} is not a node id.");
            Assert.NotNull(value!["Id"]);
        }

        public async Task NodeReadStaticArrayQualifiedNameValueVariableTestAsync(CancellationToken ct = default)
        {
            var browser = _services();
            const string node = "http://test.org/UA/Data/#i=2246";
            var expected = await _readExpected(_connection, node).ConfigureAwait(false);

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

            Assert.True(result.Value.IsListOfValues(), $"{result.Value} is not a list.");
            if (result.Value.Count() == 0)
            {
                return;
            }

            Assert.True(result.Value[0].IsString(), $"{result.Value[0]} is not a string.");
            Assert.Equal("QualifiedName", result.DataType);
        }

        public async Task NodeReadStaticArrayLocalizedTextValueVariableTestAsync(CancellationToken ct = default)
        {
            var browser = _services();
            const string node = "http://test.org/UA/Data/#i=2247";
            var expected = await _readExpected(_connection, node).ConfigureAwait(false);

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

            Assert.True(result.Value.IsListOfValues(), $"{result.Value} is not a list.");
            if (result.Value.Count() == 0)
            {
                return;
            }

            Assert.True(result.Value[0].IsObject(), $"{result.Value[0]} is not an object.");
            Assert.Equal("LocalizedText", result.DataType);
        }

        public async Task NodeReadStaticArrayStatusCodeValueVariableTestAsync(CancellationToken ct = default)
        {
            var browser = _services();
            const string node = "http://test.org/UA/Data/#i=2248";
            var expected = await _readExpected(_connection, node).ConfigureAwait(false);

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

            Assert.True(result.Value.IsListOfValues(), $"{result.Value} is not a list.");
            if (result.Value.Count() == 0)
            {
                return;
            }
            Assert.True(
               result.Value[0].IsObject() ||
               result.Value[0].IsInteger(), $"{result.Value[0]} is not a integer or object.");
            Assert.Equal("StatusCode", result.DataType);
        }

        public async Task NodeReadStaticArrayVariantValueVariableTestAsync(CancellationToken ct = default)
        {
            var browser = _services();
            const string node = "http://test.org/UA/Data/#i=2249";
            var expected = await _readExpected(_connection, node).ConfigureAwait(false);

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

            Assert.True(result.Value.IsListOfValues(), $"{result.Value} is not a list.");
        }

        public async Task NodeReadStaticArrayEnumerationValueVariableTestAsync(CancellationToken ct = default)
        {
            var browser = _services();
            const string node = "http://test.org/UA/Data/#i=2250";
            var expected = await _readExpected(_connection, node).ConfigureAwait(false);

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

            Assert.True(result.Value.IsListOfValues(), $"{result.Value} is not a list.");
            if (result.Value.Count() == 0)
            {
                return;
            }

            Assert.True(result.Value[0].IsInteger(), $"{result.Value[0]} is not an integer.");
            Assert.Equal("Int32", result.DataType);
        }

        public async Task NodeReadStaticArrayStructureValueVariableTestAsync(CancellationToken ct = default)
        {
            var browser = _services();
            const string node = "http://test.org/UA/Data/#i=2251";
            var expected = await _readExpected(_connection, node).ConfigureAwait(false);

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

            Assert.True(result.Value.IsListOfValues(), $"{result.Value} is not a list.");
            if (result.Value.Count() == 0)
            {
                return;
            }

            Assert.True(result.Value[0].IsObject(), $"{result.Value[0]} is not an object.");
            // TODO: Assert.Equal(VariantValueType.Bytes, (result.Value)[0].Type);
            Assert.Equal("ExtensionObject", result.DataType);
        }

        public async Task NodeReadStaticArrayNumberValueVariableTestAsync(CancellationToken ct = default)
        {
            var browser = _services();
            const string node = "http://test.org/UA/Data/#i=2252";
            var expected = await _readExpected(_connection, node).ConfigureAwait(false);

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

            Assert.True(result.Value.IsArray(), $"Not an array {result.Value}");
            if (result.Value.Count() == 0)
            {
                return;
            }
            Assert.True(result.Value[0].IsObject(), $"Not an object {result.Value[0]}");
            Assert.NotNull(result.Value[0]!["Value"]);
        }

        public async Task NodeReadStaticArrayIntegerValueVariableTestAsync(CancellationToken ct = default)
        {
            var browser = _services();
            const string node = "http://test.org/UA/Data/#i=2253";
            var expected = await _readExpected(_connection, node).ConfigureAwait(false);

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

            Assert.True(result.Value.IsListOfValues(), $"{result.Value} is not a list.");
            if (result.Value.Count() == 0)
            {
                return;
            }

            AssertAbstractNumericElement(result.Value[0]);
        }

        public async Task NodeReadStaticArrayUIntegerValueVariableTestAsync(CancellationToken ct = default)
        {
            var browser = _services();
            const string node = "http://test.org/UA/Data/#i=2254";
            var expected = await _readExpected(_connection, node).ConfigureAwait(false);

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

            Assert.True(result.Value.IsArray(), $"Not an array {result.Value}");
            if (result.Value.Count() == 0)
            {
                return;
            }

            AssertAbstractNumericElement(result.Value[0]);
        }

        private static void AssertAbstractNumericElement(JsonNode? value)
        {
            if (value is null)
            {
                return;
            }
            if (value.IsObject())
            {
                Assert.NotNull(value!["Value"]);
                return;
            }
            Assert.Equal(JsonValueKind.Number, value.GetValueKind());
        }

        /// <summary>
        /// Helper to compare equal value
        /// </summary>
        /// <param name="expected"></param>
        /// <param name="value"></param>
        private static void AssertEqualValue(JsonNode? expected, JsonNode? value)
        {
            Assert.True(JsonNode.DeepEquals(expected, value),
                $"Expected: {expected}  != Actual: {value} ");
        }

        private readonly T _connection;
        private readonly Func<T, string, Task<JsonNode?>> _readExpected;
        private readonly Func<INodeServices<T>> _services;
    }
}
