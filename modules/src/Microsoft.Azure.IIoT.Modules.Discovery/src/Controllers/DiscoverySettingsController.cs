// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Modules.Discovery.Controllers {
    using Microsoft.Azure.IIoT.Module.Framework;
    using Microsoft.Azure.IIoT.OpcUa.Edge;
    using Microsoft.Azure.IIoT.OpcUa.Registry.Models;
    using Microsoft.Azure.IIoT.Hub;
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    /// <summary>
    /// Discovery settings controller
    /// </summary>
    [Version(1)]
    [Version(2)]
    public class DiscoverySettingsController : ISettingsController {

        /// <summary>
        /// Enable or disable discovery on supervisor
        /// </summary>
        public DiscoveryMode? Discovery {
            set => _mode =
                value ?? DiscoveryMode.Off;
            get => _discovery.Mode;
        }

        /// <summary>
        /// Address ranges to scan (null == all wired)
        /// </summary>
        public string AddressRangesToScan {
            set => _config.AddressRangesToScan =
                string.IsNullOrEmpty(value) ? null : value;
            get => _discovery.Configuration.AddressRangesToScan;
        }

        /// <summary>
        /// Network probe timeout.
        /// </summary>
        public TimeSpan? NetworkProbeTimeout {
            set => _config.NetworkProbeTimeout =
                value;
            get => _discovery.Configuration.NetworkProbeTimeout;
        }

        /// <summary>
        /// Max network probes that should ever run.
        /// </summary>
        public int? MaxNetworkProbes {
            set => _config.MaxNetworkProbes =
                value;
            get => _discovery.Configuration.MaxNetworkProbes;
        }

        /// <summary>
        /// Port ranges to scan (null == all unassigned)
        /// </summary>
        public string PortRangesToScan {
            set => _config.PortRangesToScan =
                string.IsNullOrEmpty(value) ? null : value;
            get => _discovery.Configuration.PortRangesToScan;
        }

        /// <summary>
        /// Port probe timeout
        /// </summary>
        public TimeSpan? PortProbeTimeout {
            set => _config.PortProbeTimeout =
                value;
            get => _discovery.Configuration.PortProbeTimeout;
        }

        /// <summary>
        /// Max port probes that should ever run.
        /// </summary>
        public int? MaxPortProbes {
            set => _config.MaxPortProbes =
                value;
            get => _discovery.Configuration.MaxPortProbes;
        }

        /// <summary>
        /// Probes that must always be there as percent of max.
        /// </summary>
        public int? MinPortProbesPercent {
            set => _config.MinPortProbesPercent =
                value;
            get => _discovery.Configuration.MinPortProbesPercent;
        }

        /// <summary>
        /// Delay time between discovery sweeps
        /// </summary>
        public TimeSpan? IdleTimeBetweenScans {
            set => _config.IdleTimeBetweenScans =
                value;
            get => _discovery.Configuration.IdleTimeBetweenScans;
        }

        /// <summary>
        /// Discovery Urls to scan
        /// </summary>
        public Dictionary<string, string> DiscoveryUrls {
            set => _config.DiscoveryUrls = value.DecodeAsList();
            get => _config.DiscoveryUrls.EncodeAsDictionary();
        }

        /// <summary>
        /// Create controller with service
        /// </summary>
        /// <param name="discovery"></param>
        public DiscoverySettingsController(IScannerServices discovery) {
            _discovery = discovery ??
                throw new ArgumentNullException(nameof(discovery));

            _config = new DiscoveryConfigModel();
            _mode = DiscoveryMode.Off;
        }

        /// <summary>
        /// Called to update discovery configuration and schedule new scan
        /// </summary>
        /// <returns></returns>
        public async Task ApplyAsync() {
            await _discovery.ConfigureAsync(_mode, _config.Clone());
            await _discovery.ScanAsync();
        }

        private readonly IScannerServices _discovery;
        private readonly DiscoveryConfigModel _config;
        private DiscoveryMode _mode;
    }
}
