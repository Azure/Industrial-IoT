// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Opc.Ua.Extensions {
    using Opc.Ua;
    using Xunit;

    public class TypeInfoExTests {

        [Fact]
        public void ScalarBoolNullTest1() {
            var variant = TypeInfo.Scalars.Boolean.CreateVariant(null);
            Assert.Equal(false, variant.Value);
        }

        [Fact]
        public void ScalarBoolTest1() {
            var variant = TypeInfo.Scalars.Boolean.CreateVariant(true);
            Assert.Equal(true, variant.Value);
        }

        [Fact]
        public void ScalarByteTest1() {
            var variant = TypeInfo.Scalars.Byte.CreateVariant((byte)5);
            Assert.Equal((byte)5, variant.Value);
        }

        [Fact]
        public void ScalarByteNullTest1() {
            var variant = TypeInfo.Scalars.Byte.CreateVariant(null);
            Assert.Equal((byte)0, variant.Value);
        }

        [Fact]
        public void ScalarStringTest1() {
            var variant = TypeInfo.Scalars.String.CreateVariant("test");
            Assert.Equal("test", variant.Value);
        }

        [Fact]
        public void ScalarExtensionObjectTest1() {
            var variant = TypeInfo.Scalars.ExtensionObject.CreateVariant(new ExtensionObject("test"));
            Assert.Equal(new ExtensionObject("test"), variant.Value);
        }

        [Fact]
        public void ArrayBoolNullTest1() {
            var variant = TypeInfo.Arrays.Boolean.CreateVariant(null);
            Assert.Equal(new bool[0], variant.Value);
        }

        [Fact]
        public void ArrayByteTest1() {
            var variant = TypeInfo.Arrays.Byte.CreateVariant(new byte[] { 1, 2, 3 });
            Assert.Equal(new byte[] { 1, 2, 3 }, variant.Value);
        }

        [Fact]
        public void ArrayStringTest1() {
            var variant = TypeInfo.Arrays.String.CreateVariant(new string[] { "", "", "" });
            Assert.Equal(new string[] { "", "", "" }, variant.Value);
        }

        [Fact]
        public void ArrayStringNullTest1() {
            var variant = TypeInfo.Arrays.String.CreateVariant(null);
            Assert.Equal(new string[0], variant.Value);
        }

        [Fact]
        public void ArrayExtensionObjectTest1() {
            var expected = new[] {
                new ExtensionObject("test1"),
                new ExtensionObject("test2"),
                new ExtensionObject("test3"),
                new ExtensionObject("test4"),
            };
            var variant = TypeInfo.Arrays.ExtensionObject.CreateVariant(expected);
            Assert.Equal(expected, variant.Value);
        }
    }
}
