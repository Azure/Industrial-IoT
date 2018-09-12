// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Module.Deployment {
    using Microsoft.Azure.IIoT.Hub.Models;

    /// <summary>
    /// Creates edge deployments
    /// </summary>
    public interface IEdgeDeploymentFactory {

        /// <summary>
        /// Create deployment for selected iotedge device.
        /// </summary>
        /// <param name="deviceId"></param>
        /// <param name="configuration"></param>
        /// <returns></returns>
        IEdgeDeployment Create(string deviceId,
            ConfigurationContentModel configuration);

        /// <summary>
        /// Create deployment for a fleet of devices identified
        /// by the target condition and given the desired name.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="condition"></param>
        /// <param name="priority"></param>
        /// <param name="configuration"></param>
        /// <returns></returns>
        IEdgeDeployment Create(string name, string condition,
            int priority, ConfigurationContentModel configuration);
    }
}
