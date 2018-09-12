// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Protocol.Stack.Encoders {
    using Opc.Ua.Encoders;
    using Opc.Ua.Models;
    using Opc.Ua;
    using System;
    using Xunit;
    using System.Globalization;

    public class TypeSerializerTests {

        /// <summary>
        /// Test encode a date time
        /// </summary>
        [Theory]
        [InlineData(ContentEncodings.MimeTypeUaJson, ContentEncodings.MimeTypeUaJsonReference)]
        [InlineData(ContentEncodings.MimeTypeUaJsonReference, ContentEncodings.MimeTypeUaJson)]
        [InlineData(ContentEncodings.MimeTypeUaJsonReference, ContentEncodings.MimeTypeUaJsonReference)]
        [InlineData(ContentEncodings.MimeTypeUaNonReversibleJsonReference, ContentEncodings.MimeTypeUaJson)]
        [InlineData(ContentEncodings.MimeTypeUaNonReversibleJson, ContentEncodings.MimeTypeUaJson)]
        [InlineData(ContentEncodings.MimeTypeUaJson, ContentEncodings.MimeTypeUaJson)]
        [InlineData(ContentEncodings.MimeTypeUaBinary, ContentEncodings.MimeTypeUaBinary)]
        [InlineData(ContentEncodings.MimeTypeUaXml, ContentEncodings.MimeTypeUaXml)]
        public void ReadWriteDateTime(string encoderType, string decoderType) {
            var expected = DateTime.UtcNow;
            CreateSerializers(encoderType, decoderType, out var encoder, out var decoder);

            var buffer = encoder.Encode(e => e.WriteDateTime("test", expected));
            var result = decoder.Decode(buffer, d => d.ReadDateTime("test"));

            Assert.Equal(expected, result);
        }

        /// <summary>
        /// Test encode a date time array
        /// </summary>
        [Theory]
        // [InlineData(ContentEncodings.MimeTypeUaJson, ContentEncodings.MimeTypeUaJsonReference)]
        // [InlineData(ContentEncodings.MimeTypeUaJsonReference, ContentEncodings.MimeTypeUaJson)]
        // [InlineData(ContentEncodings.MimeTypeUaJsonReference, ContentEncodings.MimeTypeUaJsonReference)]
        // [InlineData(ContentEncodings.MimeTypeUaNonReversibleJsonReference, ContentEncodings.MimeTypeUaJson)]
        [InlineData(ContentEncodings.MimeTypeUaNonReversibleJson, ContentEncodings.MimeTypeUaJson)]
        [InlineData(ContentEncodings.MimeTypeUaJson, ContentEncodings.MimeTypeUaJson)]
        [InlineData(ContentEncodings.MimeTypeUaBinary, ContentEncodings.MimeTypeUaBinary)]
        [InlineData(ContentEncodings.MimeTypeUaXml, ContentEncodings.MimeTypeUaXml)]
        public void ReadWriteDateTimeArray(string encoderType, string decoderType) {
            var expected = new[] {
                DateTime.UtcNow, DateTime.UtcNow, DateTime.UtcNow, DateTime.UtcNow, DateTime.UtcNow,
                DateTime.UtcNow, DateTime.UtcNow, DateTime.UtcNow, DateTime.UtcNow, DateTime.UtcNow,
                DateTime.UtcNow, DateTime.UtcNow, DateTime.UtcNow, DateTime.UtcNow, DateTime.UtcNow,
                DateTime.UtcNow, DateTime.UtcNow, DateTime.UtcNow, DateTime.UtcNow, DateTime.UtcNow
            };
            CreateSerializers(encoderType, decoderType, out var encoder, out var decoder);

            var buffer = encoder.Encode(e => e.WriteDateTimeArray("test", expected));
            var result = decoder.Decode(buffer, d => d.ReadDateTimeArray("test"));

            Assert.Equal(expected, result);
        }

        /// <summary>
        /// Test encode qualified name
        /// </summary>
        [Theory]
        // [InlineData(ContentEncodings.MimeTypeUaJson, ContentEncodings.MimeTypeUaJsonReference)]
        // [InlineData(ContentEncodings.MimeTypeUaJsonReference, ContentEncodings.MimeTypeUaJson)]
        // [InlineData(ContentEncodings.MimeTypeUaJsonReference, ContentEncodings.MimeTypeUaJsonReference)]
        // [InlineData(ContentEncodings.MimeTypeUaNonReversibleJsonReference, ContentEncodings.MimeTypeUaJson)]
        [InlineData(ContentEncodings.MimeTypeUaNonReversibleJson, ContentEncodings.MimeTypeUaJson)]
        [InlineData(ContentEncodings.MimeTypeUaJson, ContentEncodings.MimeTypeUaJson)]
        [InlineData(ContentEncodings.MimeTypeUaBinary, ContentEncodings.MimeTypeUaBinary)]
        [InlineData(ContentEncodings.MimeTypeUaXml, ContentEncodings.MimeTypeUaXml)]
        public void ReadWriteQualifiedName(string encoderType, string decoderType) {
            var expected = new QualifiedName("hello");
            CreateSerializers(encoderType, decoderType, out var encoder, out var decoder);

            var buffer = encoder.Encode(e => e.WriteQualifiedName("test", expected));
            var result = decoder.Decode(buffer, d => d.ReadQualifiedName("test"));

            Assert.Equal(expected, result);
        }

        /// <summary>
        /// Test encode qualified name
        /// </summary>
        [Theory]
        // [InlineData(ContentEncodings.MimeTypeUaJson, ContentEncodings.MimeTypeUaJsonReference)]
        // [InlineData(ContentEncodings.MimeTypeUaJsonReference, ContentEncodings.MimeTypeUaJson)]
        // [InlineData(ContentEncodings.MimeTypeUaJsonReference, ContentEncodings.MimeTypeUaJsonReference)]
        // [InlineData(ContentEncodings.MimeTypeUaNonReversibleJsonReference, ContentEncodings.MimeTypeUaJson)]
        [InlineData(ContentEncodings.MimeTypeUaNonReversibleJson, ContentEncodings.MimeTypeUaJson)]
        [InlineData(ContentEncodings.MimeTypeUaJson, ContentEncodings.MimeTypeUaJson)]
        [InlineData(ContentEncodings.MimeTypeUaBinary, ContentEncodings.MimeTypeUaBinary)]
        [InlineData(ContentEncodings.MimeTypeUaXml, ContentEncodings.MimeTypeUaXml)]
        public void ReadWriteQualifiedNameArray(string encoderType, string decoderType) {
            var expected = new[] {
                new QualifiedName("bla", 0),
                new QualifiedName("bla44", 0),
                new QualifiedName("bla2", 0),
                new QualifiedName("bla", 0),
                };
            CreateSerializers(encoderType, decoderType, out var encoder, out var decoder);

            var buffer = encoder.Encode(e => e.WriteQualifiedNameArray("test", expected));
            var result = decoder.Decode(buffer, d => d.ReadQualifiedNameArray("test"));

            Assert.Equal(expected, result);
        }

        /// <summary>
        /// Test encode localized text
        /// </summary>
        [Theory]
        [InlineData(ContentEncodings.MimeTypeUaJson, ContentEncodings.MimeTypeUaJsonReference)]
        [InlineData(ContentEncodings.MimeTypeUaJsonReference, ContentEncodings.MimeTypeUaJson)]
        [InlineData(ContentEncodings.MimeTypeUaJsonReference, ContentEncodings.MimeTypeUaJsonReference)]
        [InlineData(ContentEncodings.MimeTypeUaNonReversibleJsonReference, ContentEncodings.MimeTypeUaJson)]
        [InlineData(ContentEncodings.MimeTypeUaNonReversibleJson, ContentEncodings.MimeTypeUaJson)]
        [InlineData(ContentEncodings.MimeTypeUaJson, ContentEncodings.MimeTypeUaJson)]
        [InlineData(ContentEncodings.MimeTypeUaBinary, ContentEncodings.MimeTypeUaBinary)]
        [InlineData(ContentEncodings.MimeTypeUaXml, ContentEncodings.MimeTypeUaXml)]
        public void ReadWriteLocalizedText1(string encoderType, string decoderType) {
            var expected = new LocalizedText("hello");
            CreateSerializers(encoderType, decoderType, out var encoder, out var decoder);

            var buffer = encoder.Encode(e => e.WriteLocalizedText("test", expected));
            var result = decoder.Decode(buffer, d => d.ReadLocalizedText("test"));

            Assert.Equal(expected, result);
        }

        /// <summary>
        /// Test encode localized text
        /// </summary>
        [Theory]
        [InlineData(ContentEncodings.MimeTypeUaJson, ContentEncodings.MimeTypeUaJsonReference)]
        [InlineData(ContentEncodings.MimeTypeUaJsonReference, ContentEncodings.MimeTypeUaJson)]
        [InlineData(ContentEncodings.MimeTypeUaJsonReference, ContentEncodings.MimeTypeUaJsonReference)]
        [InlineData(ContentEncodings.MimeTypeUaNonReversibleJson, ContentEncodings.MimeTypeUaJson)]
        [InlineData(ContentEncodings.MimeTypeUaJson, ContentEncodings.MimeTypeUaJson)]
        [InlineData(ContentEncodings.MimeTypeUaBinary, ContentEncodings.MimeTypeUaBinary)]
        [InlineData(ContentEncodings.MimeTypeUaXml, ContentEncodings.MimeTypeUaXml)]
        public void ReadWriteLocalizedText2(string encoderType, string decoderType) {
            var expected = new LocalizedText(CultureInfo.CurrentCulture.Name, "hello");
            CreateSerializers(encoderType, decoderType, out var encoder, out var decoder);

            var buffer = encoder.Encode(e => e.WriteLocalizedText("test", expected));
            var result = decoder.Decode(buffer, d => d.ReadLocalizedText("test"));

            Assert.Equal(expected, result);
        }

        /// <summary>
        /// Test encode localized text
        /// </summary>
        [Theory]
        [InlineData(ContentEncodings.MimeTypeUaJson, ContentEncodings.MimeTypeUaJsonReference)]
        [InlineData(ContentEncodings.MimeTypeUaJsonReference, ContentEncodings.MimeTypeUaJson)]
        [InlineData(ContentEncodings.MimeTypeUaJsonReference, ContentEncodings.MimeTypeUaJsonReference)]
        [InlineData(ContentEncodings.MimeTypeUaNonReversibleJsonReference, ContentEncodings.MimeTypeUaJson)]
        [InlineData(ContentEncodings.MimeTypeUaNonReversibleJson, ContentEncodings.MimeTypeUaJson)]
        [InlineData(ContentEncodings.MimeTypeUaJson, ContentEncodings.MimeTypeUaJson)]
        [InlineData(ContentEncodings.MimeTypeUaBinary, ContentEncodings.MimeTypeUaBinary)]
        [InlineData(ContentEncodings.MimeTypeUaXml, ContentEncodings.MimeTypeUaXml)]
        public void ReadWriteLocalizedTextArray1(string encoderType, string decoderType) {
            var expected = new[] {
                new LocalizedText("hello"),
                new LocalizedText("world"),
                new LocalizedText("here"),
                new LocalizedText("I am"),
                };
            CreateSerializers(encoderType, decoderType, out var encoder, out var decoder);

            var buffer = encoder.Encode(e => e.WriteLocalizedTextArray("test", expected));
            var result = decoder.Decode(buffer, d => d.ReadLocalizedTextArray("test"));

            Assert.Equal(expected, result);
        }

        /// <summary>
        /// Test encode localized text
        /// </summary>
        [Theory]
        [InlineData(ContentEncodings.MimeTypeUaJson, ContentEncodings.MimeTypeUaJsonReference)]
        [InlineData(ContentEncodings.MimeTypeUaJsonReference, ContentEncodings.MimeTypeUaJson)]
        [InlineData(ContentEncodings.MimeTypeUaJsonReference, ContentEncodings.MimeTypeUaJsonReference)]
        [InlineData(ContentEncodings.MimeTypeUaNonReversibleJson, ContentEncodings.MimeTypeUaJson)]
        [InlineData(ContentEncodings.MimeTypeUaJson, ContentEncodings.MimeTypeUaJson)]
        [InlineData(ContentEncodings.MimeTypeUaBinary, ContentEncodings.MimeTypeUaBinary)]
        [InlineData(ContentEncodings.MimeTypeUaXml, ContentEncodings.MimeTypeUaXml)]
        public void ReadWriteLocalizedTextArray2(string encoderType, string decoderType) {
            var expected = new[] {
                new LocalizedText(CultureInfo.CurrentCulture.Name, "hello"),
                new LocalizedText(CultureInfo.CurrentCulture.Name, "world"),
                new LocalizedText(CultureInfo.CurrentCulture.Name, "here"),
                new LocalizedText(CultureInfo.CurrentCulture.Name, "I am"),
                };
            CreateSerializers(encoderType, decoderType, out var encoder, out var decoder);

            var buffer = encoder.Encode(e => e.WriteLocalizedTextArray("test", expected));
            var result = decoder.Decode(buffer, d => d.ReadLocalizedTextArray("test"));

            Assert.Equal(expected, result);
        }

        /// <summary>
        /// Test encode status code value
        /// </summary>
        [Theory]
        [InlineData(ContentEncodings.MimeTypeUaJson, ContentEncodings.MimeTypeUaJsonReference)]
        [InlineData(ContentEncodings.MimeTypeUaJsonReference, ContentEncodings.MimeTypeUaJson)]
        [InlineData(ContentEncodings.MimeTypeUaJsonReference, ContentEncodings.MimeTypeUaJsonReference)]
        [InlineData(ContentEncodings.MimeTypeUaNonReversibleJsonReference, ContentEncodings.MimeTypeUaJson)]
        [InlineData(ContentEncodings.MimeTypeUaNonReversibleJson, ContentEncodings.MimeTypeUaJson)]
        [InlineData(ContentEncodings.MimeTypeUaJson, ContentEncodings.MimeTypeUaJson)]
        [InlineData(ContentEncodings.MimeTypeUaBinary, ContentEncodings.MimeTypeUaBinary)]
        [InlineData(ContentEncodings.MimeTypeUaXml, ContentEncodings.MimeTypeUaXml)]
        public void ReadWriteStatusCode(string encoderType, string decoderType) {
            // Create graph
            var expected = new StatusCode(StatusCodes.BadAggregateInvalidInputs);
            CreateSerializers(encoderType, decoderType, out var encoder, out var decoder);

            var buffer = encoder.Encode(e => e.WriteStatusCode("test", expected));
            var result = decoder.Decode(buffer, d => d.ReadStatusCode("test"));

            Assert.Equal(expected, result);
        }

        /// <summary>
        /// Test encode argument
        /// </summary>
        [Theory]
        [InlineData(ContentEncodings.MimeTypeUaJson, ContentEncodings.MimeTypeUaJsonReference)]
        [InlineData(ContentEncodings.MimeTypeUaJsonReference, ContentEncodings.MimeTypeUaJson)]
        [InlineData(ContentEncodings.MimeTypeUaJsonReference, ContentEncodings.MimeTypeUaJsonReference)]
        [InlineData(ContentEncodings.MimeTypeUaNonReversibleJsonReference, ContentEncodings.MimeTypeUaJson)]
        [InlineData(ContentEncodings.MimeTypeUaNonReversibleJson, ContentEncodings.MimeTypeUaJson)]
        [InlineData(ContentEncodings.MimeTypeUaJson, ContentEncodings.MimeTypeUaJson)]
        [InlineData(ContentEncodings.MimeTypeUaBinary, ContentEncodings.MimeTypeUaBinary)]
        [InlineData(ContentEncodings.MimeTypeUaXml, ContentEncodings.MimeTypeUaXml)]
        public void ReadWriteArgument(string encoderType, string decoderType) {
            var expected = new Argument("something1",
                    new NodeId(2354), -1, "somedesciroeioi") {
                ArrayDimensions = new uint[0]
            };
            CreateSerializers(encoderType, decoderType, out var encoder, out var decoder);

            // read back
            var buffer = encoder.Encode(e => e.WriteEncodeable("test", expected, typeof(Argument)));
            var result = decoder.Decode(buffer, d => d.ReadEncodeable("test", typeof(Argument)));

            Assert.True(result.IsEqual(expected));
        }

        /// <summary>
        /// Test encode argument as array
        /// </summary>
        [Theory]
        // [InlineData(ContentEncodings.MimeTypeUaJson, ContentEncodings.MimeTypeUaJsonReference)]
        [InlineData(ContentEncodings.MimeTypeUaJsonReference, ContentEncodings.MimeTypeUaJson)]
        [InlineData(ContentEncodings.MimeTypeUaJsonReference, ContentEncodings.MimeTypeUaJsonReference)]
        [InlineData(ContentEncodings.MimeTypeUaNonReversibleJsonReference, ContentEncodings.MimeTypeUaJson)]
        [InlineData(ContentEncodings.MimeTypeUaNonReversibleJson, ContentEncodings.MimeTypeUaJson)]
        [InlineData(ContentEncodings.MimeTypeUaJson, ContentEncodings.MimeTypeUaJson)]
        [InlineData(ContentEncodings.MimeTypeUaBinary, ContentEncodings.MimeTypeUaBinary)]
        [InlineData(ContentEncodings.MimeTypeUaXml, ContentEncodings.MimeTypeUaXml)]
        public void ReadWriteArgumentArray(string encoderType, string decoderType) {
            var expected = new [] {
                new Argument("something1",
                    new NodeId(2354), -1, "somedesciroeioi") { ArrayDimensions = new uint[0] },
                new Argument("something2",
                    new NodeId(23), -1, "fdsadfsdaf") { ArrayDimensions = new uint[0] },
                new Argument("something3",
                    new NodeId(44), 1, "fsadf  sadfsdfsadfsd") { ArrayDimensions = new uint[0] },
                new Argument("something4",
                    new NodeId(23), 1, "dfad  sdafdfdf  fasdf") { ArrayDimensions = new uint[0] }
            };
            CreateSerializers(encoderType, decoderType, out var encoder, out var decoder);

            var buffer = encoder.Encode(e => e.WriteEncodeableArray("test", expected, typeof(Argument)));
            var test = System.Text.Encoding.UTF8.GetString(buffer);
            var result = (ArgumentCollection)decoder.Decode(buffer, d => d.ReadEncodeableArray("test", typeof(Argument)));

            for (var i = 0; i < result.Count; i++) {
                Assert.True(result[i].IsEqual(expected[i]));
            }
        }

        /// <summary>
        /// Test encode string array
        /// </summary>
        [Theory]
        [InlineData(ContentEncodings.MimeTypeUaJson, ContentEncodings.MimeTypeUaJsonReference)]
        [InlineData(ContentEncodings.MimeTypeUaJsonReference, ContentEncodings.MimeTypeUaJson)]
        [InlineData(ContentEncodings.MimeTypeUaJsonReference, ContentEncodings.MimeTypeUaJsonReference)]
        [InlineData(ContentEncodings.MimeTypeUaNonReversibleJsonReference, ContentEncodings.MimeTypeUaJson)]
        [InlineData(ContentEncodings.MimeTypeUaNonReversibleJson, ContentEncodings.MimeTypeUaJson)]
        [InlineData(ContentEncodings.MimeTypeUaJson, ContentEncodings.MimeTypeUaJson)]
        [InlineData(ContentEncodings.MimeTypeUaBinary, ContentEncodings.MimeTypeUaBinary)]
        [InlineData(ContentEncodings.MimeTypeUaXml, ContentEncodings.MimeTypeUaXml)]
        public void ReadWriteStringArray(string encoderType, string decoderType) {
            var expected = new string[] { "1", "2", "3", "4", "5" };
            CreateSerializers(encoderType, decoderType, out var encoder, out var decoder);

            var buffer = encoder.Encode(e => e.WriteStringArray("test", expected));
            var result = decoder.Decode(buffer, d => d.ReadStringArray("test"));

            Assert.Equal(expected, result);
        }

        /// <summary>
        /// Test encode string variant
        /// </summary>
        [Theory]
        // [InlineData(ContentEncodings.MimeTypeUaJson, ContentEncodings.MimeTypeUaJsonReference)]
        // [InlineData(ContentEncodings.MimeTypeUaJsonReference, ContentEncodings.MimeTypeUaJson)]
        // [InlineData(ContentEncodings.MimeTypeUaJsonReference, ContentEncodings.MimeTypeUaJsonReference)]
        // [InlineData(ContentEncodings.MimeTypeUaNonReversibleJsonReference, ContentEncodings.MimeTypeUaJson)]
        [InlineData(ContentEncodings.MimeTypeUaNonReversibleJson, ContentEncodings.MimeTypeUaJson)]
        [InlineData(ContentEncodings.MimeTypeUaJson, ContentEncodings.MimeTypeUaJson)]
        [InlineData(ContentEncodings.MimeTypeUaBinary, ContentEncodings.MimeTypeUaBinary)]
        [InlineData(ContentEncodings.MimeTypeUaXml, ContentEncodings.MimeTypeUaXml)]
        public void ReadWriteStringVariant(string encoderType, string decoderType) {
            var expected = new Variant("5");
            CreateSerializers(encoderType, decoderType, out var encoder, out var decoder);

            var buffer = encoder.Encode(e => e.WriteVariant("test", expected));
            var result = decoder.Decode(buffer, d => d.ReadVariant("test"));

            Assert.Equal(expected, result);
        }

        /// <summary>
        /// Test encode string variant
        /// </summary>
        [Theory]
        [InlineData(ContentEncodings.MimeTypeUaJson, ContentEncodings.MimeTypeUaJsonReference)]
        [InlineData(ContentEncodings.MimeTypeUaJsonReference, ContentEncodings.MimeTypeUaJson)]
        [InlineData(ContentEncodings.MimeTypeUaJsonReference, ContentEncodings.MimeTypeUaJsonReference)]
        [InlineData(ContentEncodings.MimeTypeUaJson, ContentEncodings.MimeTypeUaJson)]
        [InlineData(ContentEncodings.MimeTypeUaBinary, ContentEncodings.MimeTypeUaBinary)]
        [InlineData(ContentEncodings.MimeTypeUaXml, ContentEncodings.MimeTypeUaXml)]
        public void ReadWriteUintVariant(string encoderType, string decoderType) {
            var expected = new Variant((uint)99);
            CreateSerializers(encoderType, decoderType, out var encoder, out var decoder);

            var buffer = encoder.Encode(e => e.WriteVariant("test", expected));
            var result = decoder.Decode(buffer, d => d.ReadVariant("test"));

            Assert.Equal(expected, result);
        }

        /// <summary>
        /// Test encode string variant
        /// </summary>
        [Theory]
        [InlineData(ContentEncodings.MimeTypeUaJson, ContentEncodings.MimeTypeUaJsonReference)]
        [InlineData(ContentEncodings.MimeTypeUaJsonReference, ContentEncodings.MimeTypeUaJson)]
        [InlineData(ContentEncodings.MimeTypeUaJsonReference, ContentEncodings.MimeTypeUaJsonReference)]
        [InlineData(ContentEncodings.MimeTypeUaNonReversibleJsonReference, ContentEncodings.MimeTypeUaJson)]
        [InlineData(ContentEncodings.MimeTypeUaNonReversibleJson, ContentEncodings.MimeTypeUaJson)]
        [InlineData(ContentEncodings.MimeTypeUaJson, ContentEncodings.MimeTypeUaJson)]
        [InlineData(ContentEncodings.MimeTypeUaBinary, ContentEncodings.MimeTypeUaBinary)]
        [InlineData(ContentEncodings.MimeTypeUaXml, ContentEncodings.MimeTypeUaXml)]
        public void ReadWriteUint64AsUint16(string encoderType, string decoderType) {
            var expected = (ulong)99;
            CreateSerializers(encoderType, decoderType, out var encoder, out var decoder);

            var buffer = encoder.Encode(e => e.WriteUInt64("test", expected));
            var result = decoder.Decode(buffer, d => d.ReadUInt16("test"));

            Assert.Equal(expected, result);
        }

        /// <summary>
        /// Test encode string variant - cannot do this on binary
        /// </summary>
        [Theory]
        [InlineData(ContentEncodings.MimeTypeUaJson, ContentEncodings.MimeTypeUaJsonReference)]
        [InlineData(ContentEncodings.MimeTypeUaJsonReference, ContentEncodings.MimeTypeUaJson)]
        [InlineData(ContentEncodings.MimeTypeUaJsonReference, ContentEncodings.MimeTypeUaJsonReference)]
        [InlineData(ContentEncodings.MimeTypeUaNonReversibleJsonReference, ContentEncodings.MimeTypeUaJson)]
        [InlineData(ContentEncodings.MimeTypeUaNonReversibleJson, ContentEncodings.MimeTypeUaJson)]
        [InlineData(ContentEncodings.MimeTypeUaJson, ContentEncodings.MimeTypeUaJson)]
        [InlineData(ContentEncodings.MimeTypeUaXml, ContentEncodings.MimeTypeUaXml)]
        public void ReadWriteUint64AsString(string encoderType, string decoderType) {
            var expected = (ulong)99;
            CreateSerializers(encoderType, decoderType, out var encoder, out var decoder);

            var buffer = encoder.Encode(e => e.WriteString("test", "99"));
            var result = decoder.Decode(buffer, d => d.ReadUInt64("test"));

            Assert.Equal(expected, result);
        }

        /// <summary>
        /// Test encode string array variant
        /// </summary>
        [Theory]
        // [InlineData(ContentEncodings.MimeTypeUaJson, ContentEncodings.MimeTypeUaJsonReference)]
        [InlineData(ContentEncodings.MimeTypeUaJsonReference, ContentEncodings.MimeTypeUaJson)]
        // [InlineData(ContentEncodings.MimeTypeUaJsonReference, ContentEncodings.MimeTypeUaJsonReference)]
        [InlineData(ContentEncodings.MimeTypeUaNonReversibleJsonReference, ContentEncodings.MimeTypeUaJson)]
        [InlineData(ContentEncodings.MimeTypeUaNonReversibleJson, ContentEncodings.MimeTypeUaJson)]
        [InlineData(ContentEncodings.MimeTypeUaJson, ContentEncodings.MimeTypeUaJson)]
        [InlineData(ContentEncodings.MimeTypeUaBinary, ContentEncodings.MimeTypeUaBinary)]
        [InlineData(ContentEncodings.MimeTypeUaXml, ContentEncodings.MimeTypeUaXml)]
        public void ReadWriteStringArrayVariant(string encoderType, string decoderType) {
            var expected = new Variant(new string[] { "1", "2", "3", "4", "5" });
            CreateSerializers(encoderType, decoderType, out var encoder, out var decoder);

            var buffer = encoder.Encode(e => e.WriteVariant("test", expected));
            var result = decoder.Decode(buffer, d => d.ReadVariant("test"));

            Assert.Equal(expected, result);
        }

        /// <summary>
        /// Test encode variant collection
        /// </summary>
        [Theory]
        // Broken [InlineData(ContentEncodings.MimeTypeUaJson, ContentEncodings.MimeTypeUaJsonReference)]
        [InlineData(ContentEncodings.MimeTypeUaJsonReference, ContentEncodings.MimeTypeUaJson)]
        // Broken [InlineData(ContentEncodings.MimeTypeUaJsonReference, ContentEncodings.MimeTypeUaJsonReference)]
        // Broken [InlineData(ContentEncodings.MimeTypeUaNonReversibleJsonReference, ContentEncodings.MimeTypeUaJson)]
        [InlineData(ContentEncodings.MimeTypeUaNonReversibleJson, ContentEncodings.MimeTypeUaJson)]
        [InlineData(ContentEncodings.MimeTypeUaJson, ContentEncodings.MimeTypeUaJson)]
        [InlineData(ContentEncodings.MimeTypeUaBinary, ContentEncodings.MimeTypeUaBinary)]
        [InlineData(ContentEncodings.MimeTypeUaXml, ContentEncodings.MimeTypeUaXml)]
        public void ReadWriteVariantCollection(string encoderType, string decoderType) {
            var expected = new VariantCollection {
                new Variant(4L),
                new Variant("test"),
                new Variant(new long[] {1, 2, 3, 4, 5 }),
                new Variant(new string[] {"1", "2", "3", "4", "5" })
            };
            CreateSerializers(encoderType, decoderType, out var encoder, out var decoder);

            var buffer = encoder.Encode(e => e.WriteVariantArray("test", expected));
            var test = System.Text.Encoding.UTF8.GetString(buffer);
            var result = decoder.Decode(buffer, d => d.ReadVariantArray("test"));

            Assert.Equal(expected, result);
        }


        /// <summary>
        /// Test encode string array variant
        /// </summary>
        [Theory]
        // [InlineData(ContentEncodings.MimeTypeUaJson, ContentEncodings.MimeTypeUaJsonReference)]
        // [InlineData(ContentEncodings.MimeTypeUaJsonReference, ContentEncodings.MimeTypeUaJson)]
        // [InlineData(ContentEncodings.MimeTypeUaJsonReference, ContentEncodings.MimeTypeUaJsonReference)]
        [InlineData(ContentEncodings.MimeTypeUaJson, ContentEncodings.MimeTypeUaJson)]
        [InlineData(ContentEncodings.MimeTypeUaBinary, ContentEncodings.MimeTypeUaBinary)]
        [InlineData(ContentEncodings.MimeTypeUaXml, ContentEncodings.MimeTypeUaXml)]
        public void ReadWriteGenericVariableNode(string encoderType, string decoderType) {
            var expected = new GenericNode();
            var map = new AttributeMap();
            expected.SetAttribute(Attributes.NodeClass, NodeClass.Variable);
            expected.SetAttribute(Attributes.BrowseName, new QualifiedName("Somename"));
            expected.SetAttribute(Attributes.NodeId, new NodeId(Guid.NewGuid()));
            expected.SetAttribute(Attributes.DisplayName, new LocalizedText("hello world"));
            expected.SetAttribute(Attributes.Value, 1235);
            expected.SetAttribute(Attributes.Description, new LocalizedText("test"));
            expected.SetAttribute(Attributes.DataType, new NodeId(Guid.NewGuid()));
            CreateSerializers(encoderType, decoderType, out var encoder, out var decoder);

            var buffer = encoder.Encode(e => e.WriteEncodeable("test", expected, expected.GetType()));
            var result = decoder.Decode(buffer, d => d.ReadEncodeable("test", typeof(GenericNode)));

            Assert.Equal(expected, result);
        }

        /// <summary>
        /// test encoding
        /// </summary>
        [Theory]
        // [InlineData(ContentEncodings.MimeTypeUaJson, ContentEncodings.MimeTypeUaJsonReference)]
        [InlineData(ContentEncodings.MimeTypeUaJsonReference, ContentEncodings.MimeTypeUaJson)]
        // [InlineData(ContentEncodings.MimeTypeUaJsonReference, ContentEncodings.MimeTypeUaJsonReference)]
        // [InlineData(ContentEncodings.MimeTypeUaNonReversibleJsonReference, ContentEncodings.MimeTypeUaJson)]
        [InlineData(ContentEncodings.MimeTypeUaNonReversibleJson, ContentEncodings.MimeTypeUaJson)]
        [InlineData(ContentEncodings.MimeTypeUaJson, ContentEncodings.MimeTypeUaJson)]
        // TODO [InlineData(ContentEncodings.MimeTypeUaBinary, ContentEncodings.MimeTypeUaBinary)]
        [InlineData(ContentEncodings.MimeTypeUaXml, ContentEncodings.MimeTypeUaXml)]
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
                    Opc.Ua.Utils.Nonce.CreateNonce(32)),
                LastTransitionTime = DateTime.UtcNow - TimeSpan.FromDays(23)
            };
            CreateSerializers(encoderType, decoderType, out var encoder, out var decoder);

            var buffer = encoder.Encode(e => e.WriteEncodeable("test", expected, typeof(ProgramDiagnostic2DataType)));
            var result = decoder.Decode(buffer, d => d.ReadEncodeable("test", typeof(ProgramDiagnostic2DataType)));
            Assert.True(result.IsEqual(expected));
        }

        private static ServiceMessageContext CreateSerializers(
            string encodeType, string decodeType,
            out ITypeSerializer encoder, out ITypeSerializer decoder) {
            var context = new ServiceMessageContext();
            encoder = new TypeSerializer(encodeType, context);
            decoder = new TypeSerializer(decodeType, context);
            return context;
        }
    }
}
