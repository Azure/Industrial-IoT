// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Modules.Discovery {
    using Microsoft.Extensions.Configuration;
    using System;
    using System.IO;
    using System.Diagnostics;
    using System.Threading;
    using System.Linq;

    /// <summary>
    /// Main entry point
    /// </summary>
    public static class Program {

        /// <summary>
        /// Module entry point
        /// </summary>
        public static void Main(string[] args) {

            // Load hosting configuration
            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", true)
                .AddEnvironmentVariables()
                .AddEnvironmentVariables(EnvironmentVariableTarget.User)
                .AddFromDotEnvFile()
                .AddCommandLine(args)
                .Build();
#if DEBUG
            if (args.Any(a => a.ToLowerInvariant().Contains("wfd") ||
                    a.ToLowerInvariant().Contains("waitfordebugger"))) {
                Console.WriteLine("Waiting for debugger being attached...");
                while (!Debugger.IsAttached) {
                    Thread.Sleep(1000);
                }
                Console.WriteLine("Debugger attached.");
            }
#endif
            var process = new ModuleProcess(config);
            process.RunAsync().Wait();
        }
    }
}
