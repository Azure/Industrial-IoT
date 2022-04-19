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

            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", true)
                .AddEnvironmentVariables()
                .AddEnvironmentVariables(EnvironmentVariableTarget.User)
                .AddCommandLine(args)
                .AddStandalonePublisherCommandLine(args)
                // making sure the standalone arguments are processed at last so they are not overriden
                .Build();

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
