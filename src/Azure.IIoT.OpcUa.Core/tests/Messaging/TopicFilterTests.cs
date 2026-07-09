// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Core.Messaging
{
    using FluentAssertions;
    using Xunit;

    /// <summary>
    /// Locks the MQTT topic escaping / matching behavior relied upon by the
    /// publisher's writer-group topic building.
    /// </summary>
    public sealed class TopicFilterTests
    {
        [Theory]
        [InlineData("plain", "plain")]
        [InlineData("a/b", "a\\x2fb")]
        [InlineData("a+b", "a\\x2bb")]
        [InlineData("a#b", "a\\x23b")]
        [InlineData("a\\b", "a\\x5cb")]
        [InlineData("a/b+c#d", "a\\x2fb\\x2bc\\x23d")]
        public void EscapeReplacesReservedCharacters(string input, string expected)
        {
            TopicFilter.Escape(input).Should().Be(expected);
        }

        [Fact]
        public void EscapeReturnsSameReferenceWhenNothingToEscape()
        {
            const string input = "no-reserved-chars";
            TopicFilter.Escape(input).Should().BeSameAs(input);
        }

        [Theory]
        [InlineData("foo/bar", "foo/bar", true)]
        [InlineData("foo/bar", "foo/+", true)]
        [InlineData("foo/bar", "foo/#", true)]
        [InlineData("foo/bar/baz", "foo/#", true)]
        [InlineData("foo/bar", "foo/baz", false)]
        [InlineData("foo/bar", "bar/#", false)]
        public void MatchesEvaluatesWildcards(string topic, string filter, bool expected)
        {
            TopicFilter.Matches(topic, filter).Should().Be(expected);
        }

        [Theory]
        [InlineData("foo/+/bar", true)]
        [InlineData("foo/#", true)]
        [InlineData("foo/bar", true)]
        [InlineData(null, false)]
        [InlineData("foo/#/bar", false)]
        [InlineData("foo+/bar", false)]
        public void IsValidChecksFilterSyntax(string? filter, bool expected)
        {
            TopicFilter.IsValid(filter).Should().Be(expected);
        }
    }
}
