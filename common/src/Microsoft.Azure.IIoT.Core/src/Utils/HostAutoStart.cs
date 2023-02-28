// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Utils
{
    using Microsoft.Extensions.Logging;
    using Autofac;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    /// <summary>
    /// Host auto starter
    /// </summary>
    public sealed class HostAutoStart : IDisposable, IStartable
    {
        /// <summary>
        /// Create host auto starter
        /// </summary>
        /// <param name="hosts"></param>
        /// <param name="logger"></param>
        public HostAutoStart(IEnumerable<IHostProcess> hosts, ILogger logger)
        {
            _host = hosts?.ToList() ?? throw new ArgumentNullException(nameof(hosts));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc/>
        public void Start()
        {
            try
            {
                _logger.LogDebug("Starting all hosts...");
                Task.WhenAll(_host.Select(async h => await h.StartAsync().ConfigureAwait(false))).Wait();
                _logger.LogInformation("All hosts started.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to start some hosts.");
                throw;
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            try
            {
                _logger.LogDebug("Stopping all hosts...");
                Task.WhenAll(_host.OfType<IAsyncDisposable>()
                    .Select(async h => await h.DisposeAsync().ConfigureAwait(false))).Wait();
                _logger.LogInformation("All hosts stopped.");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to stop all hosts.");
            }
        }

        private readonly List<IHostProcess> _host;
        private readonly ILogger _logger;
    }
}
