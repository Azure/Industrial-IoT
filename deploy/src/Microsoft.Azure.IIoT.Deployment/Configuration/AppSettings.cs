// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Deployment.Configuration {

    using System;
    using Microsoft.Azure.IIoT.Deployment.Deployment;

    class AppSettings {

        /// <summary> Application execution mode details </summary>
        public RunMode? RunMode { get; set; }

        /// <summary> Azure authentication details </summary>
        public AuthenticationSettings Auth { get; set; }

        /// <summary> Azure subscription details </summary>
        public Guid? SubscriptionId { get; set; }

        /// <summary> Azure IIoT deployment instance name details </summary>
        public string ApplicationName { get; set; }

        /// <summary> Application base URL details </summary>
        public string ApplicationUrl { get; set; }

        /// <summary> Resource group details </summary>
        public ResourceGroupSettings ResourceGroup { get; set; }

        /// <summary> Helm chart details </summary>
        public HelmSettings Helm { get; set; }

        /// <summary> Azure App Registration details for service, client and aks apps </summary>
        public ApplicationRegistrationDefinitionSettings ApplicationRegistration { get; set; }

        /// <summary> Flag to determine whether to create .env file after deployment or not </summary>
        public bool? SaveEnvFile { get; set; }

        /// <summary> Flag to determine whether to perform cleanup if deployment error occurs </summary>
        public bool? NoCleanup { get; set; }
    }
}
