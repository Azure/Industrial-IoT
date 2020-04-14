// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Opc.Ua.Encoders {
    using Opc.Ua.Models;
    using System;
    using Xunit;
    using System.Globalization;
    using Microsoft.Azure.IIoT;
    using System.Text;
    using Xunit.Abstractions;
    using System.Linq;
    using Opc.Ua.Extensions;

    public class TypeSerializerTests {

        public TypeSerializerTests(ITestOutputHelper output) {
            this.output = output;
        }

        [Theory]
        [InlineData(ContentMimeType.UaJson, ContentMimeType.UaJsonReference)]
        [InlineData(ContentMimeType.UaJsonReference, ContentMimeType.UaJson)]
        [InlineData(ContentMimeType.UaJsonReference, ContentMimeType.UaJsonReference)]
        [InlineData(ContentMimeType.UaNonReversibleJsonReference, ContentMimeType.UaJson)]
        [InlineData(ContentMimeType.UaNonReversibleJson, ContentMimeType.UaJson)]
        [InlineData(ContentMimeType.UaJson, ContentMimeType.UaJson)]
        [InlineData(ContentMimeType.UaBinary, ContentMimeType.UaBinary)]
        [InlineData(ContentMimeType.UaXml, ContentMimeType.UaXml)]
        public void ReadWriteIntArray(string encoderType, string decoderType) {
            var expected = new int[] { 1 };
            CreateSerializers(out var encoder, out var decoder);

            var buffer = encoder.Encode(encoderType, e => e.WriteInt32Array("test", expected));
            OutputJsonBuffer(encoderType, buffer);
            var result = decoder.Decode(decoderType, buffer, d => d.ReadInt32Array("test"));

            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData(ContentMimeType.UaJson, ContentMimeType.UaJsonReference)]
        [InlineData(ContentMimeType.UaJsonReference, ContentMimeType.UaJson)]
        [InlineData(ContentMimeType.UaJsonReference, ContentMimeType.UaJsonReference)]
        [InlineData(ContentMimeType.UaNonReversibleJsonReference, ContentMimeType.UaJson)]
        [InlineData(ContentMimeType.UaNonReversibleJson, ContentMimeType.UaJson)]
        [InlineData(ContentMimeType.UaJson, ContentMimeType.UaJson)]
        [InlineData(ContentMimeType.UaBinary, ContentMimeType.UaBinary)]
        [InlineData(ContentMimeType.UaXml, ContentMimeType.UaXml)]
        public void ReadWriteEmptyIntArray(string encoderType, string decoderType) {
            var expected = new int[0];
            CreateSerializers(out var encoder, out var decoder);

            var buffer = encoder.Encode(encoderType, e => e.WriteInt32Array("test", expected));
            OutputJsonBuffer(encoderType, buffer);
            var result = decoder.Decode(decoderType, buffer, d => d.ReadInt32Array("test"));

            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData(ContentMimeType.UaJson, ContentMimeType.UaJsonReference)]
        [InlineData(ContentMimeType.UaJsonReference, ContentMimeType.UaJson)]
        [InlineData(ContentMimeType.UaJsonReference, ContentMimeType.UaJsonReference)]
        [InlineData(ContentMimeType.UaNonReversibleJsonReference, ContentMimeType.UaJson)]
        [InlineData(ContentMimeType.UaNonReversibleJson, ContentMimeType.UaJson)]
        [InlineData(ContentMimeType.UaJson, ContentMimeType.UaJson)]
        [InlineData(ContentMimeType.UaBinary, ContentMimeType.UaBinary)]
        [InlineData(ContentMimeType.UaXml, ContentMimeType.UaXml)]
        public void ReadWriteDateTime(string encoderType, string decoderType) {
            var expected = DateTime.UtcNow;
            CreateSerializers(out var encoder, out var decoder);

            var buffer = encoder.Encode(encoderType, e => e.WriteDateTime("test", expected));
            OutputJsonBuffer(encoderType, buffer);
            var result = decoder.Decode(decoderType, buffer, d => d.ReadDateTime("test"));

            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData(ContentMimeType.UaJson, ContentMimeType.UaJsonReference)]
        [InlineData(ContentMimeType.UaJsonReference, ContentMimeType.UaJson)]
        [InlineData(ContentMimeType.UaJsonReference, ContentMimeType.UaJsonReference)]
        [InlineData(ContentMimeType.UaNonReversibleJsonReference, ContentMimeType.UaJson)]
        [InlineData(ContentMimeType.UaNonReversibleJson, ContentMimeType.UaJson)]
        [InlineData(ContentMimeType.UaJson, ContentMimeType.UaJson)]
        [InlineData(ContentMimeType.UaBinary, ContentMimeType.UaBinary)]
        [InlineData(ContentMimeType.UaXml, ContentMimeType.UaXml)]
        public void ReadWriteDateTimeArray(string encoderType, string decoderType) {
            var expected = new[] {
                DateTime.UtcNow, DateTime.Now, DateTime.UtcNow, DateTime.MinValue, new DateTime(2001, 1, 1, 12, 0, 0, 0, DateTimeKind.Unspecified),
                DateTime.UtcNow, DateTime.Now, DateTime.UtcNow, DateTime.MaxValue, new DateTime(2001, 1, 1, 12, 0, 0, 0, DateTimeKind.Local),
                DateTime.UtcNow, DateTime.Now, DateTime.UtcNow, DateTime.MinValue, new DateTime(2001, 1, 1, 12, 0, 0, 0, DateTimeKind.Utc),
                DateTime.UtcNow, DateTime.Now, DateTime.UtcNow, DateTime.MaxValue, new DateTime(2001, 1, 1, 12, 0, 0, 0),
            };
            CreateSerializers(out var encoder, out var decoder);

            var buffer = encoder.Encode(encoderType, e => e.WriteDateTimeArray("test", expected));
            expected = expected.Select(d => d.ToOpcUaUniversalTime()).ToArray();
            OutputJsonBuffer(encoderType, buffer);
            var result = decoder.Decode(decoderType, buffer, d => d.ReadDateTimeArray("test"));
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData(ContentMimeType.UaJson, ContentMimeType.UaJsonReference)]
        [InlineData(ContentMimeType.UaJsonReference, ContentMimeType.UaJson)]
        [InlineData(ContentMimeType.UaJsonReference, ContentMimeType.UaJsonReference)]
        [InlineData(ContentMimeType.UaNonReversibleJsonReference, ContentMimeType.UaJson)]
        [InlineData(ContentMimeType.UaNonReversibleJson, ContentMimeType.UaJson)]
        [InlineData(ContentMimeType.UaJson, ContentMimeType.UaJson)]
        [InlineData(ContentMimeType.UaBinary, ContentMimeType.UaBinary)]
        [InlineData(ContentMimeType.UaXml, ContentMimeType.UaXml)]
        public void ReadWriteQualifiedName(string encoderType, string decoderType) {
            var expected = new QualifiedName("hello");
            CreateSerializers(out var encoder, out var decoder);

            var buffer = encoder.Encode(encoderType, e => e.WriteQualifiedName("test", expected));
            OutputJsonBuffer(encoderType, buffer);
            var result = decoder.Decode(decoderType, buffer, d => d.ReadQualifiedName("test"));

            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData(ContentMimeType.UaJson, ContentMimeType.UaJsonReference)]
        [InlineData(ContentMimeType.UaJsonReference, ContentMimeType.UaJson)]
        [InlineData(ContentMimeType.UaJsonReference, ContentMimeType.UaJsonReference)]
        [InlineData(ContentMimeType.UaNonReversibleJsonReference, ContentMimeType.UaJson)]
        [InlineData(ContentMimeType.UaNonReversibleJson, ContentMimeType.UaJson)]
        [InlineData(ContentMimeType.UaJson, ContentMimeType.UaJson)]
        [InlineData(ContentMimeType.UaBinary, ContentMimeType.UaBinary)]
        [InlineData(ContentMimeType.UaXml, ContentMimeType.UaXml)]
        public void ReadWriteQualifiedNameArray(string encoderType, string decoderType) {
            var expected = new[] {
                new QualifiedName("bla", 0),
                new QualifiedName("bla44", 1),
                new QualifiedName("bla2", 2),
                new QualifiedName("bla", 0),
                };
            CreateSerializers(out var encoder, out var decoder);

            var buffer = encoder.Encode(encoderType, e => e.WriteQualifiedNameArray("test", expected));
            OutputJsonBuffer(encoderType, buffer);
            var result = decoder.Decode(decoderType, buffer, d => d.ReadQualifiedNameArray("test"));

            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData(ContentMimeType.UaJson, ContentMimeType.UaJsonReference)]
        [InlineData(ContentMimeType.UaJsonReference, ContentMimeType.UaJson)]
        [InlineData(ContentMimeType.UaJsonReference, ContentMimeType.UaJsonReference)]
        [InlineData(ContentMimeType.UaNonReversibleJsonReference, ContentMimeType.UaJson)]
        [InlineData(ContentMimeType.UaNonReversibleJson, ContentMimeType.UaJson)]
        [InlineData(ContentMimeType.UaJson, ContentMimeType.UaJson)]
        [InlineData(ContentMimeType.UaBinary, ContentMimeType.UaBinary)]
        [InlineData(ContentMimeType.UaXml, ContentMimeType.UaXml)]
        public void ReadWriteDataValue(string encoderType, string decoderType) {
            var expected = new DataValue(new Variant("hello"), StatusCodes.BadAggregateConfigurationRejected);
            CreateSerializers(out var encoder, out var decoder);

            var buffer = encoder.Encode(encoderType, e => e.WriteDataValue("test", expected));
            OutputJsonBuffer(encoderType, buffer);
            var result = decoder.Decode(decoderType, buffer, d => d.ReadDataValue("test"));

            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData(ContentMimeType.UaJson, ContentMimeType.UaJsonReference)]
        [InlineData(ContentMimeType.UaJsonReference, ContentMimeType.UaJson)]
        [InlineData(ContentMimeType.UaJsonReference, ContentMimeType.UaJsonReference)]
        [InlineData(ContentMimeType.UaNonReversibleJsonReference, ContentMimeType.UaJson)]
        [InlineData(ContentMimeType.UaNonReversibleJson, ContentMimeType.UaJson)]
        [InlineData(ContentMimeType.UaJson, ContentMimeType.UaJson)]
        [InlineData(ContentMimeType.UaBinary, ContentMimeType.UaBinary)]
        [InlineData(ContentMimeType.UaXml, ContentMimeType.UaXml)]
        public void ReadWriteLocalizedText1(string encoderType, string decoderType) {
            var expected = new LocalizedText("hello");
            CreateSerializers(out var encoder, out var decoder);

            var buffer = encoder.Encode(encoderType, e => e.WriteLocalizedText("test", expected));
            OutputJsonBuffer(encoderType, buffer);
            var result = decoder.Decode(decoderType, buffer, d => d.ReadLocalizedText("test"));

            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData(ContentMimeType.UaJson, ContentMimeType.UaJsonReference)]
        [InlineData(ContentMimeType.UaJsonReference, ContentMimeType.UaJson)]
        [InlineData(ContentMimeType.UaJsonReference, ContentMimeType.UaJsonReference)]
        [InlineData(ContentMimeType.UaNonReversibleJsonReference, ContentMimeType.UaJson)]
        [InlineData(ContentMimeType.UaNonReversibleJson, ContentMimeType.UaJson)]
        [InlineData(ContentMimeType.UaJson, ContentMimeType.UaJson)]
        [InlineData(ContentMimeType.UaBinary, ContentMimeType.UaBinary)]
        [InlineData(ContentMimeType.UaXml, ContentMimeType.UaXml)]
        public void ReadWriteLocalizedText2(string encoderType, string decoderType) {
            var expected = new LocalizedText(CultureInfo.CurrentCulture.Name, "hello");
            CreateSerializers(out var encoder, out var decoder);

            var buffer = encoder.Encode(encoderType, e => e.WriteLocalizedText("test", expected));
            OutputJsonBuffer(encoderType, buffer);
            var result = decoder.Decode(decoderType, buffer, d => d.ReadLocalizedText("test"));

            // The NR encoding ignores the Locale, skip the type validation
            if (!IsNonReversibleEncoding(encoderType)) {
                Assert.Equal(expected, result);
            }
        }

        [Theory]
        [InlineData(ContentMimeType.UaJson, ContentMimeType.UaJsonReference)]
        [InlineData(ContentMimeType.UaJsonReference, ContentMimeType.UaJson)]
        [InlineData(ContentMimeType.UaJsonReference, ContentMimeType.UaJsonReference)]
        [InlineData(ContentMimeType.UaNonReversibleJsonReference, ContentMimeType.UaJson)]
        [InlineData(ContentMimeType.UaNonReversibleJson, ContentMimeType.UaJson)]
        [InlineData(ContentMimeType.UaJson, ContentMimeType.UaJson)]
        [InlineData(ContentMimeType.UaBinary, ContentMimeType.UaBinary)]
        [InlineData(ContentMimeType.UaXml, ContentMimeType.UaXml)]
        public void ReadWriteLocalizedTextArray1(string encoderType, string decoderType) {
            var expected = new[] {
                new LocalizedText("hello"),
                new LocalizedText("world"),
                new LocalizedText("here"),
                new LocalizedText("I am"),
                };
            CreateSerializers(out var encoder, out var decoder);

            var buffer = encoder.Encode(encoderType, e => e.WriteLocalizedTextArray("test", expected));
            OutputJsonBuffer(encoderType, buffer);
            var result = decoder.Decode(decoderType, buffer, d => d.ReadLocalizedTextArray("test"));

            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData(ContentMimeType.UaJson, ContentMimeType.UaJsonReference)]
        [InlineData(ContentMimeType.UaJsonReference, ContentMimeType.UaJson)]
        [InlineData(ContentMimeType.UaJsonReference, ContentMimeType.UaJsonReference)]
        [InlineData(ContentMimeType.UaNonReversibleJsonReference, ContentMimeType.UaJson)]
        [InlineData(ContentMimeType.UaNonReversibleJson, ContentMimeType.UaJson)]
        [InlineData(ContentMimeType.UaJson, ContentMimeType.UaJson)]
        [InlineData(ContentMimeType.UaBinary, ContentMimeType.UaBinary)]
        [InlineData(ContentMimeType.UaXml, ContentMimeType.UaXml)]
        public void ReadWriteLocalizedTextArray2(string encoderType, string decoderType) {
            var expected = new[] {
                new LocalizedText(CultureInfo.CurrentCulture.Name, "hello"),
                new LocalizedText(CultureInfo.CurrentCulture.Name, "world"),
                new LocalizedText(CultureInfo.CurrentCulture.Name, "here"),
                new LocalizedText(CultureInfo.CurrentCulture.Name, "I am"),
                };
            CreateSerializers(out var encoder, out var decoder);

            var buffer = encoder.Encode(encoderType, e => e.WriteLocalizedTextArray("test", expected));
            OutputJsonBuffer(encoderType, buffer);
            var result = decoder.Decode(decoderType, buffer, d => d.ReadLocalizedTextArray("test"));

            // The NR encoding ignores the Locale, skip the type validation
            if (!IsNonReversibleEncoding(encoderType)) {
                Assert.Equal(expected, result);
            }
        }

        [Theory]
        [InlineData(ContentMimeType.UaJson, ContentMimeType.UaJsonReference)]
        [InlineData(ContentMimeType.UaJsonReference, ContentMimeType.UaJson)]
        [InlineData(ContentMimeType.UaJsonReference, ContentMimeType.UaJsonReference)]
        [InlineData(ContentMimeType.UaNonReversibleJsonReference, ContentMimeType.UaJson)]
        [InlineData(ContentMimeType.UaNonReversibleJson, ContentMimeType.UaJson)]
        [InlineData(ContentMimeType.UaJson, ContentMimeType.UaJson)]
        [InlineData(ContentMimeType.UaBinary, ContentMimeType.UaBinary)]
        [InlineData(ContentMimeType.UaXml, ContentMimeType.UaXml)]
        public void ReadWriteStatusCode(string encoderType, string decoderType) {
            var expected = new StatusCode(StatusCodes.BadAggregateInvalidInputs);
            CreateSerializers(out var encoder, out var decoder);

            var buffer = encoder.Encode(encoderType, e => e.WriteStatusCode("test", expected));
            OutputJsonBuffer(encoderType, buffer);
            var result = decoder.Decode(decoderType, buffer, d => d.ReadStatusCode("test"));

            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData(ContentMimeType.UaJson, ContentMimeType.UaJsonReference)]
        [InlineData(ContentMimeType.UaJsonReference, ContentMimeType.UaJson)]
        [InlineData(ContentMimeType.UaJsonReference, ContentMimeType.UaJsonReference)]
        [InlineData(ContentMimeType.UaNonReversibleJsonReference, ContentMimeType.UaJson)]
        [InlineData(ContentMimeType.UaNonReversibleJson, ContentMimeType.UaJson)]
        [InlineData(ContentMimeType.UaJson, ContentMimeType.UaJson)]
        [InlineData(ContentMimeType.UaBinary, ContentMimeType.UaBinary)]
        [InlineData(ContentMimeType.UaXml, ContentMimeType.UaXml)]
        public void ReadWriteArgument(string encoderType, string decoderType) {
            var expected = new Argument("something1",
                    new NodeId(2354), -1, "somedesciroeioi") {
                ArrayDimensions = new uint[0]
            };
            CreateSerializers(out var encoder, out var decoder);

            // read back
            var buffer = encoder.Encode(encoderType, e => e.WriteEncodeable("test", expected, typeof(Argument)));
            OutputJsonBuffer(encoderType, buffer);
            var result = decoder.Decode(decoderType, buffer, d => d.ReadEncodeable("test", typeof(Argument)));

            Assert.True(result.IsEqual(expected));
        }

        [Theory]
        [InlineData(ContentMimeType.UaJson, ContentMimeType.UaJsonReference)]
        [InlineData(ContentMimeType.UaJsonReference, ContentMimeType.UaJson)]
        [InlineData(ContentMimeType.UaJsonReference, ContentMimeType.UaJsonReference)]
        [InlineData(ContentMimeType.UaNonReversibleJsonReference, ContentMimeType.UaJson)]
        [InlineData(ContentMimeType.UaNonReversibleJson, ContentMimeType.UaJson)]
        [InlineData(ContentMimeType.UaJson, ContentMimeType.UaJson)]
        [InlineData(ContentMimeType.UaBinary, ContentMimeType.UaBinary)]
        [InlineData(ContentMimeType.UaXml, ContentMimeType.UaXml)]
        public void ReadWriteArgumentArray(string encoderType, string decoderType) {
            var expected = new[] {
                new Argument("something1",
                    new NodeId(2354), -1, "somedesciroeioi") { ArrayDimensions = new uint[0] },
                new Argument("something2",
                    new NodeId(23), -1, "fdsadfsdaf") { ArrayDimensions = new uint[0] },
                new Argument("something3",
                    new NodeId(44), 1, "fsadf  sadfsdfsadfsd") { ArrayDimensions = new uint[0] },
                new Argument("something4",
                    new NodeId(23), 1, "dfad  sdafdfdf  fasdf") { ArrayDimensions = new uint[0] }
            };
            CreateSerializers(out var encoder, out var decoder);

            var buffer = encoder.Encode(encoderType, e => e.WriteEncodeableArray(
                "test", expected, typeof(Argument)));
            OutputJsonBuffer(encoderType, buffer);
            var result = (ArgumentCollection)decoder.Decode(decoderType, buffer,
                d => d.ReadEncodeableArray("test", typeof(Argument)));

            for (var i = 0; i < result.Count; i++) {
                Assert.True(result[i].IsEqual(expected[i]));
            }
        }

        [Theory]
        [InlineData(ContentMimeType.UaJson, ContentMimeType.UaJsonReference)]
        [InlineData(ContentMimeType.UaJsonReference, ContentMimeType.UaJson)]
        [InlineData(ContentMimeType.UaJsonReference, ContentMimeType.UaJsonReference)]
        [InlineData(ContentMimeType.UaNonReversibleJsonReference, ContentMimeType.UaJson)]
        [InlineData(ContentMimeType.UaNonReversibleJson, ContentMimeType.UaJson)]
        [InlineData(ContentMimeType.UaJson, ContentMimeType.UaJson)]
        [InlineData(ContentMimeType.UaBinary, ContentMimeType.UaBinary)]
        [InlineData(ContentMimeType.UaXml, ContentMimeType.UaXml)]
        public void ReadWriteStringArray(string encoderType, string decoderType) {
            var expected = new string[] { "1", "2", "3", "4", "5" };
            CreateSerializers(out var encoder, out var decoder);

            var buffer = encoder.Encode(encoderType, e => e.WriteStringArray("test", expected));
            OutputJsonBuffer(encoderType, buffer);
            var result = decoder.Decode(decoderType, buffer, d => d.ReadStringArray("test"));

            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData(ContentMimeType.UaJson, ContentMimeType.UaJsonReference)]
        [InlineData(ContentMimeType.UaJsonReference, ContentMimeType.UaJson)]
        [InlineData(ContentMimeType.UaJsonReference, ContentMimeType.UaJsonReference)]
        [InlineData(ContentMimeType.UaNonReversibleJsonReference, ContentMimeType.UaJson)]
        [InlineData(ContentMimeType.UaNonReversibleJson, ContentMimeType.UaJson)]
        [InlineData(ContentMimeType.UaJson, ContentMimeType.UaJson)]
        [InlineData(ContentMimeType.UaBinary, ContentMimeType.UaBinary)]
        [InlineData(ContentMimeType.UaXml, ContentMimeType.UaXml)]
        public void ReadWriteEmptyStringArray(string encoderType, string decoderType) {
            var expected = new string[0];
            CreateSerializers(out var encoder, out var decoder);

            var buffer = encoder.Encode(encoderType, e => e.WriteStringArray("test", expected));
            OutputJsonBuffer(encoderType, buffer);
            var result = decoder.Decode(decoderType, buffer, d => d.ReadStringArray("test"));

            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData(ContentMimeType.UaJson, ContentMimeType.UaJsonReference)]
        [InlineData(ContentMimeType.UaJsonReference, ContentMimeType.UaJson)]
        [InlineData(ContentMimeType.UaJsonReference, ContentMimeType.UaJsonReference)]
        [InlineData(ContentMimeType.UaNonReversibleJsonReference, ContentMimeType.UaJson)]
        [InlineData(ContentMimeType.UaNonReversibleJson, ContentMimeType.UaJson)]
        [InlineData(ContentMimeType.UaJson, ContentMimeType.UaJson)]
        [InlineData(ContentMimeType.UaBinary, ContentMimeType.UaBinary)]
        [InlineData(ContentMimeType.UaXml, ContentMimeType.UaXml)]
        public void ReadWriteStringVariant(string encoderType, string decoderType) {
            var expected = new Variant("5");
            CreateSerializers(out var encoder, out var decoder);

            var buffer = encoder.Encode(encoderType, e => e.WriteVariant("test", expected));
            OutputJsonBuffer(encoderType, buffer);
            var result = decoder.Decode(decoderType, buffer, d => d.ReadVariant("test"));

            Assert.Equal(expected, result);
        }

        [Theory]
        //[InlineData(ContentMimeType.UaJson, ContentMimeType.UaJsonReference)]
        //[InlineData(ContentMimeType.UaJsonReference, ContentMimeType.UaJson)]
        //[InlineData(ContentMimeType.UaJsonReference, ContentMimeType.UaJsonReference)]
        //[InlineData(ContentMimeType.UaNonReversibleJsonReference, ContentMimeType.UaJson)]
        [InlineData(ContentMimeType.UaNonReversibleJson, ContentMimeType.UaJson)]
        [InlineData(ContentMimeType.UaJson, ContentMimeType.UaJson)]
        [InlineData(ContentMimeType.UaBinary, ContentMimeType.UaBinary)]
        [InlineData(ContentMimeType.UaXml, ContentMimeType.UaXml)]
        public void ReadWriteStringMatrixVariant(string encoderType, string decoderType) {
            var expected = new Variant(new[, ,] {
                { { "1", "1", "1" }, { "2", "2", "1" }, { "3", "3", "1" } },
                { { "1", "1", "2" }, { "2", "2", "2" }, { "3", "3", "2" } },
                { { "1", "1", "3" }, { "2", "2", "3" }, { "3", "3", "3" } },
                { { "1", "1", "4" }, { "2", "2", "4" }, { "3", "3", "4" } },
                { { "1", "1", "5" }, { "2", "2", "5" }, { "3", "3", "5" } },
                { { "1", "1", "6" }, { "2", "2", "6" }, { "3", "3", "6" } },
                { { "1", "1", "7" }, { "2", "2", "7" }, { "3", "3", "7" } }
            });
            CreateSerializers(out var encoder, out var decoder);
            var buffer = encoder.Encode(encoderType, e => e.WriteVariant("test", expected));
            OutputJsonBuffer(encoderType, buffer);
            var result = decoder.Decode(decoderType, buffer, d => d.ReadVariant("test"));
            Assert.True(expected.Value is Matrix);
            Assert.True(result.Value is Matrix);
            Assert.Equal(((Matrix)expected.Value).Elements, ((Matrix)result.Value).Elements);
            Assert.Equal(((Matrix)expected.Value).Dimensions, ((Matrix)result.Value).Dimensions);
        }

        [Theory]
        //[InlineData(ContentMimeType.UaJson, ContentMimeType.UaJsonReference)]
        //[InlineData(ContentMimeType.UaJsonReference, ContentMimeType.UaJson)]
        //[InlineData(ContentMimeType.UaJsonReference, ContentMimeType.UaJsonReference)]
        //[InlineData(ContentMimeType.UaNonReversibleJsonReference, ContentMimeType.UaJson)]
        [InlineData(ContentMimeType.UaNonReversibleJson, ContentMimeType.UaJson)]
        [InlineData(ContentMimeType.UaJson, ContentMimeType.UaJson)]
        [InlineData(ContentMimeType.UaBinary, ContentMimeType.UaBinary)]
        [InlineData(ContentMimeType.UaXml, ContentMimeType.UaXml)]
        public void ReadWriteIntMatrixVariant(string encoderType, string decoderType) {
            var expected = new Variant(new[, ,] {
                { { 1L, 1L, 1L }, { 2L, 2L, 2L }, { 3L, 3L, 3L } },
                { { 1L, 1L, 1L }, { 2L, 2L, 2L }, { 3L, 3L, 3L } },
                { { 1L, 1L, 1L }, { 2L, 2L, 2L }, { 3L, 3L, 3L } },
                { { 1L, 1L, 1L }, { 2L, 2L, 2L }, { 3L, 3L, 3L } },
                { { 1L, 1L, 1L }, { 2L, 2L, 2L }, { 3L, 3L, 3L } },
                { { 1L, 1L, 1L }, { 2L, 2L, 2L }, { 3L, 3L, 3L } },
                { { 1L, 1L, 1L }, { 2L, 2L, 2L }, { 3L, 3L, 3L } }
            });
            CreateSerializers(out var encoder, out var decoder);
            var buffer = encoder.Encode(encoderType, e => e.WriteVariant("test", expected));
            OutputJsonBuffer(encoderType, buffer);
            var result = decoder.Decode(decoderType, buffer, d => d.ReadVariant("test"));
            Assert.True(expected.Value is Matrix);
            Assert.True(result.Value is Matrix);
            Assert.Equal(((Matrix)expected.Value).Elements, ((Matrix)result.Value).Elements);
            Assert.Equal(((Matrix)expected.Value).Dimensions, ((Matrix)result.Value).Dimensions);
        }

        [Theory]
        //[InlineData(ContentMimeType.UaJson, ContentMimeType.UaJsonReference)]
        //[InlineData(ContentMimeType.UaJsonReference, ContentMimeType.UaJson)]
        //[InlineData(ContentMimeType.UaJsonReference, ContentMimeType.UaJsonReference)]
        //[InlineData(ContentMimeType.UaNonReversibleJsonReference, ContentMimeType.UaJson)]
        [InlineData(ContentMimeType.UaNonReversibleJson, ContentMimeType.UaJson)]
        [InlineData(ContentMimeType.UaJson, ContentMimeType.UaJson)]
        [InlineData(ContentMimeType.UaBinary, ContentMimeType.UaBinary)]
        [InlineData(ContentMimeType.UaXml, ContentMimeType.UaXml)]
        public void ReadWriteBooleanMatrixVariant(string encoderType, string decoderType) {
            var expected = new Variant(new[, ,] {
                { { true, false, true }, { false, true, false }, { true, false, false } },
                { { true, false, true }, { false, true, false }, { true, false, false } },
                { { true, false, true }, { false, true, false }, { true, false, false } },
                { { true, false, true }, { false, true, false }, { true, false, false } },
                { { true, false, true }, { false, true, false }, { true, false, false } },
                { { true, false, true }, { false, true, false }, { true, false, false } },
                { { true, false, true }, { false, true, false }, { true, false, false } }
            });
            CreateSerializers(out var encoder, out var decoder);
            var buffer = encoder.Encode(encoderType, e => e.WriteVariant("test", expected));
            OutputJsonBuffer(encoderType, buffer);
            var result = decoder.Decode(decoderType, buffer, d => d.ReadVariant("test"));
            Assert.True(expected.Value is Matrix);
            Assert.True(result.Value is Matrix);
            Assert.Equal(((Matrix)expected.Value).Elements, ((Matrix)result.Value).Elements);
            Assert.Equal(((Matrix)expected.Value).Dimensions, ((Matrix)result.Value).Dimensions);
        }

        [Theory]
        [InlineData(ContentMimeType.UaJson, ContentMimeType.UaJsonReference)]
        [InlineData(ContentMimeType.UaJsonReference, ContentMimeType.UaJson)]
        [InlineData(ContentMimeType.UaJsonReference, ContentMimeType.UaJsonReference)]
        [InlineData(ContentMimeType.UaNonReversibleJsonReference, ContentMimeType.UaJson)]
        [InlineData(ContentMimeType.UaNonReversibleJson, ContentMimeType.UaJson)]
        [InlineData(ContentMimeType.UaJson, ContentMimeType.UaJson)]
        [InlineData(ContentMimeType.UaBinary, ContentMimeType.UaBinary)]
        [InlineData(ContentMimeType.UaXml, ContentMimeType.UaXml)]
        public void ReadWriteNullVariant(string encoderType, string decoderType) {
            var expected = Variant.Null;
            CreateSerializers(out var encoder, out var decoder);

            var buffer = encoder.Encode(encoderType, e => e.WriteVariant("test", expected));
            OutputJsonBuffer(encoderType, buffer);
            var result = decoder.Decode(decoderType, buffer, d => d.ReadVariant("test"));

            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData(ContentMimeType.UaJson, ContentMimeType.UaJsonReference)]
        [InlineData(ContentMimeType.UaJsonReference, ContentMimeType.UaJson)]
        [InlineData(ContentMimeType.UaJsonReference, ContentMimeType.UaJsonReference)]
        [InlineData(ContentMimeType.UaJson, ContentMimeType.UaJson)]
        [InlineData(ContentMimeType.UaBinary, ContentMimeType.UaBinary)]
        [InlineData(ContentMimeType.UaXml, ContentMimeType.UaXml)]
        public void ReadWriteUintVariant(string encoderType, string decoderType) {
            var expected = new Variant((uint)99);
            CreateSerializers(out var encoder, out var decoder);

            var buffer = encoder.Encode(encoderType, e => e.WriteVariant("test", expected));
            OutputJsonBuffer(encoderType, buffer);
            var result = decoder.Decode(decoderType, buffer, d => d.ReadVariant("test"));

            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData(ContentMimeType.UaJsonReference, ContentMimeType.UaJson)]
        // ulong is encoded with surrounding "", but UInt16 expects a number 
        //[InlineData(ContentMimeType.UaJsonReference, ContentMimeType.UaJsonReference)]
        //[InlineData(ContentMimeType.UaJson, ContentMimeType.UaJsonReference)]
        [InlineData(ContentMimeType.UaNonReversibleJsonReference, ContentMimeType.UaJson)]
        [InlineData(ContentMimeType.UaNonReversibleJson, ContentMimeType.UaJson)]
        [InlineData(ContentMimeType.UaJson, ContentMimeType.UaJson)]
        [InlineData(ContentMimeType.UaBinary, ContentMimeType.UaBinary)]
        [InlineData(ContentMimeType.UaXml, ContentMimeType.UaXml)]
        public void ReadWriteUInt64AsUInt16(string encoderType, string decoderType) {
            var expected = (ulong)99;
            CreateSerializers(out var encoder, out var decoder);

            var buffer = encoder.Encode(encoderType, e => e.WriteUInt64("test", expected));
            OutputJsonBuffer(encoderType, buffer);
            var result = decoder.Decode(decoderType, buffer, d => d.ReadUInt16("test"));

            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData(ContentMimeType.UaJson, ContentMimeType.UaJsonReference)]
        [InlineData(ContentMimeType.UaJsonReference, ContentMimeType.UaJson)]
        [InlineData(ContentMimeType.UaJsonReference, ContentMimeType.UaJsonReference)]
        [InlineData(ContentMimeType.UaNonReversibleJsonReference, ContentMimeType.UaJson)]
        [InlineData(ContentMimeType.UaNonReversibleJson, ContentMimeType.UaJson)]
        [InlineData(ContentMimeType.UaJson, ContentMimeType.UaJson)]
        [InlineData(ContentMimeType.UaBinary, ContentMimeType.UaBinary)]
        [InlineData(ContentMimeType.UaXml, ContentMimeType.UaXml)]
        public void ReadWriteUInt64(string encoderType, string decoderType) {
            UInt64 expected = 123456789123456789;
            CreateSerializers(out var encoder, out var decoder);

            var buffer = encoder.Encode(encoderType, e => e.WriteUInt64("test", expected));
            OutputJsonBuffer(encoderType, buffer);
            var result = decoder.Decode(decoderType, buffer, d => d.ReadUInt64("test"));

            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData(ContentMimeType.UaJson, ContentMimeType.UaJsonReference)]
        [InlineData(ContentMimeType.UaJsonReference, ContentMimeType.UaJson)]
        [InlineData(ContentMimeType.UaJsonReference, ContentMimeType.UaJsonReference)]
        [InlineData(ContentMimeType.UaNonReversibleJsonReference, ContentMimeType.UaJson)]
        [InlineData(ContentMimeType.UaNonReversibleJson, ContentMimeType.UaJson)]
        [InlineData(ContentMimeType.UaJson, ContentMimeType.UaJson)]
        [InlineData(ContentMimeType.UaBinary, ContentMimeType.UaBinary)]
        [InlineData(ContentMimeType.UaXml, ContentMimeType.UaXml)]
        public void ReadWriteInt64(string encoderType, string decoderType) {
            Int64 expected = -123456789123456789;
            CreateSerializers(out var encoder, out var decoder);

            var buffer = encoder.Encode(encoderType, e => e.WriteInt64("test", expected));
            OutputJsonBuffer(encoderType, buffer);
            var result = decoder.Decode(decoderType, buffer, d => d.ReadInt64("test"));

            Assert.Equal(expected, result);
        }

        /// <summary>
        /// Test encode string variant - cannot do this on binary
        /// </summary>
        [Theory]
        [InlineData(ContentMimeType.UaJson, ContentMimeType.UaJsonReference)]
        [InlineData(ContentMimeType.UaJsonReference, ContentMimeType.UaJson)]
        [InlineData(ContentMimeType.UaJsonReference, ContentMimeType.UaJsonReference)]
        [InlineData(ContentMimeType.UaNonReversibleJsonReference, ContentMimeType.UaJson)]
        [InlineData(ContentMimeType.UaNonReversibleJson, ContentMimeType.UaJson)]
        [InlineData(ContentMimeType.UaJson, ContentMimeType.UaJson)]
        [InlineData(ContentMimeType.UaXml, ContentMimeType.UaXml)]
        public void ReadWriteUInt64AsString(string encoderType, string decoderType) {
            UInt64 expected = 123456789123456789;
            CreateSerializers(out var encoder, out var decoder);

            var buffer = encoder.Encode(encoderType, e => e.WriteString("test", "123456789123456789"));
            OutputJsonBuffer(encoderType, buffer);
            var result = decoder.Decode(decoderType, buffer, d => d.ReadUInt64("test"));

            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData(ContentMimeType.UaJson, ContentMimeType.UaJsonReference)]
        [InlineData(ContentMimeType.UaJsonReference, ContentMimeType.UaJson)]
        [InlineData(ContentMimeType.UaJsonReference, ContentMimeType.UaJsonReference)]
        [InlineData(ContentMimeType.UaNonReversibleJsonReference, ContentMimeType.UaJson)]
        [InlineData(ContentMimeType.UaNonReversibleJson, ContentMimeType.UaJson)]
        [InlineData(ContentMimeType.UaJson, ContentMimeType.UaJson)]
        [InlineData(ContentMimeType.UaBinary, ContentMimeType.UaBinary)]
        [InlineData(ContentMimeType.UaXml, ContentMimeType.UaXml)]
        public void ReadWriteStringArrayVariant(string encoderType, string decoderType) {
            var expected = new Variant(new string[] { "1", "2", "3", "4", "5" });
            CreateSerializers(out var encoder, out var decoder);

            var buffer = encoder.Encode(encoderType, e => e.WriteVariant("test", expected));
            OutputJsonBuffer(encoderType, buffer);
            var result = decoder.Decode(decoderType, buffer, d => d.ReadVariant("test"));

            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData(ContentMimeType.UaJson, ContentMimeType.UaJsonReference)]
        [InlineData(ContentMimeType.UaJsonReference, ContentMimeType.UaJson)]
        [InlineData(ContentMimeType.UaJsonReference, ContentMimeType.UaJsonReference)]
        [InlineData(ContentMimeType.UaNonReversibleJsonReference, ContentMimeType.UaJson)]
        [InlineData(ContentMimeType.UaNonReversibleJson, ContentMimeType.UaJson)]
        [InlineData(ContentMimeType.UaJson, ContentMimeType.UaJson)]
        [InlineData(ContentMimeType.UaBinary, ContentMimeType.UaBinary)]
        [InlineData(ContentMimeType.UaXml, ContentMimeType.UaXml)]
        public void ReadWriteVariantCollection(string encoderType, string decoderType) {
            var expected = new VariantCollection {
                new Variant(4L),
                new Variant("test"),
                new Variant(new long[] {1, 2, 3, 4, 5 }),
                new Variant(new string[] {"1", "2", "3", "4", "5" })
            };
            CreateSerializers(out var encoder, out var decoder);

            var buffer = encoder.Encode(encoderType, e => e.WriteVariantArray("test", expected));
            OutputJsonBuffer(encoderType, buffer);
            var result = decoder.Decode(decoderType, buffer, d => d.ReadVariantArray("test"));
            // The NR encoding cannot distinguish between the long[] and string[], skip the type validation
            if (!IsNonReversibleEncoding(encoderType)) {
                Assert.Equal(expected, result);
            }
        }

        /// <summary>
        /// Test encode string array variant
        /// </summary>
        [Theory]
        [InlineData(ContentMimeType.UaJson, ContentMimeType.UaJsonReference)]
        [InlineData(ContentMimeType.UaJsonReference, ContentMimeType.UaJson)]
        [InlineData(ContentMimeType.UaJsonReference, ContentMimeType.UaJsonReference)]
        // TODO: JsonWriterException in JsonEncoderEx
        // [InlineData(ContentMimeType.UaNonReversibleJson, ContentMimeType.UaJson)]
        [InlineData(ContentMimeType.UaNonReversibleJsonReference, ContentMimeType.UaJson)]
        [InlineData(ContentMimeType.UaJson, ContentMimeType.UaJson)]
        [InlineData(ContentMimeType.UaBinary, ContentMimeType.UaBinary)]
        [InlineData(ContentMimeType.UaXml, ContentMimeType.UaXml)]
        public void ReadNodeAttributeSet(string encoderType, string decoderType) {
            var expected = new NodeAttributeSet();
            var map = new AttributeMap();
            expected.SetAttribute(Attributes.NodeClass, NodeClass.Variable);
            expected.SetAttribute(Attributes.BrowseName, new QualifiedName("Somename"));
            expected.SetAttribute(Attributes.NodeId, new NodeId(Guid.NewGuid()));
            expected.SetAttribute(Attributes.DisplayName, new LocalizedText("en-us", "hello world"));
            expected.SetAttribute(Attributes.Value, 1235);
            expected.SetAttribute(Attributes.Description, new LocalizedText("test"));
            expected.SetAttribute(Attributes.DataType, new NodeId(Guid.NewGuid()));
            CreateSerializers(out var encoder, out var decoder);

            var buffer = encoder.Encode(encoderType, e => e.WriteEncodeable("test", expected, expected.GetType()));
            OutputJsonBuffer(encoderType, buffer);
            var result = decoder.Decode(decoderType, buffer, d => d.ReadEncodeable("test", typeof(NodeAttributeSet)));

            // The NR encoding ignores the Locale, skip the validation
            if (!IsNonReversibleEncoding(encoderType)) {
                Assert.True(expected.IsEqual(result));
            }
        }

        [Theory]
        [InlineData(ContentMimeType.UaJson, ContentMimeType.UaJsonReference)]
        [InlineData(ContentMimeType.UaJsonReference, ContentMimeType.UaJson)]
        [InlineData(ContentMimeType.UaJsonReference, ContentMimeType.UaJsonReference)]
        [InlineData(ContentMimeType.UaNonReversibleJsonReference, ContentMimeType.UaJson)]
        [InlineData(ContentMimeType.UaNonReversibleJson, ContentMimeType.UaJson)]
        [InlineData(ContentMimeType.UaJson, ContentMimeType.UaJson)]
        // TODO [InlineData(ContentMimeType.UaBinary, ContentMimeType.UaBinary)]
        [InlineData(ContentMimeType.UaXml, ContentMimeType.UaXml)]
        public void ReadWriteProgramDiagnostic2DataType(string encoderType, string decoderType) {

            // Create dummy type
            var expected = new ProgramDiagnostic2DataType {
                CreateClientName = "Testname",
                CreateSessionId = new NodeId(Guid.NewGuid()),
                InvocationCreationTime = DateTime.UtcNow,
                LastMethodCall = "swappido",
                LastMethodCallTime = DateTime.UtcNow,
                LastMethodInputArguments = new ArgumentCollection {
                    new Argument("something1",
                        new NodeId(2354), -1, "somedesciroeioi") { ArrayDimensions = new uint[0] },
                    new Argument("something2",
                        new NodeId(23), -1, "fdsadfsdaf") { ArrayDimensions = new uint[0] },
                    new Argument("something3",
                        new NodeId(44), 1, "fsadf  sadfsdfsadfsd") { ArrayDimensions = new uint[0] },
                    new Argument("something4",
                        new NodeId(23), 1, "dfad  sdafdfdf  fasdf") { ArrayDimensions = new uint[0] }
                },
                LastMethodInputValues = new VariantCollection {
                    new Variant(4L),
                    new Variant("test"),
                    new Variant(new long[] {1, 2, 3, 4, 5 }),
                    new Variant(new string[] {"1", "2", "3", "4", "5" })
                },
                LastMethodOutputArguments = new ArgumentCollection {
                    new Argument("foo1",
                        new NodeId(2354), -1, "somedesciroeioi") { ArrayDimensions = new uint[0] },
                    new Argument("foo2",
                        new NodeId(33), -1, "fdsadfsdaf") { ArrayDimensions = new uint[0] },
                    new Argument("adfsdafsdsdsafdsfa",
                        new NodeId("absc"), 1, "fsadf  sadfsdfsadfsd") { ArrayDimensions = new uint[0] },
                    new Argument("ddddd",
                        new NodeId(25), 1, "dfad  sdafdfdf  fasdf") { ArrayDimensions = new uint[0] }
                },
                LastMethodOutputValues = new VariantCollection {
                    new Variant(4L),
                    new Variant("test"),
                    new Variant(new long[] {1, 2, 3, 4, 5 }),
                    new Variant(new string[] {"1", "2", "3", "4", "5" })
                },
                LastMethodReturnStatus = new StatusResult(
                    StatusCodes.BadAggregateConfigurationRejected),
                LastMethodSessionId = new NodeId(
                    Utils.Nonce.CreateNonce(32)),
                LastTransitionTime = DateTime.UtcNow - TimeSpan.FromDays(23)
            };
            CreateSerializers(out var encoder, out var decoder);

            var buffer = encoder.Encode(encoderType, e => e.WriteEncodeable("test", expected, typeof(ProgramDiagnostic2DataType)));
            OutputJsonBuffer(encoderType, buffer);
            var result = decoder.Decode(decoderType, buffer, d => d.ReadEncodeable("test", typeof(ProgramDiagnostic2DataType)));
            // The NR encoding cannot distinguish between the long[] and string[], skip the type validation
            if (!IsNonReversibleEncoding(encoderType)) {
                Assert.True(result.IsEqual(expected));
            }
        }

        /// <summary>
        /// Create the encoder/decoder pair for tests.
        /// </summary>
        private static ServiceMessageContext CreateSerializers(
            out ITypeSerializer encoder, out ITypeSerializer decoder) {
            var context = new ServiceMessageContext();
            encoder = new TypeSerializer(context);
            decoder = new TypeSerializer(context);
            return context;
        }

        /// <summary>
        /// Return true if encoder produces non reversible output.
        /// </summary>
        private bool IsNonReversibleEncoding(string encoderType) {
            return
                encoderType.Equals(ContentMimeType.UaNonReversibleJsonReference) ||
                encoderType.Equals(ContentMimeType.UaNonReversibleJson);
        }

        /// <summary>
        /// Output a JSON or Xml buffer to the log, skip otherwise.
        /// </summary>
        private void OutputJsonBuffer(string encoderType, byte[] buffer) {
            switch (encoderType) {
                case ContentMimeType.UaJson:
                case ContentMimeType.UaJsonReference:
                case ContentMimeType.UaNonReversibleJsonReference:
                case ContentMimeType.UaNonReversibleJson:
                case ContentMimeType.UaXml:
                    var formattedBuffer = Encoding.UTF8.GetString(buffer);
                    output.WriteLine(formattedBuffer);
                    break;
                default:
                    break;
            }
        }

        private readonly ITestOutputHelper output;
    }
}
