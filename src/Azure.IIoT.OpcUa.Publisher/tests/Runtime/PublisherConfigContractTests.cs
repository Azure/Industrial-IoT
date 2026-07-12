// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Tests.Runtime
{
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Azure.IIoT.OpcUa.Core.Messaging;
    using Microsoft.Extensions.Configuration;
    using System.Collections.Generic;
    using Xunit;

    /// <summary>
    /// Characterizes defaults consumed by published-nodes conversion and transports.
    /// </summary>
    [Trait("Compatibility", "Authoritative")]
    public sealed class PublisherConfigContractTests
    {
        [Fact]
        public void StrictConfigurationSelectsPubSubDefaults()
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    [PublisherConfig.PublisherIdKey] = "publisher",
                    [PublisherConfig.UseStandardsCompliantEncodingKey] = "true"
                })
                .Build();

            var options = new PublisherConfig(configuration).ToOptions().Value;

            Assert.Equal("publisher", options.PublisherId);
            Assert.True(options.UseStandardsCompliantEncoding);
            Assert.Equal(MessagingMode.PubSub, options.MessagingProfile!.MessagingMode);
            Assert.Equal(MessageEncoding.Json, options.MessagingProfile.MessageEncoding);
            Assert.Equal(0, options.BatchSize);
            Assert.Equal(System.TimeSpan.Zero, options.BatchTriggerInterval);
            Assert.Equal(PublisherConfig.MaxNodesPerDataSetDefault, options.MaxNodesPerDataSet);
            Assert.False(options.DisableDataSetMetaData);
        }

    }
}
