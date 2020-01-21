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

    public class TypeSerializerTests {

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
            var result = decoder.Decode(decoderType, buffer, d => d.ReadDateTime("test"));

            Assert.Equal(expected, result);
        }

        [Theory]
        // [InlineData(ContentEncodings.MimeTypeUaJson, ContentEncodings.MimeTypeUaJsonReference)]
        // [InlineData(ContentEncodings.MimeTypeUaJsonReference, ContentEncodings.MimeTypeUaJson)]
        // [InlineData(ContentEncodings.MimeTypeUaJsonReference, ContentEncodings.MimeTypeUaJsonReference)]
        // [InlineData(ContentEncodings.MimeTypeUaNonReversibleJsonReference, ContentEncodings.MimeTypeUaJson)]
        [InlineData(ContentMimeType.UaNonReversibleJson, ContentMimeType.UaJson)]
        [InlineData(ContentMimeType.UaJson, ContentMimeType.UaJson)]
        [InlineData(ContentMimeType.UaBinary, ContentMimeType.UaBinary)]
        [InlineData(ContentMimeType.UaXml, ContentMimeType.UaXml)]
        public void ReadWriteDateTimeArray(string encoderType, string decoderType) {
            var expected = new[] {
                DateTime.UtcNow, DateTime.UtcNow, DateTime.UtcNow, DateTime.UtcNow, DateTime.UtcNow,
                DateTime.UtcNow, DateTime.UtcNow, DateTime.UtcNow, DateTime.UtcNow, DateTime.UtcNow,
                DateTime.UtcNow, DateTime.UtcNow, DateTime.UtcNow, DateTime.UtcNow, DateTime.UtcNow,
                DateTime.UtcNow, DateTime.UtcNow, DateTime.UtcNow, DateTime.UtcNow, DateTime.UtcNow
            };
            CreateSerializers(out var encoder, out var decoder);

            var buffer = encoder.Encode(encoderType, e => e.WriteDateTimeArray("test", expected));
            var result = decoder.Decode(decoderType, buffer, d => d.ReadDateTimeArray("test"));

            Assert.Equal(expected, result);
        }

        [Theory]
        // [InlineData(ContentEncodings.MimeTypeUaJson, ContentEncodings.MimeTypeUaJsonReference)]
        // [InlineData(ContentEncodings.MimeTypeUaJsonReference, ContentEncodings.MimeTypeUaJson)]
        // [InlineData(ContentEncodings.MimeTypeUaJsonReference, ContentEncodings.MimeTypeUaJsonReference)]
        // [InlineData(ContentEncodings.MimeTypeUaNonReversibleJsonReference, ContentEncodings.MimeTypeUaJson)]
        [InlineData(ContentMimeType.UaNonReversibleJson, ContentMimeType.UaJson)]
        [InlineData(ContentMimeType.UaJson, ContentMimeType.UaJson)]
        [InlineData(ContentMimeType.UaBinary, ContentMimeType.UaBinary)]
        [InlineData(ContentMimeType.UaXml, ContentMimeType.UaXml)]
        public void ReadWriteQualifiedName(string encoderType, string decoderType) {
            var expected = new QualifiedName("hello");
            CreateSerializers(out var encoder, out var decoder);

            var buffer = encoder.Encode(encoderType, e => e.WriteQualifiedName("test", expected));
            var result = decoder.Decode(decoderType, buffer, d => d.ReadQualifiedName("test"));

            Assert.Equal(expected, result);
        }

        [Theory]
        // [InlineData(ContentEncodings.MimeTypeUaJson, ContentEncodings.MimeTypeUaJsonReference)]
        // [InlineData(ContentEncodings.MimeTypeUaJsonReference, ContentEncodings.MimeTypeUaJson)]
        // [InlineData(ContentEncodings.MimeTypeUaJsonReference, ContentEncodings.MimeTypeUaJsonReference)]
        // [InlineData(ContentEncodings.MimeTypeUaNonReversibleJsonReference, ContentEncodings.MimeTypeUaJson)]
        [InlineData(ContentMimeType.UaNonReversibleJson, ContentMimeType.UaJson)]
        [InlineData(ContentMimeType.UaJson, ContentMimeType.UaJson)]
        [InlineData(ContentMimeType.UaBinary, ContentMimeType.UaBinary)]
        [InlineData(ContentMimeType.UaXml, ContentMimeType.UaXml)]
        public void ReadWriteQualifiedNameArray(string encoderType, string decoderType) {
            var expected = new[] {
                new QualifiedName("bla", 0),
                new QualifiedName("bla44", 0),
                new QualifiedName("bla2", 0),
                new QualifiedName("bla", 0),
                };
            CreateSerializers(out var encoder, out var decoder);

            var buffer = encoder.Encode(encoderType, e => e.WriteQualifiedNameArray("test", expected));
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
            var result = decoder.Decode(decoderType, buffer, d => d.ReadLocalizedText("test"));

            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData(ContentMimeType.UaJson, ContentMimeType.UaJsonReference)]
        [InlineData(ContentMimeType.UaJsonReference, ContentMimeType.UaJson)]
        [InlineData(ContentMimeType.UaJsonReference, ContentMimeType.UaJsonReference)]
        [InlineData(ContentMimeType.UaNonReversibleJson, ContentMimeType.UaJson)]
        [InlineData(ContentMimeType.UaJson, ContentMimeType.UaJson)]
        [InlineData(ContentMimeType.UaBinary, ContentMimeType.UaBinary)]
        [InlineData(ContentMimeType.UaXml, ContentMimeType.UaXml)]
        public void ReadWriteLocalizedText2(string encoderType, string decoderType) {
            var expected = new LocalizedText(CultureInfo.CurrentCulture.Name, "hello");
            CreateSerializers(out var encoder, out var decoder);

            var buffer = encoder.Encode(encoderType, e => e.WriteLocalizedText("test", expected));
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
        public void ReadWriteLocalizedTextArray1(string encoderType, string decoderType) {
            var expected = new[] {
                new LocalizedText("hello"),
                new LocalizedText("world"),
                new LocalizedText("here"),
                new LocalizedText("I am"),
                };
            CreateSerializers(out var encoder, out var decoder);

            var buffer = encoder.Encode(encoderType, e => e.WriteLocalizedTextArray("test", expected));
            var result = decoder.Decode(decoderType, buffer, d => d.ReadLocalizedTextArray("test"));

            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData(ContentMimeType.UaJson, ContentMimeType.UaJsonReference)]
        [InlineData(ContentMimeType.UaJsonReference, ContentMimeType.UaJson)]
        [InlineData(ContentMimeType.UaJsonReference, ContentMimeType.UaJsonReference)]
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
        public void ReadWriteStatusCode(string encoderType, string decoderType) {
            var expected = new StatusCode(StatusCodes.BadAggregateInvalidInputs);
            CreateSerializers(out var encoder, out var decoder);

            var buffer = encoder.Encode(encoderType, e => e.WriteStatusCode("test", expected));
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
            var result = decoder.Decode(decoderType, buffer, d => d.ReadEncodeable("test", typeof(Argument)));

            Assert.True(result.IsEqual(expected));
        }

        [Theory]
        // [InlineData(ContentEncodings.MimeTypeUaJson, ContentEncodings.MimeTypeUaJsonReference)]
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

            var buffer = encoder.Encode(encoderType, e => e.WriteEncodeableArray("test", expected, typeof(Argument)));
            var test = System.Text.Encoding.UTF8.GetString(buffer);
            var result = (ArgumentCollection)decoder.Decode(decoderType, buffer, d => d.ReadEncodeableArray("test", typeof(Argument)));

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
            var result = decoder.Decode(decoderType, buffer, d => d.ReadStringArray("test"));

            Assert.Equal(expected, result);
        }

        [Theory]
        // [InlineData(ContentEncodings.MimeTypeUaJson, ContentEncodings.MimeTypeUaJsonReference)]
        // [InlineData(ContentEncodings.MimeTypeUaJsonReference, ContentEncodings.MimeTypeUaJson)]
        // [InlineData(ContentEncodings.MimeTypeUaJsonReference, ContentEncodings.MimeTypeUaJsonReference)]
        // [InlineData(ContentEncodings.MimeTypeUaNonReversibleJsonReference, ContentEncodings.MimeTypeUaJson)]
        [InlineData(ContentMimeType.UaNonReversibleJson, ContentMimeType.UaJson)]
        [InlineData(ContentMimeType.UaJson, ContentMimeType.UaJson)]
        [InlineData(ContentMimeType.UaBinary, ContentMimeType.UaBinary)]
        [InlineData(ContentMimeType.UaXml, ContentMimeType.UaXml)]
        public void ReadWriteStringVariant(string encoderType, string decoderType) {
            var expected = new Variant("5");
            CreateSerializers(out var encoder, out var decoder);

            var buffer = encoder.Encode(encoderType, e => e.WriteVariant("test", expected));
            var result = decoder.Decode(decoderType, buffer, d => d.ReadVariant("test"));

            Assert.Equal(expected, result);
        }

        [Theory]
        // [InlineData(ContentEncodings.MimeTypeUaJson, ContentEncodings.MimeTypeUaJsonReference)]
        // [InlineData(ContentEncodings.MimeTypeUaJsonReference, ContentEncodings.MimeTypeUaJson)]
        // [InlineData(ContentEncodings.MimeTypeUaJsonReference, ContentEncodings.MimeTypeUaJsonReference)]
        // [InlineData(ContentEncodings.MimeTypeUaNonReversibleJsonReference, ContentEncodings.MimeTypeUaJson)]
        [InlineData(ContentMimeType.UaNonReversibleJson, ContentMimeType.UaJson)]
        [InlineData(ContentMimeType.UaJson, ContentMimeType.UaJson)]
        [InlineData(ContentMimeType.UaBinary, ContentMimeType.UaBinary)]
        [InlineData(ContentMimeType.UaXml, ContentMimeType.UaXml)]
        public void ReadWriteStringMatrixVariant(string encoderType, string decoderType) {
            var expected = new Variant(new[, ,] {
                { { "1", "1", "1" }, { "2", "2", "2" }, { "3", "3", "3" } },
                { { "1", "1", "1" }, { "2", "2", "2" }, { "3", "3", "3" } },
                { { "1", "1", "1" }, { "2", "2", "2" }, { "3", "3", "3" } },
                { { "1", "1", "1" }, { "2", "2", "2" }, { "3", "3", "3" } },
                { { "1", "1", "1" }, { "2", "2", "2" }, { "3", "3", "3" } },
                { { "1", "1", "1" }, { "2", "2", "2" }, { "3", "3", "3" } },
                { { "1", "1", "1" }, { "2", "2", "2" }, { "3", "3", "3" } }
            });
            CreateSerializers(out var encoder, out var decoder);
            var buffer = encoder.Encode(encoderType, e => e.WriteVariant("test", expected));
            var result = decoder.Decode(decoderType, buffer, d => d.ReadVariant("test"));
            Assert.True(expected.Value is Matrix);
            Assert.True(result.Value is Matrix);
            Assert.Equal(((Matrix)expected.Value).Elements, ((Matrix)result.Value).Elements);
            Assert.Equal(((Matrix)expected.Value).Dimensions, ((Matrix)result.Value).Dimensions);
        }

        [Theory]
        // [InlineData(ContentEncodings.MimeTypeUaJson, ContentEncodings.MimeTypeUaJsonReference)]
        // [InlineData(ContentEncodings.MimeTypeUaJsonReference, ContentEncodings.MimeTypeUaJson)]
        // [InlineData(ContentEncodings.MimeTypeUaJsonReference, ContentEncodings.MimeTypeUaJsonReference)]
        // [InlineData(ContentEncodings.MimeTypeUaNonReversibleJsonReference, ContentEncodings.MimeTypeUaJson)]
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
            var result = decoder.Decode(decoderType, buffer, d => d.ReadVariant("test"));
            Assert.True(expected.Value is Matrix);
            Assert.True(result.Value is Matrix);
            Assert.Equal(((Matrix)expected.Value).Elements, ((Matrix)result.Value).Elements);
            Assert.Equal(((Matrix)expected.Value).Dimensions, ((Matrix)result.Value).Dimensions);
        }

        [Theory]
        // [InlineData(ContentEncodings.MimeTypeUaJson, ContentEncodings.MimeTypeUaJsonReference)]
        // [InlineData(ContentEncodings.MimeTypeUaJsonReference, ContentEncodings.MimeTypeUaJson)]
        // [InlineData(ContentEncodings.MimeTypeUaJsonReference, ContentEncodings.MimeTypeUaJsonReference)]
        // [InlineData(ContentEncodings.MimeTypeUaNonReversibleJsonReference, ContentEncodings.MimeTypeUaJson)]
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
            var result = decoder.Decode(decoderType, buffer, d => d.ReadVariant("test"));
            Assert.True(expected.Value is Matrix);
            Assert.True(result.Value is Matrix);
            Assert.Equal(((Matrix)expected.Value).Elements, ((Matrix)result.Value).Elements);
            Assert.Equal(((Matrix)expected.Value).Dimensions, ((Matrix)result.Value).Dimensions);
        }

        [Theory]
        // [InlineData(ContentEncodings.MimeTypeUaJson, ContentEncodings.MimeTypeUaJsonReference)]
        // [InlineData(ContentEncodings.MimeTypeUaJsonReference, ContentEncodings.MimeTypeUaJson)]
        // [InlineData(ContentEncodings.MimeTypeUaJsonReference, ContentEncodings.MimeTypeUaJsonReference)]
        // [InlineData(ContentEncodings.MimeTypeUaNonReversibleJsonReference, ContentEncodings.MimeTypeUaJson)]
        [InlineData(ContentMimeType.UaNonReversibleJson, ContentMimeType.UaJson)]
        [InlineData(ContentMimeType.UaJson, ContentMimeType.UaJson)]
        [InlineData(ContentMimeType.UaBinary, ContentMimeType.UaBinary)]
        [InlineData(ContentMimeType.UaXml, ContentMimeType.UaXml)]
        public void ReadWriteNullVariant(string encoderType, string decoderType) {
            var expected = Variant.Null;
            CreateSerializers(out var encoder, out var decoder);

            var buffer = encoder.Encode(encoderType, e => e.WriteVariant("test", expected));
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
        public void ReadWriteUint64AsUint16(string encoderType, string decoderType) {
            var expected = (ulong)99;
            CreateSerializers(out var encoder, out var decoder);

            var buffer = encoder.Encode(encoderType, e => e.WriteUInt64("test", expected));
            var result = decoder.Decode(decoderType, buffer, d => d.ReadUInt16("test"));

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
        public void ReadWriteUint64AsString(string encoderType, string decoderType) {
            var expected = (ulong)99;
            CreateSerializers(out var encoder, out var decoder);

            var buffer = encoder.Encode(encoderType, e => e.WriteString("test", "99"));
            var result = decoder.Decode(decoderType, buffer, d => d.ReadUInt64("test"));

            Assert.Equal(expected, result);
        }

        [Theory]
        // [InlineData(ContentEncodings.MimeTypeUaJson, ContentEncodings.MimeTypeUaJsonReference)]
        [InlineData(ContentMimeType.UaJsonReference, ContentMimeType.UaJson)]
        // [InlineData(ContentEncodings.MimeTypeUaJsonReference, ContentEncodings.MimeTypeUaJsonReference)]
        [InlineData(ContentMimeType.UaNonReversibleJsonReference, ContentMimeType.UaJson)]
        [InlineData(ContentMimeType.UaNonReversibleJson, ContentMimeType.UaJson)]
        [InlineData(ContentMimeType.UaJson, ContentMimeType.UaJson)]
        [InlineData(ContentMimeType.UaBinary, ContentMimeType.UaBinary)]
        [InlineData(ContentMimeType.UaXml, ContentMimeType.UaXml)]
        public void ReadWriteStringArrayVariant(string encoderType, string decoderType) {
            var expected = new Variant(new string[] { "1", "2", "3", "4", "5" });
            CreateSerializers(out var encoder, out var decoder);

            var buffer = encoder.Encode(encoderType, e => e.WriteVariant("test", expected));
            var result = decoder.Decode(decoderType, buffer, d => d.ReadVariant("test"));

            Assert.Equal(expected, result);
        }

        [Theory]
        // Broken [InlineData(ContentEncodings.MimeTypeUaJson, ContentEncodings.MimeTypeUaJsonReference)]
        [InlineData(ContentMimeType.UaJsonReference, ContentMimeType.UaJson)]
        // Broken [InlineData(ContentEncodings.MimeTypeUaJsonReference, ContentEncodings.MimeTypeUaJsonReference)]
        // Broken [InlineData(ContentEncodings.MimeTypeUaNonReversibleJsonReference, ContentEncodings.MimeTypeUaJson)]
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
            var test = System.Text.Encoding.UTF8.GetString(buffer);
            var result = decoder.Decode(decoderType, buffer, d => d.ReadVariantArray("test"));

            Assert.Equal(expected, result);
        }

        /// <summary>
        /// Test encode string array variant
        /// </summary>
        [Theory]
        // [InlineData(ContentEncodings.MimeTypeUaJson, ContentEncodings.MimeTypeUaJsonReference)]
        // [InlineData(ContentEncodings.MimeTypeUaJsonReference, ContentEncodings.MimeTypeUaJson)]
        // [InlineData(ContentEncodings.MimeTypeUaJsonReference, ContentEncodings.MimeTypeUaJsonReference)]
        [InlineData(ContentMimeType.UaJson, ContentMimeType.UaJson)]
        [InlineData(ContentMimeType.UaBinary, ContentMimeType.UaBinary)]
        [InlineData(ContentMimeType.UaXml, ContentMimeType.UaXml)]
        public void ReadNodeAttributeSet(string encoderType, string decoderType) {
            var expected = new NodeAttributeSet();
            var map = new AttributeMap();
            expected.SetAttribute(Attributes.NodeClass, NodeClass.Variable);
            expected.SetAttribute(Attributes.BrowseName, new QualifiedName("Somename"));
            expected.SetAttribute(Attributes.NodeId, new NodeId(Guid.NewGuid()));
            expected.SetAttribute(Attributes.DisplayName, new LocalizedText("hello world"));
            expected.SetAttribute(Attributes.Value, 1235);
            expected.SetAttribute(Attributes.Description, new LocalizedText("test"));
            expected.SetAttribute(Attributes.DataType, new NodeId(Guid.NewGuid()));
            CreateSerializers(out var encoder, out var decoder);

            var buffer = encoder.Encode(encoderType, e => e.WriteEncodeable("test", expected, expected.GetType()));
            var result = decoder.Decode(decoderType, buffer, d => d.ReadEncodeable("test", typeof(NodeAttributeSet)));

            Assert.True(expected.IsEqual(result));
        }

        [Theory]
        // [InlineData(ContentEncodings.MimeTypeUaJson, ContentEncodings.MimeTypeUaJsonReference)]
        [InlineData(ContentMimeType.UaJsonReference, ContentMimeType.UaJson)]
        // [InlineData(ContentEncodings.MimeTypeUaJsonReference, ContentEncodings.MimeTypeUaJsonReference)]
        // [InlineData(ContentEncodings.MimeTypeUaNonReversibleJsonReference, ContentEncodings.MimeTypeUaJson)]
        [InlineData(ContentMimeType.UaNonReversibleJson, ContentMimeType.UaJson)]
        [InlineData(ContentMimeType.UaJson, ContentMimeType.UaJson)]
        // TODO [InlineData(ContentEncodings.MimeTypeUaBinary, ContentEncodings.MimeTypeUaBinary)]
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
            var result = decoder.Decode(decoderType, buffer, d => d.ReadEncodeable("test", typeof(ProgramDiagnostic2DataType)));
            Assert.True(result.IsEqual(expected));
        }

        private static ServiceMessageContext CreateSerializers(
            out ITypeSerializer encoder, out ITypeSerializer decoder) {
            var context = new ServiceMessageContext();
            encoder = new TypeSerializer(context);
            decoder = new TypeSerializer(context);
            return context;
        }
    }
}
