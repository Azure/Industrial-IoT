// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Encoders.Models
{
    using Opc.Ua;
    using Opc.Ua.Extensions;
    using System.IO;
    using Xunit;

    public sealed class KeyDataValuePairTests
    {
        [Fact]
        public void Constructor_WithValues_SetsProperties()
        {
            var value = CreateDataValue("value");

            var pair = new KeyDataValuePair("field", value);

            Assert.Equal("field", pair.Key);
            Assert.Equal(value, pair.Value);
        }

        [Fact]
        public void DefaultConstructor_InitializesEmptyKeyAndNullValue()
        {
            var pair = new KeyDataValuePair();

            Assert.Equal(string.Empty, pair.Key);
            Assert.Null(pair.Value);
        }

        [Fact]
        public void EncodingIdsAreUnsetForTheBasePair()
        {
            //
            // The base pair declares the encoding ids but never assigns them,
            // so they are the default rather than null - ExpandedNodeId is a
            // value type on the 2.0 stack. A derived pair is expected to supply
            // real ids.
            //
            var pair = new KeyDataValuePair();

            Assert.Equal(ExpandedNodeId.Null, pair.TypeId);
            Assert.Equal(ExpandedNodeId.Null, pair.BinaryEncodingId);
            Assert.Equal(ExpandedNodeId.Null, pair.XmlEncodingId);
            Assert.Equal(ExpandedNodeId.Null, pair.JsonEncodingId);
        }

        [Fact]
        public void EncodeDecode_Binary_RoundTripsKeyAndValue()
        {
            var context = new ServiceMessageContext();
            var expected = new KeyDataValuePair("temperature", CreateDataValue(42));
            var encoded = expected.AsBinary(context);
            var actual = new KeyDataValuePair();

            using (var stream = new MemoryStream(encoded))
            using (var decoder = new BinaryDecoder(stream, context, true))
            {
                actual.Decode(decoder);
            }

            Assert.Equal(expected.Key, actual.Key);
            Assert.Equal(expected.Value?.Value, actual.Value?.Value);
            Assert.True(expected.IsEqual(actual));
        }

        [Fact]
        public void EncodeDecode_Json_RoundTripsKeyAndValue()
        {
            var context = new ServiceMessageContext();
            var expected = new KeyDataValuePair("message", CreateDataValue("hello"));
            var encoded = expected.AsJson(context);
            var actual = new KeyDataValuePair();

            using (var decoder = new JsonDecoder(encoded, context))
            {
                actual.Decode(decoder);
            }

            Assert.Equal(expected.Key, actual.Key);
            Assert.Equal(expected.Value?.Value, actual.Value?.Value);
            Assert.True(expected.IsEqual(actual));
        }

        [Fact]
        public void IsEqual_SameReference_ReturnsTrue()
        {
            var pair = new KeyDataValuePair("field", CreateDataValue(1));

            Assert.True(pair.IsEqual(pair));
        }

        [Fact]
        public void IsEqual_NonPair_ReturnsFalse()
        {
            var pair = new KeyDataValuePair("field", CreateDataValue(1));

            Assert.False(pair.IsEqual(new EncodeableVariantValue()));
        }

        [Fact]
        public void IsEqual_DifferentKey_ReturnsFalse()
        {
            var left = new KeyDataValuePair("left", CreateDataValue(1));
            var right = new KeyDataValuePair("right", CreateDataValue(1));

            Assert.False(left.IsEqual(right));
        }

        [Fact]
        public void IsEqual_DifferentValue_ReturnsFalse()
        {
            var left = new KeyDataValuePair("field", CreateDataValue(1));
            var right = new KeyDataValuePair("field", CreateDataValue(2));

            Assert.False(left.IsEqual(right));
        }

        [Fact]
        public void Clone_CreatesDifferentPairWithSameContent()
        {
            var pair = new KeyDataValuePair("field", CreateDataValue(1));

            var clone = Assert.IsType<KeyDataValuePair>(pair.Clone());

            Assert.NotSame(pair, clone);
            Assert.Equal(pair.Key, clone.Key);
            Assert.Equal(pair.Value, clone.Value);
            Assert.True(pair.IsEqual(clone));
        }

        [Fact]
        public void CollectionConversions_NullArrayAndNullCollection_HandleNulls()
        {
            KeyDataValuePair[]? source = null;
            KeyDataValuePairCollection? nullCollection = null;

            KeyDataValuePairCollection collection = source!;
            var array = (KeyDataValuePair[]?)nullCollection!;

            Assert.Empty(collection);
            Assert.Null(array);
        }

        [Fact]
        public void CollectionConversions_ArrayAndCollection_CopyEntries()
        {
            var pair = new KeyDataValuePair("field", CreateDataValue(1));
            KeyDataValuePairCollection collection = new[] { pair };

            var array = (KeyDataValuePair[]?)collection;

            var item = Assert.Single(collection);
            Assert.Same(pair, item);
            var arrayItem = Assert.Single(array!);
            Assert.Same(pair, arrayItem);
        }

        [Fact]
        public void CollectionMemberwiseClone_ClonesEachPair()
        {
            var pair = new KeyDataValuePair("field", CreateDataValue(1));
            var collection = new KeyDataValuePairCollection([pair]);

            var clone = Assert.IsType<KeyDataValuePairCollection>(collection.MemberwiseClone());

            var clonedPair = Assert.Single(clone);
            Assert.NotSame(pair, clonedPair);
            Assert.True(pair.IsEqual(clonedPair));
        }

        private static DataValue CreateDataValue(object value)
        {
            return new DataValue(new Variant(value));
        }
    }
}
