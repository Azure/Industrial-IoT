// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Opc.Ua.Encoders {
    using Xunit;
    using System.IO;

    public class EncodeableDictionaryTests {

        [Fact]
        public void WriteReadKeyDataValuePairs() {
            var expectedKey1 = "Key1";
            var expectedKey2 = "Key2";
            var expectedKey3 = "Key3";
            var expectedValue1 = new DataValue(new Variant(123));
            var expectedValue2 = new DataValue(new Variant(456));
            var expectedValue3 = new DataValue(new Variant(789));

            var encodeableDictionary = new EncodeableDictionary {
                new KeyDataValuePair { Key = expectedKey1, Value = expectedValue1 },
                new KeyDataValuePair { Key = expectedKey2, Value = expectedValue2 },
                new KeyDataValuePair { Key = expectedKey3, Value = expectedValue3 },
            };

            byte[] buffer;
            var context = new ServiceMessageContext();
            using (var stream = new MemoryStream()) {
                using (var encoder = new JsonEncoderEx(stream, context, JsonEncoderEx.JsonEncoding.Object)) {
                    encodeableDictionary.Encode(encoder);
                }

                // Encoder must be closed before getting buffer.
                buffer = stream.ToArray();
            }

            using (var stream = new MemoryStream(buffer)) {
                using var decoder = new JsonDecoderEx(stream, context);
                var actual = new EncodeableDictionary();
                actual.Decode(decoder);
                Assert.Equal(3, actual.Count);
                Assert.Equal(expectedKey1, actual[0].Key);
                Assert.Equal(expectedValue1, actual[0].Value);
                Assert.Equal(expectedKey2, actual[1].Key);
                Assert.Equal(expectedValue2, actual[1].Value);
                Assert.Equal(expectedKey3, actual[2].Key);
                Assert.Equal(expectedValue3, actual[2].Value);
                var eof = decoder.ReadDataValue(null);
                Assert.Null(eof);
            }
        }

        [Fact]
        public void WriteReadNoKeyDataValuePairs() {
            var encodeableDictionary = new EncodeableDictionary();

            byte[] buffer;
            var context = new ServiceMessageContext();
            using (var stream = new MemoryStream()) {
                using (var encoder = new JsonEncoderEx(stream, context, JsonEncoderEx.JsonEncoding.Object)) {
                    encodeableDictionary.Encode(encoder);
                }

                // Encoder must be closed before getting buffer.
                buffer = stream.ToArray();
            }

            using (var stream = new MemoryStream(buffer)) {
                using var decoder = new JsonDecoderEx(stream, context);
                var actual = new EncodeableDictionary();
                actual.Decode(decoder);
                Assert.Empty(actual);
                var eof = decoder.ReadDataValue(null);
                Assert.Null(eof);
            }
        }

        [Fact]
        public void WriteReadEmptyKeyDataValuePairs() {
            var value = new DataValue(new Variant(123));

            var encodeableDictionary = new EncodeableDictionary {
                new KeyDataValuePair { Key = string.Empty, Value = value },
                new KeyDataValuePair { Key = string.Empty, Value = value },
                new KeyDataValuePair { Key = string.Empty, Value = value },
            };

            byte[] buffer;
            var context = new ServiceMessageContext();
            using (var stream = new MemoryStream()) {
                using (var encoder = new JsonEncoderEx(stream, context, JsonEncoderEx.JsonEncoding.Object)) {
                    encodeableDictionary.Encode(encoder);
                }

                // Encoder must be closed before getting buffer.
                buffer = stream.ToArray();
            }

            using (var stream = new MemoryStream(buffer)) {
                using var decoder = new JsonDecoderEx(stream, context);
                var actual = new EncodeableDictionary();
                actual.Decode(decoder);
                Assert.Empty(actual);
                var eof = decoder.ReadDataValue(null);
                Assert.Null(eof);
            }
        }

        [Fact]
        public void WriteReadNoNullKeyDataValuePairs() {
            var expectedKey1 = "Key1";
            var expectedKey2 = "Key2";
            var expectedKey3 = "Key3";

            var encodeableDictionary = new EncodeableDictionary {
                new KeyDataValuePair { Key = expectedKey1, Value = null },
                new KeyDataValuePair { Key = expectedKey2, Value = null },
                new KeyDataValuePair { Key = expectedKey3, Value = null },
            };

            byte[] buffer;
            var context = new ServiceMessageContext();
            using (var stream = new MemoryStream()) {
                using (var encoder = new JsonEncoderEx(stream, context, JsonEncoderEx.JsonEncoding.Object)) {
                    encodeableDictionary.Encode(encoder);
                }

                // Encoder must be closed before getting buffer.
                buffer = stream.ToArray();
            }

            using (var stream = new MemoryStream(buffer)) {
                using var decoder = new JsonDecoderEx(stream, context);
                var actual = new EncodeableDictionary();
                actual.Decode(decoder);
                Assert.Empty(actual);
                var eof = decoder.ReadDataValue(null);
                Assert.Null(eof);
            }
        }
    }
}
