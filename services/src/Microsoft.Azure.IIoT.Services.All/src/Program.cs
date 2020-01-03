// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Services.All {
    using Microsoft.AspNetCore;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Azure.IIoT.Diagnostics;
    using Autofac.Extensions.Hosting;
    using Serilog;

    /// <summary>
    /// All in one services host
    /// </summary>
    public class Program {

        /// <summary>
        /// Main entry point for all in one services host
        /// </summary>
        /// <param name="args"></param>
        public static void Main(string[] args) {
            Log.Logger = ConsoleLogger.Create();
            LogControl.Level.MinimumLevel = Serilog.Events.LogEventLevel.Debug;
            CreateWebHostBuilder(args).Build().Run();
        }

        /// <summary>
        /// Create host builder
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public static IWebHostBuilder CreateWebHostBuilder(string[] args) {
            return WebHost.CreateDefaultBuilder<Startup>(args)
                .UseUrls("http://*:9080")
                .UseAutofac()
                .UseSerilog()
                .UseKestrel(o => o.AddServerHeader = false);
        }
    }
}
