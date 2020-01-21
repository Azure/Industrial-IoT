// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Edge.Discovery.Services {
    using Microsoft.Azure.IIoT.OpcUa.Registry.Models;
    using Serilog;
    using System;
    using System.Collections.Generic;
    using System.Net;

    /// <summary>
    /// Discovery progress logger
    /// </summary>
    public class ProgressLogger : IDiscoveryProgress {

        /// <summary>
        /// Create listener
        /// </summary>
        /// <param name="logger"></param>
        public ProgressLogger(ILogger logger) {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc/>
        public void OnDiscoveryPending(DiscoveryRequestModel request,
            int pending) {
            Send(new DiscoveryProgressModel {
                EventType = DiscoveryProgressType.Pending,
                Request = request,
                Total = pending
            });
        }

        /// <inheritdoc/>
        public void OnDiscoveryStarted(DiscoveryRequestModel request) {
            Send(new DiscoveryProgressModel {
                EventType = DiscoveryProgressType.Started,
                Request = request
            });
        }

        /// <inheritdoc/>
        public void OnDiscoveryCancelled(DiscoveryRequestModel request) {
            Send(new DiscoveryProgressModel {
                TimeStamp = DateTime.UtcNow,
                EventType = DiscoveryProgressType.Cancelled,
                Request = request
            });
        }

        /// <inheritdoc/>
        public void OnDiscoveryError(DiscoveryRequestModel request,
            Exception ex) {
            Send(new DiscoveryProgressModel {
                TimeStamp = DateTime.UtcNow,
                EventType = DiscoveryProgressType.Error,
                Request = request,
                Result = ex.Message,
                ResultDetails = new Dictionary<string, string> {
                    ["exception"] = ex.ToString()
                },
            });
        }

        /// <inheritdoc/>
        public void OnDiscoveryFinished(DiscoveryRequestModel request) {
            Send(new DiscoveryProgressModel {
                TimeStamp = DateTime.UtcNow,
                EventType = DiscoveryProgressType.Finished,
                Request = request
            });
        }

        /// <inheritdoc/>
        public void OnNetScanStarted(DiscoveryRequestModel request,
            int workers, int progress, int total) {
            Send(new DiscoveryProgressModel {
                TimeStamp = DateTime.UtcNow,
                EventType = DiscoveryProgressType.NetworkScanStarted,
                Workers = workers,
                Progress = progress,
                Total = total,
                Request = request
            });
        }

        /// <inheritdoc/>
        public void OnNetScanResult(DiscoveryRequestModel request,
            int workers, int progress, int total, int discovered, IPAddress address) {
            Send(new DiscoveryProgressModel {
                TimeStamp = DateTime.UtcNow,
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
            int workers, int progress, int total, int discovered) {
            Send(new DiscoveryProgressModel {
                TimeStamp = DateTime.UtcNow,
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
            int workers, int progress, int total, int discovered) {
            Send(new DiscoveryProgressModel {
                TimeStamp = DateTime.UtcNow,
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
            int workers, int progress, int total) {
            Send(new DiscoveryProgressModel {
                TimeStamp = DateTime.UtcNow,
                EventType = DiscoveryProgressType.PortScanStarted,
                Workers = workers,
                Progress = progress,
                Total = total,
                Request = request
            });
        }

        /// <inheritdoc/>
        public void OnPortScanResult(DiscoveryRequestModel request,
            int workers, int progress, int total, int discovered, IPEndPoint result) {
            Send(new DiscoveryProgressModel {
                TimeStamp = DateTime.UtcNow,
                EventType = DiscoveryProgressType.PortScanResult,
                Workers = workers,
                Progress = progress,
                Total = total,
                Discovered = discovered,
                Result = result.ToString(),
                Request = request
            });
        }

        /// <inheritdoc/>
        public void OnPortScanProgress(DiscoveryRequestModel request,
            int workers, int progress, int total, int discovered) {
            Send(new DiscoveryProgressModel {
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
            int workers, int progress, int total, int discovered) {
            Send(new DiscoveryProgressModel {
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
            int workers, int progress, int total) {
            Send(new DiscoveryProgressModel {
                EventType = DiscoveryProgressType.ServerDiscoveryStarted,
                Workers = workers,
                Progress = progress,
                Total = total,
                Request = request
            });
        }

        /// <inheritdoc/>
        public void OnFindEndpointsStarted(DiscoveryRequestModel request,
            int workers, int progress, int total, int discovered, Uri url, IPAddress address) {
            Send(new DiscoveryProgressModel {
                EventType = DiscoveryProgressType.EndpointsDiscoveryStarted,
                RequestDetails = new Dictionary<string, string> {
                    ["url"] = url.ToString(),
                    ["address"] = address.ToString()
                },
                Workers = workers,
                Progress = progress,
                Total = total,
                Discovered = discovered,
                Request = request.Clone()
            });
        }

        /// <inheritdoc/>
        public void OnFindEndpointsFinished(DiscoveryRequestModel request,
            int workers, int progress, int total, int discovered, Uri url,
            IPAddress address, int endpoints) {
            Send(new DiscoveryProgressModel {
                EventType = DiscoveryProgressType.EndpointsDiscoveryFinished,
                RequestDetails = new Dictionary<string, string> {
                    ["url"] = url.ToString(),
                    ["address"] = address.ToString()
                },
                Workers = workers,
                Progress = progress,
                Total = total,
                Discovered = discovered,
                Result = endpoints.ToString(),
                Request = request
            });
        }

        /// <inheritdoc/>
        public void OnServerDiscoveryFinished(DiscoveryRequestModel request,
            int workers, int progress, int total, int discovered) {
            Send(new DiscoveryProgressModel {
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
        protected virtual void Send(DiscoveryProgressModel progress) {
            progress.TimeStamp = DateTime.UtcNow;
            _logger.LogProgress(progress);
        }

        private readonly ILogger _logger;
    }
}
