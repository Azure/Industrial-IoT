// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Http.HealthChecks
{
    /// <summary>
    /// Health checks interface.
    /// </summary>
    public interface IHealthCheckManager
    {
        /// <summary>
        /// Live status for liveness probe.
        /// </summary>
        public bool IsLive { get; set; }

        /// <summary>
        /// Ready status for readiness probe.
        /// </summary>
        public bool IsReady { get; set; }

        /// <summary>
        /// Start health checks.
        /// </summary>
        /// <param name="port">Port for where to run health checks.</param>
        public void Start(uint port = 8080);

        /// <summary>
        /// Stop health checks.
        /// </summary>
        public void Stop();
    }
}
