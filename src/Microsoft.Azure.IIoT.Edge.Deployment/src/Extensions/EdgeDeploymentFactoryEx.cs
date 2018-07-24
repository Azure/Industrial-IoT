// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Edge.Deployment {
    using Microsoft.Azure.IIoT.Hub.Models;
    using Newtonsoft.Json;
    using System.Threading.Tasks;

    public static class EdgeDeploymentFactoryEx {

        /// <summary>
        /// Create deployment for a fleet of devices identified by
        /// the target condition and given the desired name.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="condition"></param>
        /// <returns></returns>
        public static IEdgeDeployment Create(this IEdgeDeploymentFactory factory,
            string name, string condition, int priority = 0) =>
            factory.Create(name, condition, priority, null);

        /// <summary>
        /// Create deployment for a fleet of devices identified by
        /// the target condition and given the desired name.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="condition"></param>
        /// <param name="priority"></param>
        /// <param name="deploymentJson"></param>
        /// <returns></returns>
        public static IEdgeDeployment CreateFromDeploymentJson(
            this IEdgeDeploymentFactory factory, string name, string condition,
            int priority, string deploymentJson) => factory.Create(name, condition, priority,
                JsonConvertEx.DeserializeObject<ConfigurationContentModel>(deploymentJson));

        /// <summary>
        /// Create deployment for a fleet of devices identified by
        /// the target condition and given the desired name.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="condition"></param>
        /// <param name="deploymentJson"></param>
        /// <returns></returns>
        public static IEdgeDeployment CreateFromDeploymentJson(
            this IEdgeDeploymentFactory factory, string name, string condition,
            string deploymentJson) => factory.Create(name, condition, 0,
                JsonConvertEx.DeserializeObject<ConfigurationContentModel>(deploymentJson));

        /// <summary>
        /// Create deployment for a single device.
        /// </summary>
        /// <param name="factory"></param>
        /// <param name="deviceId"></param>
        /// <returns></returns>
        public static IEdgeDeployment Create(this IEdgeDeploymentFactory factory,
            string deviceId) => factory.Create(deviceId, null);

        /// <summary>
        /// Create deployment for a single device.
        /// </summary>
        /// <param name="factory"></param>
        /// <param name="deviceId"></param>
        /// <param name="deploymentJson"></param>
        /// <returns></returns>
        public static IEdgeDeployment CreateFromDeploymentJson(
            this IEdgeDeploymentFactory factory, string deviceId, string deploymentJson) =>
            factory.Create(deviceId, JsonConvertEx.DeserializeObject<ConfigurationContentModel>(
                deploymentJson));

        /// <summary>
        /// Create and apply deployment to single device
        /// </summary>
        /// <param name="manifestJson"></param>
        /// <param name="deviceId"></param>
        /// <returns></returns>
        public static Task DeployManifestAsync(this IEdgeDeploymentFactory factory,
            string deviceId, string manifestJson) =>
            factory.Create(deviceId).WithManifest(manifestJson).ApplyAsync();

        /// <summary>
        /// Deploy to single device
        /// </summary>
        /// <param name="deploymentJson"></param>
        /// <param name="deviceId"></param>
        /// <returns></returns>
        public static Task DeployAsync(this IEdgeDeploymentFactory factory,
            string deviceId, string deploymentJson) =>
            factory.CreateFromDeploymentJson(deviceId, deploymentJson).ApplyAsync();

        /// <summary>
        /// Deploy to fleet of devices
        /// </summary>
        /// <param name="manifestJson"></param>
        /// <param name="name"></param>
        /// <param name="condition"></param>
        /// <param name="priority"></param>
        /// <returns></returns>
        public static Task DeployManifestAsync(this IEdgeDeploymentFactory factory,
            string name, string condition, int priority, string manifestJson) =>
            factory.Create(name, condition, priority).WithManifest(manifestJson)
                .ApplyAsync();

        /// <summary>
        /// Deploy to fleet of devices
        /// </summary>
        /// <param name="deploymentJson"></param>
        /// <param name="name"></param>
        /// <param name="condition"></param>
        /// <param name="priority"></param>
        /// <returns></returns>
        public static Task DeployAsync(this IEdgeDeploymentFactory factory,
            string name, string condition, int priority, string deploymentJson) =>
            factory.CreateFromDeploymentJson(name, condition, priority, deploymentJson)
                .ApplyAsync();
    }
}
