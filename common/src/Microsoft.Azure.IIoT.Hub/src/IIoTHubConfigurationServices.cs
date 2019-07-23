// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Hub {
    using Microsoft.Azure.IIoT.Hub.Models;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Configuration Management services
    /// </summary>
    public interface IIoTHubConfigurationServices {

        /// <summary>
        /// Apply configuration to a single device. Used for
        /// mostly adhoc deployments.
        /// </summary>
        /// <param name="deviceId"></param>
        /// <param name="configuration"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task ApplyConfigurationAsync(string deviceId,
            ConfigurationContentModel configuration,
            CancellationToken ct = default);

        /// <summary>
        /// Create new configuration for a fleet of devices or
        /// update an existing one.
        /// </summary>
        /// <param name="configuration"></param>
        /// <param name="forceUpdate"></param>
        /// <param name="ct"></param>
        /// <returns>new device</returns>
        Task<ConfigurationModel> CreateOrUpdateConfigurationAsync(
            ConfigurationModel configuration, bool forceUpdate,
            CancellationToken ct = default);

        /// <summary>
        /// Returns a single fleet configuration
        /// </summary>
        /// <param name="configurationId"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<ConfigurationModel> GetConfigurationAsync(
            string configurationId, CancellationToken ct = default);

        /// <summary>
        /// Returns fleet configurations
        /// </summary>
        /// <param name="maxCount"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<IEnumerable<ConfigurationModel>> ListConfigurationsAsync(
            int? maxCount, CancellationToken ct = default);

        /// <summary>
        /// Delete fleet configuration
        /// </summary>
        /// <param name="configurationId"></param>
        /// <param name="etag"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task DeleteConfigurationAsync(string configurationId,
            string etag, CancellationToken ct = default);
    }
}
