// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

#nullable enable

namespace Azure.IIoT.OpcUa.Core.AzureSdk
{
    using Microsoft.Extensions.Configuration;
    using System;
    using System.Collections.Generic;
    using Xunit;

    public sealed class LegacyBooleanConfigurationTests
    {
        [Theory]
        [InlineData("TRUE", true)]
        [InlineData("YES", true)]
        [InlineData("Y", true)]
        [InlineData("1", true)]
        [InlineData("FALSE", false)]
        [InlineData("NO", false)]
        [InlineData("N", false)]
        [InlineData("0", false)]
        public void CredentialBinderAcceptsLegacyBooleanAliases(string value, bool expected)
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    [nameof(CredentialOptions.AllowInteractiveLogin)] = value
                })
                .Build();

            var options = new CredentialConfig(configuration).ToOptions().Value;

            Assert.Equal(expected, options.AllowInteractiveLogin);
        }

        [Fact]
        public void CredentialBinderRejectsInvalidBooleanValue()
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    [nameof(CredentialOptions.AllowInteractiveLogin)] = "maybe"
                })
                .Build();

            Assert.Throws<InvalidOperationException>(() =>
                new CredentialConfig(configuration).ToOptions());
        }
    }
}
