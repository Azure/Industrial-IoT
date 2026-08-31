// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Tests.Runtime
{
    using FluentAssertions;
    using System;
    using Xunit;

    public sealed class KubernetesEnvironmentTests
    {
        [Theory]
        [InlineData("10.1.2.3", "6443", null, "https://10.1.2.3:6443")]
        [InlineData("fd00::1", "6443", "443", "https://[fd00::1]:443")]
        public void ServiceEnvironmentSignalsInClusterAndBuildsHost(
            string host, string port, string httpsPort, string expectedHost)
        {
            using var environment = new KubernetesServiceEnvironment(host, port, httpsPort);

            KubernetesEnvironment.IsInCluster().Should().BeTrue();
            KubernetesEnvironment.Host.Should().Be(expectedHost);
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
