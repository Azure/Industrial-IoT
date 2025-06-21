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
            var requestId = progress.Request?.Id;
            switch (progress.EventType)
            {
                case DiscoveryProgressType.Pending:
                    _logger.DiscoveryPending(requestId);
                    break;
                case DiscoveryProgressType.Started:
                    _logger.DiscoveryStarted(requestId);
                    break;
                case DiscoveryProgressType.Cancelled:
                    _logger.DiscoveryCancelled(requestId);
                    break;
                case DiscoveryProgressType.Error:
                    _logger.DiscoveryError(requestId, progress.Result);
                    break;
                case DiscoveryProgressType.Finished:
                    _logger.DiscoveryFinished(requestId);
                    break;
                case DiscoveryProgressType.NetworkScanStarted:
                    _logger.NetworkScanStarted(requestId, progress.Workers);
                    break;
                case DiscoveryProgressType.NetworkScanResult:
                    _logger.NetworkScanResult(requestId, progress.Result, progress.Progress);
                    break;
                case DiscoveryProgressType.NetworkScanProgress:
                    _logger.NetworkScanProgress(requestId, progress.Progress, progress.Discovered, progress.Workers);
                    break;
                case DiscoveryProgressType.NetworkScanFinished:
                    _logger.NetworkScanFinished(requestId, progress.Discovered, progress.Progress);
                    break;
                case DiscoveryProgressType.PortScanStarted:
                    _logger.PortScanStarted(requestId, progress.Workers);
                    break;
                case DiscoveryProgressType.PortScanResult:
                    _logger.PortScanResult(requestId, progress.Result, progress.Progress);
                    break;
                case DiscoveryProgressType.PortScanProgress:
                    _logger.PortScanProgress(requestId, progress.Progress, progress.Discovered, progress.Workers);
                    break;
                case DiscoveryProgressType.PortScanFinished:
                    _logger.PortScanFinished(requestId, progress.Discovered, progress.Progress);
                    break;
                case DiscoveryProgressType.ServerDiscoveryStarted:
                    _logger.ServerDiscoveryStarted(requestId, progress.Total);
                    break;
                case DiscoveryProgressType.EndpointsDiscoveryStarted:
                    _logger.EndpointsDiscoveryStarted(requestId, progress.RequestDetails?["url"]);
                    break;
                case DiscoveryProgressType.EndpointsDiscoveryFinished:
                    if (!progress.Discovered.HasValue || progress.Discovered == 0)
                    {
                        _logger.NoEndpointsDiscovered(requestId, progress.RequestDetails?["url"]);
                    }
                    _logger.EndpointsDiscoveryFinished(requestId, progress.Discovered ?? 0, progress.RequestDetails?["url"]);
                    break;
                case DiscoveryProgressType.ServerDiscoveryFinished:
                    _logger.ServerDiscoveryFinished(requestId, progress.Discovered);
                    break;
            }
        }

        private readonly ILogger _logger;
        private readonly TimeProvider _timeProvider;
    }

    /// <summary>
    /// Source-generated logging extensions for ProgressLogger
    /// </summary>
    internal static partial class ProgressLoggerLogging
    {
        private const int EventClass = 30;

        [LoggerMessage(EventId = EventClass + 1, Level = LogLevel.Trace,
            Message = "{RequestId}: Discovery operations pending.")]
        internal static partial void DiscoveryPending(this ILogger logger, string? requestId);

        [LoggerMessage(EventId = EventClass + 2, Level = LogLevel.Information,
            Message = "{RequestId}: Discovery operation started.")]
        internal static partial void DiscoveryStarted(this ILogger logger, string? requestId);

        [LoggerMessage(EventId = EventClass + 3, Level = LogLevel.Information,
            Message = "{RequestId}: Discovery operation cancelled.")]
        internal static partial void DiscoveryCancelled(this ILogger logger, string? requestId);

        [LoggerMessage(EventId = EventClass + 4, Level = LogLevel.Error,
            Message = "{RequestId}: Error {Error} during discovery run.")]
        internal static partial void DiscoveryError(this ILogger logger, string? requestId,
            string? error);

        [LoggerMessage(EventId = EventClass + 5, Level = LogLevel.Information,
            Message = "{RequestId}: Discovery operation completed.")]
        internal static partial void DiscoveryFinished(this ILogger logger, string? requestId);

        [LoggerMessage(EventId = EventClass + 6, Level = LogLevel.Information,
            Message = "{RequestId}: Starting network scan ({Active} probes active)...")]
        internal static partial void NetworkScanStarted(this ILogger logger, string? requestId,
            int? active);

        [LoggerMessage(EventId = EventClass + 7, Level = LogLevel.Information,
            Message = "{RequestId}: Found address {Address} ({Scanned} scanned)...")]
        internal static partial void NetworkScanResult(this ILogger logger, string? requestId,
            string? address, int? scanned);

        [LoggerMessage(EventId = EventClass + 8, Level = LogLevel.Information,
            Message = "{RequestId}: {Scanned} addresses scanned - {Discovered} discovered ({Active} probes active)...")]
        internal static partial void NetworkScanProgress(this ILogger logger, string? requestId,
            int? scanned, int? discovered, int? active);

        [LoggerMessage(EventId = EventClass + 9, Level = LogLevel.Information,
            Message = "{RequestId}: Found {Count} addresses. ({Scanned} scanned)...")]
        internal static partial void NetworkScanFinished(this ILogger logger, string? requestId,
            int? count, int? scanned);

        [LoggerMessage(EventId = EventClass + 10, Level = LogLevel.Information,
            Message = "{RequestId}: Starting port scanning ({Active} probes active)...")]
        internal static partial void PortScanStarted(this ILogger logger, string? requestId,
            int? active);

        [LoggerMessage(EventId = EventClass + 11, Level = LogLevel.Information,
            Message = "{RequestId}: Found server {Endpoint} ({Scanned} scanned)...")]
        internal static partial void PortScanResult(this ILogger logger, string? requestId,
            string? endpoint, int? scanned);

        [LoggerMessage(EventId = EventClass + 12, Level = LogLevel.Information,
            Message = "{RequestId}: {Scanned} ports scanned - {Discovered} discovered ({Active} probes active)...")]
        internal static partial void PortScanProgress(this ILogger logger, string? requestId,
            int? scanned, int? discovered, int? active);

        [LoggerMessage(EventId = EventClass + 13, Level = LogLevel.Information,
            Message = "{RequestId}: Found {Count} ports on servers ({Scanned} scanned)...")]
        internal static partial void PortScanFinished(this ILogger logger, string? requestId,
            int? count, int? scanned);

        [LoggerMessage(EventId = EventClass + 14, Level = LogLevel.Information,
            Message = "{RequestId}: Searching {Count} discovery urls for endpoints...")]
        internal static partial void ServerDiscoveryStarted(this ILogger logger, string? requestId,
            int? count);

        [LoggerMessage(EventId = EventClass + 15, Level = LogLevel.Information,
            Message = "{RequestId}: Trying to find endpoints on {Details}...")]
        internal static partial void EndpointsDiscoveryStarted(this ILogger logger, string? requestId,
            string? details);

        [LoggerMessage(EventId = EventClass + 16, Level = LogLevel.Information,
            Message = "{RequestId}: No endpoints discovered on {Details}.")]
        internal static partial void NoEndpointsDiscovered(this ILogger logger, string? requestId,
            string? details);

        [LoggerMessage(EventId = EventClass + 17, Level = LogLevel.Information,
            Message = "{RequestId}: Found {Count} endpoints on {Details}.")]
        internal static partial void EndpointsDiscoveryFinished(this ILogger logger, string? requestId,
            int count, string? details);

        [LoggerMessage(EventId = EventClass + 18, Level = LogLevel.Information,
            Message = "{RequestId}: Found total of {Count} servers ...")]
        internal static partial void ServerDiscoveryFinished(this ILogger logger, string? requestId,
            int? count);
    }
}
