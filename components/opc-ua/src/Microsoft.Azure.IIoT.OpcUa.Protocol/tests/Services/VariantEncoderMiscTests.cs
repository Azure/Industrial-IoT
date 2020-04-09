// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Protocol.Services {
    using Opc.Ua;
    using Xunit;
    using System.Xml;
    using Opc.Ua.Extensions;

    public class VariantEncoderMiscTests {

        [Fact]
        public void DecodeEncodeStringAsUInt32() {
            var codec = new VariantEncoderFactory().Default;
            var str = "123";
            var context = new ServiceMessageContext();
            var variant = codec.Decode(str, BuiltInType.UInt32);
            var expected = new Variant(123u);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(str, encoded);
        }

        [Fact]
        public void DecodeEncodeStringAsInt32() {
            var codec = new VariantEncoderFactory().Default;
            var str = "-1";
            var variant = codec.Decode(str, BuiltInType.Int32);
            var expected = new Variant(-1);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(str, encoded);
        }

        [Fact]
        public void DecodeEncodeStringAsSbyte() {
            var codec = new VariantEncoderFactory().Default;
            var str = "-12";
            var variant = codec.Decode(str, BuiltInType.SByte);
            var expected = new Variant((sbyte)-12);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(str, encoded);
        }

        [Fact]
        public void DecodeEncodeStringAsByte() {
            var codec = new VariantEncoderFactory().Default;
            var str = "1";
            var variant = codec.Decode(str, BuiltInType.Byte);
            var expected = new Variant((byte)1);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(str, encoded);
        }

        [Fact]
        public void DecodeEncodeString1() {
            var codec = new VariantEncoderFactory().Default;
            var str = "\\\"fffffffff\\\"";
            var variant = codec.Decode(str, BuiltInType.String);
            var expected = new Variant(str);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(str, encoded);
        }

        [Fact]
        public void DecodeEncodeString2() {
            var codec = new VariantEncoderFactory().Default;
            var str = "fffffffff";
            var variant = codec.Decode(str, BuiltInType.String);
            var expected = new Variant(str);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(str, encoded);
        }

        [Fact]
        public void DecodeEncodeString3() {
            var codec = new VariantEncoderFactory().Default;
            var str = "\"fffffffff\"";
            var variant = codec.Decode(str, BuiltInType.String);
            var expected = new Variant("fffffffff");
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal("fffffffff", encoded);
        }

        [Fact]
        public void DecodeEncodeIntArray1() {
            var codec = new VariantEncoderFactory().Default;
            var str = "1,2,3,4,5,6";
            var variant = codec.Decode(str, BuiltInType.Int32);
            var expected = new Variant(new int[] { 1, 2, 3, 4, 5, 6 });
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
        }

        [Fact]
        public void DecodeEncodeIntArray2() {
            var codec = new VariantEncoderFactory().Default;
            var str = "[1,2,3,4,5,6]";
            var variant = codec.Decode(str, BuiltInType.Int32);
            var expected = new Variant(new int[] { 1, 2, 3, 4, 5, 6 });
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
        }

        [Fact]
        public void DecodeEncodeStringArray() {
            var codec = new VariantEncoderFactory().Default;
            var str = "\"test1\", \"test2\"";
            var variant = codec.Decode(str, BuiltInType.String);
            var expected = new Variant(new string[] { "test1", "test2" });
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
        }

        [Fact]
        public void DecodeEmptyStringArray() {
            var codec = new VariantEncoderFactory().Default;
            var str = "[]";
            var variant = codec.Decode(str, BuiltInType.String);
            var expected = new Variant(new string[0]);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.True(encoded.Equals(str));
            Assert.Equal(str, encoded);
        }

        [Fact]
        public void DecodeEmptyShortArray() {
            var codec = new VariantEncoderFactory().Default;
            var str = "[]";
            var variant = codec.Decode(str, BuiltInType.Int16);
            var expected = new Variant(new short[0]);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.True(encoded.Equals(str));
            Assert.Equal(str, encoded);
        }

        [Fact]
        public void EncodeDecodeXmlElement() {
            var codec = new VariantEncoderFactory().Default;
            var doc = new XmlDocument();
            doc.LoadXml(
          @"<?xml version=""1.0"" encoding=""UTF-8""?>
            <note>
                <to>Tove</to>
                <from>Jani</from>
                <heading test=""1.0"">Reminder</heading>
                <author><nothing/></author>
                <body>Don't forget me this weekend!</body>
            </note>"
            );
            var expected = new Variant(doc.DocumentElement);
            var encoded = codec.Encode(expected);
            var variant = codec.Decode(encoded, BuiltInType.XmlElement);
            Assert.Equal(expected, variant);
        }

        [Fact]
        public void EncodeDecodeLocalizedText() {
            var codec = new VariantEncoderFactory().Default;
            var expected = new Variant(new LocalizedText("en-US", "text"));
            var encoded = codec.Encode(expected);
            var variant = codec.Decode(encoded, BuiltInType.LocalizedText);
            Assert.Equal(expected, variant);
        }

        [Fact]
        public void EncodeDecodeLocalizedTextFromString1() {
            var codec = new VariantEncoderFactory().Default;
            var str = "text@en-US";
            var expected = new Variant(new LocalizedText("en-US", "text"));
            var variant = codec.Decode(str, BuiltInType.LocalizedText);
            var encoded = codec.Encode(expected);
            Assert.Equal(expected, variant);
        }

        [Fact]
        public void EncodeDecodeLocalizedTextFromString2() {
            var codec = new VariantEncoderFactory().Default;
            var str = "text";
            var expected = new Variant(new LocalizedText("text"));
            var variant = codec.Decode(str, BuiltInType.LocalizedText);
            var encoded = codec.Encode(expected);
            Assert.Equal(expected, variant);
        }

        [Fact]
        public void EncodeDecodeNodeId() {
            var codec = new VariantEncoderFactory().Default;

            var expected = new Variant(new NodeId(2354));

            var encoded = codec.Encode(expected);
            var variant = codec.Decode(encoded, BuiltInType.NodeId);
            Assert.Equal(expected, variant);
        }

        [Fact]
        public void EncodeDecodeExpandedNodeId1() {
            var codec = new VariantEncoderFactory().Default;

            var expected = new Variant(new ExpandedNodeId(2354u, 0, "http://test.org/test", 0));

            var encoded = codec.Encode(expected);
            var variant = codec.Decode(encoded, BuiltInType.ExpandedNodeId);
            Assert.Equal(expected, variant);
        }

        [Fact]
        public void EncodeDecodeExpandedNodeId2() {
            var codec = new VariantEncoderFactory().Default;

            var expected = new Variant(new ExpandedNodeId(2354u, 0, "http://test/", 0));

            var encoded = codec.Encode(expected);
            var variant = codec.Decode(encoded, BuiltInType.ExpandedNodeId);
            Assert.Equal(expected, variant);
        }

        [Fact]
        public void EncodeDecodeExpandedNodeId3() {
            var codec = new VariantEncoderFactory().Default;

            var expected1 = new Variant(new ExpandedNodeId(2354u, 0, "http://test/", 0));
            var expected2 = new Variant(new ExpandedNodeId(2354u, 0, "http://test/UA", 0));
            var expected3 = new Variant(new ExpandedNodeId(2355u, 0, "http://test/", 0));
            var expected4 = new Variant(new ExpandedNodeId(2355u, 0, null, 0));
            var expected5 = new Variant(new ExpandedNodeId(new NodeId(2355u, 1), "http://test/", 0));

            var encoded1 = codec.Encode(expected1);
            var encoded2 = codec.Encode(expected2);
            var encoded3 = codec.Encode(expected3);
            var encoded4 = codec.Encode(expected4);
            var encoded5 = codec.Encode(expected5);

            var variant1 = codec.Decode(encoded1, BuiltInType.ExpandedNodeId);
            var variant2 = codec.Decode(encoded2, BuiltInType.ExpandedNodeId);
            var variant3 = codec.Decode(encoded3, BuiltInType.ExpandedNodeId);
            var variant4 = codec.Decode(encoded4, BuiltInType.ExpandedNodeId);
            var variant5 = codec.Decode(encoded5, BuiltInType.ExpandedNodeId);

            Assert.Equal(expected1, variant1);
            Assert.Equal(expected2, variant2);
            Assert.Equal(expected3, variant3);
            Assert.Equal(expected4, variant4);
            Assert.Equal(expected5, variant5);
        }

        [Fact]
        public void EncodeDecodeArgument1() {
            var codec = new VariantEncoderFactory().Default;

            var expected = new Variant(new ExtensionObject {
                Body = new Argument("something1", new NodeId(2354), -1, "somedesciroeioi") {
                    ArrayDimensions = new uint[0]
                },
                TypeId = DataTypeIds.Argument
            });

            var encoded = codec.Encode(expected);
            var variant = codec.Decode(encoded, BuiltInType.ExtensionObject);
            var obj = variant.Value as ExtensionObject;

            Assert.NotNull(obj);
            Assert.Equal(ExtensionObjectEncoding.EncodeableObject, obj.Encoding);
            Assert.True(obj.Body is Argument);
            Assert.Equal(expected, variant);
        }

        [Fact]
        public void EncodeDecodeArgument2() {
            var codec = new VariantEncoderFactory().Default;

            var expected = new Variant(new ExtensionObject {
                Body = new Argument("something2", new NodeId(2334), -1, "asdfsadfffd") {
                    ArrayDimensions = new uint[0]
                }.AsXmlElement(ServiceMessageContext.GlobalContext),
                TypeId = "http://test.org/i=444444"
            });

            var encoded = codec.Encode(expected);
            var variant = codec.Decode(encoded, BuiltInType.ExtensionObject);
            var obj = variant.Value as ExtensionObject;

            Assert.NotNull(obj);
            Assert.Equal(ExtensionObjectEncoding.Xml, obj.Encoding);
            Assert.True(obj.Body is XmlElement);
            Assert.Equal(expected, variant);
        }

        [Fact]
        public void EncodeDecodeArgument3() {
            var codec = new VariantEncoderFactory().Default;

            var expected = new Variant(new ExtensionObject {
                Body = new Argument("something3", new NodeId(2364), -1, "dd f s fdd fd") {
                    ArrayDimensions = new uint[0]
                }.AsBinary(ServiceMessageContext.GlobalContext),
                TypeId = "http://test.org/i=444445"
            });

            var encoded = codec.Encode(expected);
            var variant = codec.Decode(encoded, BuiltInType.ExtensionObject);
            var obj = variant.Value as ExtensionObject;

            Assert.NotNull(obj);
            Assert.Equal(ExtensionObjectEncoding.Binary, obj.Encoding);
            Assert.True(obj.Body is byte[]);
            Assert.Equal(expected, variant);
        }
    }
}
