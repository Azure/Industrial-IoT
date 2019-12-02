// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Deployment.Cli {

    using CommandLine;

    [Verb("init", HelpText = "Create application registration.")]
    class InitOptions {
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

        [Option('a', "application-name", Required = false, HelpText = "Application name")]
        public string ApplicationName { get; set; }

        [Option("application-uri", Required = false, HelpText = "Application URI for Redirect URIs")]
        public string ApplicationUri { get; set; }
    }
}
