// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Edge.Discovery {
    using Microsoft.Azure.IIoT.OpcUa.Registry.Models;
    using Microsoft.Azure.IIoT.Net;
    using System;
    using System.Collections.Generic;
    using System.Net;

    /// <summary>
    /// Event listener
    /// </summary>
    public interface IDiscoveryListener {

        /// <summary>
        /// Server discovery started
        /// </summary>
        /// <param name="request"></param>
        /// <param name="discoveryUrls"></param>
        void OnServerDiscoveryStarted(DiscoveryRequestModel request,
            IDictionary<IPEndPoint, Uri> discoveryUrls);

        /// <summary>
        /// Discovery started
        /// </summary>
        /// <param name="request"></param>
        void OnDiscoveryStarted(DiscoveryRequestModel request);

        /// <summary>
        /// Discovery cancelled
        /// </summary>
        /// <param name="request"></param>
        void OnDiscoveryCancelled(DiscoveryRequestModel request);

        /// <summary>
        /// Discovery complted
        /// </summary>
        /// <param name="request"></param>
        void OnDiscoveryComplete(DiscoveryRequestModel request);

        /// <summary>
        /// Discovery error
        /// </summary>
        /// <param name="request"></param>
        /// <param name="ex"></param>
        void OnDiscoveryError(DiscoveryRequestModel request, Exception ex);

        /// <summary>
        /// Finding endpoints completed
        /// </summary>
        /// <param name="request"></param>
        /// <param name="url"></param>
        /// <param name="address"></param>
        /// <param name="endpoints"></param>
        void OnFindEndpointsComplete(DiscoveryRequestModel request,
            Uri url, IPAddress address, IEnumerable<string> endpoints);

        /// <summary>
        /// Finding endpoints started
        /// </summary>
        /// <param name="request"></param>
        /// <param name="url"></param>
        /// <param name="address"></param>
        void OnFindEndpointsStarted(DiscoveryRequestModel request,
            Uri url, IPAddress address);

        /// <summary>
        /// Network scanning completed
        /// </summary>
        /// <param name="request"></param>
        /// <param name="netscanner"></param>
        /// <param name="addresses"></param>
        /// <param name="elapsed"></param>
        void OnNetScanComplete(DiscoveryRequestModel request,
            IScanner netscanner, IEnumerable<IPAddress> addresses, TimeSpan elapsed);

        /// <summary>
        /// Network scanning progress
        /// </summary>
        /// <param name="request"></param>
        /// <param name="netscanner"></param>
        /// <param name="addresses"></param>
        void OnNetScanProgress(DiscoveryRequestModel request,
            IScanner netscanner, IEnumerable<IPAddress> addresses);

        /// <summary>
        /// Network scanning result
        /// </summary>
        /// <param name="request"></param>
        /// <param name="netscanner"></param>
        /// <param name="address"></param>
        void OnNetScanResult(DiscoveryRequestModel request,
            IScanner netscanner, IPAddress address);

        /// <summary>
        /// Network scanning started
        /// </summary>
        /// <param name="request"></param>
        /// <param name="netscanner"></param>
        void OnNetScanStarted(DiscoveryRequestModel request, IScanner netscanner);

        /// <summary>
        /// Port scanning completed
        /// </summary>
        /// <param name="request"></param>
        /// <param name="portscan"></param>
        /// <param name="ports"></param>
        /// <param name="elapsed"></param>
        void OnPortScanComplete(DiscoveryRequestModel request,
            IScanner portscan, IEnumerable<IPEndPoint> ports, TimeSpan elapsed);

        /// <summary>
        /// Port scanning progress
        /// </summary>
        /// <param name="request"></param>
        /// <param name="portscan"></param>
        /// <param name="ports"></param>
        void OnPortScanProgress(DiscoveryRequestModel request,
            IScanner portscan, IEnumerable<IPEndPoint> ports);

        /// <summary>
        /// Port scan result
        /// </summary>
        /// <param name="request"></param>
        /// <param name="portscan"></param>
        /// <param name="ep"></param>
        void OnPortScanResult(DiscoveryRequestModel request, IScanner portscan,
            IPEndPoint ep);

        /// <summary>
        /// Port scan started
        /// </summary>
        /// <param name="request"></param>
        /// <param name="portscan"></param>
        void OnPortScanStart(DiscoveryRequestModel request, IScanner portscan);

        /// <summary>
        /// Server discovery complete
        /// </summary>
        /// <param name="request"></param>
        /// <param name="discovered"></param>
        void OnServerDiscoveryComplete(DiscoveryRequestModel request,
            IEnumerable<ApplicationRegistrationModel> discovered);
    }
}