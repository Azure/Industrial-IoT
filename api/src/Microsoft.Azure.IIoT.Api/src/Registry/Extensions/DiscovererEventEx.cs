// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Registry.Models {
    using Microsoft.Azure.IIoT.OpcUa.Registry.Events.v2.Models;
    using Microsoft.Azure.IIoT.OpcUa.Registry.Models;

    /// <summary>
    /// Discoverer event extensions
    /// </summary>
    public static class DiscovererEventEx {

        /// <summary>
        /// Convert to api model
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static DiscovererEventApiModel ToApiModel(
            this DiscovererEventModel model) {
            return new DiscovererEventApiModel {
                EventType = (DiscovererEventType)model.EventType,
                Id = model.Id,
                Discoverer = model.Discoverer.ToApiModel()
            };
        }

        /// <summary>
        /// Create api model
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        private static DiscovererApiModel ToApiModel(
            this DiscovererModel model) {
            if (model == null) {
                return null;
            }
            return new DiscovererApiModel {
                Id = model.Id,
                SiteId = model.SiteId,
                LogLevel = (TraceLogLevel?)model.LogLevel,
                Discovery = (DiscoveryMode?)model.Discovery,
                DiscoveryConfig = model.DiscoveryConfig.ToApiModel(),
                OutOfSync = model.OutOfSync,
                Connected = model.Connected
            };
        }

        /// <summary>
        /// Create api model
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        private static DiscoveryConfigApiModel ToApiModel(
            this DiscoveryConfigModel model) {
            if (model == null) {
                return null;
            }
            return new DiscoveryConfigApiModel {
                AddressRangesToScan = model.AddressRangesToScan,
                NetworkProbeTimeout = model.NetworkProbeTimeout,
                MaxNetworkProbes = model.MaxNetworkProbes,
                PortRangesToScan = model.PortRangesToScan,
                PortProbeTimeout = model.PortProbeTimeout,
                MaxPortProbes = model.MaxPortProbes,
                MinPortProbesPercent = model.MinPortProbesPercent,
                IdleTimeBetweenScans = model.IdleTimeBetweenScans,
                ActivationFilter = model.ActivationFilter.ToApiModel(),
                Locales = model.Locales,
                DiscoveryUrls = model.DiscoveryUrls
            };
        }

        /// <summary>
        /// Create api model
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        private static EndpointActivationFilterApiModel ToApiModel(
            this EndpointActivationFilterModel model) {
            if (model == null) {
                return null;
            }
            return new EndpointActivationFilterApiModel {
                TrustLists = model.TrustLists,
                SecurityPolicies = model.SecurityPolicies,
                SecurityMode = (Core.Models.SecurityMode?)model.SecurityMode
            };
        }
    }
}