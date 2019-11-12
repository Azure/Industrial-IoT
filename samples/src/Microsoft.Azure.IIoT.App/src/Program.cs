// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.App {
    using Autofac.Extensions.DependencyInjection;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Extensions.Hosting;
    using System.IO;

    public class Program {

        /// <summary>
        /// Start application
        /// </summary>
        /// <param name="args"></param>
        public static void Main(string[] args) {
            CreateHostBuilder(args).Build().Run();
        }

        /// <summary>
        /// Create host build
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public static IHostBuilder CreateHostBuilder(string[] args) {
            return Host.CreateDefaultBuilder(args)
                .UseServiceProviderFactory(new AutofacServiceProviderFactory())
                .ConfigureWebHostDefaults(webHostBuilder => webHostBuilder
                    .UseContentRoot(Directory.GetCurrentDirectory())
                    .UseIISIntegration()
                    .UseStartup<Startup>());
        }
    }
}
