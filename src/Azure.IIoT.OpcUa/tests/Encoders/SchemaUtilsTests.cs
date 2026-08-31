// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Encoders.Schemas
{
    using Opc.Ua;
    using System;
    using System.Linq;
    using Xunit;

    public class SchemaUtilsTests
    {
        [Theory]
        [InlineData("Test")]
        [InlineData("")]
        [InlineData("Test.Test")]
        [InlineData("Test.Test.Test")]
        [InlineData("%§$%&/()=?")]
        [InlineData("           ")]
        [InlineData("     sd     ")]
        [InlineData("a ac d")]
        [InlineData("a/b/c/d")]
        [InlineData("§$§§\"\"§")]
        [InlineData("黄色) 黄色] 桃子{ 黑色 狗[ 紫色 桃子] 狗 红色 葡萄% 桃子? 猫 猴子 绵羊")]
        [InlineData("蓝色 紫色 蓝色 红色$")]
        [InlineData("_x84_")]
        [InlineData("_x8432")]
        [InlineData("x8$x8")]
        public void TestEscapeUnespace(string value)
        {
            var escaped = SchemaUtils.Escape(value);
            Assert.True(escaped.All(c => c.Equals('_') || char.IsLetterOrDigit(c)));

            var unsescaped = SchemaUtils.Unescape(escaped);
            Assert.Equal(value, unsescaped);
        }

        [Theory]
        [InlineData("https://www.Example.com/UA/Devices", "com.example.UA.Devices")]
        [InlineData("factory/line 1", "factory.line_x32_1")]
        public void NamespaceUriToNamespaceCreatesStableSchemaNamespace(string value,
            string expected)
        {
            var result = SchemaUtils.NamespaceUriToNamespace(value);

            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData(ValueRanks.Scalar, SchemaRank.Scalar)]
        [InlineData(ValueRanks.OneDimension, SchemaRank.Collection)]
        [InlineData(ValueRanks.ScalarOrOneDimension, SchemaRank.Collection)]
        [InlineData(ValueRanks.Any, SchemaRank.Matrix)]
        [InlineData(ValueRanks.TwoDimensions, SchemaRank.Matrix)]
        public void GetRankMapsOpcUaValueRanks(int valueRank, SchemaRank expected)
        {
            var result = SchemaUtils.GetRank(valueRank);

            Assert.Equal(expected, result);
        }

        [Fact]
        public void TryFindNamespaceHandlesNamespaceZero()
        {
            var namespaces = new NamespaceTable();

            var found = namespaces.TryFindNamespace(SchemaUtils.NamespaceZeroName,
                out var index, out var namespaceUri);

            Assert.Equal(true, found);
            Assert.Equal(0u, index);
            Assert.Equal(Namespaces.OpcUa, namespaceUri);
        }

        [Fact]
        public void TryFindNamespaceFindsConvertedNamespaceUri()
        {
            var namespaces = new NamespaceTable();
            namespaces.GetIndexOrAppend(kNamespace);

            var found = namespaces.TryFindNamespace("com.example.UA",
                out var index, out var namespaceUri);

            Assert.Equal(true, found);
            Assert.Equal(1u, index);
            Assert.Equal(kNamespace, namespaceUri);
        }

        [Fact]
        public void TryFindNamespaceReturnsFalseWhenNamespaceIsMissing()
        {
            var namespaces = new NamespaceTable();

            var found = namespaces.TryFindNamespace("com.example.Missing",
                out var index, out var namespaceUri);

            Assert.False(found);
            Assert.Equal(0u, index);
            Assert.Null(namespaceUri);
        }

        [Fact]
        public void SplitNodeIdReturnsNamespaceUriAndIdentifier()
        {
            var context = CreateContext();

            var result = SchemaUtils.SplitNodeId("nsu=" + kNamespace + ";s=Temperature",
                context, escape: false);

            Assert.Equal(kNamespace, result.Namespace);
            Assert.Equal("s=Temperature", result.Id);
        }

        [Fact]
        public void SplitNodeIdEscapesNamespaceZeroNodeId()
        {
            var context = CreateContext();

            var result = SchemaUtils.SplitNodeId("i=2258", context, escape: true);

            Assert.Equal(SchemaUtils.NamespaceZeroName, result.Namespace);
            Assert.Equal("i_x95_2258", result.Id);
        }

        [Fact]
        public void SplitQualifiedNameQualifiesNamesFromDifferentNamespace()
        {
            var context = CreateContext();

            var result = SchemaUtils.SplitQualifiedName("nsu=" + kNamespace + ";Temperature",
                context, outerNamespace: SchemaUtils.NamespaceZeroName);

            Assert.Equal("com.example.UA.Temperature", result);
        }

        [Fact]
        public void SplitQualifiedNameOmitsMatchingOuterNamespace()
        {
            var context = CreateContext();

            var result = SchemaUtils.SplitQualifiedName("nsu=" + kNamespace + ";Temperature",
                context, outerNamespace: "com.example.UA");

            Assert.Equal("Temperature", result);
        }

        [Fact]
        public void GetFullNameReturnsNullWhenTypeIdIsNull()
        {
            ExpandedNodeId? typeId = null;

            var result = typeId.GetFullName("BrowseName", CreateContext());

            Assert.Null(result);
        }

        [Fact]
        public void GetFullNameUsesQualifiedNameNamespaceWhenPresent()
        {
            var context = CreateContext();
            ExpandedNodeId? typeId = new ExpandedNodeId("Type", 0, kNamespace, 0);

            var result = typeId.GetFullName("nsu=" + kNamespace + ";BrowseName", context);

            Assert.Equal("com.example.UA.BrowseName", result);
        }

        private static ServiceMessageContext CreateContext()
        {
            var context = new ServiceMessageContext();
            context.NamespaceUris.GetIndexOrAppend(kNamespace);
            return context;
        }

        private const string kNamespace = "http://example.com/UA";
    }
}
