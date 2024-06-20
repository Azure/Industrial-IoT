// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Discovery
{
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Microsoft.Extensions.Logging;
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Net;

    /// <summary>
    /// Discovery progress logger
    /// </summary>
    public class ProgressLogger : IDiscoveryProgress
    {
        /// <summary>
        /// Create listener
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="timeProvider"></param>
        internal ProgressLogger(ILogger logger, TimeProvider? timeProvider = null)
        {
            _logger = logger;
            _timeProvider = timeProvider ?? TimeProvider.System;
        }

        /// <inheritdoc/>
        public void OnDiscoveryPending(DiscoveryRequestModel request,
            int pending)
        {
            Send(new DiscoveryProgressModel
            {
                EventType = DiscoveryProgressType.Pending,
                Request = request,
                Total = pending
            });
        }

        /// <inheritdoc/>
        public void OnDiscoveryStarted(DiscoveryRequestModel request)
        {
            Send(new DiscoveryProgressModel
            {
                EventType = DiscoveryProgressType.Started,
                Request = request
            });
        }

        /// <inheritdoc/>
        public void OnDiscoveryCancelled(DiscoveryRequestModel request)
        {
            Send(new DiscoveryProgressModel
            {
                TimeStamp = _timeProvider.GetUtcNow(),
                EventType = DiscoveryProgressType.Cancelled,
                Request = request
            });
        }

        /// <inheritdoc/>
        public void OnDiscoveryError(DiscoveryRequestModel request,
            Exception ex)
        {
            Send(new DiscoveryProgressModel
            {
                TimeStamp = _timeProvider.GetUtcNow(),
                EventType = DiscoveryProgressType.Error,
                Request = request,
                Result = ex.Message,
                ResultDetails = new Dictionary<string, string>
                {
                    ["exception"] = ex.ToString()
                }
            });
        }

        /// <inheritdoc/>
        public void OnDiscoveryFinished(DiscoveryRequestModel request)
        {
            Send(new DiscoveryProgressModel
            {
                TimeStamp = _timeProvider.GetUtcNow(),
                EventType = DiscoveryProgressType.Finished,
                Request = request
            });
        }

        /// <inheritdoc/>
        public void OnNetScanStarted(DiscoveryRequestModel request,
            int workers, int progress, int total)
        {
            Send(new DiscoveryProgressModel
            {
                TimeStamp = _timeProvider.GetUtcNow(),
                EventType = DiscoveryProgressType.NetworkScanStarted,
                Workers = workers,
                Progress = progress,
                Total = total,
                Request = request
            });
        }

        /// <inheritdoc/>
        public void OnNetScanResult(DiscoveryRequestModel request,
            int workers, int progress, int total, int discovered, IPAddress address)
        {
            Send(new DiscoveryProgressModel
            {
                TimeStamp = _timeProvider.GetUtcNow(),
                EventType = DiscoveryProgressType.NetworkScanResult,
                Workers = workers,
                Progress = progress,
                Total = total,
                Discovered = discovered,
                Result = address.ToString(),
                Request = request
            });
        }

        /// <inheritdoc/>
        public void OnNetScanProgress(DiscoveryRequestModel request,
            int workers, int progress, int total, int discovered)
        {
            Send(new DiscoveryProgressModel
            {
                TimeStamp = _timeProvider.GetUtcNow(),
                EventType = DiscoveryProgressType.NetworkScanProgress,
                Workers = workers,
                Progress = progress,
                Total = total,
                Discovered = discovered,
                Request = request
            });
        }

        /// <inheritdoc/>
        public void OnNetScanFinished(DiscoveryRequestModel request,
            int workers, int progress, int total, int discovered)
        {
            Send(new DiscoveryProgressModel
            {
                TimeStamp = _timeProvider.GetUtcNow(),
                EventType = DiscoveryProgressType.NetworkScanFinished,
                Workers = workers,
                Progress = progress,
                Total = total,
                Discovered = discovered,
                Request = request
            });
        }

        /// <inheritdoc/>
        public void OnPortScanStart(DiscoveryRequestModel request,
            int workers, int progress, int total)
        {
            Send(new DiscoveryProgressModel
            {
                TimeStamp = _timeProvider.GetUtcNow(),
                EventType = DiscoveryProgressType.PortScanStarted,
                Workers = workers,
                Progress = progress,
                Total = total,
                Request = request
            });
        }

        /// <inheritdoc/>
        public void OnPortScanResult(DiscoveryRequestModel request,
            int workers, int progress, int total, int discovered, IPEndPoint ep)
        {
            Send(new DiscoveryProgressModel
            {
                TimeStamp = _timeProvider.GetUtcNow(),
                EventType = DiscoveryProgressType.PortScanResult,
                Workers = workers,
                Progress = progress,
                Total = total,
                Discovered = discovered,
                Result = ep.ToString(),
                Request = request
            });
        }

        /// <inheritdoc/>
        public void OnPortScanProgress(DiscoveryRequestModel request,
            int workers, int progress, int total, int discovered)
        {
            Send(new DiscoveryProgressModel
            {
                EventType = DiscoveryProgressType.PortScanProgress,
                Workers = workers,
                Progress = progress,
                Total = total,
                Discovered = discovered,
                Request = request
            });
        }

        /// <inheritdoc/>
        public void OnPortScanFinished(DiscoveryRequestModel request,
            int workers, int progress, int total, int discovered)
        {
            Send(new DiscoveryProgressModel
            {
                EventType = DiscoveryProgressType.PortScanFinished,
                Workers = workers,
                Progress = progress,
                Total = total,
                Discovered = discovered,
                Request = request
            });
        }

        /// <inheritdoc/>
        public void OnServerDiscoveryStarted(DiscoveryRequestModel request,
            int workers, int progress, int total)
        {
            Send(new DiscoveryProgressModel
            {
                EventType = DiscoveryProgressType.ServerDiscoveryStarted,
                Workers = workers,
                Progress = progress,
                Total = total,
                Request = request
            });
        }

        /// <inheritdoc/>
        public void OnFindEndpointsStarted(DiscoveryRequestModel request,
            int workers, int progress, int total, int discovered, Uri url, IPAddress address)
        {
            Send(new DiscoveryProgressModel
            {
                EventType = DiscoveryProgressType.EndpointsDiscoveryStarted,
                RequestDetails = new Dictionary<string, string>
                {
                    ["url"] = url.ToString(),
                    ["address"] = address.ToString()
                },
                Workers = workers,
                Progress = progress,
                Total = total,
                Discovered = discovered,
                Request = request.Clone(_timeProvider)
            });
        }

        /// <inheritdoc/>
        public void OnFindEndpointsFinished(DiscoveryRequestModel request,
            int workers, int progress, int total, int discovered, Uri url,
            IPAddress address, int endpoints)
        {
            Send(new DiscoveryProgressModel
            {
                EventType = DiscoveryProgressType.EndpointsDiscoveryFinished,
                RequestDetails = new Dictionary<string, string>
                {
                    ["url"] = url.ToString(),
                    ["address"] = address.ToString()
                },
                Workers = workers,
                Progress = progress,
                Total = total,
                Discovered = discovered,
                Result = endpoints.ToString(CultureInfo.InvariantCulture),
                Request = request
            });
        }

        /// <inheritdoc/>
        public void OnServerDiscoveryFinished(DiscoveryRequestModel request,
            int workers, int progress, int total, int discovered)
        {
            Send(new DiscoveryProgressModel
            {
                EventType = DiscoveryProgressType.ServerDiscoveryFinished,
                Workers = workers,
                Progress = progress,
                Total = total,
                Discovered = discovered,
                Request = request
            });
        }

        /// <summary>
        /// Send progress
        /// </summary>
        /// <param name="progress"></param>
        protected virtual void Send(DiscoveryProgressModel progress)
        {
            progress.TimeStamp = _timeProvider.GetUtcNow();
            switch (progress.EventType)
            {
                case DiscoveryProgressType.Pending:
                    _logger.LogTrace("{Request}: Discovery operations pending.",
                        progress.Request.Id);
                    break;
                case DiscoveryProgressType.Started:
                    _logger.LogInformation("{Request}: Discovery operation started.",
                        progress.Request.Id);
                    break;
                case DiscoveryProgressType.Cancelled:
                    _logger.LogInformation("{Request}: Discovery operation cancelled.",
                        progress.Request.Id);
                    break;
                case DiscoveryProgressType.Error:
                    _logger.LogError("{Request}: Error {Error} during discovery run.",
                        progress.Request.Id, progress.Result);
                    break;
                case DiscoveryProgressType.Finished:
                    _logger.LogInformation("{Request}: Discovery operation completed.",
                        progress.Request.Id);
                    break;
                case DiscoveryProgressType.NetworkScanStarted:
                    _logger.LogInformation(
                        "{Request}: Starting network scan ({Active} probes active)...",
                        progress.Request.Id, progress.Workers);
                    break;
                case DiscoveryProgressType.NetworkScanResult:
                    _logger.LogInformation("{Request}: Found address {Address} ({Scanned} scanned)...",
                        progress.Request.Id, progress.Result, progress.Progress);
                    break;
                case DiscoveryProgressType.NetworkScanProgress:
                    _logger.LogInformation("{Request}: {Scanned} addresses scanned - {Discovered} " +
                        "ev.Discovered ({Active} probes active)...", progress.Request.Id,
                        progress.Progress, progress.Discovered, progress.Workers);
                    break;
                case DiscoveryProgressType.NetworkScanFinished:
                    _logger.LogInformation("{Request}: Found {Count} addresses. " +
                        "({Scanned} scanned)...", progress.Request.Id,
                        progress.Discovered, progress.Progress);
                    break;
                case DiscoveryProgressType.PortScanStarted:
                    _logger.LogInformation(
                        "{Request}: Starting port scanning ({Active} probes active)...",
                        progress.Request.Id, progress.Workers);
                    break;
                case DiscoveryProgressType.PortScanResult:
                    _logger.LogInformation("{Request}: Found server {Endpoint} ({Scanned} scanned)...",
                        progress.Request.Id, progress.Result, progress.Progress);
                    break;
                case DiscoveryProgressType.PortScanProgress:
                    _logger.LogInformation("{Request}: {Scanned} ports scanned - {Discovered} discovered" +
                        " ({Active} probes active)...", progress.Request.Id,
                        progress.Progress, progress.Discovered, progress.Workers);
                    break;
                case DiscoveryProgressType.PortScanFinished:
                    _logger.LogInformation("{Request}: Found {Count} ports on servers " +
                        "({Scanned} scanned)...",
                        progress.Request.Id, progress.Discovered, progress.Progress);
                    break;
                case DiscoveryProgressType.ServerDiscoveryStarted:
                    _logger.LogInformation(
                        "{Request}: Searching {Count} discovery urls for endpoints...",
                        progress.Request.Id, progress.Total);
                    break;
                case DiscoveryProgressType.EndpointsDiscoveryStarted:
                    _logger.LogInformation(
                        "{Request}: Trying to find endpoints on {Details}...",
                        progress.Request.Id, progress.RequestDetails?["url"]);
                    break;
                case DiscoveryProgressType.EndpointsDiscoveryFinished:
                    if (!progress.Discovered.HasValue || progress.Discovered == 0)
                    {
                        _logger.LogInformation(
                            "{Request}: No endpoints ev.Discovered on {Details}.",
                            progress.Request.Id, progress.RequestDetails?["url"]);
                    }
                    _logger.LogInformation(
                        "{Request}: Found {Count} endpoints on {Details}.",
                        progress.Request.Id, progress.Discovered, progress.RequestDetails?["url"]);
                    break;
                case DiscoveryProgressType.ServerDiscoveryFinished:
                    _logger.LogInformation("{Request}: Found total of {Count} servers ...",
                        progress.Request.Id, progress.Discovered);
                    break;
            }
        }

        private readonly ILogger _logger;
        private readonly TimeProvider _timeProvider;
    }
}
