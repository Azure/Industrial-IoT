// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

extern alias Quickstarts;

namespace Azure.IIoT.OpcUa.Publisher.Testing.Tests
{
    using Azure.IIoT.OpcUa.Core.Serialization;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using System.Text.Json.Nodes;
    using MemoryBuffer = Quickstarts::MemoryBuffer;
    using TestData = Quickstarts::TestData;
    using Opc.Ua;
    using Opc.Ua.Extensions;
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Xml;
    using Xunit;

    public class WriteScalarValueTests<T>
    {
        /// <summary>
        /// Create node services tests
        /// </summary>
        /// <param name="services"></param>
        /// <param name="connection"></param>
        /// <param name="readExpected"></param>
        public WriteScalarValueTests(Func<INodeServices<T>> services, T connection,
            Func<T, string, Task<JsonNode?>> readExpected)
        {
            _services = services;
            _connection = connection;
            _readExpected = readExpected;
        }

        public async Task NodeWriteStaticScalarBooleanValueVariableTestAsync(CancellationToken ct = default)
        {
            var services = _services();
            const string node = "http://test.org/UA/Data/#i=2039";

            JsonNode? expected = false;

            // Act
            var result = await services.ValueWriteAsync(_connection, new ValueWriteRequestModel
            {
                NodeId = node,
                Value = expected,
                DataType = "Boolean"
            }, ct).ConfigureAwait(false);

            // Assert
            await AssertResultAsync(node, expected, result).ConfigureAwait(false);

            expected = true;

            // Act
            result = await services.ValueWriteAsync(_connection, new ValueWriteRequestModel
            {
                NodeId = $"ns={await GetTestDataNamespaceIndexAsync(ct).ConfigureAwait(false)};i=2039",
                Value = expected,
                DataType = "Boolean"
            }, ct).ConfigureAwait(false);

            // Assert
            await AssertResultAsync(node, expected, result).ConfigureAwait(false);
        }

        public async Task NodeWriteStaticScalarBooleanValueVariableWithBrowsePathTest1Async(CancellationToken ct = default)
        {
            var services = _services();
            const string node = "http://test.org/UA/Data/#i=1976"; // Scalar
            var path = new[] {
                "http://test.org/UA/Data/#BooleanValue"
            };

            JsonNode? expected = false;

            // Act
            var result = await services.ValueWriteAsync(_connection, new ValueWriteRequestModel
            {
                NodeId = node,
                BrowsePath = path,
                Value = expected,
                DataType = "Boolean"
            }, ct).ConfigureAwait(false);

            // Assert
            await AssertResultAsync("http://test.org/UA/Data/#i=2039", expected, result).ConfigureAwait(false);

            expected = true;

            // Act
            result = await services.ValueWriteAsync(_connection, new ValueWriteRequestModel
            {
                NodeId = $"ns={await GetTestDataNamespaceIndexAsync(ct).ConfigureAwait(false)};i=1976",
                BrowsePath = path,
                Value = expected,
                DataType = "Boolean"
            }, ct).ConfigureAwait(false);

            // Assert
            await AssertResultAsync("http://test.org/UA/Data/#i=2039", expected, result).ConfigureAwait(false);
        }

        public async Task NodeWriteStaticScalarBooleanValueVariableWithBrowsePathTest2Async(CancellationToken ct = default)
        {
            var services = _services();
            const string node = "http://test.org/UA/Data/#i=1976"; // Scalar
            var path = new[] {
                "http://test.org/UA/Data/#BooleanValue"
            };

            JsonNode? expected = false;

            // Act
            var result = await services.ValueWriteAsync(_connection, new ValueWriteRequestModel
            {
                NodeId = node,
                BrowsePath = path,
                Value = expected,
                DataType = "Boolean"
            }, ct).ConfigureAwait(false);

            // Assert
            await AssertResultAsync("http://test.org/UA/Data/#i=2039", expected, result).ConfigureAwait(false);

            expected = true;

            // Act
            result = await services.ValueWriteAsync(_connection, new ValueWriteRequestModel
            {
                NodeId = $"ns={await GetTestDataNamespaceIndexAsync(ct).ConfigureAwait(false)};i=1976",
                BrowsePath = path,
                Value = expected,
                DataType = "Boolean"
            }, ct).ConfigureAwait(false);

            // Assert
            await AssertResultAsync("http://test.org/UA/Data/#i=2039", expected, result).ConfigureAwait(false);
        }

        public async Task NodeWriteStaticScalarBooleanValueVariableWithBrowsePathTest3Async(CancellationToken ct = default)
        {
            var services = _services();
            var path = new[] {
                "Objects",
                "http://test.org/UA/Data/#Data",
                "http://test.org/UA/Data/#Static",
                "http://test.org/UA/Data/#Scalar",
                "http://test.org/UA/Data/#BooleanValue"
            };

            JsonNode? expected = false;

            // Act
            var result = await services.ValueWriteAsync(_connection, new ValueWriteRequestModel
            {
                BrowsePath = path,
                Value = expected,
                DataType = "Boolean"
            }, ct).ConfigureAwait(false);

            // Assert
            await AssertResultAsync("http://test.org/UA/Data/#i=2039", expected, result).ConfigureAwait(false);

            expected = true;

            // Act
            result = await services.ValueWriteAsync(_connection, new ValueWriteRequestModel
            {
                BrowsePath = path,
                Value = expected,
                DataType = "Boolean"
            }, ct).ConfigureAwait(false);

            // Assert
            await AssertResultAsync("http://test.org/UA/Data/#i=2039", expected, result).ConfigureAwait(false);
        }

        public async Task NodeWriteStaticScalarSByteValueVariableTestAsync(CancellationToken ct = default)
        {
            var services = _services();
            const string node = "http://test.org/UA/Data/#i=2040";

            var expected = JsonNode.Parse("-61");

            // Act
            var result = await services.ValueWriteAsync(_connection, new ValueWriteRequestModel
            {
                NodeId = node,
                Value = expected,
                DataType = "SByte"
            }, ct).ConfigureAwait(false);

            // Assert
            await AssertResultAsync(node, expected, result).ConfigureAwait(false);
        }

        public async Task NodeWriteStaticScalarByteValueVariableTestAsync(CancellationToken ct = default)
        {
            var services = _services();
            const string node = "http://test.org/UA/Data/#i=2041";

            var expected = JsonNode.Parse("216");

            // Act
            var result = await services.ValueWriteAsync(_connection, new ValueWriteRequestModel
            {
                NodeId = node,
                Value = expected,
                DataType = "Byte"
            }, ct).ConfigureAwait(false);

            // Assert
            await AssertResultAsync(node, expected, result).ConfigureAwait(false);
        }

        public async Task NodeWriteStaticScalarInt16ValueVariableTestAsync(CancellationToken ct = default)
        {
            var services = _services();
            const string node = "http://test.org/UA/Data/#i=2042";

            var expected = JsonNode.Parse("15373");

            // Act
            var result = await services.ValueWriteAsync(_connection, new ValueWriteRequestModel
            {
                NodeId = node,
                Value = expected,
                DataType = "Int16"
            }, ct).ConfigureAwait(false);

            // Assert
            await AssertResultAsync(node, expected, result).ConfigureAwait(false);
        }

        public async Task NodeWriteStaticScalarUInt16ValueVariableTestAsync(CancellationToken ct = default)
        {
            var services = _services();
            const string node = "http://test.org/UA/Data/#i=2043";

            var expected = JsonNode.Parse("52454");

            // Act
            var result = await services.ValueWriteAsync(_connection, new ValueWriteRequestModel
            {
                NodeId = node,
                Value = expected,
                DataType = "UInt16"
            }, ct).ConfigureAwait(false);

            // Assert
            await AssertResultAsync(node, expected, result).ConfigureAwait(false);
        }

        public async Task NodeWriteStaticScalarInt32ValueVariableTestAsync(CancellationToken ct = default)
        {
            var services = _services();
            const string node = "http://test.org/UA/Data/#i=2044";

            var expected = JsonNode.Parse(
                "1966214362");

            // Act
            var result = await services.ValueWriteAsync(_connection, new ValueWriteRequestModel
            {
                NodeId = node,
                Value = expected,
                DataType = "Int32"
            }, ct).ConfigureAwait(false);

            // Assert
            await AssertResultAsync(node, expected, result).ConfigureAwait(false);
        }

        public async Task NodeWriteStaticScalarUInt32ValueVariableTestAsync(CancellationToken ct = default)
        {
            var services = _services();
            const string node = "http://test.org/UA/Data/#i=2045";

            var expected = JsonNode.Parse("2235103439");

            // Act
            var result = await services.ValueWriteAsync(_connection, new ValueWriteRequestModel
            {
                NodeId = node,
                Value = expected,
                DataType = "UInt32"
            }, ct).ConfigureAwait(false);

            // Assert
            await AssertResultAsync(node, expected, result).ConfigureAwait(false);
        }

        public async Task NodeWriteStaticScalarInt64ValueVariableTestAsync(CancellationToken ct = default)
        {
            var services = _services();
            const string node = "http://test.org/UA/Data/#i=2046";

            var expected = JsonNode.Parse("1485146186671575531");

            // Act
            var result = await services.ValueWriteAsync(_connection, new ValueWriteRequestModel
            {
                NodeId = node,
                Value = expected,
                DataType = "Int64"
            }, ct).ConfigureAwait(false);

            // Assert
            await AssertResultAsync(node, expected, result).ConfigureAwait(false);
        }

        public async Task NodeWriteStaticScalarUInt64ValueVariableTestAsync(CancellationToken ct = default)
        {
            var services = _services();
            const string node = "http://test.org/UA/Data/#i=2047";

            var expected = JsonNode.Parse("5415129398295885582");

            // Act
            var result = await services.ValueWriteAsync(_connection, new ValueWriteRequestModel
            {
                NodeId = node,
                Value = expected,
                DataType = "UInt64"
            }, ct).ConfigureAwait(false);

            // Assert
            await AssertResultAsync(node, expected, result).ConfigureAwait(false);
        }

        public async Task NodeWriteStaticScalarFloatValueVariableTestAsync(CancellationToken ct = default)
        {
            var services = _services();
            const string node = "http://test.org/UA/Data/#i=2048";

            var expected = JsonNodeValueExtensions.FromObject(1.65278221E-37f);

            // Act
            var result = await services.ValueWriteAsync(_connection, new ValueWriteRequestModel
            {
                NodeId = node,
                Value = expected,
                DataType = "Float"
            }, ct).ConfigureAwait(false);

            // Assert
            await AssertResultAsync(node, expected, result).ConfigureAwait(false);
        }

        public async Task NodeWriteStaticScalarDoubleValueVariableTestAsync(CancellationToken ct = default)
        {
            var services = _services();
            const string node = "http://test.org/UA/Data/#i=2049";

            var expected = JsonNode.Parse("103.27073669433594");

            // Act
            var result = await services.ValueWriteAsync(_connection, new ValueWriteRequestModel
            {
                NodeId = node,
                Value = expected,
                DataType = "Double"
            }, ct).ConfigureAwait(false);

            // Assert
            await AssertResultAsync(node, expected, result).ConfigureAwait(false);
        }

        public async Task NodeWriteStaticScalarStringValueVariableTestAsync(CancellationToken ct = default)
        {
            var services = _services();
            const string node = "http://test.org/UA/Data/#i=2050";

            var expected = JsonNode.Parse(
                "\"Red+ Green] Cow^ Purple Horse~ Elephant^ Horse Lime\"");

            // Act
            var result = await services.ValueWriteAsync(_connection, new ValueWriteRequestModel
            {
                NodeId = node,
                Value = expected,
                DataType = "String"
            }, ct).ConfigureAwait(false);

            // Assert
            await AssertResultAsync(node, expected, result).ConfigureAwait(false);
        }

        public async Task NodeWriteStaticScalarDateTimeValueVariableTestAsync(CancellationToken ct = default)
        {
            var services = _services();
            const string node = "http://test.org/UA/Data/#i=2051";

            var expected = JsonNodeValueExtensions.FromObject(
                DateTime.UtcNow + TimeSpan.FromDays(11));

            // Act
            var result = await services.ValueWriteAsync(_connection, new ValueWriteRequestModel
            {
                NodeId = node,
                Value = expected,
                DataType = "DateTime"
            }, ct).ConfigureAwait(false);

            // Assert
            await AssertResultAsync(node, expected, result).ConfigureAwait(false);
        }

        public async Task NodeWriteStaticScalarGuidValueVariableTestAsync(CancellationToken ct = default)
        {
            var services = _services();
            const string node = "http://test.org/UA/Data/#i=2052";

            JsonNode? expected = "bdc1d303-2355-6173-9314-1816b7315b96";

            // Act
            var result = await services.ValueWriteAsync(_connection, new ValueWriteRequestModel
            {
                NodeId = node,
                Value = expected,
                DataType = "Guid"
            }, ct).ConfigureAwait(false);

            // Assert
            await AssertResultAsync(node, expected, result).ConfigureAwait(false);
        }

        public async Task NodeWriteStaticScalarByteStringValueVariableTestAsync(CancellationToken ct = default)
        {
            var services = _services();
            const string node = "http://test.org/UA/Data/#i=2053";

            var expected = JsonNode.Parse(
               "\"+1q+tSjpWzavev/hDIb4gk/xHLZGD4VscxJEWo2QzUU145zcKKra6WaGpq" +
               "hzgIeNIJNnQD/gruzUUkIWpQA=\"");

            // Act
            var result = await services.ValueWriteAsync(_connection, new ValueWriteRequestModel
            {
                NodeId = node,
                Value = expected,
                DataType = "ByteString"
            }, ct).ConfigureAwait(false);

            // Assert
            await AssertResultAsync(node, expected, result).ConfigureAwait(false);
        }

        public async Task NodeWriteStaticScalarXmlElementValueVariableTestAsync(CancellationToken ct = default)
        {
            var services = _services();
            const string node = "http://test.org/UA/Data/#i=2054";

            var expected = JsonNodeValueExtensions.FromObject(XmlElementEx.SerializeObject(
                new MemoryBuffer.MemoryBufferInstance
                {
                    Name = "test",
                    TagCount = 333,
                    DataType = "Byte"
                }));

            // Act
            var result = await services.ValueWriteAsync(_connection, new ValueWriteRequestModel
            {
                NodeId = node,
                Value = expected,
                DataType = "XmlElement"
            }, ct).ConfigureAwait(false);

            // Assert
            await AssertResultAsync(node, expected, result).ConfigureAwait(false);
        }

        public async Task NodeWriteStaticScalarNodeIdValueVariableTestAsync(CancellationToken ct = default)
        {
            var services = _services();
            const string node = "http://test.org/UA/Data/#i=2055";

            //
            // The Quickstarts fixture initializes this node randomly and can
            // produce an empty opaque identifier. Its JSON projection is
            // indistinguishable from null on readback, so a write test must not
            // use that random value as its expected input.
            //
            var expected = JsonValue.Create("i=84");

            var request = new ValueWriteRequestModel
            {
                NodeId = node,
                Value = expected,
                DataType = "NodeId"
            };

            JsonNode? actual = null;
            for (var attempt = 0; attempt < 3; attempt++)
            {
                var result = await services.ValueWriteAsync(_connection, request, ct)
                    .ConfigureAwait(false);
                Assert.Null(result.ErrorInfo);
                actual = await _readExpected(_connection, node).ConfigureAwait(false);
                if (JsonNode.DeepEquals(expected, actual))
                {
                    return;
                }
            }
            Assert.Fail($"{expected} != {actual} after three successful writes.");
        }

        public async Task NodeWriteStaticScalarExpandedNodeIdValueVariableTestAsync(CancellationToken ct = default)
        {
            var services = _services();
            const string node = "http://test.org/UA/Data/#i=2056";

            var value = JsonValue.Create(
                "nsu=http://test.org/UA/Data/;i=84");
            var expected = JsonValue.Create(
                "nsu=http://test.org/UA/Data/;i=84");

            // Act
            var result = await services.ValueWriteAsync(_connection, new ValueWriteRequestModel
            {
                NodeId = node,
                Value = value,
                DataType = "ExpandedNodeId"
            }, ct).ConfigureAwait(false);

            // Assert
            await AssertResultAsync(node, expected, result).ConfigureAwait(false);
        }

        public async Task NodeWriteStaticScalarQualifiedNameValueVariableTestAsync(CancellationToken ct = default)
        {
            var services = _services();
            const string node = "http://test.org/UA/Data/#i=2057";

            var expected = await ReadCanonicalValueAsync(node).ConfigureAwait(false);

            // Act
            var result = await services.ValueWriteAsync(_connection, new ValueWriteRequestModel
            {
                NodeId = node,
                Value = expected,
                DataType = "QualifiedName"
            }, ct).ConfigureAwait(false);

            // Assert
            await AssertResultAsync(node, expected, result).ConfigureAwait(false);
        }

        public async Task NodeWriteStaticScalarLocalizedTextValueVariableTestAsync(CancellationToken ct = default)
        {
            var services = _services();
            const string node = "http://test.org/UA/Data/#i=2058";

            var expected = JsonNode.Parse(
                "{\"Text\":\"자주색 들쭉) 망고 고양이\",\"Locale\":\"ko\"}");

            // Act
            var result = await services.ValueWriteAsync(_connection, new ValueWriteRequestModel
            {
                NodeId = node,
                Value = expected,
                DataType = "LocalizedText"
            }, ct).ConfigureAwait(false);

            // Assert
            await AssertResultAsync(node, expected, result).ConfigureAwait(false);
        }

        public async Task NodeWriteStaticScalarStatusCodeValueVariableTestAsync(CancellationToken ct = default)
        {
            var services = _services();
            const string node = "http://test.org/UA/Data/#i=2059";

            var expected = JsonNode.Parse("""{"Code":11927552}""");

            // Act
            var result = await services.ValueWriteAsync(_connection, new ValueWriteRequestModel
            {
                NodeId = node,
                Value = expected,
                DataType = "StatusCode"
            }, ct).ConfigureAwait(false);

            // Assert
            await AssertResultAsync(node, expected, result).ConfigureAwait(false);
        }

        public async Task NodeWriteStaticScalarVariantValueVariableTestAsync(CancellationToken ct = default)
        {
            var services = _services();
            const string node = "http://test.org/UA/Data/#i=2060";

            var expected = JsonNode.Parse("-2.5828845095702735E-29");

            // Act
            var result = await services.ValueWriteAsync(_connection, new ValueWriteRequestModel
            {
                NodeId = node,
                Value = expected,
                DataType = "BaseDataType"
            }, ct).ConfigureAwait(false);

            // Assert
            await AssertResultAsync(node, expected, result).ConfigureAwait(false);
        }

        public async Task NodeWriteStaticScalarEnumerationValueVariableTestAsync(CancellationToken ct = default)
        {
            var services = _services();
            const string node = "http://test.org/UA/Data/#i=2061";

            var expected = JsonNode.Parse("1137262927");

            // Act
            var result = await services.ValueWriteAsync(_connection, new ValueWriteRequestModel
            {
                NodeId = node,
                Value = expected,
                DataType = "Int32"
                // TODO: Assert.Equal("Enumeration", result.DataType);
            }, ct).ConfigureAwait(false);

            // Assert
            await AssertResultAsync(node, expected, result).ConfigureAwait(false);
        }

        public async Task NodeWriteStaticScalarStructuredValueVariableTestAsync(CancellationToken ct = default)
        {
            var services = _services();
            const string node = "http://test.org/UA/Data/#i=2062";

            var expected = JsonNode.Parse("""

{
    "TypeId": "http://test.org/UA/Data/#i=1078",
    "Encoding": "Json",
    "Body": {
        "BooleanValue": false,
        "SByteValue": 101,
        "ByteValue": 16,
        "Int16Value": -15522,
        "UInt16Value": 30310,
        "Int32Value": 1931620437,
        "UInt32Value": 1871434347,
        "Int64Value": -485429667643080766,
        "UInt64Value": 455062722452308260,
        "FloatValue": -5.00243E+26,
        "DoubleValue": 0.00046682002721354365,
        "StringValue": "黄色) 黄色] 桃子{ 黑色 狗[ 紫色 桃子] 狗 红色 葡萄% 桃子? 猫 猴子 绵羊",
        "DateTimeValue": "2027-02-05T11:29:29.9135123Z",
        "GuidValue": "64a055c1-1e60-67a1-e801-f996fece3eec",
        "ByteStringValue": "XmIaOczWGerdvT4+Y1BOuQ==",
        "XmlElementValue": "PG4wOum7hOiJsiDjg5bjgr/jg6Ljg6I9IlZhY2EiIOOBhOOBoeOBlD0iQ2VyZG8iIOefs+eBsD0iQXLDoW5kYW5vIiDppqw9IlBlcnJvIiB4bWxuczpuMD0iaHR0cDovL+efs+eBsCI+PG4wOue0q+iJsj5Nb25vIFZlcmRlIFV2YSBTZXJwaWVudGUgTW9ubyBBenVsIFBpw7FhIE92ZWphLiBNYW5nbyBMaW1hPC9uMDrntKvoibI+PG4wOuefs+eBsD5NZWxvY290w7NuOyBQZXJybyBBcsOhbmRhbm8gTGltw7NuJmd0OyBBbWFyaWxsbzwvbjA655+z54GwPjxuMDrjg5bjg4njgqY+T3ZlamF+IFBlcnJvIFDDunJwdXJhXiBMaW1hIFJhdGEhIEJsYW5jb18gUMO6cnB1cmE9IEdhdG88L24wOuODluODieOCpj48L24wOum7hOiJsj4=",
        "NodeIdValue": "nsu=DataAccess;s=狗绵羊",
        "ExpandedNodeIdValue": "http://test.org/UA/Data//Instance#b=pQ%3d%3d",
        "QualifiedNameValue": "http://test.org/UA/Data/#%e3%83%98%e3%83%93",
        "LocalizedTextValue": {
            "Text": "蓝色 紫色 蓝色 红色$",
            "Locale": "zh-CN"
        },
        "StatusCodeValue": 1835008,
        "VariantValue": {
            "Type": "Int32",
            "Body": 184297559
        },
        "EnumerationValue": 0,
        "StructureValue": { "TypeId": null },
        "Number": {
            "Type": "Double",
            "Body": 0.0
        },
        "Integer": {
            "Type": "Int64",
            "Body": 5
        },
        "UInteger": {
            "Type": "UInt64",
            "Body": 0
        }
    }
}

""");
            expected = CreateStructureValue();
            var input = AddTestDataStructureTypeId(expected.DeepClone());

            // Act
            var result = await services.ValueWriteAsync(_connection, new ValueWriteRequestModel
            {
                NodeId = node,
                Value = input,
                DataType = "ExtensionObject"
            }, ct).ConfigureAwait(false);

            // Assert
            await AssertStructuredResultAsync(node, result).ConfigureAwait(false);
        }

        public async Task NodeWriteStaticScalarNumberValueVariableTestAsync(CancellationToken ct = default)
        {
            var services = _services();
            const string node = "http://test.org/UA/Data/#i=2063";

            var expected = JsonNode.Parse("-44");

            // Act
            var result = await services.ValueWriteAsync(_connection, new ValueWriteRequestModel
            {
                NodeId = node,
                Value = expected,
                DataType = "SByte"
                // Assert.Equal("Number", result.DataType);
            }, ct).ConfigureAwait(false);

            // Assert
            await AssertResultAsync(node, expected, result).ConfigureAwait(false);
        }

        public async Task NodeWriteStaticScalarIntegerValueVariableTestAsync(CancellationToken ct = default)
        {
            var services = _services();
            const string node = "http://test.org/UA/Data/#i=2064";

            var expected = JsonNode.Parse("94903859");

            // Act
            var result = await services.ValueWriteAsync(_connection, new ValueWriteRequestModel
            {
                NodeId = node,
                Value = expected,
                DataType = "Int32"
                // Assert.Equal("Integer", result.DataType);
            }, ct).ConfigureAwait(false);

            // Assert
            await AssertResultAsync(node, expected, result).ConfigureAwait(false);
        }

        public async Task NodeWriteStaticScalarUIntegerValueVariableTestAsync(CancellationToken ct = default)
        {
            var services = _services();
            const string node = "http://test.org/UA/Data/#i=2065";

            var expected = JsonNode.Parse("64817");

            // Act
            var result = await services.ValueWriteAsync(_connection, new ValueWriteRequestModel
            {
                NodeId = node,
                Value = expected,
                DataType = "UInt32"
                // Assert.Equal("UInteger", result.DataType);
            }, ct).ConfigureAwait(false);

            // Assert
            await AssertResultAsync(node, expected, result).ConfigureAwait(false);
        }

        private async Task AssertResultAsync(string node, JsonNode? expected,
            ValueWriteResponseModel result)
        {
            Assert.Null(result.ErrorInfo);
            var value = await _readExpected(_connection, node).ConfigureAwait(false);
            Assert.NotNull(value);

            Assert.True(JsonNode.DeepEquals(expected, value), $"{expected} != {value}");
        }

        private async Task<JsonNode> ReadCanonicalValueAsync(string node)
        {
            var value = await _readExpected(_connection, node).ConfigureAwait(false);
            return Assert.IsAssignableFrom<JsonNode>(value);
        }

        private async Task<int> GetTestDataNamespaceIndexAsync(CancellationToken ct)
        {
            var result = await _services().ValueReadAsync(_connection,
                new ValueReadRequestModel
                {
                    NodeId = Opc.Ua.VariableIds.Server_NamespaceArray.ToString()
                }, ct).ConfigureAwait(false);
            Assert.Null(result.ErrorInfo);
            var namespaces = Assert.IsType<JsonArray>(result.Value);
            for (var index = 0; index < namespaces.Count; index++)
            {
                if (namespaces[index]?.GetValue<string>() == kTestDataNamespaceUri)
                {
                    return index;
                }
            }
            throw new Xunit.Sdk.XunitException(
                $"The TestData namespace '{kTestDataNamespaceUri}' is not advertised.");
        }

        private static JsonNode AddTestDataStructureTypeId(JsonNode value)
        {
            Assert.IsType<JsonObject>(value)["UaTypeId"] = kTestDataStructureEncodingId;
            return value;
        }

        private static JsonNode CreateStructureValue()
        {
            var value = new TestData.ScalarStructureDataType();
            return new JsonObject
            {
                ["UaEncoding"] = (byte)ExtensionObjectEncoding.Binary,
                ["UaBody"] = Convert.ToBase64String(
                    value.AsBinary(new ServiceMessageContext()))
            };
        }

        private async Task AssertStructuredResultAsync(string node,
            ValueWriteResponseModel result)
        {
            Assert.Null(result.ErrorInfo);
            var value = await _readExpected(_connection, node).ConfigureAwait(false);
            var structure = Assert.IsType<JsonObject>(value);
            Assert.False(structure["BooleanValue"]!.GetValue<bool>());
            Assert.Equal((sbyte)0, structure["SByteValue"]!.GetValue<sbyte>());
            Assert.Equal((byte)0, structure["ByteValue"]!.GetValue<byte>());
            Assert.Equal((short)0, structure["Int16Value"]!.GetValue<short>());
            Assert.Equal((ushort)0, structure["UInt16Value"]!.GetValue<ushort>());
            Assert.Equal(0, structure["Int32Value"]!.GetValue<int>());
            Assert.Equal(0u, structure["UInt32Value"]!.GetValue<uint>());
            Assert.Equal("0", structure["Int64Value"]!.GetValue<string>());
            Assert.Equal("0", structure["UInt64Value"]!.GetValue<string>());
        }

        private const string kTestDataNamespaceUri = "http://test.org/UA/Data/";
        private const string kTestDataStructureEncodingId =
            "nsu=http://test.org/UA/Data/;i=1078";
        private readonly T _connection;
        private readonly Func<T, string, Task<JsonNode?>> _readExpected;
        private readonly Func<INodeServices<T>> _services;
    }
}
