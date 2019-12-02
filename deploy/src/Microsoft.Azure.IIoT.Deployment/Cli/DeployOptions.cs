// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Deployment.Cli {

    using CommandLine;

    [Verb("deploy", HelpText = "Deploy Industrial IoT solution.")]
    class DeployOptions {

        [Option('u', "username", Required = false, HelpText = "User name")]
        public string Username { get; set; }

        [Option('p', "password", Required = false, HelpText = "User password")]
        public string Pasword { get; set; }

        // ToDo: Do we need interactive login only ?
        [Option('i', "interactive", Required = false, HelpText = "Interactive login")]
        public bool Interactive { get; set; }

        [Option('e', "azure-environment", Required = false, HelpText = "Azure environment")]
        public AzureEnvironmentType AzureEnvironment { get; set; }

        [Option('t', "tenant-id", Required = false, HelpText = "Tenant ID")]
        public string TenantId { get; set; }

        [Option('s', "subscription-id", Required = false, HelpText = "Subscription ID")]
        public string SubscriptionId { get; set; }

        [Option('a', "application-name", Required = false, HelpText = "Application name")]
        public string ApplicationName { get; set; }

        [Option("resource-group-name", Required = false, HelpText = "Resource Group name")]
        public string ResourceGroupName { get; set; }

        [Option("use-existing-resource-group", Required = false, HelpText = "Use existing Resource Group instead of creating one")]
        public bool UseExistingResourceGroup { get; set; }

        [Option("region", Required = false, HelpText = "Azure region")]
        public RegionType Region { get; set; }

        [Option("save-env-file", Required = false, HelpText = "Save .env file")]
        public bool SaveEnvFile { get; set; }

        [Option("no-cleanup", Required = false, HelpText = "Disable cleanup of Azure resources on error")]
        public bool NoCleanup { get; set; }
    }
}
