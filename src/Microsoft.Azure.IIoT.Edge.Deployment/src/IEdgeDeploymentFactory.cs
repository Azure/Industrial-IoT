// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Edge.Deployment {
    using Microsoft.Azure.IIoT.Hub.Models;

    public interface IEdgeDeploymentFactory {

        /// <summary>
        /// Create edge deployment for selected edge device.
        /// </summary>
        /// <param name="deviceId"></param>
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
        /// <returns></returns>
        IEdgeDeployment Create(string name, string condition,
            int priority, ConfigurationContentModel configuration);
    }
}
