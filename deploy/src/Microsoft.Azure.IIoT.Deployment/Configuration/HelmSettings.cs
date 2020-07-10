﻿// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Deployment.Configuration {
    using System;
    using System.Collections.Generic;
    using System.Text;

    /// <summary>
    /// Represents Helm chart settings that are configurable.
    /// </summary>
    class HelmSettings {

        // Defaults
        public static string _defaultRepoUrl = "https://microsoft.github.io/charts/repo";
        public static string _defaultChartVersion = "0.3.1";
        public static string _defaultImageTag = "2.7.170";

        /// <summary> Helm repository URL </summary>
        public string RepoUrl { get; set; }

        /// <summary> Helm chart version </summary>
        public string ChartVersion { get; set; }

        /// <summary> Azure IIoT components image tag to deploy </summary>
        public string ImageTag { get; set; }
    }
}
