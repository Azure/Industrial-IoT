// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Module.Tests.Runtime
{
    using Azure.IIoT.OpcUa.Publisher;
    using Azure.IIoT.OpcUa.Publisher.Module.Runtime;
    using FluentAssertions;
    using Microsoft.Extensions.Configuration;
    using System;
    using System.Collections.Generic;
    using Xunit;

    [Collection("EnvironmentVariables")]
    public sealed class KubernetesEnvironmentTests
    {
        [Fact]
        public void ServiceEnvironmentSignalsInClusterAndBuildsHost()
        {
            using var environment = new KubernetesServiceEnvironment(
                host: "10.1.2.3", port: "6443", httpsPort: null);

            KubernetesEnvironment.IsInCluster().Should().BeTrue();
            KubernetesEnvironment.Host.Should().Be("https://10.1.2.3:6443");
        }

        [Fact]
        public void AioConfigurationUsesInClusterServiceEnvironment()
        {
            using var environment = new KubernetesServiceEnvironment(
                host: "10.1.2.3", port: null, httpsPort: "443");
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    [Configuration.Aio.ConnectorId] = "connector"
                })
                .Build();
            var options = new PublisherOptions();

            new Configuration.Aio(configuration).Configure(options);

            options.IsAzureIoTOperationsConnector.Should().BeTrue();
            options.PublisherId.Should().Be("connector");
        }

        private sealed class KubernetesServiceEnvironment : IDisposable
        {
            public KubernetesServiceEnvironment(string host, string port, string httpsPort)
            {
                _host = Environment.GetEnvironmentVariable(
                    KubernetesEnvironment.ServiceHostEnvironmentVariable);
                _port = Environment.GetEnvironmentVariable(
                    KubernetesEnvironment.ServicePortEnvironmentVariable);
                _httpsPort = Environment.GetEnvironmentVariable(
                    KubernetesEnvironment.ServicePortHttpsEnvironmentVariable);
                Environment.SetEnvironmentVariable(
                    KubernetesEnvironment.ServiceHostEnvironmentVariable, host);
                Environment.SetEnvironmentVariable(
                    KubernetesEnvironment.ServicePortEnvironmentVariable, port);
                Environment.SetEnvironmentVariable(
                    KubernetesEnvironment.ServicePortHttpsEnvironmentVariable, httpsPort);
            }

            public void Dispose()
            {
                Environment.SetEnvironmentVariable(
                    KubernetesEnvironment.ServiceHostEnvironmentVariable, _host);
                Environment.SetEnvironmentVariable(
                    KubernetesEnvironment.ServicePortEnvironmentVariable, _port);
                Environment.SetEnvironmentVariable(
                    KubernetesEnvironment.ServicePortHttpsEnvironmentVariable, _httpsPort);
            }

            private readonly string _host;
            private readonly string _port;
            private readonly string _httpsPort;
        }
    }
}
