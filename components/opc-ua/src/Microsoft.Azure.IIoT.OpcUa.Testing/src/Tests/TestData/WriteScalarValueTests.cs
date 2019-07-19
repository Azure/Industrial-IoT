// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Testing.Tests {
    using MemoryBuffer;
    using Microsoft.Azure.IIoT.OpcUa.Twin;
    using Microsoft.Azure.IIoT.OpcUa.Twin.Models;
    using Newtonsoft.Json.Linq;
    using System;
    using System.Threading.Tasks;
    using System.Xml;
    using Xunit;

    public class WriteScalarValueTests<T> {

        /// <summary>
        /// Create node services tests
        /// </summary>
        /// <param name="services"></param>
        /// <param name="endpoint"></param>
        /// <param name="readExpected"></param>
        public WriteScalarValueTests(Func<INodeServices<T>> services, T endpoint,
            Func<T, string, Task<JToken>> readExpected) {
            _services = services;
            _endpoint = endpoint;
            _readExpected = readExpected;
        }

        public async Task NodeWriteStaticScalarBooleanValueVariableTest() {

            var browser = _services();
            var node = "http://test.org/UA/Data/#i=10216";

            JToken expected = false;

            // Act
            var result = await browser.NodeValueWriteAsync(_endpoint,
                new ValueWriteRequestModel {
                    NodeId = node,
                    Value = expected,
                    DataType = "Boolean"
                });

            // Assert
            await AssertResult(node, expected, result);

            expected = true;

            // Act
            result = await browser.NodeValueWriteAsync(_endpoint,
                new ValueWriteRequestModel {
                    NodeId = "ns=2;i=10216",
                    Value = expected,
                    DataType = "Boolean"
                });

            // Assert
            await AssertResult(node, expected, result);
        }


        public async Task NodeWriteStaticScalarBooleanValueVariableWithBrowsePathTest1() {

            var browser = _services();
            var node = "http://test.org/UA/Data/#i=10159"; // Scalar
            var path = new[] {
                ".http://test.org/UA/Data/#BooleanValue"
            };

            JToken expected = false;

            // Act
            var result = await browser.NodeValueWriteAsync(_endpoint,
                new ValueWriteRequestModel {
                    NodeId = node,
                    BrowsePath = path,
                    Value = expected,
                    DataType = "Boolean"
                });

            // Assert
            await AssertResult("http://test.org/UA/Data/#i=10216", expected, result);

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
            await AssertResult("http://test.org/UA/Data/#i=10216", expected, result);
        }


        public async Task NodeWriteStaticScalarBooleanValueVariableWithBrowsePathTest2() {

            var browser = _services();
            var node = "http://test.org/UA/Data/#i=10159"; // Scalar
            var path = new[] {
                "http://test.org/UA/Data/#BooleanValue"
            };

            JToken expected = false;

            // Act
            var result = await browser.NodeValueWriteAsync(_endpoint,
                new ValueWriteRequestModel {
                    NodeId = node,
                    BrowsePath = path,
                    Value = expected,
                    DataType = "Boolean"
                });

            // Assert
            await AssertResult("http://test.org/UA/Data/#i=10216", expected, result);

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
            await AssertResult("http://test.org/UA/Data/#i=10216", expected, result);
        }


        public async Task NodeWriteStaticScalarBooleanValueVariableWithBrowsePathTest3() {

            var browser = _services();
            var path = new[] {
                "Objects",
                "http://test.org/UA/Data/#Data",
                "http://test.org/UA/Data/#Static",
                "http://test.org/UA/Data/#Scalar",
                "http://test.org/UA/Data/#BooleanValue"
            };

            JToken expected = false;

            // Act
            var result = await browser.NodeValueWriteAsync(_endpoint,
                new ValueWriteRequestModel {
                    BrowsePath = path,
                    Value = expected,
                    DataType = "Boolean"
                });

            // Assert
            await AssertResult("http://test.org/UA/Data/#i=10216", expected, result);

            expected = true;

            // Act
            result = await browser.NodeValueWriteAsync(_endpoint,
                new ValueWriteRequestModel {
                    BrowsePath = path,
                    Value = expected,
                    DataType = "Boolean"
                });

            // Assert
            await AssertResult("http://test.org/UA/Data/#i=10216", expected, result);
        }


        public async Task NodeWriteStaticScalarSByteValueVariableTest() {

            var browser = _services();
            var node = "http://test.org/UA/Data/#i=10217";

            var expected = JToken.Parse("-61");

            // Act
            var result = await browser.NodeValueWriteAsync(_endpoint,
                new ValueWriteRequestModel {
                    NodeId = node,
                    Value = expected,
                    DataType = "SByte"
                });

            // Assert
            await AssertResult(node, expected, result);
        }


        public async Task NodeWriteStaticScalarByteValueVariableTest() {

            var browser = _services();
            var node = "http://test.org/UA/Data/#i=10218";

            var expected = JToken.Parse("216");

            // Act
            var result = await browser.NodeValueWriteAsync(_endpoint,
                new ValueWriteRequestModel {
                    NodeId = node,
                    Value = expected,
                    DataType = "Byte"
                });

            // Assert
            await AssertResult(node, expected, result);
        }


        public async Task NodeWriteStaticScalarInt16ValueVariableTest() {

            var browser = _services();
            var node = "http://test.org/UA/Data/#i=10219";

            var expected = JToken.Parse("15373");

            // Act
            var result = await browser.NodeValueWriteAsync(_endpoint,
                new ValueWriteRequestModel {
                    NodeId = node,
                    Value = expected,
                    DataType = "Int16"
                });

            // Assert
            await AssertResult(node, expected, result);
        }


        public async Task NodeWriteStaticScalarUInt16ValueVariableTest() {

            var browser = _services();
            var node = "http://test.org/UA/Data/#i=10220";

            var expected = JToken.Parse("52454");

            // Act
            var result = await browser.NodeValueWriteAsync(_endpoint,
                new ValueWriteRequestModel {
                    NodeId = node,
                    Value = expected,
                    DataType = "UInt16"
                });

            // Assert
            await AssertResult(node, expected, result);
        }


        public async Task NodeWriteStaticScalarInt32ValueVariableTest() {

            var browser = _services();
            var node = "http://test.org/UA/Data/#i=10221";

            var expected = JToken.Parse(
                "1966214362");

            // Act
            var result = await browser.NodeValueWriteAsync(_endpoint,
                new ValueWriteRequestModel {
                    NodeId = node,
                    Value = expected,
                    DataType = "Int32"
                });

            // Assert
            await AssertResult(node, expected, result);
        }



        public async Task NodeWriteStaticScalarUInt32ValueVariableTest() {

            var browser = _services();
            var node = "http://test.org/UA/Data/#i=10222";

            var expected = JToken.Parse("2235103439");

            // Act
            var result = await browser.NodeValueWriteAsync(_endpoint,
                new ValueWriteRequestModel {
                    NodeId = node,
                    Value = expected,
                    DataType = "UInt32"
                });

            // Assert
            await AssertResult(node, expected, result);
        }



        public async Task NodeWriteStaticScalarInt64ValueVariableTest() {

            var browser = _services();
            var node = "http://test.org/UA/Data/#i=10223";

            var expected = JToken.Parse("1485146186671575531");

            // Act
            var result = await browser.NodeValueWriteAsync(_endpoint,
                new ValueWriteRequestModel {
                    NodeId = node,
                    Value = expected,
                    DataType = "Int64"
                });

            // Assert
            await AssertResult(node, expected, result);
        }



        public async Task NodeWriteStaticScalarUInt64ValueVariableTest() {

            var browser = _services();
            var node = "http://test.org/UA/Data/#i=10224";

            var expected = JToken.Parse("5415129398295885582");

            // Act
            var result = await browser.NodeValueWriteAsync(_endpoint,
                new ValueWriteRequestModel {
                    NodeId = node,
                    Value = expected,
                    DataType = "UInt64"
                });

            // Assert
            await AssertResult(node, expected, result);
        }



        public async Task NodeWriteStaticScalarFloatValueVariableTest() {

            var browser = _services();
            var node = "http://test.org/UA/Data/#i=10225";

            var expected = JToken.Parse(
                "1.65278221E-37");

            // Act
            var result = await browser.NodeValueWriteAsync(_endpoint,
                new ValueWriteRequestModel {
                    NodeId = node,
                    Value = expected,
                    DataType = "Float"
                });

            // Assert
            await AssertResult(node, expected, result);
        }



        public async Task NodeWriteStaticScalarDoubleValueVariableTest() {

            var browser = _services();
            var node = "http://test.org/UA/Data/#i=10226";

            var expected = JToken.Parse("103.27073669433594");

            // Act
            var result = await browser.NodeValueWriteAsync(_endpoint,
                new ValueWriteRequestModel {
                    NodeId = node,
                    Value = expected,
                    DataType = "Double"
                });

            // Assert
            await AssertResult(node, expected, result);
        }



        public async Task NodeWriteStaticScalarStringValueVariableTest() {

            var browser = _services();
            var node = "http://test.org/UA/Data/#i=10227";

            var expected = JToken.Parse(
                "\"Red+ Green] Cow^ Purple Horse~ Elephant^ Horse Lime\"");

            // Act
            var result = await browser.NodeValueWriteAsync(_endpoint,
                new ValueWriteRequestModel {
                    NodeId = node,
                    Value = expected,
                    DataType = "String"
                });

            // Assert
            await AssertResult(node, expected, result);
        }



        public async Task NodeWriteStaticScalarDateTimeValueVariableTest() {

            var browser = _services();
            var node = "http://test.org/UA/Data/#i=10228";

            JToken expected = DateTime.UtcNow + TimeSpan.FromDays(11);

            // Act
            var result = await browser.NodeValueWriteAsync(_endpoint,
                new ValueWriteRequestModel {
                    NodeId = node,
                    Value = expected,
                    DataType = "DateTime"
                });

            // Assert
            await AssertResult(node, expected, result);
        }



        public async Task NodeWriteStaticScalarGuidValueVariableTest() {

            var browser = _services();
            var node = "http://test.org/UA/Data/#i=10229";

            JToken expected = "bdc1d303-2355-6173-9314-1816b7315b96";

            // Act
            var result = await browser.NodeValueWriteAsync(_endpoint,
                new ValueWriteRequestModel {
                    NodeId = node,
                    Value = expected,
                    DataType = "Guid"
                });

            // Assert
            await AssertResult(node, expected, result);
        }



        public async Task NodeWriteStaticScalarByteStringValueVariableTest() {

            var browser = _services();
            var node = "http://test.org/UA/Data/#i=10230";

            var expected = JToken.Parse(
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
            await AssertResult(node, expected, result);
        }



        public async Task NodeWriteStaticScalarXmlElementValueVariableTest() {

            var browser = _services();
            var node = "http://test.org/UA/Data/#i=10231";

            var expected = JToken.FromObject(XmlElementEx.SerializeObject(
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
            await AssertResult(node, expected, result);
        }


        public async Task NodeWriteStaticScalarNodeIdValueVariableTest() {

            var browser = _services();
            var node = "http://test.org/UA/Data/#i=10232";

            JToken expected = "http://samples.org/UA/memorybuffer#i=2040578002";

            // Act
            var result = await browser.NodeValueWriteAsync(_endpoint,
                new ValueWriteRequestModel {
                    NodeId = node,
                    Value = expected,
                    DataType = "NodeId"
                });

            // Assert
            await AssertResult(node, expected, result);
        }



        public async Task NodeWriteStaticScalarExpandedNodeIdValueVariableTest() {

            var browser = _services();
            var node = "http://test.org/UA/Data/#i=10233";

            JToken expected = "http://opcfoundation.org/UA/Diagnostics#i=1375605653";

            // Act
            var result = await browser.NodeValueWriteAsync(_endpoint,
                new ValueWriteRequestModel {
                    NodeId = node,
                    Value = expected,
                    DataType = "ExpandedNodeId"
                });

            // Assert
            await AssertResult(node, expected, result);
        }



        public async Task NodeWriteStaticScalarQualifiedNameValueVariableTest() {

            var browser = _services();
            var node = "http://test.org/UA/Data/#i=10234";

            var expected = JToken.FromObject("http://test.org/UA/Data/#testname");

            // Act
            var result = await browser.NodeValueWriteAsync(_endpoint,
                new ValueWriteRequestModel {
                    NodeId = node,
                    Value = expected,
                    DataType = "QualifiedName"
                });

            // Assert
            await AssertResult(node, expected, result);
        }


        public async Task NodeWriteStaticScalarLocalizedTextValueVariableTest() {

            var browser = _services();
            var node = "http://test.org/UA/Data/#i=10235";

            var expected = JToken.Parse(
                "{\"Text\":\"자주색 들쭉) 망고 고양이\",\"Locale\":\"ko\"}");

            // Act
            var result = await browser.NodeValueWriteAsync(_endpoint,
                new ValueWriteRequestModel {
                    NodeId = node,
                    Value = expected,
                    DataType = "LocalizedText"
                });

            // Assert
            await AssertResult(node, expected, result);
        }


        public async Task NodeWriteStaticScalarStatusCodeValueVariableTest() {

            var browser = _services();
            var node = "http://test.org/UA/Data/#i=10236";

            var expected = JToken.Parse("11927552");

            // Act
            var result = await browser.NodeValueWriteAsync(_endpoint,
                new ValueWriteRequestModel {
                    NodeId = node,
                    Value = expected,
                    DataType = "StatusCode"
                });

            // Assert
            await AssertResult(node, expected, result);
        }


        public async Task NodeWriteStaticScalarVariantValueVariableTest() {

            var browser = _services();
            var node = "http://test.org/UA/Data/#i=10237";

            var expected = JToken.Parse("-2.5828845095702735E-29");

            // Act
            var result = await browser.NodeValueWriteAsync(_endpoint,
                new ValueWriteRequestModel {
                    NodeId = node,
                    Value = expected,
                    DataType = "BaseDataType"
                });

            // Assert
            await AssertResult(node, expected, result);
        }


        public async Task NodeWriteStaticScalarEnumerationValueVariableTest() {

            var browser = _services();
            var node = "http://test.org/UA/Data/#i=10238";

            var expected = JToken.Parse("1137262927");

            // Act
            var result = await browser.NodeValueWriteAsync(_endpoint,
                new ValueWriteRequestModel {
                    NodeId = node,
                    Value = expected,
                    DataType = "Int32"
                    // TODO: Assert.Equal("Enumeration", result.DataType);
                });

            // Assert
            await AssertResult(node, expected, result);
        }



        public async Task NodeWriteStaticScalarStructuredValueVariableTest() {

            var browser = _services();
            var node = "http://test.org/UA/Data/#i=10239";

            var expected = JToken.Parse(
                "{\"TypeId\":\"http://test.org/UA/Data/#i=11437\",\"Body\":\"" +
                "AGUQXsNmdlUwInNr0otvwr+yCEpoQ/kkwUqSIrVQBk7lzusAAADA8Jc+P18A" +
                "AADpu4ToibIpIOm7hOiJsl0g5qGD5a2QeyDpu5HoibIg54uXWyDntKvoibIg" +
                "5qGD5a2QXSDni5cg57qi6ImyIOiRoeiQhCUg5qGD5a2QPyDnjKsg54y05a2Q" +
                "IOe7tee+ipPWICpktd0BwVWgZGAeoWfoAfmW/s4+7BAAAABeYho5zNYZ6t29" +
                "Pj5jUE65awQAADxuMDpNYW5nbyBNb25rZXk9Ik1vbmtleSIgU25ha2U9IkNv" +
                "dyIgUmVkPSJXaGl0ZSIgR3JhcGU9IkxlbW9uIiBCYW5hbmE9IkdyZWVuIiBM" +
                "aW1lPSJTaGVlcCIgU3RyYXdiZXJyeT0iTWFuZ28iIEVsZXBoYW50PSJTdHJh" +
                "d2JlcnJ5IiBQdXJwbGU9IkdyZWVuIiBTaGVlcD0iTGVtb24iIHhtbG5zOm4w" +
                "PSJodHRwOi8vUGVhY2giPjxuMDpHcmFwZT5ZZWxsb3crIEVsZXBoYW50IEVs" +
                "ZXBoYW50JSBEcmFnb257IFBpbmVhcHBsZSggUmVkKiBQaW5lYXBwbGUnIFdo" +
                "aXRlIEJsYWNrPyBQaWcgV2hpdGU8L24wOkdyYXBlPjxuMDpXaGl0ZT5QaW5l" +
                "YXBwbGUgQmx1ZXsgRG9nIExlbW9uIENhdCBMaW1lIFBpbmVhcHBsZTsgQmxh" +
                "Y2ssIFJhdCBNYW5nbzwvbjA6V2hpdGU+PG4wOlJlZD5QaW5lYXBwbGUgU2hl" +
                "ZXAgQmFuYW5hIE1hbmdvfiBQZWFjaF0gR3JlZW4mbHQ7IEJsYWNrLiBHcmVl" +
                "biBCbGFjay4gTWFuZ28gUGluZWFwcGxlIENvdzsgUGluZWFwcGxlIFJlZD08" +
                "L24wOlJlZD48bjA6UmVkPldoaXRlJmd0OyBCYW5hbmEgQmxhY2smZ3Q7IFB1" +
                "cnBsZSBTbmFrZTogUmVkYCBHcmVlbiBCbHVlXiBFbGVwaGFudCBXaGl0ZSBC" +
                "bHVlYmVycnkgQ2F0IFNoZWVwPC9uMDpSZWQ+PG4wOlBpbmVhcHBsZT5HcmVl" +
                "biBZZWxsb3cgQ2F0IEJsYWNrIFB1cnBsZSwgTW9ua2V5IENvdyogTGltZSBQ" +
                "dXJwbGV7IFB1cnBsZSogUGlnKCBMZW1vbicgQmFuYW5hLSBTaGVlcCM8L24w" +
                "OlBpbmVhcHBsZT48bjA6R3JhcGU+TW9ua2V5IEdyYXBlIE1hbmdvKyBQaW5l" +
                "YXBwbGUgU25ha2UgRG9nIFJlZCBNYW5nb30gUGluZWFwcGxlJyBQaW5lYXBw" +
                "bGUgUGlnXSBFbGVwaGFudDwvbjA6R3JhcGU+PG4wOlNuYWtlPkhvcnNlIEJs" +
                "dWViZXJyeSZndDsgQmxhY2sgV2hpdGUlIEhvcnNlIFJlZEAgR3JhcGUkIFdo" +
                "aXRlIFB1cnBsZTwvbjA6U25ha2U+PG4wOkdyYXBlPlJhdCBQdXJwbGU6IFN0" +
                "cmF3YmVycnktIFBlYWNoIEJsYWNrIiBZZWxsb3ddIFN0cmF3YmVycnkgQmxh" +
                "Y2sgQmFuYW5hIyBIb3JzZSggUGVhY2g/PC9uMDpHcmFwZT48bjA6TWFuZ28+" +
                "TGltZSEgQmFuYW5hJmd0OyBTdHJhd2JlcnJ5IFNoZWVwfiBCbHVlYmVycnkl" +
                "IE1vbmtleSIgR3JlZW4vIFNoZWVwIEhvcnNlXiBTbmFrZSBSZWRAPC9uMDpN" +
                "YW5nbz48L24wOk1hbmdvPgMFAAkAAADni5fnu7XnvoqFAAABAAAApSEAAABo" +
                "dHRwOi8vdGVzdC5vcmcvVUEvRGF0YS8vSW5zdGFuY2UCAAYAAADjg5jjg5MD" +
                "BQAAAHpoLUNOHAAAAOiTneiJsiDntKvoibIg6JOd6ImyIOe6ouiJsiQAABwA" +
                "BifC2W0AAAAAAAAACwAAAAAAAAAACAAAAAAAAAAACQAAAAAAAAAA\"}");

            // Act
            var result = await browser.NodeValueWriteAsync(_endpoint,
                new ValueWriteRequestModel {
                    NodeId = node,
                    Value = expected,
                    DataType = "ExtensionObject"
                    // TODO: Assert.Equal("Structure", result.DataType);
                });

            // Assert
            await AssertResult(node, expected, result);
        }


        public async Task NodeWriteStaticScalarNumberValueVariableTest() {

            var browser = _services();
            var node = "http://test.org/UA/Data/#i=10240";

            var expected = JToken.Parse("-44");

            // Act
            var result = await browser.NodeValueWriteAsync(_endpoint,
                new ValueWriteRequestModel {
                    NodeId = node,
                    Value = expected,
                    DataType = "SByte"
                    // Assert.Equal("Number", result.DataType);
                });

            // Assert
            await AssertResult(node, expected, result);
        }


        public async Task NodeWriteStaticScalarIntegerValueVariableTest() {

            var browser = _services();
            var node = "http://test.org/UA/Data/#i=10241";

            var expected = JToken.Parse("94903859");

            // Act
            var result = await browser.NodeValueWriteAsync(_endpoint,
                new ValueWriteRequestModel {
                    NodeId = node,
                    Value = expected,
                    DataType = "Int32"
                    // Assert.Equal("Integer", result.DataType);
                });

            // Assert
            await AssertResult(node, expected, result);
        }


        public async Task NodeWriteStaticScalarUIntegerValueVariableTest() {

            var browser = _services();
            var node = "http://test.org/UA/Data/#i=10242";

            var expected = JToken.Parse("64817");

            // Act
            var result = await browser.NodeValueWriteAsync(_endpoint,
                new ValueWriteRequestModel {
                    NodeId = node,
                    Value = expected,
                    DataType = "UInt32"
                    // Assert.Equal("UInteger", result.DataType);
                });

            // Assert
            await AssertResult(node, expected, result);
        }

        private async Task AssertResult(string node, JToken expected,
            ValueWriteResultModel result) {
            var value = await _readExpected(_endpoint, node);
            Assert.NotNull(value);
            Assert.Null(result.ErrorInfo);
            Assert.True(JToken.DeepEquals(expected, value),
                $"Expected: {expected} != Actual: {value}");
        }

        private readonly T _endpoint;
        private readonly Func<T, string, Task<JToken>> _readExpected;
        private readonly Func<INodeServices<T>> _services;
    }
}
