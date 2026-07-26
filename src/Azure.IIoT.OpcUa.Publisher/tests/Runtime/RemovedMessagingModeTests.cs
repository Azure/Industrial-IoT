// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

#nullable enable

namespace Azure.IIoT.OpcUa.Publisher.Tests.Runtime
{
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Microsoft.Extensions.Configuration;
    using System.Collections.Generic;
    using System.Configuration;
    using Xunit;

    /// <summary>
    /// The proprietary sample messaging modes were removed in 3.0. A
    /// configuration naming one must fail with a migration error rather than
    /// silently publishing a different message format.
    /// </summary>
    public sealed class RemovedMessagingModeTests
    {
        [Theory]
        [InlineData("Samples", "PubSub")]
        [InlineData("samples", "PubSub")]
        [InlineData("FullSamples", "FullNetworkMessages")]
        [InlineData("fullsamples", "FullNetworkMessages")]
        public void RemovedMessagingModeIsRejectedWithItsReplacement(string mode,
            string replacement)
        {
            var configuration = Configuration(
                (PublisherConfig.MessagingModeKey, mode));

            var exception = Assert.Throws<ConfigurationErrorsException>(
                () => new PublisherConfig(configuration).ToOptions());

            Assert.Contains(mode, exception.Message, System.StringComparison.Ordinal);
            Assert.Contains(replacement, exception.Message, System.StringComparison.Ordinal);
        }

        [Theory]
        [InlineData("PubSub", MessagingMode.PubSub)]
        [InlineData("FullNetworkMessages", MessagingMode.FullNetworkMessages)]
        [InlineData("DataSetMessages", MessagingMode.DataSetMessages)]
        [InlineData("SingleDataSetMessage", MessagingMode.SingleDataSetMessage)]
        [InlineData("RawDataSets", MessagingMode.RawDataSets)]
        public void SupportedMessagingModesRemainConfigurable(string mode,
            MessagingMode expected)
        {
            var options = new PublisherConfig(Configuration(
                (PublisherConfig.MessagingModeKey, mode))).ToOptions();

            Assert.NotNull(options.Value.MessagingProfile);
            Assert.Equal(expected, options.Value.MessagingProfile!.MessagingMode);
        }

        [Fact]
        public void AbsentMessagingModeDefaultsToPubSub()
        {
            var options = new PublisherConfig(Configuration()).ToOptions();

            Assert.NotNull(options.Value.MessagingProfile);
            Assert.Equal(MessagingMode.PubSub,
                options.Value.MessagingProfile!.MessagingMode);
        }

        private static IConfiguration Configuration(
            params (string Key, string Value)[] values)
        {
            var settings = new Dictionary<string, string?>();
            foreach (var (key, value) in values)
            {
                settings[key] = value;
            }
            return new ConfigurationBuilder()
                .AddInMemoryCollection(settings)
                .Build();
        }
    }
}
