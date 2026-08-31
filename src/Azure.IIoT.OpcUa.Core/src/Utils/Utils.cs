// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Core.Utils
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Net;
    using System.Reflection;

    /// <summary>
    /// Small process and assembly helpers.
    /// </summary>
    public static class Utils
    {
        /// <summary>
        /// Get host name.
        /// </summary>
        /// <returns></returns>
        public static string GetHostName()
        {
            return Dns.GetHostName();
        }

        /// <summary>
        /// Get assembly software version.
        /// </summary>
        /// <returns></returns>
        public static string GetAssemblySoftwareVersion()
        {
            return Assembly.GetEntryAssembly()?
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
                .InformationalVersion ??
                Assembly.GetEntryAssembly()?.GetName().Version?.ToString() ??
                typeof(Utils).Assembly.GetName().Version?.ToString() ??
                "1.0.0";
        }

        /// <summary>
        /// Get assembly build number.
        /// </summary>
        /// <returns></returns>
        public static string GetAssemblyBuildNumber()
        {
            return Assembly.GetEntryAssembly()?.GetName().Version?.Build
                .ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "0";
        }

        /// <summary>
        /// Get assembly timestamp.
        /// </summary>
        /// <returns></returns>
        [System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessage(
            "SingleFile", "IL3000",
            Justification = "Assembly.Location returning empty for a single-file " +
                "app is expected and handled: the empty/missing path falls back to " +
                "the process start time below, which is the intended behavior.")]
        public static DateTime GetAssemblyTimestamp()
        {
            var location = Assembly.GetEntryAssembly()?.Location;
            if (!string.IsNullOrEmpty(location) && File.Exists(location))
            {
                return File.GetLastWriteTimeUtc(location);
            }
            return Process.GetCurrentProcess().StartTime.ToUniversalTime();
        }
    }
}
