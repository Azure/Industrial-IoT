// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Testing.Tests {
    using Microsoft.Azure.IIoT.OpcUa.Twin.Models;
    using Microsoft.Azure.IIoT.OpcUa.Twin;
    using Microsoft.Azure.IIoT.Serializers;
    using Microsoft.Azure.IIoT.Serializers.NewtonSoft;
    using MemoryBuffer;
    using System;
    using System.Threading.Tasks;
    using System.Xml;
    using Xunit;

    public class WriteScalarValueTests<T> {

        /// <summary>
        /// Create node services tests
        /// </summary>
        public WriteScalarValueTests(Func<INodeServices<T>> services, T endpoint,
            Func<T, string, Task<VariantValue>> readExpected) {
            _services = services;
            _endpoint = endpoint;
            _readExpected = readExpected;
            _serializer = new NewtonSoftJsonSerializer();
        }

        public async Task NodeWriteStaticScalarBooleanValueVariableTestAsync() {

            var browser = _services();
            var node = "http://test.org/UA/Data/#i=10216";

            VariantValue expected = false;

            // Act
            var result = await browser.NodeValueWriteAsync(_endpoint,
                new ValueWriteRequestModel {
                    NodeId = node,
                    Value = expected,
                    DataType = "Boolean"
                });

            // Assert
            await AssertResultAsync(node, expected, result);

            expected = true;

            // Act
            result = await browser.NodeValueWriteAsync(_endpoint,
                new ValueWriteRequestModel {
                    NodeId = "ns=2;i=10216",
                    Value = expected,
                    DataType = "Boolean"
                });

            // Assert
            await AssertResultAsync(node, expected, result);
        }


        public async Task NodeWriteStaticScalarBooleanValueVariableWithBrowsePathTest1Async() {

            var browser = _services();
            var node = "http://test.org/UA/Data/#i=10159"; // Scalar
            var path = new[] {
                ".http://test.org/UA/Data/#BooleanValue"
            };

            VariantValue expected = false;

            // Act
            var result = await browser.NodeValueWriteAsync(_endpoint,
                new ValueWriteRequestModel {
                    NodeId = node,
                    BrowsePath = path,
                    Value = expected,
                    DataType = "Boolean"
                });

            // Assert
            await AssertResultAsync("http://test.org/UA/Data/#i=10216", expected, result);

            expected = true;

            // Act
            result = await browser.NodeValueWriteAsync(_endpoint,
                new ValueWriteRequestModel {
                    NodeId = "ns=2;i=10159",
                    BrowsePath = path,
                    Value = expected,
                    DataType = "Boolean"
                });

            // Assert
            await AssertResultAsync("http://test.org/UA/Data/#i=10216", expected, result);
        }


        public async Task NodeWriteStaticScalarBooleanValueVariableWithBrowsePathTest2Async() {

            var browser = _services();
            var node = "http://test.org/UA/Data/#i=10159"; // Scalar
            var path = new[] {
                "http://test.org/UA/Data/#BooleanValue"
            };

            VariantValue expected = false;

            // Act
            var result = await browser.NodeValueWriteAsync(_endpoint,
                new ValueWriteRequestModel {
                    NodeId = node,
                    BrowsePath = path,
                    Value = expected,
                    DataType = "Boolean"
                });

            // Assert
            await AssertResultAsync("http://test.org/UA/Data/#i=10216", expected, result);

            expected = true;

            // Act
            result = await browser.NodeValueWriteAsync(_endpoint,
                new ValueWriteRequestModel {
                    NodeId = "ns=2;i=10159",
                    BrowsePath = path,
                    Value = expected,
                    DataType = "Boolean"
                });

            // Assert
            await AssertResultAsync("http://test.org/UA/Data/#i=10216", expected, result);
        }


        public async Task NodeWriteStaticScalarBooleanValueVariableWithBrowsePathTest3Async() {

            var browser = _services();
            var path = new[] {
                "Objects",
                "http://test.org/UA/Data/#Data",
                "http://test.org/UA/Data/#Static",
                "http://test.org/UA/Data/#Scalar",
                "http://test.org/UA/Data/#BooleanValue"
            };

            VariantValue expected = false;

            // Act
            var result = await browser.NodeValueWriteAsync(_endpoint,
                new ValueWriteRequestModel {
                    BrowsePath = path,
                    Value = expected,
                    DataType = "Boolean"
                });

            // Assert
            await AssertResultAsync("http://test.org/UA/Data/#i=10216", expected, result);

            expected = true;

            // Act
            result = await browser.NodeValueWriteAsync(_endpoint,
                new ValueWriteRequestModel {
                    BrowsePath = path,
                    Value = expected,
                    DataType = "Boolean"
                });

            // Assert
            await AssertResultAsync("http://test.org/UA/Data/#i=10216", expected, result);
        }


        public async Task NodeWriteStaticScalarSByteValueVariableTestAsync() {

            var browser = _services();
            var node = "http://test.org/UA/Data/#i=10217";

            var expected = _serializer.Parse("-61");

            // Act
            var result = await browser.NodeValueWriteAsync(_endpoint,
                new ValueWriteRequestModel {
                    NodeId = node,
                    Value = expected,
                    DataType = "SByte"
                });

            // Assert
            await AssertResultAsync(node, expected, result);
        }


        public async Task NodeWriteStaticScalarByteValueVariableTestAsync() {

            var browser = _services();
            var node = "http://test.org/UA/Data/#i=10218";

            var expected = _serializer.Parse("216");

            // Act
            var result = await browser.NodeValueWriteAsync(_endpoint,
                new ValueWriteRequestModel {
                    NodeId = node,
                    Value = expected,
                    DataType = "Byte"
                });

            // Assert
            await AssertResultAsync(node, expected, result);
        }


        public async Task NodeWriteStaticScalarInt16ValueVariableTestAsync() {

            var browser = _services();
            var node = "http://test.org/UA/Data/#i=10219";

            var expected = _serializer.Parse("15373");

            // Act
            var result = await browser.NodeValueWriteAsync(_endpoint,
                new ValueWriteRequestModel {
                    NodeId = node,
                    Value = expected,
                    DataType = "Int16"
                });

            // Assert
            await AssertResultAsync(node, expected, result);
        }


        public async Task NodeWriteStaticScalarUInt16ValueVariableTestAsync() {

            var browser = _services();
            var node = "http://test.org/UA/Data/#i=10220";

            var expected = _serializer.Parse("52454");

            // Act
            var result = await browser.NodeValueWriteAsync(_endpoint,
                new ValueWriteRequestModel {
                    NodeId = node,
                    Value = expected,
                    DataType = "UInt16"
                });

            // Assert
            await AssertResultAsync(node, expected, result);
        }


        public async Task NodeWriteStaticScalarInt32ValueVariableTestAsync() {

            var browser = _services();
            var node = "http://test.org/UA/Data/#i=10221";

            var expected = _serializer.Parse(
                "1966214362");

            // Act
            var result = await browser.NodeValueWriteAsync(_endpoint,
                new ValueWriteRequestModel {
                    NodeId = node,
                    Value = expected,
                    DataType = "Int32"
                });

            // Assert
            await AssertResultAsync(node, expected, result);
        }


        public async Task NodeWriteStaticScalarUInt32ValueVariableTestAsync() {

            var browser = _services();
            var node = "http://test.org/UA/Data/#i=10222";

            var expected = _serializer.Parse("2235103439");

            // Act
            var result = await browser.NodeValueWriteAsync(_endpoint,
                new ValueWriteRequestModel {
                    NodeId = node,
                    Value = expected,
                    DataType = "UInt32"
                });

            // Assert
            await AssertResultAsync(node, expected, result);
        }


        public async Task NodeWriteStaticScalarInt64ValueVariableTestAsync() {

            var browser = _services();
            var node = "http://test.org/UA/Data/#i=10223";

            var expected = _serializer.Parse("1485146186671575531");

            // Act
            var result = await browser.NodeValueWriteAsync(_endpoint,
                new ValueWriteRequestModel {
                    NodeId = node,
                    Value = expected,
                    DataType = "Int64"
                });

            // Assert
            await AssertResultAsync(node, expected, result);
        }


        public async Task NodeWriteStaticScalarUInt64ValueVariableTestAsync() {

            var browser = _services();
            var node = "http://test.org/UA/Data/#i=10224";

            var expected = _serializer.Parse("5415129398295885582");

            // Act
            var result = await browser.NodeValueWriteAsync(_endpoint,
                new ValueWriteRequestModel {
                    NodeId = node,
                    Value = expected,
                    DataType = "UInt64"
                });

            // Assert
            await AssertResultAsync(node, expected, result);
        }


        public async Task NodeWriteStaticScalarFloatValueVariableTestAsync() {

            var browser = _services();
            var node = "http://test.org/UA/Data/#i=10225";

            var expected = _serializer.Parse(
                "1.65278221E-37");

            // Act
            var result = await browser.NodeValueWriteAsync(_endpoint,
                new ValueWriteRequestModel {
                    NodeId = node,
                    Value = expected,
                    DataType = "Float"
                });

            // Assert
            await AssertResultAsync(node, expected, result);
        }


        public async Task NodeWriteStaticScalarDoubleValueVariableTestAsync() {

            var browser = _services();
            var node = "http://test.org/UA/Data/#i=10226";

            var expected = _serializer.Parse("103.27073669433594");

            // Act
            var result = await browser.NodeValueWriteAsync(_endpoint,
                new ValueWriteRequestModel {
                    NodeId = node,
                    Value = expected,
                    DataType = "Double"
                });

            // Assert
            await AssertResultAsync(node, expected, result);
        }


        public async Task NodeWriteStaticScalarStringValueVariableTestAsync() {

            var browser = _services();
            var node = "http://test.org/UA/Data/#i=10227";

            var expected = _serializer.Parse(
                "\"Red+ Green] Cow^ Purple Horse~ Elephant^ Horse Lime\"");

            // Act
            var result = await browser.NodeValueWriteAsync(_endpoint,
                new ValueWriteRequestModel {
                    NodeId = node,
                    Value = expected,
                    DataType = "String"
                });

            // Assert
            await AssertResultAsync(node, expected, result);
        }


        public async Task NodeWriteStaticScalarDateTimeValueVariableTestAsync() {

            var browser = _services();
            var node = "http://test.org/UA/Data/#i=10228";

            VariantValue expected = DateTime.UtcNow + TimeSpan.FromDays(11);

            // Act
            var result = await browser.NodeValueWriteAsync(_endpoint,
                new ValueWriteRequestModel {
                    NodeId = node,
                    Value = expected,
                    DataType = "DateTime"
                });

            // Assert
            await AssertResultAsync(node, expected, result);
        }


        public async Task NodeWriteStaticScalarGuidValueVariableTestAsync() {

            var browser = _services();
            var node = "http://test.org/UA/Data/#i=10229";

            VariantValue expected = "bdc1d303-2355-6173-9314-1816b7315b96";

            // Act
            var result = await browser.NodeValueWriteAsync(_endpoint,
                new ValueWriteRequestModel {
                    NodeId = node,
                    Value = expected,
                    DataType = "Guid"
                });

            // Assert
            await AssertResultAsync(node, expected, result);
        }


        public async Task NodeWriteStaticScalarByteStringValueVariableTestAsync() {

            var browser = _services();
            var node = "http://test.org/UA/Data/#i=10230";

            var expected = _serializer.Parse(
               "\"+1q+tSjpWzavev/hDIb4gk/xHLZGD4VscxJEWo2QzUU145zcKKra6WaGpq" +
               "hzgIeNIJNnQD/gruzUUkIWpQA=\"");

            // Act
            var result = await browser.NodeValueWriteAsync(_endpoint,
                new ValueWriteRequestModel {
                    NodeId = node,
                    Value = expected,
                    DataType = "ByteString"
                });

            // Assert
            await AssertResultAsync(node, expected, result);
        }


        public async Task NodeWriteStaticScalarXmlElementValueVariableTestAsync() {

            var browser = _services();
            var node = "http://test.org/UA/Data/#i=10231";

            var expected = _serializer.FromObject(XmlElementEx.SerializeObject(
                new MemoryBufferInstance {
                    Name = "test",
                    TagCount = 333,
                    DataType = "Byte"
                }));

            // Act
            var result = await browser.NodeValueWriteAsync(_endpoint,
                new ValueWriteRequestModel {
                    NodeId = node,
                    Value = expected,
                    DataType = "XmlElement"
                });

            // Assert
            await AssertResultAsync(node, expected, result);
        }


        public async Task NodeWriteStaticScalarNodeIdValueVariableTestAsync() {

            var browser = _services();
            var node = "http://test.org/UA/Data/#i=10232";

            VariantValue expected = "http://samples.org/UA/memorybuffer#i=2040578002";

            // Act
            var result = await browser.NodeValueWriteAsync(_endpoint,
                new ValueWriteRequestModel {
                    NodeId = node,
                    Value = expected,
                    DataType = "NodeId"
                });

            // Assert
            await AssertResultAsync(node, expected, result);
        }


        public async Task NodeWriteStaticScalarExpandedNodeIdValueVariableTestAsync() {

            var browser = _services();
            var node = "http://test.org/UA/Data/#i=10233";

            VariantValue expected = "http://opcfoundation.org/UA/Diagnostics#i=1375605653";

            // Act
            var result = await browser.NodeValueWriteAsync(_endpoint,
                new ValueWriteRequestModel {
                    NodeId = node,
                    Value = expected,
                    DataType = "ExpandedNodeId"
                });

            // Assert
            await AssertResultAsync(node, expected, result);
        }



        public async Task NodeWriteStaticScalarQualifiedNameValueVariableTestAsync() {

            var browser = _services();
            var node = "http://test.org/UA/Data/#i=10234";

            var expected = _serializer.FromObject("http://test.org/UA/Data/#testname");

            // Act
            var result = await browser.NodeValueWriteAsync(_endpoint,
                new ValueWriteRequestModel {
                    NodeId = node,
                    Value = expected,
                    DataType = "QualifiedName"
                });

            // Assert
            await AssertResultAsync(node, expected, result);
        }


        public async Task NodeWriteStaticScalarLocalizedTextValueVariableTestAsync() {

            var browser = _services();
            var node = "http://test.org/UA/Data/#i=10235";

            var expected = _serializer.Parse(
                "{\"Text\":\"자주색 들쭉) 망고 고양이\",\"Locale\":\"ko\"}");

            // Act
            var result = await browser.NodeValueWriteAsync(_endpoint,
                new ValueWriteRequestModel {
                    NodeId = node,
                    Value = expected,
                    DataType = "LocalizedText"
                });

            // Assert
            await AssertResultAsync(node, expected, result);
        }


        public async Task NodeWriteStaticScalarStatusCodeValueVariableTestAsync() {

            var browser = _services();
            var node = "http://test.org/UA/Data/#i=10236";

            var expected = _serializer.Parse("11927552");

            // Act
            var result = await browser.NodeValueWriteAsync(_endpoint,
                new ValueWriteRequestModel {
                    NodeId = node,
                    Value = expected,
                    DataType = "StatusCode"
                });

            // Assert
            await AssertResultAsync(node, expected, result);
        }


        public async Task NodeWriteStaticScalarVariantValueVariableTestAsync() {

            var browser = _services();
            var node = "http://test.org/UA/Data/#i=10237";

            var expected = _serializer.Parse("-2.5828845095702735E-29");

            // Act
            var result = await browser.NodeValueWriteAsync(_endpoint,
                new ValueWriteRequestModel {
                    NodeId = node,
                    Value = expected,
                    DataType = "BaseDataType"
                });

            // Assert
            await AssertResultAsync(node, expected, result);
        }


        public async Task NodeWriteStaticScalarEnumerationValueVariableTestAsync() {

            var browser = _services();
            var node = "http://test.org/UA/Data/#i=10238";

            var expected = _serializer.Parse("1137262927");

            // Act
            var result = await browser.NodeValueWriteAsync(_endpoint,
                new ValueWriteRequestModel {
                    NodeId = node,
                    Value = expected,
                    DataType = "Int32"
                    // TODO: Assert.Equal("Enumeration", result.DataType);
                });

            // Assert
            await AssertResultAsync(node, expected, result);
        }


        public async Task NodeWriteStaticScalarStructuredValueVariableTestAsync() {

            var browser = _services();
            var node = "http://test.org/UA/Data/#i=10239";

            var expected = _serializer.Parse(@"
{
    ""TypeId"": ""http://test.org/UA/Data/#i=11437"",
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
        ""XmlElementValue"": {
            ""n0:Mango"": {
                ""@Monkey"": ""Monkey"",
                ""@Snake"": ""Cow"",
                ""@Red"": ""White"",
                ""@Grape"": ""Lemon"",
                ""@Banana"": ""Green"",
                ""@Lime"": ""Sheep"",
                ""@Strawberry"": ""Mangod"",
                ""@Elephant"": ""Strawberry"",
                ""@Purple"": ""Greend"",
                ""@Sheep"": ""Lemond"",
                ""@xmlns:n0"": ""http://Peach"",
                ""n0:Grape"": [ ""Yellow+ Elephant Elephant% Dragon{ Pineapple( Red* Pineapple' White Black? Pig White"", ""Monkey Grape Mango+ Pineapple Snake Dog Red Mango} Pineapple' Pineapple Pig] Elephant"", ""Rat Purple: Strawberry- Peach Black\"" Yellow] Strawberry Black Banana# Horse( Peach?"" ],
                ""n0:White"": ""Pineapple Blue{ Dog Lemon Cat Lime Pineapple; Black, Rat Mango"",
                ""n0:Red"": [ ""Pineapple Sheep Banana Mango~ Peach] Green< Black. Green Black. Mango Pineapple Cow; Pineapple Red="", ""White> Banana Black> Purple Snake: Red` Green Blue^ Elephant White Blueberry Cat Sheep"" ],
                ""n0:Pineapple"": ""Green Yellow Cat Black Purple, Monkey Cow* Lime Purple{ Purple* Pig( Lemon' Banana- Sheep#"",
                ""n0:Snake"": ""Horse Blueberry> Black White% Horse Red@ Grape$ White Purple"",
                ""n0:Mango"": ""Lime! Banana> Strawberry Sheep~ Blueberry% Monkey\"" Green/ Sheep Horse^ Snake Red@""
            }
        },
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
            var result = await browser.NodeValueWriteAsync(_endpoint,
                new ValueWriteRequestModel {
                    NodeId = node,
                    Value = expected,
                    DataType = "ExtensionObject"
                });

            // Assert
            await AssertResultAsync(node, expected, result);
        }


        public async Task NodeWriteStaticScalarNumberValueVariableTestAsync() {

            var browser = _services();
            var node = "http://test.org/UA/Data/#i=10240";

            var expected = _serializer.Parse("-44");

            // Act
            var result = await browser.NodeValueWriteAsync(_endpoint,
                new ValueWriteRequestModel {
                    NodeId = node,
                    Value = expected,
                    DataType = "SByte"
                    // Assert.Equal("Number", result.DataType);
                });

            // Assert
            await AssertResultAsync(node, expected, result);
        }


        public async Task NodeWriteStaticScalarIntegerValueVariableTestAsync() {

            var browser = _services();
            var node = "http://test.org/UA/Data/#i=10241";

            var expected = _serializer.Parse("94903859");

            // Act
            var result = await browser.NodeValueWriteAsync(_endpoint,
                new ValueWriteRequestModel {
                    NodeId = node,
                    Value = expected,
                    DataType = "Int32"
                    // Assert.Equal("Integer", result.DataType);
                });

            // Assert
            await AssertResultAsync(node, expected, result);
        }


        public async Task NodeWriteStaticScalarUIntegerValueVariableTestAsync() {

            var browser = _services();
            var node = "http://test.org/UA/Data/#i=10242";

            var expected = _serializer.Parse("64817");

            // Act
            var result = await browser.NodeValueWriteAsync(_endpoint,
                new ValueWriteRequestModel {
                    NodeId = node,
                    Value = expected,
                    DataType = "UInt32"
                    // Assert.Equal("UInteger", result.DataType);
                });

            // Assert
            await AssertResultAsync(node, expected, result);
        }

        private async Task AssertResultAsync(string node, VariantValue expected,
            ValueWriteResultModel result) {
            var value = await _readExpected(_endpoint, node);
            Assert.NotNull(value);
            Assert.Null(result.ErrorInfo);

            Assert.True(expected.Equals(value), $"{expected} != {value}");
            Assert.Equal(expected, value);
        }

        private readonly T _endpoint;
        private readonly IJsonSerializer _serializer;
        private readonly Func<T, string, Task<VariantValue>> _readExpected;
        private readonly Func<INodeServices<T>> _services;
    }
}
