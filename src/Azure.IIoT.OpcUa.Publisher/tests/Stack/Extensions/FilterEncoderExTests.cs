// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Stack
{
    using Azure.IIoT.OpcUa.Encoders;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Furly.Extensions.Serializers.Newtonsoft;
    using Opc.Ua;
    using Xunit;

    public sealed class FilterEncoderExTests
    {
        private static IVariantEncoder CreateEncoder()
        {
            return new JsonVariantEncoder(new ServiceMessageContext(),
                new NewtonsoftJsonSerializer());
        }

        [Fact]
        public void StringLiteralOperandShouldDecodeAsStringNotNodeId()
        {
            var encoder = CreateEncoder();
            var model = new FilterOperandModel
            {
                Value = "Error"
            };

            var operand = encoder.Decode(model);

            var literal = Assert.IsType<LiteralOperand>(operand);
            Assert.Equal(BuiltInType.String, literal.Value.TypeInfo.BuiltInType);
            Assert.Equal("Error", literal.Value.Value);
        }

        [Fact]
        public void StringLiteralOperandWithWildcardShouldDecodeAsString()
        {
            var encoder = CreateEncoder();
            var model = new FilterOperandModel
            {
                Value = "Main%"
            };

            var operand = encoder.Decode(model);

            var literal = Assert.IsType<LiteralOperand>(operand);
            Assert.Equal(BuiltInType.String, literal.Value.TypeInfo.BuiltInType);
            Assert.Equal("Main%", literal.Value.Value);
        }

        [Fact]
        public void IntegerLiteralOperandShouldDecodeAsInteger()
        {
            var encoder = CreateEncoder();
            var model = new FilterOperandModel
            {
                Value = 42
            };

            var operand = encoder.Decode(model);

            var literal = Assert.IsType<LiteralOperand>(operand);
            Assert.NotEqual(BuiltInType.NodeId, literal.Value.TypeInfo.BuiltInType);
            Assert.Equal(42L, System.Convert.ToInt64(literal.Value.Value,
                System.Globalization.CultureInfo.InvariantCulture));
        }

        [Fact]
        public void NodeIdLiteralOperandWithDataTypeHintShouldDecodeAsNodeId()
        {
            var encoder = CreateEncoder();
            var model = new FilterOperandModel
            {
                Value = "i=10751",
                DataType = "NodeId"
            };

            var operand = encoder.Decode(model);

            var literal = Assert.IsType<LiteralOperand>(operand);
            Assert.Equal(BuiltInType.NodeId, literal.Value.TypeInfo.BuiltInType);
        }

        [Fact]
        public void NodeIdShapedStringWithoutDataTypeShouldStillDecodeAsNodeId()
        {
            // Backwards-compat: when the value is a string that follows the
            // OPC UA NodeId textual format, it should still be promoted to a
            // NodeId literal even without an explicit DataType hint, so that
            // existing usages (e.g. OfType operator with "ns=2;i=235") keep
            // working.
            var encoder = CreateEncoder();
            var model = new FilterOperandModel
            {
                Value = "ns=2;i=235"
            };

            var operand = encoder.Decode(model);

            var literal = Assert.IsType<LiteralOperand>(operand);
            Assert.Equal(BuiltInType.NodeId, literal.Value.TypeInfo.BuiltInType);
            Assert.Equal(new NodeId("ns=2;i=235"), literal.Value.Value);
        }
    }
}
