// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace OpcPublisherAEE2ETests.Deploy
{
    using Microsoft.Azure.Devices;
    using System.Threading;
    using System.Threading.Tasks;

    public interface IIoTHubEdgeDeployment
    {
        /// <summary>
        /// Create a new layered deployment or update an existing one.
        /// </summary>
        /// <param name="token">The token to cancel the async task</param>
        /// <param name="replaceExisting">
        /// When false an already existing deployment with the same identifier
        /// is left untouched. Use this for deployments that are shared by
        /// several test jobs - recreating them would restart the modules of
        /// the jobs running in parallel.
        /// </param>
        /// <returns>true if create or update was successful otherwise false</returns>
        Task<bool> CreateOrUpdateLayeredDeploymentAsync(CancellationToken token,
            bool replaceExisting = true);

        /// <summary>
        /// Get deployment configuration.
        /// </summary>
        Configuration GetDeploymentConfiguration();
    }
}
