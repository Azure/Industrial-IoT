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
        /// Discovery started
        /// </summary>
        /// <param name="request"></param>
        void OnDiscoveryStarted(DiscoveryRequestModel request);

        /// <summary>
        /// Network scanning completed
        /// </summary>
        /// <param name="request"></param>
        /// <param name="netscanner"></param>
        /// <param name="found"></param>
        void OnNetScanFinished(DiscoveryRequestModel request,
            IScanner netscanner, int found);

        /// <summary>
        /// Network scanning progress
        /// </summary>
        /// <param name="request"></param>
        /// <param name="netscanner"></param>
        /// <param name="found"></param>
        void OnNetScanProgress(DiscoveryRequestModel request,
            IScanner netscanner, int found);

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
        void OnNetScanStarted(DiscoveryRequestModel request,
            IScanner netscanner);

        /// <summary>
        /// Port scanning completed
        /// </summary>
        /// <param name="request"></param>
        /// <param name="portscan"></param>
        /// <param name="found"></param>
        void OnPortScanFinished(DiscoveryRequestModel request,
            IScanner portscan, int found);

        /// <summary>
        /// Port scanning progress
        /// </summary>
        /// <param name="request"></param>
        /// <param name="portscan"></param>
        /// <param name="found"></param>
        void OnPortScanProgress(DiscoveryRequestModel request,
            IScanner portscan, int found);

        /// <summary>
        /// Port scan result
        /// </summary>
        /// <param name="request"></param>
        /// <param name="portscan"></param>
        /// <param name="ep"></param>
        void OnPortScanResult(DiscoveryRequestModel request,
            IScanner portscan,
            IPEndPoint ep);

        /// <summary>
        /// Port scan started
        /// </summary>
        /// <param name="request"></param>
        /// <param name="portscan"></param>
        void OnPortScanStart(DiscoveryRequestModel request,
            IScanner portscan);

        /// <summary>
        /// Server discovery started
        /// </summary>
        /// <param name="request"></param>
        /// <param name="discoveryUrls"></param>
        void OnServerDiscoveryStarted(DiscoveryRequestModel request,
            IDictionary<IPEndPoint, Uri> discoveryUrls);

        /// <summary>
        /// Finding endpoints started
        /// </summary>
        /// <param name="request"></param>
        /// <param name="url"></param>
        /// <param name="address"></param>
        void OnFindEndpointsStarted(DiscoveryRequestModel request,
            Uri url, IPAddress address);

        /// <summary>
        /// Finding endpoints completed
        /// </summary>
        /// <param name="request"></param>
        /// <param name="url"></param>
        /// <param name="address"></param>
        /// <param name="found"></param>
        void OnFindEndpointsFinished(DiscoveryRequestModel request,
            Uri url, IPAddress address, int found);

        /// <summary>
        /// Server discovery complete
        /// </summary>
        /// <param name="request"></param>
        /// <param name="found"></param>
        void OnServerDiscoveryFinished(DiscoveryRequestModel request,
            int found);

        /// <summary>
        /// Discovery cancelled
        /// </summary>
        /// <param name="request"></param>
        void OnDiscoveryCancelled(DiscoveryRequestModel request);

        /// <summary>
        /// Discovery finished successfully
        /// </summary>
        /// <param name="request"></param>
        void OnDiscoveryFinished(DiscoveryRequestModel request);

        /// <summary>
        /// Discovery finished with error
        /// </summary>
        /// <param name="request"></param>
        /// <param name="ex"></param>
        void OnDiscoveryError(DiscoveryRequestModel request,
            Exception ex);
    }
}