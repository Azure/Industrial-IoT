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
    using Microsoft.Azure.IIoT.Deployment.Configuration;
    using Microsoft.Azure.IIoT.Deployment.Deployment;

    class Program {

        public const int SUCCESS = 0;
        public const int ERROR = 1;

        static int Main(string[] args) {

            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables("AZURE_IIOT_")
                .AddCommandLine(args);

            var configuration = builder.Build();

            var exitcCode = Run(configuration);
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

        static int Run(IConfigurationRoot configuration) {
            try {
                SetupLogger();

                var appSettings = configuration.Get<AppSettings>();
                var configurationProvider = new ConsoleConfigurationProvider(appSettings);

                using var cts = new CancellationTokenSource();
                using var deploymentExecutor = new DeploymentExecutor(configurationProvider);
                deploymentExecutor
                    .RunAsync(cts.Token)
                    .Wait();

                return SUCCESS;
            }
            catch (Exception ex) {
                Log.Error(ex, "Unhandled exception");
                return ERROR;
            }
        }
    }
}
