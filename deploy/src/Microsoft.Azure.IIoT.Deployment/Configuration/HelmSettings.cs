// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Deployment.Configuration {

    /// <summary>
    /// Represents Helm chart settings that are configurable.
    /// </summary>
    class HelmSettings {

        // Defaults
        public static string _defaultRepoUrl = "https://azure.github.io/Industrial-IoT/helm";
        public static string _defaultChartVersion = "0.4.4";
        public static string _defaultImageTag = "2.8.4";
        public static string _defaultImageNamespace = "";
        public static string _defaultContainerRegistryServer = "mcr.microsoft.com";
        public static string _defaultContainerRegistryUsername = "";
        public static string _defaultContainerRegistryPassword = "";

        /// <summary> Helm repository URL </summary>
        public string RepoUrl { get; set; }

        /// <summary> Helm chart version </summary>
        public string ChartVersion { get; set; }

        /// <summary> Azure IIoT components image tag to deploy </summary>
        public string ImageTag { get; set; }
        /// <summary> Azure IIoT components image namespace </summary>
        public string ImageNamespace { get; set; }
        /// <summary> Container registry server to use </summary>
        public string ContainerRegistryServer { get; set; }
        /// <summary> Container registry username  </summary>
        public string ContainerRegistryUsername { get; set; }
        /// <summary> Container registry password </summary>
        public string ContainerRegistryPassword { get; set; }
    }
}
