// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using Microsoft.AspNetCore.Hosting;
using Serilog.Events;

namespace Serilog {

    /// <summary>
    /// Serilog extensions
    /// </summary>
    public static class SerilogEx {

        /// <summary>
        /// Configure serilog
        /// </summary>
        /// <param name="context"></param>
        /// <param name="configuration"></param>
        public static void Console(WebHostBuilderContext context,
            LoggerConfiguration configuration) =>
            configuration.Console(context.Configuration)
                .MinimumLevel.Override("Microsoft", LogEventLevel.Information);

        /// <summary>
        /// Configure serilog
        /// </summary>
        /// <param name="context"></param>
        /// <param name="configuration"></param>
        public static void Trace(WebHostBuilderContext context,
            LoggerConfiguration configuration) =>
            configuration.Trace(context.Configuration)
                .MinimumLevel.Override("Microsoft", LogEventLevel.Information);
    }
}
