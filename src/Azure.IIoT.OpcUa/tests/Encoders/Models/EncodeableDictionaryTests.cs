// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Encoders.Models
{
    using Opc.Ua;
    using Opc.Ua.Extensions;
    using System;
    using Xunit;

    public sealed class EncodeableDictionaryTests
    {
        [Fact]
        public void EncodingIds_ReturnDeclaredIds()
        {
            var dictionary = new EncodeableDictionary();

            Assert.Equal((ExpandedNodeId)"s=EncodeableDictionary", dictionary.TypeId);
            Assert.Equal((ExpandedNodeId)"s=EncodeableDictionary_Encoding_DefaultBinary",
                dictionary.BinaryEncodingId);
            Assert.Equal((ExpandedNodeId)"s=EncodeableDictionary_Encoding_DefaultXml",
                dictionary.XmlEncodingId);
            Assert.Equal((ExpandedNodeId)"s=EncodeableDictionary_Encoding_DefaultJson",
                dictionary.JsonEncodingId);
        }

        [Fact]
        public void Constructors_CreateEmptyCapacityAndCollectionInstances()
        {
            var pair = new KeyDataValuePair("field", CreateDataValue(1));

            var empty = new EncodeableDictionary();
            var withCapacity = new EncodeableDictionary(2);
            var withCollection = new EncodeableDictionary([pair]);

            Assert.Empty(empty);
            Assert.Empty(withCapacity);
            Assert.Same(pair, Assert.Single(withCollection));
        }

        [Fact]
        public void Encode_Json_WritesValidEntriesAndSkipsInvalidEntries()
        {
            var dictionary = new EncodeableDictionary
            {
                new("valid", CreateDataValue(1)),
                new(string.Empty, CreateDataValue(2)),
                new("nullValue", null),
                new("localizedTextWithoutContent", new DataValue(new Variant(new LocalizedText((string?)null, (string?)null))))
            };

            var json = dictionary.AsJson(new ServiceMessageContext());

            Assert.Contains("valid", json, StringComparison.Ordinal);
            Assert.DoesNotContain("nullValue", json, StringComparison.Ordinal);
            Assert.DoesNotContain("localizedTextWithoutContent", json, StringComparison.Ordinal);
        }

        [Fact]
        public void Decode_ThrowsNotSupportedException()
        {
            var dictionary = new EncodeableDictionary();

            var exception = Assert.Throws<NotSupportedException>(() =>
            {
                using var decoder = new JsonDecoder("{}", new ServiceMessageContext());
                dictionary.Decode(decoder);
            });
            Assert.Equal("EncodeableDictionary decoding is deferred to Phase 5.",
                exception.Message);
        }

        [Fact]
        public void IsEqual_SameReference_ReturnsTrue()
        {
            var dictionary = new EncodeableDictionary();

            Assert.True(dictionary.IsEqual(dictionary));
        }

        [Fact]
        public void IsEqual_NonDictionary_ReturnsFalse()
        {
            var dictionary = new EncodeableDictionary();

            Assert.False(dictionary.IsEqual(new KeyDataValuePair()));
        }

        [Fact]
        public void IsEqual_DifferentEntries_ReturnsFalse()
        {
            var left = new EncodeableDictionary
            {
                new("field", CreateDataValue(1))
            };
            var right = new EncodeableDictionary
            {
                new("field", CreateDataValue(2))
            };

            Assert.False(left.IsEqual(right));
        }

        [Fact]
        public void Clone_CreatesNewListWithSameEntries()
        {
            var pair = new KeyDataValuePair("field", CreateDataValue(1));
            var dictionary = new EncodeableDictionary([pair]);

            var clone = Assert.IsType<EncodeableDictionary>(dictionary.Clone());

            Assert.NotSame(dictionary, clone);
            Assert.Same(pair, Assert.Single(clone));
        }

        private static DataValue CreateDataValue(object value)
        {
            return new DataValue(new Variant(value));
        }
    }
}
