// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Deployment.Cli {

    using System;
    using System.Collections.Generic;
    using System.Text;


    class AuthenticationSettings {
        public AzureEnvironmentType? AzureEnvironment { get; set; }
        public Guid? TenantId { get; set; }
        public Guid? ClientId { get; set; }
        public string ClientSecret { get; set; }
    }

    class ResourceGroupSettings {
        public string Name { get; set; }
        public bool? UseExisting { get; set; }
        public RegionType? Region { get; set; }
    }

    class ApplicationRegistrationSettings {
        public Guid? ClientsApplicationId { get; set; }
        public Guid? ServicesApplicationId { get; set; }
        public Guid? AksApplicatoinId { get; set; }
        public string AksApplicatoinRbacSecret { get; set; }

    }

    class AppSettings {
        public AuthenticationSettings Auth { get; set; }
        public Guid? SubscriptionId { get; set; }
        public bool? ApplicationRegistrationOnly { get; set; }
        public bool? DeploymentOnly { get; set; }
        public string ApplicationName { get; set; }
        public string ApplicationURL { get; set; }
        public ResourceGroupSettings ResourceGroup { get; set; }
        public ApplicationRegistrationSettings ApplicationRegistration { get; set; }

        public bool? SaveEnvFile { get; set; }
        public bool? NoCleanup { get; set; }
    }
}
