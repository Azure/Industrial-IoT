// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Edge.Discovery {
    using Microsoft.Azure.IIoT.OpcUa.Registry.Models;
    using Microsoft.Azure.IIoT.Module;
    using Microsoft.Azure.IIoT.Net;
    using Microsoft.Azure.IIoT.Tasks;
    using Microsoft.Azure.IIoT.Utils;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using Serilog;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Text;
    using System.Threading.Tasks;

    /// <summary>
    /// Discovery progress message sender
    /// </summary>
    public class DiscoveryMessagePublisher : DiscoveryLogger, IDiscoveryListener {

        /// <summary>
        /// Create listener
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="events"></param>
        /// <param name="processor"></param>
        public DiscoveryMessagePublisher(ILogger logger, IEventEmitter events,
            ITaskProcessor processor) : base(logger) {
            _events = events ?? throw new ArgumentNullException(nameof(events));
            _processor = processor ?? throw new ArgumentNullException(nameof(processor));
            _supervisorId = SupervisorModelEx.CreateSupervisorId(_events.DeviceId,
                _events.ModuleId);
        }

        /// <inheritdoc/>
        public override void OnDiscoveryStarted(DiscoveryRequestModel request) {
            Send(new DiscoveryMessageModel {
                SupervisorId = _supervisorId,
                Event = DiscoveryMessageType.Started,
                Request = request
            });
            base.OnDiscoveryStarted(request);
        }

        /// <inheritdoc/>
        public override void OnDiscoveryCancelled(DiscoveryRequestModel request) {
            Send(new DiscoveryMessageModel {
                TimeStamp = DateTime.UtcNow,
                SupervisorId = _supervisorId,
                Event = DiscoveryMessageType.Cancelled,
                Request = request
            });
            base.OnDiscoveryCancelled(request);
        }

        /// <inheritdoc/>
        public override void OnDiscoveryError(DiscoveryRequestModel request,
            Exception ex) {
            Send(new DiscoveryMessageModel {
                TimeStamp = DateTime.UtcNow,
                SupervisorId = _supervisorId,
                Event = DiscoveryMessageType.Error,
                Request = request,
                Result = JToken.FromObject(ex)
            });
            base.OnDiscoveryError(request, ex);
        }

        /// <inheritdoc/>
        public override void OnDiscoveryFinished(DiscoveryRequestModel request) {
            Send(new DiscoveryMessageModel {
                TimeStamp = DateTime.UtcNow,
                SupervisorId = _supervisorId,
                Event = DiscoveryMessageType.Finished,
                Request = request
            });
            base.OnDiscoveryFinished(request);
        }

        /// <inheritdoc/>
        public override void OnNetScanStarted(DiscoveryRequestModel request,
            IScanner scanner) {
            Send(new DiscoveryMessageModel {
                TimeStamp = DateTime.UtcNow,
                SupervisorId = _supervisorId,
                Event = DiscoveryMessageType.NetworkScanStarted,
                Workers = scanner.ActiveProbes,
                Progress = scanner.ScanCount,
                Request = request
            });
            base.OnNetScanStarted(request, scanner);
        }

        /// <inheritdoc/>
        public override void OnNetScanResult(DiscoveryRequestModel request,
            IScanner scanner, IPAddress address) {
            Send(new DiscoveryMessageModel {
                TimeStamp = DateTime.UtcNow,
                SupervisorId = _supervisorId,
                Event = DiscoveryMessageType.NetworkScanResult,
                Workers = scanner.ActiveProbes,
                Progress = scanner.ScanCount,
                Result = JToken.FromObject(address.ToString()),
                Request = request
            });
            base.OnNetScanResult(request, scanner, address);
        }

        /// <inheritdoc/>
        public override void OnNetScanProgress(DiscoveryRequestModel request,
            IScanner scanner, int count) {
            Send(new DiscoveryMessageModel {
                TimeStamp = DateTime.UtcNow,
                SupervisorId = _supervisorId,
                Event = DiscoveryMessageType.NetworkScanProgress,
                Workers = scanner.ActiveProbes,
                Progress = scanner.ScanCount,
                Discovered = count,
                Request = request
            });
            base.OnNetScanProgress(request, scanner, count);
        }

        /// <inheritdoc/>
        public override void OnNetScanFinished(DiscoveryRequestModel request,
            IScanner scanner, int count) {
            Send(new DiscoveryMessageModel {
                TimeStamp = DateTime.UtcNow,
                SupervisorId = _supervisorId,
                Event = DiscoveryMessageType.NetworkScanFinished,
                Workers = scanner.ActiveProbes,
                Progress = scanner.ScanCount,
                Discovered = count,
                Request = request
            });
            base.OnNetScanFinished(request, scanner, count);
        }

        /// <inheritdoc/>
        public override void OnPortScanStart(DiscoveryRequestModel request, IScanner scanner) {
            Send(new DiscoveryMessageModel {
                TimeStamp = DateTime.UtcNow,
                SupervisorId = _supervisorId,
                Event = DiscoveryMessageType.PortScanStarted,
                Workers = scanner.ActiveProbes,
                Progress = scanner.ScanCount,
                Request = request
            });
            base.OnPortScanStart(request, scanner);
        }

        /// <inheritdoc/>
        public override void OnPortScanResult(DiscoveryRequestModel request,
            IScanner scanner, IPEndPoint result) {
            Send(new DiscoveryMessageModel {
                TimeStamp = DateTime.UtcNow,
                SupervisorId = _supervisorId,
                Event = DiscoveryMessageType.PortScanResult,
                Workers = scanner.ActiveProbes,
                Progress = scanner.ScanCount,
                Result = JToken.FromObject(result.ToString()),
                Request = request
            });
            base.OnPortScanResult(request, scanner, result);
        }

        /// <inheritdoc/>
        public override void OnPortScanProgress(DiscoveryRequestModel request,
            IScanner scanner, int count) {
            Send(new DiscoveryMessageModel {
                TimeStamp = DateTime.UtcNow,
                SupervisorId = _supervisorId,
                Event = DiscoveryMessageType.PortScanProgress,
                Workers = scanner.ActiveProbes,
                Progress = scanner.ScanCount,
                Discovered = count,
                Request = request
            });
            base.OnPortScanProgress(request, scanner, count);
        }

        /// <inheritdoc/>
        public override void OnPortScanFinished(DiscoveryRequestModel request,
            IScanner scanner, int count) {
            Send(new DiscoveryMessageModel {
                TimeStamp = DateTime.UtcNow,
                SupervisorId = _supervisorId,
                Event = DiscoveryMessageType.PortScanFinished,
                Workers = scanner.ActiveProbes,
                Progress = scanner.ScanCount,
                Discovered = count,
                Request = request
            });
            base.OnPortScanFinished(request, scanner, count);
        }

        /// <inheritdoc/>
        public override void OnServerDiscoveryStarted(DiscoveryRequestModel request,
            IDictionary<IPEndPoint, Uri> discoveryUrls) {
            Send(new DiscoveryMessageModel {
                TimeStamp = DateTime.UtcNow,
                SupervisorId = _supervisorId,
                Event = DiscoveryMessageType.ServerDiscoveryStarted,
                Discovered = discoveryUrls.Count,
                Request = request
            });
            base.OnServerDiscoveryStarted(request, discoveryUrls);
        }

        /// <inheritdoc/>
        public override void OnFindEndpointsStarted(DiscoveryRequestModel request,
            Uri url, IPAddress address) {
            var cloned = request.Clone();
            Send(new DiscoveryMessageModel {
                TimeStamp = DateTime.UtcNow,
                SupervisorId = _supervisorId,
                Event = DiscoveryMessageType.EndpointsDiscoveryStarted,
                RequestDetails = JToken.FromObject(new { url, address = address.ToString() }),
                Discovered = 1,
                Request = request.Clone()
            });
            base.OnFindEndpointsStarted(request, url, address);
        }

        /// <inheritdoc/>
        public override void OnFindEndpointsFinished(DiscoveryRequestModel request,
            Uri url, IPAddress address, int found) {
            Send(new DiscoveryMessageModel {
                TimeStamp = DateTime.UtcNow,
                SupervisorId = _supervisorId,
                Event = DiscoveryMessageType.EndpointsDiscoveryFinished,
                RequestDetails = JToken.FromObject(new { url, address = address.ToString() }),
                Discovered = found,
                Request = request
            });
            base.OnFindEndpointsFinished(request, url, address, found);
        }

        /// <inheritdoc/>
        public override void OnServerDiscoveryFinished(DiscoveryRequestModel request,
            int found) {
            Send(new DiscoveryMessageModel {
                TimeStamp = DateTime.UtcNow,
                SupervisorId = _supervisorId,
                Event = DiscoveryMessageType.ServerDiscoveryFinished,
                Discovered = found,
                Request = request
            });
            base.OnServerDiscoveryFinished(request, found);
        }

        /// <summary>
        /// Send message
        /// </summary>
        /// <param name="message"></param>
        private void Send(DiscoveryMessageModel message) {
            _processor.TrySchedule(() => SendAsync(message));
        }

        /// <summary>
        /// Send message
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        private Task SendAsync(DiscoveryMessageModel message) {
            return Try.Async(() => _events.SendAsync(
                Encoding.UTF8.GetBytes(JsonConvertEx.SerializeObject(message)),
                ContentTypes.DiscoveryMessage));
        }

        private readonly IEventEmitter _events;
        private readonly ITaskProcessor _processor;
        private readonly string _supervisorId;
    }
}
