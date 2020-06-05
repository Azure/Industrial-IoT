// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.App {
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Extensions.Hosting;
    using Autofac.Extensions.Hosting;
    using System;
    using System.IO;
    using Serilog;
    using Serilog.Events;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Logging.ApplicationInsights;

    public class Program {

        /// <summary>
        /// Start application
        /// </summary>
        /// <param name="args"></param>
        public static void Main(string[] args) {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .MinimumLevel.Override("Microsoft.AspNetCore.Components", LogEventLevel.Information)
                .MinimumLevel.Override("Microsoft.AspNetCore.SignalR", LogEventLevel.Information)
                .MinimumLevel.ControlledBy(Diagnostics.LogControl.Level)
                .Enrich.FromLogContext()
                .WriteTo.Console()
                .CreateLogger();
#if DEBUG
            Diagnostics.LogControl.Level.MinimumLevel = LogEventLevel.Debug;
#endif
            try {
                CreateHostBuilder(args).Build().Run();
            }
            catch (Exception ex) {
                Log.Fatal(ex, "Host terminated unexpectedly");
                throw;
            }
            finally {
                Log.CloseAndFlush();
            }
        }

        /// <summary>
        /// Create host build
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public static IHostBuilder CreateHostBuilder(string[] args) {
            return Host.CreateDefaultBuilder(args)
                .UseAutofac()
                .ConfigureWebHostDefaults(builder => builder
                    .UseContentRoot(Directory.GetCurrentDirectory())
                    .UseStartup<Startup>()
                    .UseKestrel(o => o.AddServerHeader = false)
                    .UseIISIntegration()
                    .UseSetting(WebHostDefaults.DetailedErrorsKey, "true"))
                .UseSerilog()
                .ConfigureLogging(logging =>
                    logging.AddFilter<ApplicationInsightsLoggerProvider>
                        ("", LogLevel.Information));
        }
    }
}
