// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Testing.Tests
{
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Furly.Extensions.Serializers;
    using Furly.Extensions.Serializers.Json;
    using MemoryBuffer;
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
            Func<T, string, IJsonSerializer, Task<VariantValue>> readExpected)
        {
            _services = services;
            _connection = connection;
            _readExpected = readExpected;
            _serializer = new DefaultJsonSerializer();
        }

        public async Task NodeWriteStaticScalarBooleanValueVariableTestAsync(CancellationToken ct = default)
        {
            var services = _services();
            const string node = "http://test.org/UA/Data/#i=10216";

            VariantValue expected = false;

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
                NodeId = "ns=2;i=10216",
                Value = expected,
                DataType = "Boolean"
            }, ct).ConfigureAwait(false);

            // Assert
            await AssertResultAsync(node, expected, result).ConfigureAwait(false);
        }

        public async Task NodeWriteStaticScalarBooleanValueVariableWithBrowsePathTest1Async(CancellationToken ct = default)
        {
            var services = _services();
            const string node = "http://test.org/UA/Data/#i=10159"; // Scalar
            var path = new[] {
                ".http://test.org/UA/Data/#BooleanValue"
            };

            VariantValue expected = false;

            // Act
            var result = await services.ValueWriteAsync(_connection, new ValueWriteRequestModel
            {
                NodeId = node,
                BrowsePath = path,
                Value = expected,
                DataType = "Boolean"
            }, ct).ConfigureAwait(false);

            // Assert
            await AssertResultAsync("http://test.org/UA/Data/#i=10216", expected, result).ConfigureAwait(false);

            expected = true;

            // Act
            result = await services.ValueWriteAsync(_connection, new ValueWriteRequestModel
            {
                NodeId = "ns=2;i=10159",
                BrowsePath = path,
                Value = expected,
                DataType = "Boolean"
            }, ct).ConfigureAwait(false);

            // Assert
            await AssertResultAsync("http://test.org/UA/Data/#i=10216", expected, result).ConfigureAwait(false);
        }

        public async Task NodeWriteStaticScalarBooleanValueVariableWithBrowsePathTest2Async(CancellationToken ct = default)
        {
            var services = _services();
            const string node = "http://test.org/UA/Data/#i=10159"; // Scalar
            var path = new[] {
                "http://test.org/UA/Data/#BooleanValue"
            };

            VariantValue expected = false;

            // Act
            var result = await services.ValueWriteAsync(_connection, new ValueWriteRequestModel
            {
                NodeId = node,
                BrowsePath = path,
                Value = expected,
                DataType = "Boolean"
            }, ct).ConfigureAwait(false);

            // Assert
            await AssertResultAsync("http://test.org/UA/Data/#i=10216", expected, result).ConfigureAwait(false);

            expected = true;

            // Act
            result = await services.ValueWriteAsync(_connection, new ValueWriteRequestModel
            {
                NodeId = "ns=2;i=10159",
                BrowsePath = path,
                Value = expected,
                DataType = "Boolean"
            }, ct).ConfigureAwait(false);

            // Assert
            await AssertResultAsync("http://test.org/UA/Data/#i=10216", expected, result).ConfigureAwait(false);
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

            VariantValue expected = false;

            // Act
            var result = await services.ValueWriteAsync(_connection, new ValueWriteRequestModel
            {
                BrowsePath = path,
                Value = expected,
                DataType = "Boolean"
            }, ct).ConfigureAwait(false);

            // Assert
            await AssertResultAsync("http://test.org/UA/Data/#i=10216", expected, result).ConfigureAwait(false);

            expected = true;

            // Act
            result = await services.ValueWriteAsync(_connection, new ValueWriteRequestModel
            {
                BrowsePath = path,
                Value = expected,
                DataType = "Boolean"
            }, ct).ConfigureAwait(false);

            // Assert
            await AssertResultAsync("http://test.org/UA/Data/#i=10216", expected, result).ConfigureAwait(false);
        }

        public async Task NodeWriteStaticScalarSByteValueVariableTestAsync(CancellationToken ct = default)
        {
            var services = _services();
            const string node = "http://test.org/UA/Data/#i=10217";

            var expected = _serializer.Parse("-61");

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
            const string node = "http://test.org/UA/Data/#i=10218";

            var expected = _serializer.Parse("216");

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
            const string node = "http://test.org/UA/Data/#i=10219";

            var expected = _serializer.Parse("15373");

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
            const string node = "http://test.org/UA/Data/#i=10220";

            var expected = _serializer.Parse("52454");

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
            const string node = "http://test.org/UA/Data/#i=10221";

            var expected = _serializer.Parse(
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
            const string node = "http://test.org/UA/Data/#i=10222";

            var expected = _serializer.Parse("2235103439");

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
            const string node = "http://test.org/UA/Data/#i=10223";

            var expected = _serializer.Parse("1485146186671575531");

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
            const string node = "http://test.org/UA/Data/#i=10224";

            var expected = _serializer.Parse("5415129398295885582");

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
            const string node = "http://test.org/UA/Data/#i=10225";

            var expected = _serializer.Parse(
                "1.65278221E-37");

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
            const string node = "http://test.org/UA/Data/#i=10226";

            var expected = _serializer.Parse("103.27073669433594");

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
            const string node = "http://test.org/UA/Data/#i=10227";

            var expected = _serializer.Parse(
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
            const string node = "http://test.org/UA/Data/#i=10228";

            VariantValue expected = DateTime.UtcNow + TimeSpan.FromDays(11);

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
            const string node = "http://test.org/UA/Data/#i=10229";

            VariantValue expected = "bdc1d303-2355-6173-9314-1816b7315b96";

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
            const string node = "http://test.org/UA/Data/#i=10230";

            var expected = _serializer.Parse(
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
            const string node = "http://test.org/UA/Data/#i=10231";

            var expected = _serializer.FromObject(XmlElementEx.SerializeObject(
                new MemoryBufferInstance
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
            const string node = "http://test.org/UA/Data/#i=10232";

            VariantValue expected = "http://samples.org/UA/memorybuffer#i=2040578002";

            // Act
            var result = await services.ValueWriteAsync(_connection, new ValueWriteRequestModel
            {
                NodeId = node,
                Value = expected,
                DataType = "NodeId"
            }, ct).ConfigureAwait(false);

            // Assert
            await AssertResultAsync(node, expected, result).ConfigureAwait(false);
        }

        public async Task NodeWriteStaticScalarExpandedNodeIdValueVariableTestAsync(CancellationToken ct = default)
        {
            var services = _services();
            const string node = "http://test.org/UA/Data/#i=10233";

            VariantValue expected = "http://opcfoundation.org/UA/Diagnostics#i=1375605653";

            // Act
            var result = await services.ValueWriteAsync(_connection, new ValueWriteRequestModel
            {
                NodeId = node,
                Value = expected,
                DataType = "ExpandedNodeId"
            }, ct).ConfigureAwait(false);

            // Assert
            await AssertResultAsync(node, expected, result).ConfigureAwait(false);
        }

        public async Task NodeWriteStaticScalarQualifiedNameValueVariableTestAsync(CancellationToken ct = default)
        {
            var services = _services();
            const string node = "http://test.org/UA/Data/#i=10234";

            var expected = _serializer.FromObject("http://test.org/UA/Data/#testname");

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
            const string node = "http://test.org/UA/Data/#i=10235";

            var expected = _serializer.Parse(
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
            const string node = "http://test.org/UA/Data/#i=10236";

            var expected = _serializer.Parse("11927552");

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
            const string node = "http://test.org/UA/Data/#i=10237";

            var expected = _serializer.Parse("-2.5828845095702735E-29");

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
            const string node = "http://test.org/UA/Data/#i=10238";

            var expected = _serializer.Parse("1137262927");

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
            const string node = "http://test.org/UA/Data/#i=10239";

            var expected = _serializer.Parse(@"
{
    ""TypeId"": ""http://test.org/UA/Data/#i=9440"",
    ""Encoding"": ""Json"",
    ""Body"": {
        ""BooleanValue"": false,
        ""SByteValue"": 101,
        ""ByteValue"": 16,
        ""Int16Value"": -15522,
        ""UInt16Value"": 30310,
        ""Int32Value"": 1931620437,
        ""UInt32Value"": 1871434347,
        ""Int64Value"": -485429667643080766,
        ""UInt64Value"": 455062722452308260,
        ""FloatValue"": -5.00243E+26,
        ""DoubleValue"": 0.00046682002721354365,
        ""StringValue"": ""黄色) 黄色] 桃子{ 黑色 狗[ 紫色 桃子] 狗 红色 葡萄% 桃子? 猫 猴子 绵羊"",
        ""DateTimeValue"": ""2027-02-05T11:29:29.9135123Z"",
        ""GuidValue"": ""64a055c1-1e60-67a1-e801-f996fece3eec"",
        ""ByteStringValue"": ""XmIaOczWGerdvT4+Y1BOuQ=="",
        ""XmlElementValue"": ""PG4wOum7hOiJsiDjg5bjgr/jg6Ljg6I9IlZhY2EiIOOBhOOBoeOBlD0iQ2VyZG8iIOefs+eBsD0iQXLDoW5kYW5vIiDppqw9IlBlcnJvIiB4bWxuczpuMD0iaHR0cDovL+efs+eBsCI+PG4wOue0q+iJsj5Nb25vIFZlcmRlIFV2YSBTZXJwaWVudGUgTW9ubyBBenVsIFBpw7FhIE92ZWphLiBNYW5nbyBMaW1hPC9uMDrntKvoibI+PG4wOuefs+eBsD5NZWxvY290w7NuOyBQZXJybyBBcsOhbmRhbm8gTGltw7NuJmd0OyBBbWFyaWxsbzwvbjA655+z54GwPjxuMDrjg5bjg4njgqY+T3ZlamF+IFBlcnJvIFDDunJwdXJhXiBMaW1hIFJhdGEhIEJsYW5jb18gUMO6cnB1cmE9IEdhdG88L24wOuODluODieOCpj48L24wOum7hOiJsj4="",
        ""NodeIdValue"": ""nsu=DataAccess;s=狗绵羊"",
        ""ExpandedNodeIdValue"": ""http://test.org/UA/Data//Instance#b=pQ%3d%3d"",
        ""QualifiedNameValue"": ""http://test.org/UA/Data/#%e3%83%98%e3%83%93"",
        ""LocalizedTextValue"": {
            ""Text"": ""蓝色 紫色 蓝色 红色$"",
            ""Locale"": ""zh-CN""
        },
        ""StatusCodeValue"": 1835008,
        ""VariantValue"": {
            ""Type"": ""Int32"",
            ""Body"": 184297559
        },
        ""EnumerationValue"": 0,
        ""StructureValue"": { ""TypeId"": null },
        ""Number"": {
            ""Type"": ""Double"",
            ""Body"": 0.0
        },
        ""Integer"": {
            ""Type"": ""Int64"",
            ""Body"": 5
        },
        ""UInteger"": {
            ""Type"": ""UInt64"",
            ""Body"": 0
        }
    }
}
");

            // Act
            var result = await services.ValueWriteAsync(_connection, new ValueWriteRequestModel
            {
                NodeId = node,
                Value = expected,
                DataType = "ExtensionObject"
            }, ct).ConfigureAwait(false);

            // Assert
            await AssertResultAsync(node, expected, result).ConfigureAwait(false);
        }

        public async Task NodeWriteStaticScalarNumberValueVariableTestAsync(CancellationToken ct = default)
        {
            var services = _services();
            const string node = "http://test.org/UA/Data/#i=10240";

            var expected = _serializer.Parse("-44");

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
            const string node = "http://test.org/UA/Data/#i=10241";

            var expected = _serializer.Parse("94903859");

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
            const string node = "http://test.org/UA/Data/#i=10242";

            var expected = _serializer.Parse("64817");

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

        private async Task AssertResultAsync(string node, VariantValue expected,
            ValueWriteResponseModel result)
        {
            var value = await _readExpected(_connection, node, _serializer).ConfigureAwait(false);
            Assert.NotNull(value);
            Assert.Null(result.ErrorInfo);

            Assert.True(expected.Equals(value), $"{expected} != {value}");
            Assert.Equal(expected, value);
        }

        private readonly T _connection;
        private readonly DefaultJsonSerializer _serializer;
        private readonly Func<T, string, IJsonSerializer, Task<VariantValue>> _readExpected;
        private readonly Func<INodeServices<T>> _services;
    }
}
