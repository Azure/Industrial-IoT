// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Module.Controllers
{
    using Azure.IIoT.OpcUa.Publisher.Module.Filters;
    using Furly.Tunnel.Router;
    using Microsoft.Extensions.Logging;
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// Publisher controller
    /// </summary>
    [Version("_V2")]
    [Version("")]
    [RouterExceptionFilter]
    public class PublisherController : IMethodController
    {
        /// <summary>
        /// Support restarting the module
        /// </summary>
        /// <param name="logger"></param>
        public PublisherController(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Shutdown
        /// </summary>
        /// <param name="failFast"></param>
        /// <returns></returns>
        /// <exception cref="NotSupportedException"></exception>
        public async Task ShutdownAsync(bool failFast = false)
        {
            _logger.LogInformation("Shutdown called.");
            if (failFast)
            {
                Environment.FailFast("Shutdown was invoked remotely.");
            }
            else
            {
                Environment.Exit(0);
            }
            await Task.Delay(TimeSpan.FromSeconds(10)).ConfigureAwait(false);
            throw new NotSupportedException("Failed to invoke shutdown");
        }

        private readonly ILogger _logger;
    }
}
