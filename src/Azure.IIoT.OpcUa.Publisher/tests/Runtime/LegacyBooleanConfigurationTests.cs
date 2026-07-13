// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

#nullable enable

namespace Azure.IIoT.OpcUa.Publisher.Tests.Runtime
{
    using Azure.IIoT.OpcUa.Core.Messaging;
    using Azure.IIoT.OpcUa.Publisher.Stack;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Options;
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
        public void GeneratedBindersAcceptLegacyBooleanAliases(string value, bool expected)
        {
            var subscription = new OpcUaSubscriptionConfig(Configuration(
                [OpcUaSubscriptionConfig.EnableSequentialPublishingKey] = value),
                Options.Create(new PublisherOptions())).ToOptions().Value;
            var client = new OpcUaClientConfig(Configuration(
                [OpcUaClientConfig.EnableOpcUaStackLoggingKey] = value)).ToOptions().Value;
            var nestedClient = new OpcUaClientConfig(Configuration(
                ["Security:AutoAcceptUntrustedCertificates"] = value)).ToOptions().Value;
            var publisher = new PublisherConfig(Configuration(
                [PublisherConfig.EnableCloudEventsKey] = value)).ToOptions().Value;

            Assert.Equal(expected, subscription.EnableSequentialPublishing);
            Assert.Equal(expected, client.EnableOpcUaStackLogging);
            Assert.Equal(expected, nestedClient.Security.AutoAcceptUntrustedCertificates);
            Assert.Equal(expected, publisher.EnableCloudEvents);
        }

        [Fact]
        public void InvalidGeneratedBooleanValueStillFails()
        {
            var configuration = Configuration(
                [OpcUaSubscriptionConfig.EnableSequentialPublishingKey] = "maybe");

            Assert.Throws<InvalidOperationException>(() =>
                new OpcUaSubscriptionConfig(configuration,
                    Options.Create(new PublisherOptions())).ToOptions());
        }

        private static IConfiguration Configuration(
            params KeyValuePair<string, string?>[] values)
        {
            return new ConfigurationBuilder()
                .AddInMemoryCollection(values)
                .Build();
        }
    }
}
