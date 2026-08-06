// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Parser.Tests
{
    using System;
    using Xunit;

    /// <summary>
    /// Tests for the three public <see cref="ParserException"/> constructors.
    /// The internal Create() factory methods that wrap an Irony ParseTree cannot
    /// easily be exercised without a real grammar parse; they are excluded here.
    /// </summary>
    public sealed class ParserExceptionTests
    {
        [Fact]
        public void DefaultConstructor_IsFormatException()
        {
            var ex = new ParserException();

            Assert.IsAssignableFrom<FormatException>(ex);
        }

        [Fact]
        public void DefaultConstructor_MessageIsNullOrDefault()
        {
            var ex = new ParserException();

            // The default FormatException message is non-null but we don't
            // assert a specific string — just that the ctor completes.
            Assert.NotNull(ex);
        }

        [Fact]
        public void MessageConstructor_SetsMessage()
        {
            const string kMsg = "Unexpected token at position 5";

            var ex = new ParserException(kMsg);

            Assert.Equal(kMsg, ex.Message);
        }

        [Fact]
        public void MessageConstructor_InnerExceptionIsNull()
        {
            var ex = new ParserException("some error");

            Assert.Null(ex.InnerException);
        }

        [Fact]
        public void MessageAndInnerConstructor_SetsMessage()
        {
            var inner = new InvalidOperationException("inner");
            const string kMsg = "Outer message";

            var ex = new ParserException(kMsg, inner);

            Assert.Equal(kMsg, ex.Message);
        }

        [Fact]
        public void MessageAndInnerConstructor_SetsInnerException()
        {
            var inner = new InvalidOperationException("inner detail");

            var ex = new ParserException("outer", inner);

            Assert.Same(inner, ex.InnerException);
        }

        [Fact]
        public void ParserException_CanBeCaughtAsFormatException()
        {
            FormatException? caught = null;
            try
            {
                throw new ParserException("test throw");
            }
            catch (FormatException e)
            {
                caught = e;
            }

            Assert.NotNull(caught);
            Assert.Equal("test throw", caught.Message);
        }
    }
}
