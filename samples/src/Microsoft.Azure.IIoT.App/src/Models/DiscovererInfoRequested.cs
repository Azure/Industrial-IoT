using Microsoft.Azure.IIoT.App.Services;
using Microsoft.Azure.IIoT.OpcUa.Api.Registry.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.IIoT.App.Models
{
    public class DiscovererInfoRequested
    {
        public DiscovererInfoRequested()
        {

        }
        public DiscovererInfoRequested(DiscovererApiModel data)
        {
            RequestedNetworkProbeTimeout = data.RequestedConfig.NetworkProbeTimeout.ToString();
            RequestedMaxNetworkProbes = data.RequestedConfig.MaxNetworkProbes.ToString();
            RequestedPortProbeTimeout = data.RequestedConfig.PortProbeTimeout.ToString();
            RequestedMaxPortProbes = data.RequestedConfig.MaxPortProbes.ToString();
            RequestedIdleTimeBetweenScans = data.RequestedConfig.IdleTimeBetweenScans.ToString();
            RequestedAddressRangesToScan = data.RequestedConfig.AddressRangesToScan.ToString();
            RequestedPortRangesToScan = data.RequestedConfig.PortRangesToScan.ToString();
            RequestedDiscoveryUrls = new List<string>();
            RequestedLocales = new List<string>();
        }
        /// <summary>
        /// Network probe timeout
        /// </summary>
        public string RequestedNetworkProbeTimeout { get; set; }

        /// <summary>
        /// Max network probes that should ever run.
        /// </summary>
        public string RequestedMaxNetworkProbes { get; set; }

        /// <summary>
        /// Port probe timeout
        /// </summary>
        public string RequestedPortProbeTimeout { get; set; }

        /// <summary>
        /// Max port probes that should ever run.
        /// </summary>
        public string RequestedMaxPortProbes { get; set; }

        /// <summary>
        /// Delay time between discovery sweeps in seconds
        /// </summary>
        public string RequestedIdleTimeBetweenScans { get; set; }

        /// <summary>
        /// Address ranges to scan (null == all wired nics)
        /// </summary>
        public string RequestedAddressRangesToScan { get; set; }

        /// <summary>
        /// Port ranges to scan (null == all unassigned)
        /// </summary>
        public string RequestedPortRangesToScan { get; set; }

        /// <summary>
        /// List of preset discovery urls to use
        /// </summary>
        public List<string> RequestedDiscoveryUrls { get; set; }
        /// <summary>
        /// Add url
        /// </summary>
        public void AddDiscoveryUrl(string url)
        {
            RequestedDiscoveryUrls ??= new List<string>();
            RequestedDiscoveryUrls.Add(url);
        }

        /// <summary>
        /// Remove url
        /// </summary>
        public void RemoveDiscoveryUrl(string url)
        {
            RequestedDiscoveryUrls ??= new List<string>();
            RequestedDiscoveryUrls.Remove(url);
        }

        /// <summary>
        /// List of locales to filter with during discovery
        /// </summary>
        public List<string> RequestedLocales { get; set; }

        /// <summary>
        /// Add locale
        /// </summary>
        public void AddLocale(string locale)
        {
            RequestedLocales ??= new List<string>();
            RequestedLocales.Add(locale);
        }

        /// <summary>
        /// remove locale
        /// </summary>
        public void RemoveLocale(string locale)
        {
            RequestedLocales ??= new List<string>();
            RequestedLocales.Remove(locale);
        }
    }
}
