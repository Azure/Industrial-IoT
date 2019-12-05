﻿// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Services.Common.Jobs {
    using Microsoft.AspNetCore;
    using Microsoft.AspNetCore.Hosting;
    using Serilog;

    /// <summary>
    /// Job service
    /// </summary>
    public class Program {

        /// <summary>
        /// Main entry point
        /// </summary>
        /// <param name="args"></param>
        public static void Main(string[] args) {
            CreateWebHostBuilder(args).Build().Run();
        }

        /// <summary>
        /// Create host builder
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public static IWebHostBuilder CreateWebHostBuilder(string[] args) {
            return WebHost.CreateDefaultBuilder<Startup>(args)
                .UseUrls("http://*:9046")
                .UseKestrel(o => o.AddServerHeader = false)
                .UseSerilog();
        }
    }
}