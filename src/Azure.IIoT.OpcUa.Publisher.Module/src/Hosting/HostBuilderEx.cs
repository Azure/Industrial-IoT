// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Module.Hosting
{
    using Microsoft.Extensions.Hosting;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Hosting extensions. Replaces the former
    /// <c>Furly.Extensions.Hosting.HostingExtensions</c> helper of the same name.
    /// The publisher module does not wire a leader election service (see the
    /// commented out <c>AddLeaderElection()</c> in <c>Configuration</c>), so the
    /// host is simply built and run to completion.
    /// </summary>
    public static class HostBuilderEx
    {
        /// <summary>
        /// Build the host and run it.
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public static async Task RunAsync(this IHostBuilder builder,
            CancellationToken ct = default)
        {
            using var host = builder.Build();
            await host.RunAsync(ct).ConfigureAwait(false);
        }
    }
}
