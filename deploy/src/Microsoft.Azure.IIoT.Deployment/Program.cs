// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Deployment {

    using Serilog;
    using System;
    using System.Threading;
    using System.IO;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Azure.IIoT.Deployment.Cli;
    using Microsoft.Azure.IIoT.Deployment.Configuration;

    class Program {

        public const int SUCCESS = 0;
        public const int ERROR = 1;

        static int Main(string[] args) {

            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables("AZURE_IIOT_");

            var configuration = builder.Build();
            var appSettings = configuration.Get<AppSettings>();

            var exitcCode = Run(appSettings);
            return exitcCode;
        }

        static void SetupLogger() {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Information)
                .Enrich.FromLogContext()
                .WriteTo.Console()
                .CreateLogger();
        }

        static int Run(AppSettings appSettings) {
            var returnError = false;

            try {
                SetupLogger();

                Configuration.IConfigurationProvider configurationProvider =
                    new ConsoleConfigurationProvider(appSettings);

                using var cts = new CancellationTokenSource();
                using var deploymentExecutor = new DeploymentExecutor(configurationProvider);

                try {
                    Log.Information("Starting Industrial Asset Integration Installer.");

                    deploymentExecutor.InitializeAuthenticationAsync(cts.Token).Wait();
                    deploymentExecutor.GetApplicationName();
                    deploymentExecutor.InitializeResourceGroupSelectionAsync(cts.Token).Wait();
                    deploymentExecutor.InitializeResourceManagementClients(cts.Token);
                    deploymentExecutor.RegisterResourceProvidersAsync(cts.Token).Wait();
                    deploymentExecutor.GenerateResourceNamesAsync(cts.Token).Wait();
                    deploymentExecutor.RegisterApplicationsAsync(cts.Token).Wait();
                    deploymentExecutor.CreateAzureResourcesAsync(cts.Token).Wait();

                    Log.Information("Done.");
                }
                catch (Exception ex) {
                    Log.Error(ex, "Failed to deploy Industrial IoT solution.");

                    deploymentExecutor.CleanupIfAskedAsync(cts.Token).Wait();
                    returnError = true;
                }
            }
            catch (Exception ex) {
                Log.Error(ex, "Unhandled exception");
                return ERROR;
            }

            if (returnError) {
                return ERROR;
            }

            return SUCCESS;
        }
    }
}
