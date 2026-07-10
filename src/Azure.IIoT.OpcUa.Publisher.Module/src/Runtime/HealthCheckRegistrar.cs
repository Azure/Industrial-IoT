// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Module.Runtime
{
    using Microsoft.Extensions.Diagnostics.HealthChecks;
    using Microsoft.Extensions.Options;
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Aggregates all registered <see cref="IHealthCheck"/> services into
    /// <see cref="HealthCheckServiceOptions"/> so the health check service
    /// evaluates them. Registered as <see cref="IOptions{TOptions}"/>.
    /// </summary>
    public sealed class HealthCheckRegistrar : IOptions<HealthCheckServiceOptions>
    {
        /// <inheritdoc/>
        public HealthCheckServiceOptions Value { get; }

        /// <summary>
        /// Register checks
        /// </summary>
        /// <param name="checks"></param>
        public HealthCheckRegistrar(IEnumerable<IHealthCheck> checks)
        {
            Value = new HealthCheckServiceOptions();
            foreach (var check in checks)
            {
                var name = check.GetType().FullName;
                if (name is null)
                {
                    throw new InvalidOperationException("Type name is null");
                }
                Value.Registrations.Add(new HealthCheckRegistration(
                    name, check, null, null));
            }
        }
    }
}
