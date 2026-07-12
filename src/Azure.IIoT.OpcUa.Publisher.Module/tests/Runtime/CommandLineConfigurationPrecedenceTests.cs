// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Module.Tests.Runtime
{
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Azure.IIoT.OpcUa.Publisher.Module.Runtime;
    using FluentAssertions;
    using Microsoft.Extensions.Configuration;
    using System.Collections.Generic;
    using Xunit;

    public sealed class CommandLineConfigurationPrecedenceTests
    {
        [Fact]
        public void CommandLineValuesOverrideEarlierConfiguration()
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    [PublisherConfig.PublisherIdKey] = "environment",
                    [PublisherConfig.UseStandardsCompliantEncodingKey] = "false"
                })
                .AddInMemoryCollection(new CommandLine(["--id=command-line", "--strict"]))
                .Build();

            var options = new PublisherConfig(configuration).ToOptions().Value;

            options.PublisherId.Should().Be("command-line");
            options.UseStandardsCompliantEncoding.Should().BeTrue();
        }
    }
}
