// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher
{
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Azure.IIoT.OpcUa.Publisher.Stack;
    using System;
    using System.Net;

    /// <summary>
    /// Discovery progress listener
    /// </summary>
    public interface IDiscoveryProgress
    {
        /// <summary>
        /// Pending requests ahead of this one.
        /// </summary>
        /// <param name="request"></param>
        /// <param name="pending"></param>
        void OnDiscoveryPending(DiscoveryRequestModel request,
            int pending);

        /// <summary>
        /// Discovery started
        /// </summary>
        /// <param name="request"></param>
        void OnDiscoveryStarted(DiscoveryRequestModel request);

        /// <summary>
        /// Network scanning completed
        /// </summary>
        /// <param name="request"></param>
        /// <param name="workers"></param>
        /// <param name="progress"></param>
        /// <param name="total"></param>
        /// <param name="discovered"></param>
        void OnNetScanFinished(DiscoveryRequestModel request,
            int workers, int progress, int total, int discovered);

        /// <summary>
        /// Network scanning progress
        /// </summary>
        /// <param name="request"></param>
        /// <param name="workers"></param>
        /// <param name="progress"></param>
        /// <param name="total"></param>
        /// <param name="discovered"></param>
        void OnNetScanProgress(DiscoveryRequestModel request,
            int workers, int progress, int total, int discovered);

        /// <summary>
        /// Network scanning result
        /// </summary>
        /// <param name="request"></param>
        /// <param name="workers"></param>
        /// <param name="progress"></param>
        /// <param name="total"></param>
        /// <param name="discovered"></param>
        /// <param name="address"></param>
        void OnNetScanResult(DiscoveryRequestModel request,
            int workers, int progress, int total, int discovered,
            IPAddress address);

        /// <summary>
        /// Network scanning started
        /// </summary>
        /// <param name="request"></param>
        /// <param name="workers"></param>
        /// <param name="progress"></param>
        /// <param name="total"></param>
        void OnNetScanStarted(DiscoveryRequestModel request,
            int workers, int progress, int total);

        /// <summary>
        /// Port scanning completed
        /// </summary>
        /// <param name="request"></param>
        /// <param name="workers"></param>
        /// <param name="progress"></param>
        /// <param name="total"></param>
        /// <param name="discovered"></param>
        void OnPortScanFinished(DiscoveryRequestModel request,
            int workers, int progress, int total, int discovered);

        /// <summary>
        /// Port scanning progress
        /// </summary>
        /// <param name="request"></param>
        /// <param name="workers"></param>
        /// <param name="progress"></param>
        /// <param name="total"></param>
        /// <param name="discovered"></param>
        void OnPortScanProgress(DiscoveryRequestModel request,
            int workers, int progress, int total, int discovered);

        /// <summary>
        /// Port scan result
        /// </summary>
        /// <param name="request"></param>
        /// <param name="workers"></param>
        /// <param name="progress"></param>
        /// <param name="total"></param>
        /// <param name="discovered"></param>
        /// <param name="ep"></param>
        void OnPortScanResult(DiscoveryRequestModel request,
            int workers, int progress, int total, int discovered, IPEndPoint ep);

        /// <summary>
        /// Port scan started
        /// </summary>
        /// <param name="request"></param>
        /// <param name="workers"></param>
        /// <param name="progress"></param>
        /// <param name="total"></param>
        void OnPortScanStart(DiscoveryRequestModel request,
            int workers, int progress, int total);

        /// <summary>
        /// Server discovery started
        /// </summary>
        /// <param name="request"></param>
        /// <param name="workers"></param>
        /// <param name="progress"></param>
        /// <param name="total"></param>
        void OnServerDiscoveryStarted(DiscoveryRequestModel request,
            int workers, int progress, int total);

        /// <summary>
        /// Finding endpoints started
        /// </summary>
        /// <param name="request"></param>
        /// <param name="workers"></param>
        /// <param name="progress"></param>
        /// <param name="total"></param>
        /// <param name="discovered"></param>
        /// <param name="url"></param>
        /// <param name="address"></param>
        void OnFindEndpointsStarted(DiscoveryRequestModel request,
            int workers, int progress, int total, int discovered,
            Uri url, IPAddress address);

        /// <summary>
        /// Finding endpoints completed
        /// </summary>
        /// <param name="request"></param>
        /// <param name="workers"></param>
        /// <param name="progress"></param>
        /// <param name="total"></param>
        /// <param name="discovered"></param>
        /// <param name="url"></param>
        /// <param name="address"></param>
        /// <param name="endpoints"></param>
        void OnFindEndpointsFinished(DiscoveryRequestModel request,
            int workers, int progress, int total, int discovered,
            Uri url, IPAddress address, int endpoints);

        /// <summary>
        /// Server discovery complete
        /// </summary>
        /// <param name="request"></param>
        /// <param name="workers"></param>
        /// <param name="progress"></param>
        /// <param name="total"></param>
        /// <param name="discovered"></param>
        void OnServerDiscoveryFinished(DiscoveryRequestModel request,
            int workers, int progress, int total, int discovered);

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
