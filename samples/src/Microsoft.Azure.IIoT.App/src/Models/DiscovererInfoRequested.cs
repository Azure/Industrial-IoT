// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.App.Models
{
    using System.Collections.Generic;

    public class DiscovererInfoRequested
    {
        /// <summary>
        /// Address ranges to scan (null == all wired nics)
        /// </summary>
        public string RequestedAddressRangesToScan { get; set; }

        /// <summary>
        /// Port ranges to scan (null == all unassigned)
        /// </summary>
        public string RequestedPortRangesToScan { get; set; }

        /// <summary>
        /// Max network probes that should ever run.
        /// </summary>
        public string RequestedMaxNetworkProbes { get; set; }

        /// <summary>
        /// Max port probes that should ever run.
        /// </summary>
        public string RequestedMaxPortProbes { get; set; }

        /// <summary>
        /// Network probe timeout
        /// </summary>
        public string RequestedNetworkProbeTimeout { get; set; }

        /// <summary>
        /// Port probe timeout
        /// </summary>
        public string RequestedPortProbeTimeout { get; set; }

        /// <summary>
        /// Delay time between discovery sweeps in seconds
        /// </summary>
        public string RequestedIdleTimeBetweenScans { get; set; }

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
        /// Clear url list
        /// </summary>
        public void ClearDiscoveryUrlList(List<string> list) {
            list?.Clear();
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
