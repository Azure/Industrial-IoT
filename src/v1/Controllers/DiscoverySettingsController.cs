// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Modules.OpcUa.Twin.v1.Supervisor {
    using Microsoft.Azure.IIoT.Module.Framework;
    using Microsoft.Azure.IIoT.OpcUa.Edge;
    using Microsoft.Azure.IIoT.OpcUa.Registry.Models;
    using Serilog;
    using Newtonsoft.Json.Linq;
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// Discovery settings controller
    /// </summary>
    [Version(1)]
    public class DiscoverySettingsController : ISettingsController {

        /// <summary>
        /// Enable or disable discovery on supervisor
        /// </summary>
        public JToken Discovery {
            set {
                switch (value.Type) {
                    case JTokenType.Null:
                        _discovery.Mode = DiscoveryMode.Off;
                        break;
                    case JTokenType.Boolean:
                        _discovery.Mode = (bool)value ?
                            DiscoveryMode.Local : DiscoveryMode.Off;
                        break;
                    case JTokenType.String:
                        DiscoveryMode mode;
                        if (Enum.TryParse((string)value, true, out mode)) {
                            _discovery.Mode = mode;
                            break;
                        }
                        throw new ArgumentException("bad mode value");
                    default:
                        throw new NotSupportedException("bad key value");
                }
            }
            get => JToken.FromObject(_discovery.Mode);
        }

        /// <summary>
        /// Address ranges to scan (null == all wired)
        /// </summary>
        public string AddressRangesToScan {
            set => _discovery.Configuration.AddressRangesToScan =
                string.IsNullOrEmpty(value) ? null : value;
            get => _discovery.Configuration.AddressRangesToScan;
        }

        /// <summary>
        /// Network probe timeout.
        /// </summary>
        public JToken NetworkProbeTimeout {
            set => _discovery.Configuration.NetworkProbeTimeout =
                value?.ToObject<TimeSpan>();
            get => _discovery.Configuration.NetworkProbeTimeout;
        }

        /// <summary>
        /// Max network probes that should ever run.
        /// </summary>
        public JToken MaxNetworkProbes {
            set => _discovery.Configuration.MaxNetworkProbes =
                value?.ToObject<int>();
            get => _discovery.Configuration.MaxNetworkProbes;
        }

        /// <summary>
        /// Port ranges to scan (null == all unassigned)
        /// </summary>
        public string PortRangesToScan {
            set => _discovery.Configuration.PortRangesToScan =
                string.IsNullOrEmpty(value) ? null : value;
            get => _discovery.Configuration.PortRangesToScan;
        }

        /// <summary>
        /// Port probe timeout
        /// </summary>
        public JToken PortProbeTimeout {
            set => _discovery.Configuration.PortProbeTimeout =
                value?.ToObject<TimeSpan>();
            get => _discovery.Configuration.PortProbeTimeout;
        }

        /// <summary>
        /// Max port probes that should ever run.
        /// </summary>
        public JToken MaxPortProbes {
            set => _discovery.Configuration.MaxPortProbes =
                value?.ToObject<int>();
            get => _discovery.Configuration.MaxPortProbes;
        }

        /// <summary>
        /// Probes that must always be there as percent of max.
        /// </summary>
        public JToken MinPortProbesPercent {
            set => _discovery.Configuration.MinPortProbesPercent =
                value?.ToObject<int>();
            get => _discovery.Configuration.MinPortProbesPercent;
        }

        /// <summary>
        /// Delay time between discovery sweeps
        /// </summary>
        public JToken IdleTimeBetweenScans {
            set => _discovery.Configuration.IdleTimeBetweenScans =
                value?.ToObject<TimeSpan>();
            get => _discovery.Configuration.IdleTimeBetweenScans;
        }

        /// <summary>
        /// Create controller with service
        /// </summary>
        /// <param name="discovery"></param>
        /// <param name="logger"></param>
        public DiscoverySettingsController(IScannerServices discovery, ILogger logger) {
            _discovery = discovery ?? throw new ArgumentNullException(nameof(discovery));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Called to update discovery configuration
        /// </summary>
        /// <returns></returns>
        public Task ApplyAsync() => _discovery.ScanAsync();

        private readonly IScannerServices _discovery;
        private readonly ILogger _logger;
    }
}
