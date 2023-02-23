// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Shared.Models
{
    using Microsoft.Extensions.Logging;

    /// <summary>
    /// Logger extensions
    /// </summary>
    public static class LoggerEx
    {
        /// <summary>
        /// Log ev.Progress from event model
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="ev"></param>
        public static void LogProgress(this ILogger logger, DiscoveryProgressModel ev)
        {
            switch (ev.EventType)
            {
                case DiscoveryProgressType.Pending:
                    logger.LogTrace("{Request}: Discovery operations pending.",
                        ev.Request.Id);
                    break;
                case DiscoveryProgressType.Started:
                    logger.LogInformation("{Request}: Discovery operation started.",
                        ev.Request.Id);
                    break;
                case DiscoveryProgressType.Cancelled:
                    logger.LogInformation("{Request}: Discovery operation cancelled.",
                        ev.Request.Id);
                    break;
                case DiscoveryProgressType.Error:
                    logger.LogError("{Request}: Error {Error} during discovery run.",
                        ev.Request.Id, ev.Result);
                    break;
                case DiscoveryProgressType.Finished:
                    logger.LogInformation("{Request}: Discovery operation completed.",
                        ev.Request.Id);
                    break;
                case DiscoveryProgressType.NetworkScanStarted:
                    logger.LogInformation(
                        "{Request}: Starting network scan ({Active} probes active)...",
                        ev.Request.Id, ev.Workers);
                    break;
                case DiscoveryProgressType.NetworkScanResult:
                    logger.LogInformation("{Request}: Found address {Address} ({Scanned} scanned)...",
                        ev.Request.Id, ev.Result, ev.Progress);
                    break;
                case DiscoveryProgressType.NetworkScanProgress:
                    logger.LogInformation("{Request}: {Scanned} addresses scanned - {Discovered} " +
                        "ev.Discovered ({Active} probes active)...", ev.Request.Id,
                        ev.Progress, ev.Discovered, ev.Workers);
                    break;
                case DiscoveryProgressType.NetworkScanFinished:
                    logger.LogInformation("{Request}: Found {Count} addresses. " +
                        "({Scanned} scanned)...", ev.Request.Id,
                        ev.Discovered, ev.Progress);
                    break;
                case DiscoveryProgressType.PortScanStarted:
                    logger.LogInformation(
                        "{Request}: Starting port scanning ({Active} probes active)...",
                        ev.Request.Id, ev.Workers);
                    break;
                case DiscoveryProgressType.PortScanResult:
                    logger.LogInformation("{Request}: Found server {Endpoint} ({Scanned} scanned)...",
                        ev.Request.Id, ev.Result, ev.Progress);
                    break;
                case DiscoveryProgressType.PortScanProgress:
                    logger.LogInformation("{Request}: {Scanned} ports scanned - {Discovered} discovered" +
                        " ({Active} probes active)...", ev.Request.Id,
                        ev.Progress, ev.Discovered, ev.Workers);
                    break;
                case DiscoveryProgressType.PortScanFinished:
                    logger.LogInformation("{Request}: Found {Count} ports on servers " +
                        "({Scanned} scanned)...",
                        ev.Request.Id, ev.Discovered, ev.Progress);
                    break;
                case DiscoveryProgressType.ServerDiscoveryStarted:
                    logger.LogInformation(
                        "{Request}: Searching {Count} discovery urls for endpoints...",
                        ev.Request.Id, ev.Total);
                    break;
                case DiscoveryProgressType.EndpointsDiscoveryStarted:
                    logger.LogInformation(
                        "{Request}: Trying to find endpoints on {Details}...",
                        ev.Request.Id, ev.RequestDetails["url"]);
                    break;
                case DiscoveryProgressType.EndpointsDiscoveryFinished:
                    if (!ev.Discovered.HasValue || ev.Discovered == 0)
                    {
                        logger.LogInformation(
                            "{Request}: No endpoints ev.Discovered on {Details}.",
                            ev.Request.Id, ev.RequestDetails["url"]);
                    }
                    logger.LogInformation(
                        "{Request}: Found {Count} endpoints on {Details}.",
                        ev.Request.Id, ev.Discovered, ev.RequestDetails["url"]);
                    break;
                case DiscoveryProgressType.ServerDiscoveryFinished:
                    logger.LogInformation("{Request}: Found total of {Count} servers ...",
                        ev.Request.Id, ev.Discovered);
                    break;
            }
        }
    }
}
