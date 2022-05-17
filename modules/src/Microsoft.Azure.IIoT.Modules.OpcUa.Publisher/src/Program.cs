// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Modules.OpcUa.Publisher {
    using Microsoft.Azure.IIoT.Modules.OpcUa.Publisher.Runtime;
    using Microsoft.Extensions.Configuration;
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Threading;

    /// <summary>
    /// Module
    /// </summary>
    public static class Program {

        /// <summary>
        /// Main entry point
        /// </summary>
        /// <param name="args"></param>
        public static void Main(string[] args) {
            IConfigurationBuilder configBuilder = new ConfigurationBuilder()
                   .SetBasePath(Directory.GetCurrentDirectory())
                   .AddJsonFile("appsettings.json", optional: true)
                   .AddEnvironmentVariables()
                   .AddEnvironmentVariables(EnvironmentVariableTarget.User)
                   .AddCommandLine(args)
                   // making sure the standalone arguments are processed at last so they are not overriden

                   .AddStandalonePublisherCommandLine(args);
            // additional required configuration
            var configFiles = args
                .Select((v, i) => new { value = v, index = i })
                .Where(
                    i =>
                        string.Compare(i.value.TrimStart('-'), "arc", StringComparison.InvariantCultureIgnoreCase) == 0 ||
                        string.Compare(i.value.TrimStart('-'), "AdditionalRequiredConfiguration", StringComparison.InvariantCultureIgnoreCase) == 0);

            foreach (var configFile in configFiles) {
                // todo: read config from file
                var filePath = args[configFile.index + 1];

                Console.WriteLine($"Looking for configuration file '{filePath}'...");

                while (!File.Exists(filePath)) {
                    Console.WriteLine("Configuration file not found, pausing 1 second");
                    Thread.Sleep(1000);
                }
                Console.WriteLine("Configuration file found");

                configBuilder.AddJsonFile(filePath, optional: false);
            }


            var config = configBuilder.Build();


#if DEBUG
            if (args.Any(a => a.Contains("wfd", StringComparison.InvariantCultureIgnoreCase) ||
                    a.Contains("waitfordebugger", StringComparison.InvariantCultureIgnoreCase))) {
                Console.WriteLine("Waiting for debugger being attached...");
                while (!Debugger.IsAttached) {
                    Thread.Sleep(1000);
                }
                Console.WriteLine("Debugger attached.");
            }
#endif

            var module = new ModuleProcess(config);
            module.RunAsync().GetAwaiter().GetResult();
        }
    }
}
