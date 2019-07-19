// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Module.Deployment {
    using Microsoft.Azure.IIoT.Module.Deployment.Models;
    using System.Threading.Tasks;

    /// <summary>
    /// IotEdge deployment
    /// </summary>
    public interface IEdgeDeployment {

        /// <summary>
        /// Add auser module with desired properties
        /// </summary>
        /// <param name="module"></param>
        IEdgeDeployment WithModule(EdgeDeploymentModuleModel module);

        /// <summary>
        /// Add a special route different from all to upstream.
        /// </summary>
        /// <param name="route"></param>
        IEdgeDeployment WithRoute(EdgeDeploymentRouteModel route);

        /// <summary>
        /// Apply deployment
        /// </summary>
        /// <returns></returns>
        Task ApplyAsync();
    }
}
