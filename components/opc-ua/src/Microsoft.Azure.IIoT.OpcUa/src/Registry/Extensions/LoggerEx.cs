// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Registry.Models {
    using Serilog;

    /// <summary>
    /// Logger extensions
    /// </summary>
    public static class LoggerEx {

        /// <summary>
        /// Log ev.Progress from event model
        /// </summary>
        /// <param name="_logger"></param>
        /// <param name="ev"></param>
        public static void LogProgress(this ILogger _logger, DiscoveryProgressModel ev) {
            switch (ev.EventType) {
                case DiscoveryProgressType.Pending:
                    _logger.Verbose("{request}: Discovery operations pending.",
                        ev.Request.Id);
                    break;
                case DiscoveryProgressType.Started:
                    _logger.Information("{request}: Discovery operation started.",
                        ev.Request.Id);
                    break;
                case DiscoveryProgressType.Cancelled:
                    _logger.Information("{request}: Discovery operation cancelled.",
                        ev.Request.Id);
                    break;
                case DiscoveryProgressType.Error:
                    _logger.Error("{request}: Error {error} during discovery run.",
                        ev.Request.Id, ev.Result);
                    break;
                case DiscoveryProgressType.Finished:
                    _logger.Information("{request}: Discovery operation completed.",
                        ev.Request.Id);
                    break;
                case DiscoveryProgressType.NetworkScanStarted:
                    _logger.Information(
                        "{request}: Starting network scan ({active} probes active)...",
                        ev.Request.Id, ev.Workers);
                    break;
                case DiscoveryProgressType.NetworkScanResult:
                    _logger.Information("{request}: Found address {address} ({scanned} scanned)...",
                        ev.Request.Id, ev.Result, ev.Progress);
                    break;
                case DiscoveryProgressType.NetworkScanProgress:
                    _logger.Information("{request}: {scanned} addresses scanned - {discovered} " +
                        "ev.Discovered ({active} probes active)...", ev.Request.Id,
                        ev.Progress, ev.Discovered, ev.Workers);
                    break;
                case DiscoveryProgressType.NetworkScanFinished:
                    _logger.Information("{request}: Found {count} addresses. " +
                        "({scanned} scanned)...", ev.Request.Id,
                        ev.Discovered, ev.Progress);
                    break;
                case DiscoveryProgressType.PortScanStarted:
                    _logger.Information(
                        "{request}: Starting port scanning ({active} probes active)...",
                        ev.Request.Id, ev.Workers);
                    break;
                case DiscoveryProgressType.PortScanResult:
                    _logger.Information("{request}: Found server {endpoint} ({scanned} scanned)...",
                        ev.Request.Id, ev.Result, ev.Progress);
                    break;
                case DiscoveryProgressType.PortScanProgress:
                    _logger.Information("{request}: {scanned} ports scanned - {discovered} discovered" +
                        " ({active} probes active)...", ev.Request.Id,
                        ev.Progress, ev.Discovered, ev.Workers);
                    break;
                case DiscoveryProgressType.PortScanFinished:
                    _logger.Information("{request}: Found {count} ports on servers " +
                        "({scanned} scanned)...",
                        ev.Request.Id, ev.Discovered, ev.Progress);
                    break;
                case DiscoveryProgressType.ServerDiscoveryStarted:
                    _logger.Information(
                        "{request}: Searching {count} discovery urls for endpoints...",
                        ev.Request.Id, ev.Total);
                    break;
                case DiscoveryProgressType.EndpointsDiscoveryStarted:
                    _logger.Information(
                        "{request}: Trying to find endpoints on {details}...",
                        ev.Request.Id, ev.RequestDetails["url"]);
                    break;
                case DiscoveryProgressType.EndpointsDiscoveryFinished:
                    if (!ev.Discovered.HasValue || ev.Discovered == 0) {
                        _logger.Information(
                            "{request}: No endpoints ev.Discovered on {details}.",
                            ev.Request.Id, ev.RequestDetails["url"]);
                    }
                    _logger.Information(
                        "{request}: Found {count} endpoints on {details}.",
                        ev.Request.Id, ev.Discovered, ev.RequestDetails["url"]);
                    break;
                case DiscoveryProgressType.ServerDiscoveryFinished:
                    _logger.Information("{request}: Found total of {count} servers ...",
                        ev.Request.Id, ev.Discovered);
                    break;
            }
        }
    }
}
