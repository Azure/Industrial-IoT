// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Module.Deployment.Models {
    using Docker.DotNet.Models;
    using Newtonsoft.Json;

    /// <summary>
    /// Settings extensions
    /// </summary>
    public static class EdgeAgentModuleSettingsModelEx {

        /// <summary>
        /// Set docker creation options
        /// </summary>
        /// <param name="model"></param>
        /// <param name="createOptions"></param>
        public static void SetCreateOptions(this EdgeAgentModuleSettingsModel model,
            CreateContainerParameters createOptions) {
            model.CreateOptions = JsonConvertEx.SerializeObject(createOptions);
        }

        /// <summary>
        /// Create with creation options
        /// </summary>
        /// <param name="registry"></param>
        /// <param name="imageName"></param>
        /// <param name="version"></param>
        /// <param name="createOptions"></param>
        /// <returns></returns>
        public static EdgeAgentModuleSettingsModel Create(string registry, string imageName,
            string version, CreateContainerParameters createOptions) {
            return Create($"{registry}/{imageName}:{version ?? "latest"}", createOptions);
        }

        /// <summary>
        /// Create with creation options
        /// </summary>
        /// <param name="imageName"></param>
        /// <param name="version"></param>
        /// <param name="createOptions"></param>
        /// <returns></returns>
        public static EdgeAgentModuleSettingsModel Create(string imageName,
            string version, CreateContainerParameters createOptions) {
            return Create($"{imageName}:{version ?? "latest"}", createOptions);
        }

        /// <summary>
        /// Create with creation options
        /// </summary>
        /// <param name="imageName"></param>
        /// <param name="createOptions"></param>
        /// <returns></returns>
        public static EdgeAgentModuleSettingsModel Create(string imageName,
            CreateContainerParameters createOptions = null) {
            return new EdgeAgentModuleSettingsModel {
                Image = imageName,
                CreateOptions = createOptions == null ? "" :
                    JsonConvertEx.SerializeObject(createOptions)
            };
        }
    }
}
