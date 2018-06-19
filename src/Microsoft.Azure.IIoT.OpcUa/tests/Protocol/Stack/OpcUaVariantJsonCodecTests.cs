// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Protocol.Stack {
    using Opc.Ua;
    using Xunit;
    using System.Xml;

    public class OpcUaVariantJsonCodecTests {

        [Fact]
        public void DecodeEncodeStringAsUInt32() {
            var codec = new OpcUaJsonVariantCodec();
            var str = "123";
            var variant = codec.Decode(str, BuiltInType.UInt32, null);
            var expected = new Variant(123u);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(str, encoded);
        }

        [Fact]
        public void DecodeEncodeStringAsInt32() {
            var codec = new OpcUaJsonVariantCodec();
            var str = "-1";
            var variant = codec.Decode(str, BuiltInType.Int32, null);
            var expected = new Variant(-1);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(str, encoded);
        }

        [Fact]
        public void DecodeEncodeStringAsSbyte() {
            var codec = new OpcUaJsonVariantCodec();
            var str = "-12";
            var variant = codec.Decode(str, BuiltInType.SByte, null);
            var expected = new Variant((sbyte)-12);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(str, encoded);
        }

        [Fact]
        public void DecodeEncodeStringAsByte() {
            var codec = new OpcUaJsonVariantCodec();
            var str = "1";
            var variant = codec.Decode(str, BuiltInType.Byte, null);
            var expected = new Variant((byte)1);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(str, encoded);
        }

        [Fact]
        public void DecodeEncodeString1() {
            var codec = new OpcUaJsonVariantCodec();
            var str = "\"fffffffff\"";
            var variant = codec.Decode(str, BuiltInType.String, null);
            var expected = new Variant(str.Trim('"'));
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal(str, encoded);
        }

        [Fact]
        public void DecodeEncodeString2() {
            var codec = new OpcUaJsonVariantCodec();
            var str = "fffffffff";
            var variant = codec.Decode(str, BuiltInType.String, null);
            var expected = new Variant(str);
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
            Assert.Equal($"\"{str}\"", encoded);
        }

        [Fact]
        public void DecodeEncodeIntArray1() {
            var codec = new OpcUaJsonVariantCodec();
            var str = "1,2,3,4,5,6";
            var variant = codec.Decode(str, BuiltInType.Int32, ValueRanks.OneDimension);
            var expected = new Variant(new int[] { 1, 2, 3, 4, 5, 6 });
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
        }

        [Fact]
        public void DecodeEncodeIntArray2() {
            var codec = new OpcUaJsonVariantCodec();
            var str = "[1,2,3,4,5,6]";
            var variant = codec.Decode(str, BuiltInType.Int32, null);
            var expected = new Variant(new int[] { 1, 2, 3, 4, 5, 6 });
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
        }

        [Fact]
        public void DecodeEncodeStringArray() {
            var codec = new OpcUaJsonVariantCodec();
            var str = "\"test1\", \"test2\"";
            var variant = codec.Decode(str, BuiltInType.String, ValueRanks.OneDimension);
            var expected = new Variant(new string[] { "test1", "test2" });
            var encoded = codec.Encode(variant);
            Assert.Equal(expected, variant);
        }

        [Fact]
        public void EncodeDecodeXmlElement() {
            var codec = new OpcUaJsonVariantCodec();
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
            var variant = codec.Decode(encoded, BuiltInType.XmlElement, null);
            Assert.Equal(expected, variant);
        }

        [Fact]
        public void EncodeDecodeNodeId() {
            var codec = new OpcUaJsonVariantCodec();

            var expected = new Variant(new NodeId(2354));

            var encoded = codec.Encode(expected);
            var variant = codec.Decode(encoded, BuiltInType.NodeId, null);
            Assert.Equal(expected, variant);
        }

        /// <summary>
        /// test encoding
        /// </summary>
        [Fact]
        public void EncodeDecodeExpandedNodeId() {
            var codec = new OpcUaJsonVariantCodec();

            var expected = new Variant(new ExpandedNodeId(2354, "http://test"));

            var encoded = codec.Encode(expected);
            var variant = codec.Decode(encoded, BuiltInType.ExpandedNodeId, null);
            Assert.Equal(expected, variant);
        }

        /// <summary>
        /// test encoding
        /// </summary>
        [Fact]
        public void EncodeDecodeArgument() {
            var codec = new OpcUaJsonVariantCodec();

            var expected = new Variant(new ExtensionObject {
                Body = new Argument("something1", new NodeId(2354), -1, "somedesciroeioi") {
                    ArrayDimensions = new uint[0]
                },
                TypeId = DataTypeIds.Argument
            });

            var encoded = codec.Encode(expected);
            var variant = codec.Decode(encoded, BuiltInType.ExtensionObject, null);
            Assert.Equal(expected, variant);
        }
    }
}
