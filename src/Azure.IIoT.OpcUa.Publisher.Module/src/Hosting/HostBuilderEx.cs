// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Module.Hosting
{
    using Microsoft.Extensions.Hosting;
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Hosting extensions. Replaces the former
    /// <c>Legacy.Extensions.Hosting.HostingExtensions</c> helper of the same name.
    /// The publisher module does not wire a leader election service (see the
    /// commented out <c>AddLeaderElection()</c> in <c>Configuration</c>), so the
    /// host is simply built and run to completion.
    /// </summary>
    public static class HostBuilderEx
    {
        /// <summary>
        /// Build the host and run it.
        /// </summary>
        /// <remarks>
        /// The host is disposed explicitly rather than with <c>using</c>
        /// because the publisher owns services that only tear down
        /// asynchronously - the native PubSub host and its data set sources
        /// among them. <c>Microsoft.Extensions.Hosting.Host</c> happens to
        /// forward its synchronous disposal to the asynchronous one, so this
        /// is belt and braces for that implementation, but an
        /// <see cref="IHost"/> that does not would make the service provider
        /// refuse to dispose at all.
        /// </remarks>
        /// <param name="builder"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public static async Task RunAsync(this IHostBuilder builder,
            CancellationToken ct = default)
        {
            var host = builder.Build();
            try
            {
                await host.RunAsync(ct).ConfigureAwait(false);
            }
            finally
            {
                //
                // IHost itself does not declare IAsyncDisposable, so the
                // asynchronous path has to be asked for rather than assumed.
                //
                if (host is IAsyncDisposable disposable)
                {
                    await disposable.DisposeAsync().ConfigureAwait(false);
                }
                else
                {
                    host.Dispose();
                }
            }
        }
    }
}
