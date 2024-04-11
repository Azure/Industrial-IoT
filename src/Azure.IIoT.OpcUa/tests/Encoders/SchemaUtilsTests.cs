// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using Azure.IIoT.OpcUa.Encoders.Schemas;

namespace Azure.IIoT.OpcUa.Tests.Encoders
{
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
    }
}
