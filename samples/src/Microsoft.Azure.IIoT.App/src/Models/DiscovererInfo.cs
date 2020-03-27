// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.App.Services {
    using Microsoft.Azure.IIoT.OpcUa.Api.Registry.Models;
    using System;
    using System.Collections.Generic;
    using System.Linq;

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

        /// <summary>
        /// Network probe timeout
        /// </summary>
        public string RequestedNetworkProbeTimeout {
            get => (DiscovererModel.RequestedConfig?.NetworkProbeTimeout ?? TimeSpan.MinValue)
                == TimeSpan.MinValue ?
                null : DiscovererModel.RequestedConfig.NetworkProbeTimeout.ToString();
            set {
                Patch.NetworkProbeTimeout = DiscovererModel.RequestedConfig.NetworkProbeTimeout =
                    string.IsNullOrWhiteSpace(value) ? TimeSpan.MinValue :
                    TimeSpan.Parse(value);
            }
        }

        /// <summary>
        /// Max network probes that should ever run.
        /// </summary>
        public string RequestedMaxNetworkProbes {
            get => (DiscovererModel.RequestedConfig?.MaxNetworkProbes ?? -1) < 0 ?
                null : DiscovererModel.RequestedConfig.MaxNetworkProbes.ToString();
            set {
                Patch.MaxNetworkProbes = DiscovererModel.RequestedConfig.MaxNetworkProbes =
                    string.IsNullOrWhiteSpace(value) ? -1 : int.Parse(value);
            }
        }

        /// <summary>
        /// Port probe timeout
        /// </summary>
        public string RequestedPortProbeTimeout {
            get => (DiscovererModel.RequestedConfig?.PortProbeTimeout ?? TimeSpan.MinValue)
                == TimeSpan.MinValue ?
                null : DiscovererModel.RequestedConfig.PortProbeTimeout.ToString();
            set {
                Patch.PortProbeTimeout = DiscovererModel.RequestedConfig.PortProbeTimeout =
                    string.IsNullOrWhiteSpace(value) ? TimeSpan.MinValue :
                    TimeSpan.Parse(value);
            }
        }

        /// <summary>
        /// Max port probes that should ever run.
        /// </summary>
        public string RequestedMaxPortProbes {
            get => (DiscovererModel.RequestedConfig?.MaxPortProbes ?? -1) < 0 ?
                null : DiscovererModel.RequestedConfig.MaxPortProbes.ToString();
            set {
                Patch.MaxPortProbes = DiscovererModel.RequestedConfig.MaxPortProbes =
                    string.IsNullOrWhiteSpace(value) ? -1 : int.Parse(value);
            }
        }

        /// <summary>
        /// Delay time between discovery sweeps in seconds
        /// </summary>
        public string RequestedIdleTimeBetweenScans {
            get => (DiscovererModel.RequestedConfig?.IdleTimeBetweenScans ?? TimeSpan.MinValue)
                == TimeSpan.MinValue ?
                null : DiscovererModel.RequestedConfig.IdleTimeBetweenScans.ToString();
            set {
                Patch.IdleTimeBetweenScans = DiscovererModel.RequestedConfig.IdleTimeBetweenScans =
                    string.IsNullOrWhiteSpace(value) ? TimeSpan.MinValue :
                    TimeSpan.Parse(value);
            }
        }

        /// <summary>
        /// Address ranges to scan (null == all wired nics)
        /// </summary>
        public string RequestedAddressRangesToScan {
            get => string.IsNullOrEmpty(DiscovererModel.RequestedConfig?.AddressRangesToScan) ?
                null : DiscovererModel.RequestedConfig.AddressRangesToScan;
            set {
                Patch.AddressRangesToScan = DiscovererModel.RequestedConfig.AddressRangesToScan =
                    string.IsNullOrWhiteSpace(value) ? string.Empty : value;
            }
        }

        /// <summary>
        /// Port ranges to scan (null == all unassigned)
        /// </summary>
        public string RequestedPortRangesToScan {
            get => string.IsNullOrEmpty(DiscovererModel.RequestedConfig?.PortRangesToScan) ?
                null : DiscovererModel.RequestedConfig.PortRangesToScan;
            set {
                Patch.PortRangesToScan = DiscovererModel.RequestedConfig.PortRangesToScan =
                    string.IsNullOrWhiteSpace(value) ? string.Empty : value;
            }
        }

        /// <summary>
        /// List of preset discovery urls to use
        /// </summary>
        public List<string> RequestedDiscoveryUrls {
            get => DiscovererModel.RequestedConfig?.DiscoveryUrls == null ?
                new List<string>() : DiscovererModel.RequestedConfig.DiscoveryUrls;
            set {
                if (DiscovererModel.RequestedConfig == null) {
                    DiscovererModel.RequestedConfig = new DiscoveryConfigApiModel();
                }
                DiscovererModel.RequestedConfig.DiscoveryUrls = value ?? new List<string>();
            }
        }

        /// <summary>
        /// Add url
        /// </summary>
        public void AddDiscoveryUrl(string url) {
            if (Patch.DiscoveryUrls == null) {
                Patch.DiscoveryUrls = DiscovererModel.RequestedConfig?.DiscoveryUrls?
                    .ToList() ?? new List<string>();
            }
            Patch.DiscoveryUrls.Add(url);
            RequestedDiscoveryUrls = Patch.DiscoveryUrls;
        }

        /// <summary>
        /// Remove url
        /// </summary>
        public void RemoveDiscoveryUrl(string url) {
            if (Patch.DiscoveryUrls == null) {
                Patch.DiscoveryUrls = DiscovererModel.RequestedConfig?.DiscoveryUrls?
                    .ToList() ?? new List<string>();
            }
            Patch.DiscoveryUrls.Remove(url);
            RequestedDiscoveryUrls = Patch.DiscoveryUrls;
        }

        /// <summary>
        /// List of locales to filter with during discovery
        /// </summary>
        public List<string> RequestedLocales {
            get => DiscovererModel.RequestedConfig?.Locales == null ?
                new List<string>() : DiscovererModel.RequestedConfig.Locales;
            set {
                if (DiscovererModel.RequestedConfig == null) {
                    DiscovererModel.RequestedConfig = new DiscoveryConfigApiModel();
                }
                DiscovererModel.RequestedConfig.Locales = value ?? new List<string>();
            }
        }

        /// <summary>
        /// Add locale
        /// </summary>
        public void AddLocale(string locale) {
            if (Patch.Locales == null) {
                Patch.Locales = DiscovererModel.RequestedConfig?.Locales?
                    .ToList() ?? new List<string>();
            }
            Patch.Locales.Add(locale);
            RequestedLocales = Patch.Locales;
        }

        /// <summary>
        /// remove locale
        /// </summary>
        public void RemoveLocale(string locale) {
            if (Patch.Locales == null) {
                Patch.Locales = DiscovererModel.RequestedConfig?.Locales?
                    .ToList() ?? new List<string>();
            }
            Patch.Locales.Remove(locale);
            RequestedLocales = Patch.Locales;
        }
    }
}
