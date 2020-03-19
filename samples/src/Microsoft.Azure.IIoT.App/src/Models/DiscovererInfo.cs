// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.App.Services {
    using Microsoft.Azure.IIoT.OpcUa.Api.Registry.Models;
    using System;

    public class DiscovererInfo {

        /// <summary>
        /// Discoverer models.
        /// </summary>
        public DiscovererApiModel DiscovererModel { get; set; }

        /// <summary>
        /// scan status.
        /// </summary>
        public bool ScanStatus { get; set; }

        /// <summary>
        /// is scan searching.
        /// </summary>
        public bool IsSearching { get; set; }

        /// <summary>
        /// Discoverer has found apps.
        /// </summary>
        public bool HasApplication { get; set; }

        // Bind Proxies

        /// <summary>
        /// Network probe timeout
        /// </summary>
        public string NetworkProbeTimeout {
            get => (DiscovererModel.DiscoveryConfig?.NetworkProbeTimeout ?? TimeSpan.Zero)
                == TimeSpan.Zero ?
                null : DiscovererModel.DiscoveryConfig.NetworkProbeTimeout.ToString();
            set {
                if (DiscovererModel.DiscoveryConfig == null) {
                    DiscovererModel.DiscoveryConfig = new DiscoveryConfigApiModel();
                }
                DiscovererModel.DiscoveryConfig.NetworkProbeTimeout =
                    string.IsNullOrEmpty(value) ? TimeSpan.Zero :
                    TimeSpan.Parse(value);
            }
        }

        /// <summary>
        /// Max network probes that should ever run.
        /// </summary>
        public string MaxNetworkProbes {
            get => (DiscovererModel.DiscoveryConfig?.MaxNetworkProbes ?? 0) == 0 ?
                null : DiscovererModel.DiscoveryConfig.MaxNetworkProbes.ToString();
            set {
                if (DiscovererModel.DiscoveryConfig == null) {
                    DiscovererModel.DiscoveryConfig = new DiscoveryConfigApiModel();
                }
                DiscovererModel.DiscoveryConfig.MaxNetworkProbes =
                    string.IsNullOrEmpty(value) ? 0 : int.Parse(value);
            }
        }

        /// <summary>
        /// Port probe timeout
        /// </summary>
        public string PortProbeTimeout {
            get => (DiscovererModel.DiscoveryConfig?.PortProbeTimeout ?? TimeSpan.Zero)
                == TimeSpan.Zero ?
                null : DiscovererModel.DiscoveryConfig.PortProbeTimeout.ToString();
            set {
                if (DiscovererModel.DiscoveryConfig == null) {
                    DiscovererModel.DiscoveryConfig = new DiscoveryConfigApiModel();
                }
                DiscovererModel.DiscoveryConfig.PortProbeTimeout =
                    string.IsNullOrEmpty(value) ? TimeSpan.Zero :
                    TimeSpan.Parse(value);
            }
        }

        /// <summary>
        /// Max port probes that should ever run.
        /// </summary>
        public string MaxPortProbes {
            get => (DiscovererModel.DiscoveryConfig?.MaxPortProbes ?? 0) == 0 ?
                null : DiscovererModel.DiscoveryConfig.MaxPortProbes.ToString();
            set {
                if (DiscovererModel.DiscoveryConfig == null) {
                    DiscovererModel.DiscoveryConfig = new DiscoveryConfigApiModel();
                }
                DiscovererModel.DiscoveryConfig.MaxPortProbes =
                    string.IsNullOrEmpty(value) ? 0 : int.Parse(value);
            }
        }

        /// <summary>
        /// Delay time between discovery sweeps in seconds
        /// </summary>
        public string IdleTimeBetweenScans {
            get => (DiscovererModel.DiscoveryConfig?.IdleTimeBetweenScans ?? TimeSpan.Zero)
                == TimeSpan.Zero ?
                null : DiscovererModel.DiscoveryConfig.IdleTimeBetweenScans.ToString();
            set {
                if (DiscovererModel.DiscoveryConfig == null) {
                    DiscovererModel.DiscoveryConfig = new DiscoveryConfigApiModel();
                }
                DiscovererModel.DiscoveryConfig.IdleTimeBetweenScans =
                    string.IsNullOrEmpty(value) ? TimeSpan.Zero :
                    TimeSpan.Parse(value);
            }
        }
    }
}
