// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Serilog {

    /// <summary>
    /// Serilog extensions
    /// </summary>
    internal static class SerilogEx {

        /// <summary>
        /// Create trace logger
        /// </summary>
        /// <param name="configuration"></param>
        /// <param name="addConsole"></param>
        /// <returns></returns>
        public static LoggerConfiguration Trace(this LoggerConfiguration configuration,
            bool addConsole = true) {
            return configuration.Configure((c, m) => c
                .WriteTo.Trace(outputTemplate: m), addConsole);
        }

        /// <summary>
        /// Create trace logger
        /// </summary>
        /// <param name="configuration"></param>
        /// <param name="addConsole"></param>
        /// <returns></returns>
        public static LoggerConfiguration Debug(this LoggerConfiguration configuration,
            bool addConsole = true) {
            return configuration.Configure((c, m) => c
                .WriteTo.Debug(outputTemplate: m), addConsole);
        }
    }
}
