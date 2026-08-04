// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Module.Tests.Runtime
{
    using Azure.IIoT.OpcUa.Publisher.Module.Runtime;
    using Microsoft.Extensions.Diagnostics.HealthChecks;
    using System.Threading;
    using System.Threading.Tasks;
    using Xunit;

    public sealed class HealthCheckRegistrarTests
    {
        [Fact]
        public void ConstructorRegistersEachHealthCheckByFullTypeName()
        {
            var first = new ReadyCheck();
            var second = new HealthyCheck();

            var registrar = new HealthCheckRegistrar([first, second]);

            Assert.Collection(registrar.Value.Registrations,
                registration =>
                {
                    Assert.Equal(typeof(ReadyCheck).FullName, registration.Name);
                    Assert.Same(first, registration.Factory(null!));
                    Assert.Equal(HealthStatus.Unhealthy, registration.FailureStatus);
                    Assert.Empty(registration.Tags);
                },
                registration =>
                {
                    Assert.Equal(typeof(HealthyCheck).FullName, registration.Name);
                    Assert.Same(second, registration.Factory(null!));
                    Assert.Equal(HealthStatus.Unhealthy, registration.FailureStatus);
                    Assert.Empty(registration.Tags);
                });
        }

        [Fact]
        public void ConstructorAllowsEmptyHealthCheckCollection()
        {
            var registrar = new HealthCheckRegistrar([]);

            Assert.Empty(registrar.Value.Registrations);
        }

        private sealed class ReadyCheck : IHealthCheck
        {
            public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context,
                CancellationToken cancellationToken = default)
            {
                return Task.FromResult(HealthCheckResult.Healthy());
            }
        }

        private sealed class HealthyCheck : IHealthCheck
        {
            public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context,
                CancellationToken cancellationToken = default)
            {
                return Task.FromResult(HealthCheckResult.Healthy());
            }
        }
    }
}
