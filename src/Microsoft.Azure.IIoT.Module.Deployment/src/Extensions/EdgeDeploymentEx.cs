// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Module.Deployment {
    using Microsoft.Azure.IIoT.Module.Deployment.Models;
    using Docker.DotNet.Models;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Newtonsoft.Json;

    /// <summary>
    /// Edge deployment extensions
    /// </summary>
    public static class EdgeDeploymentEx {

        /// <summary>
        /// Add image
        /// </summary>
        /// <param name="deployment"></param>
        /// <param name="name"></param>
        /// <param name="imageName"></param>
        /// <param name="version"></param>
        /// <param name="createOptions"></param>
        /// <param name="restart"></param>
        /// <param name="stopped"></param>
        /// <param name="properties"></param>
        /// <returns></returns>
        public static IEdgeDeployment WithModule(this IEdgeDeployment deployment,
            string name, string imageName, System.Version version,
            CreateContainerParameters createOptions, ModuleRestartPolicy restart,
            bool stopped, Dictionary<string, dynamic> properties) {
            return deployment.WithModule(new EdgeDeploymentModuleModel {
                CreateOptions = createOptions,
                ImageName = imageName,
                Name = name,
                Properties = properties,
                RestartPolicy = restart != ModuleRestartPolicy.Always ?
restart : (ModuleRestartPolicy?)null,
                Stopped = stopped ? true : (bool?)null,
                Version = version?.ToString()
            });
        }

        /// <summary>
        /// Add image
        /// </summary>
        /// <param name="deployment"></param>
        /// <param name="name"></param>
        /// <param name="imageName"></param>
        /// <param name="version"></param>
        /// <param name="createOptions"></param>
        /// <param name="restart"></param>
        /// <param name="properties"></param>
        /// <returns></returns>
        public static IEdgeDeployment WithModule(this IEdgeDeployment deployment,
            string name, string imageName, System.Version version,
            CreateContainerParameters createOptions, ModuleRestartPolicy restart,
            Dictionary<string, dynamic> properties) {
            return deployment.WithModule(name, imageName, version, createOptions,
restart, false, properties);
        }

        /// <summary>
        /// Add image
        /// </summary>
        /// <param name="deployment"></param>
        /// <param name="name"></param>
        /// <param name="imageName"></param>
        /// <param name="version"></param>
        /// <param name="createOptions"></param>
        /// <param name="properties"></param>
        /// <returns></returns>
        public static IEdgeDeployment WithModule(this IEdgeDeployment deployment,
            string name, string imageName, System.Version version,
            CreateContainerParameters createOptions,
            Dictionary<string, dynamic> properties) {
            return deployment.WithModule(name, imageName, version, createOptions,
ModuleRestartPolicy.Always, properties);
        }

        /// <summary>
        /// Add image
        /// </summary>
        /// <param name="deployment"></param>
        /// <param name="name"></param>
        /// <param name="imageName"></param>
        /// <param name="version"></param>
        /// <param name="properties"></param>
        public static IEdgeDeployment WithModule(this IEdgeDeployment deployment,
            string name, string imageName, System.Version version,
            Dictionary<string, dynamic> properties) {
            return deployment.WithModule(name, imageName, version, null, properties);
        }

        /// <summary>
        /// Add image
        /// </summary>
        /// <param name="deployment"></param>
        /// <param name="name"></param>
        /// <param name="imageName"></param>
        /// <param name="properties"></param>
        public static IEdgeDeployment WithModule(this IEdgeDeployment deployment,
            string name, string imageName, Dictionary<string, dynamic> properties) {
            return deployment.WithModule(name, imageName, null, null, properties);
        }

        /// <summary>
        /// Add image
        /// </summary>
        /// <param name="deployment"></param>
        /// <param name="name"></param>
        /// <param name="imageName"></param>
        public static IEdgeDeployment WithModule(this IEdgeDeployment deployment,
            string name, string imageName) {
            return deployment.WithModule(name, imageName, null, null, null);
        }

        /// <summary>
        /// Add image
        /// </summary>
        /// <param name="deployment"></param>
        /// <param name="name"></param>
        /// <param name="imageName"></param>
        /// <param name="createOptions"></param>
        /// <param name="properties"></param>
        public static IEdgeDeployment WithModule(this IEdgeDeployment deployment,
            string name, string imageName, CreateContainerParameters createOptions,
            Dictionary<string, dynamic> properties) {
            return deployment.WithModule(name, imageName, null, createOptions, properties);
        }

        /// <summary>
        /// Add image
        /// </summary>
        /// <param name="deployment"></param>
        /// <param name="name"></param>
        /// <param name="imageName"></param>
        /// <param name="createOptions"></param>
        public static IEdgeDeployment WithModule(this IEdgeDeployment deployment,
            string name, string imageName, CreateContainerParameters createOptions) {
            return deployment.WithModule(name, imageName, null, createOptions, null);
        }

        /// <summary>
        /// Add a route
        /// </summary>
        /// <param name="deployment"></param>
        /// <param name="name"></param>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <param name="condition"></param>
        /// <returns></returns>
        public static IEdgeDeployment WithRoute(this IEdgeDeployment deployment,
            string name, string from, string to, string condition) {
            return deployment.WithRoute(new EdgeDeploymentRouteModel {
                Name = name,
                From = from,
                To = to,
                Condition = condition
            });
        }

        /// <summary>
        /// Add an unconditional route
        /// </summary>
        /// <param name="deployment"></param>
        /// <param name="name"></param>
        /// <param name="from"></param>
        /// <param name="to"></param>
        public static IEdgeDeployment WithRoute(this IEdgeDeployment deployment,
            string name, string from, string to) {
            return deployment.WithRoute(name, from, to, null);
        }

        /// <summary>
        /// With deployment manifest
        /// </summary>
        /// <param name="deployment"></param>
        /// <param name="manifest"></param>
        /// <returns></returns>
        public static IEdgeDeployment WithManifest(this IEdgeDeployment deployment,
            EdgeDeploymentManifestModel manifest) {
            if (manifest.Modules != null) {
                foreach (var module in manifest.Modules) {
                    deployment.WithModule(module);
                }
            }
            if (manifest.Routes != null) {
                foreach (var route in manifest.Routes) {
                    deployment.WithRoute(route);
                }
            }
            return deployment;
        }

        /// <summary>
        /// Apply manifest from stream
        /// </summary>
        /// <param name="deployment"></param>
        /// <param name="manifestJson"></param>
        /// <returns></returns>
        public static IEdgeDeployment WithManifest(this IEdgeDeployment deployment,
            string manifestJson) {
            return deployment.WithManifest(
JsonConvertEx.DeserializeObject<EdgeDeploymentManifestModel>(manifestJson));
        }

        /// <summary>
        /// Apply manifest
        /// </summary>
        /// <param name="deployment"></param>
        /// <param name="manifest"></param>
        /// <returns></returns>
        public static Task ApplyAsync(this IEdgeDeployment deployment,
            EdgeDeploymentManifestModel manifest) {
            return deployment.WithManifest(manifest).ApplyAsync();
        }

        /// <summary>
        /// Apply manifest
        /// </summary>
        /// <param name="deployment"></param>
        /// <param name="manifestJson"></param>
        /// <returns></returns>
        public static Task ApplyAsync(this IEdgeDeployment deployment,
            string manifestJson) {
            return deployment.WithManifest(manifestJson).ApplyAsync();
        }
    }
}
