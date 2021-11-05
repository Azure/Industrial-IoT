// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.App.Models {
    using Microsoft.Azure.IIoT.OpcUa.Api.Registry.Models;
    using System;
    using System.Collections.Generic;

    public class DiscovererInfo {

        /// <summary>
        /// Discoverer models.
        /// </summary>
        public DiscovererApiModel DiscovererModel { get; set; }

        /// <summary>
        /// Patch
        /// </summary>
        public DiscoveryConfigApiModel Patch { get; set; } = new DiscoveryConfigApiModel();

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

        /// <summary>
        /// is Ad-Hoc Discovery.
        /// </summary>
        public bool isAdHocDiscovery { get; set; } = false;

        /// <summary>
        /// Id of discovery request
        /// </summary>
        public string DiscoveryRequestId { get; set; }

        // Bind Proxies

        /// <summary>
        /// Network probe timeout
        /// </summary>
        public string EffectiveNetworkProbeTimeout {
            get => (DiscovererModel.DiscoveryConfig?.NetworkProbeTimeout ?? TimeSpan.MinValue)
                == TimeSpan.MinValue ?
                null : DiscovererModel.DiscoveryConfig.NetworkProbeTimeout.ToString();
        }

        /// <summary>
        /// Max network probes that should ever run.
        /// </summary>
        public string EffectiveMaxNetworkProbes {
            get => (DiscovererModel.DiscoveryConfig?.MaxNetworkProbes ?? -1) < 0 ?
                null : DiscovererModel.DiscoveryConfig.MaxNetworkProbes.ToString();
        }

        /// <summary>
        /// Port probe timeout
        /// </summary>
        public string EffectivePortProbeTimeout {
            get => (DiscovererModel.DiscoveryConfig?.PortProbeTimeout ?? TimeSpan.MinValue)
                == TimeSpan.MinValue ?
                null : DiscovererModel.DiscoveryConfig.PortProbeTimeout.ToString();
        }

        /// <summary>
        /// Max port probes that should ever run.
        /// </summary>
        public string EffectiveMaxPortProbes {
            get => (DiscovererModel.DiscoveryConfig?.MaxPortProbes ?? -1) < 0 ?
                null : DiscovererModel.DiscoveryConfig.MaxPortProbes.ToString();
        }

        /// <summary>
        /// Delay time between discovery sweeps in seconds
        /// </summary>
        public string EffectiveIdleTimeBetweenScans {
            get => (DiscovererModel.DiscoveryConfig?.IdleTimeBetweenScans ?? TimeSpan.MinValue)
                == TimeSpan.MinValue ?
                null : DiscovererModel.DiscoveryConfig.IdleTimeBetweenScans.ToString();
        }

        /// <summary>
        /// Address ranges to scan (null == all wired nics)
        /// </summary>
        public string EffectiveAddressRangesToScan {
            get => string.IsNullOrEmpty(DiscovererModel.DiscoveryConfig?.AddressRangesToScan) ?
                null : DiscovererModel.DiscoveryConfig.AddressRangesToScan;
        }

        /// <summary>
        /// Port ranges to scan (null == all unassigned)
        /// </summary>
        public string EffectivePortRangesToScan {
            get => string.IsNullOrEmpty(DiscovererModel.DiscoveryConfig?.PortRangesToScan) ?
                null : DiscovererModel.DiscoveryConfig.PortRangesToScan;
        }

        /// <summary>
        /// List of preset discovery urls to use
        /// </summary>
        public List<string> EffectiveDiscoveryUrls {
            get => DiscovererModel.DiscoveryConfig?.DiscoveryUrls == null ?
                new List<string>() : DiscovererModel.DiscoveryConfig.DiscoveryUrls;
        }

        /// <summary>
        /// List of locales to filter with during discovery
        /// </summary>
        public List<string> EffectiveLocales {
            get => DiscovererModel.DiscoveryConfig?.Locales == null ?
                new List<string>() : DiscovererModel.DiscoveryConfig.Locales;
        }

        public bool TryUpdateData(DiscovererInfoRequested input) {
            try {
                DiscovererModel.RequestedConfig ??= new DiscoveryConfigApiModel();

                Patch.NetworkProbeTimeout = DiscovererModel.RequestedConfig.NetworkProbeTimeout =
                    string.IsNullOrWhiteSpace(input.RequestedNetworkProbeTimeout) ? TimeSpan.MinValue :
                    TimeSpan.Parse(input.RequestedNetworkProbeTimeout);

                Patch.MaxNetworkProbes = DiscovererModel.RequestedConfig.MaxNetworkProbes =
                    string.IsNullOrWhiteSpace(input.RequestedMaxNetworkProbes) ? -1 :
                    int.Parse(input.RequestedMaxNetworkProbes);

                Patch.PortProbeTimeout = DiscovererModel.RequestedConfig.PortProbeTimeout =
                    string.IsNullOrWhiteSpace(input.RequestedPortProbeTimeout) ? TimeSpan.MinValue :
                    TimeSpan.Parse(input.RequestedPortProbeTimeout);

                Patch.MaxPortProbes = DiscovererModel.RequestedConfig.MaxPortProbes =
                    string.IsNullOrWhiteSpace(input.RequestedMaxPortProbes) ? -1 :
                    int.Parse(input.RequestedMaxPortProbes);

                Patch.IdleTimeBetweenScans = DiscovererModel.RequestedConfig.IdleTimeBetweenScans =
                    string.IsNullOrWhiteSpace(input.RequestedIdleTimeBetweenScans) ? TimeSpan.MinValue :
                    TimeSpan.Parse(input.RequestedIdleTimeBetweenScans);

                Patch.AddressRangesToScan = DiscovererModel.RequestedConfig.AddressRangesToScan =
                    string.IsNullOrWhiteSpace(input.RequestedAddressRangesToScan) ? string.Empty :
                    input.RequestedAddressRangesToScan;

                Patch.PortRangesToScan = DiscovererModel.RequestedConfig.PortRangesToScan =
                    string.IsNullOrWhiteSpace(input.RequestedPortRangesToScan) ? string.Empty :
                    input.RequestedPortRangesToScan;

                Patch.DiscoveryUrls = DiscovererModel.RequestedConfig.DiscoveryUrls =
                    input.RequestedDiscoveryUrls ?? new List<string>();

                Patch.Locales = DiscovererModel.RequestedConfig.Locales =
                    input.RequestedDiscoveryUrls ?? new List<string>();

                return true;
            }
            catch (Exception) {
                return false;
            }
        }
    }
}
