// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Tests.Model
{
    using Azure.IIoT.OpcUa.Publisher.Models;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Xunit;

    /// <summary>
    /// Tests for <see cref="MessagingProfile"/> pure static methods and properties.
    /// </summary>
    public sealed class MessagingProfileTests
    {
        // ── Get ────────────────────────────────────────────────────────────────

        [Theory]
        [InlineData(MessagingMode.PubSub, MessageEncoding.Json)]
        [InlineData(MessagingMode.PubSub, MessageEncoding.Uadp)]
        [InlineData(MessagingMode.FullNetworkMessages, MessageEncoding.Json)]
        [InlineData(MessagingMode.DataSetMessages, MessageEncoding.Json)]
        [InlineData(MessagingMode.RawDataSets, MessageEncoding.Json)]
        [InlineData(MessagingMode.SingleDataSetMessage, MessageEncoding.Json)]
        public void Get_SupportedModeAndEncoding_ReturnsProfile(
            MessagingMode mode, MessageEncoding encoding)
        {
            var profile = MessagingProfile.Get(mode, encoding);

            Assert.NotNull(profile);
            Assert.Equal(mode, profile.MessagingMode);
            Assert.Equal(encoding, profile.MessageEncoding);
        }

        [Fact]
        public void Get_UnsupportedCombination_ThrowsKeyNotFoundException()
        {
            // Full network messages + Uadp has been confirmed to exist in static init
            // Let's test a combination that is not registered
            Assert.Throws<KeyNotFoundException>(() =>
                MessagingProfile.Get((MessagingMode)999, MessageEncoding.Json));
        }

        // ── IsSupported ────────────────────────────────────────────────────────

        [Theory]
        [InlineData(MessagingMode.PubSub, MessageEncoding.Json)]
        [InlineData(MessagingMode.PubSub, MessageEncoding.Uadp)]
        [InlineData(MessagingMode.FullNetworkMessages, MessageEncoding.Json)]
        [InlineData(MessagingMode.DataSetMessages, MessageEncoding.Json)]
        [InlineData(MessagingMode.RawDataSets, MessageEncoding.Json)]
        public void IsSupported_KnownCombinations_ReturnsTrue(
            MessagingMode mode, MessageEncoding encoding)
        {
            Assert.True(MessagingProfile.IsSupported(mode, encoding));
        }

        [Fact]
        public void IsSupported_UnknownMode_ReturnsFalse()
        {
            Assert.False(MessagingProfile.IsSupported((MessagingMode)999, MessageEncoding.Json));
        }

        // ── Supported ──────────────────────────────────────────────────────────

        [Fact]
        public void Supported_ReturnsNonEmptyCollection()
        {
            var supported = MessagingProfile.Supported.ToList();
            Assert.NotEmpty(supported);
        }

        [Fact]
        public void Supported_ContainsPubSubJson()
        {
            var supported = MessagingProfile.Supported.ToList();
            Assert.Contains(supported, p =>
                p.MessagingMode == MessagingMode.PubSub &&
                p.MessageEncoding == MessageEncoding.Json);
        }

        // ── SupportsMetadata ───────────────────────────────────────────────────

        [Theory]
        [InlineData(MessagingMode.PubSub)]
        [InlineData(MessagingMode.FullNetworkMessages)]
        [InlineData(MessagingMode.DataSetMessages)]
        [InlineData(MessagingMode.SingleDataSetMessage)]
        public void SupportsMetadata_NonRawDataSets_ReturnsTrue(MessagingMode mode)
        {
            var profile = MessagingProfile.Get(mode, MessageEncoding.Json);
            Assert.True(profile.SupportsMetadata);
        }

        [Fact]
        public void SupportsMetadata_RawDataSets_ReturnsFalse()
        {
            var profile = MessagingProfile.Get(MessagingMode.RawDataSets, MessageEncoding.Json);
            Assert.False(profile.SupportsMetadata);
        }

        // ── SupportsKeyFrames / SupportsKeepAlive ──────────────────────────────

        [Fact]
        public void SupportsKeyFrames_AlwaysReturnsTrue()
        {
            foreach (var profile in MessagingProfile.Supported)
            {
                Assert.True(profile.SupportsKeyFrames);
            }
        }

        [Fact]
        public void SupportsKeepAlive_AlwaysReturnsTrue()
        {
            foreach (var profile in MessagingProfile.Supported)
            {
                Assert.True(profile.SupportsKeepAlive);
            }
        }

        // ── Equals / GetHashCode ───────────────────────────────────────────────

        [Fact]
        public void Equals_SameProfile_ReturnsTrue()
        {
            var profile1 = MessagingProfile.Get(MessagingMode.PubSub, MessageEncoding.Json);
            var profile2 = MessagingProfile.Get(MessagingMode.PubSub, MessageEncoding.Json);

            Assert.Equal(profile1, profile2);
        }

        [Fact]
        public void Equals_DifferentProfiles_ReturnsFalse()
        {
            var profile1 = MessagingProfile.Get(MessagingMode.PubSub, MessageEncoding.Json);
            var profile2 = MessagingProfile.Get(MessagingMode.FullNetworkMessages, MessageEncoding.Json);

            Assert.NotEqual(profile1, profile2);
        }

        [Fact]
        public void Equals_Null_ReturnsFalse()
        {
            var profile = MessagingProfile.Get(MessagingMode.PubSub, MessageEncoding.Json);
            Assert.False(profile.Equals(null));
        }

        [Fact]
        public void GetHashCode_SameProfile_ReturnsSameHash()
        {
            var profile1 = MessagingProfile.Get(MessagingMode.PubSub, MessageEncoding.Json);
            var profile2 = MessagingProfile.Get(MessagingMode.PubSub, MessageEncoding.Json);

            Assert.Equal(profile1.GetHashCode(), profile2.GetHashCode());
        }

        // ── ToString ───────────────────────────────────────────────────────────

        [Fact]
        public void ToString_ContainsModeAndEncoding()
        {
            var profile = MessagingProfile.Get(MessagingMode.PubSub, MessageEncoding.Json);
            var result = profile.ToString();

            Assert.Contains("PubSub", result, StringComparison.Ordinal);
            Assert.Contains("Json", result, StringComparison.Ordinal);
        }

        [Fact]
        public void ToString_Format_IsModePipeEncoding()
        {
            var profile = MessagingProfile.Get(MessagingMode.PubSub, MessageEncoding.Json);
            Assert.Equal("PubSub|Json", profile.ToString());
        }

        // ── ToExpandedString ───────────────────────────────────────────────────

        [Fact]
        public void ToExpandedString_ContainsMode()
        {
            var profile = MessagingProfile.Get(MessagingMode.PubSub, MessageEncoding.Json);
            var result = profile.ToExpandedString();

            Assert.Contains("PubSub", result, StringComparison.Ordinal);
        }

        [Fact]
        public void ToExpandedString_ContainsEncoding()
        {
            var profile = MessagingProfile.Get(MessagingMode.PubSub, MessageEncoding.Json);
            var result = profile.ToExpandedString();

            Assert.Contains("Json", result, StringComparison.Ordinal);
        }

        // ── Find ───────────────────────────────────────────────────────────────

        [Fact]
        public void Find_AllNullFilters_ReturnsFirstProfile()
        {
            var result = MessagingProfile.Find(null, null, null, null);
            Assert.NotNull(result);
        }

        [Fact]
        public void Find_ByMessageEncoding_ReturnsMatchingProfile()
        {
            var result = MessagingProfile.Find(MessageEncoding.Uadp, null, null, null);
            Assert.NotNull(result);
            Assert.Equal(MessageEncoding.Uadp, result!.MessageEncoding);
        }

        [Fact]
        public void Find_UnknownEncoding_ReturnsNull()
        {
            var result = MessagingProfile.Find((MessageEncoding)999, null, null, null);
            Assert.Null(result);
        }

        // ── GetAllAsMarkdownTable ──────────────────────────────────────────────

        [Fact]
        public void GetAllAsMarkdownTable_ReturnsNonEmptyTable()
        {
            var table = MessagingProfile.GetAllAsMarkdownTable();
            Assert.NotEmpty(table);
            Assert.Contains("Messaging Mode", table, StringComparison.Ordinal);
        }
    }
}
