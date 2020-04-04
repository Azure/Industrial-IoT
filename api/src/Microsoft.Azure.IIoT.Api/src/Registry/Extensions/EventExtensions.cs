// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Registry.Models {
    using Microsoft.Azure.IIoT.OpcUa.Registry.Events.v2.Models;
    using Microsoft.Azure.IIoT.OpcUa.Registry.Models;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Event extensions
    /// </summary>
    public static class EventExtensions {

        /// <summary>
        /// Convert to api model
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static DiscoveryProgressApiModel ToApiModel(
            this DiscoveryProgressModel model) {
            return new DiscoveryProgressApiModel {
                Discovered = model.Discovered,
                EventType = (DiscoveryProgressType)model.EventType,
                Progress = model.Progress,
                Total = model.Total,
                RequestDetails = model.RequestDetails?
                    .ToDictionary(k => k.Key, v => v.Value),
                RequestId = model.Request?.Id,
                Result = model.Result,
                ResultDetails = model.ResultDetails?
                    .ToDictionary(k => k.Key, v => v.Value),
                DiscovererId = model.DiscovererId,
                TimeStamp = model.TimeStamp,
                Workers = model.Workers
            };
        }

        /// <summary>
        /// Convert to api model
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static ApplicationEventApiModel ToApiModel(
            this ApplicationEventModel model) {
            return new ApplicationEventApiModel {
                EventType = (ApplicationEventType)model.EventType,
                Id = model.Id,
                Application = model.Application.ToApiModel()
            };
        }

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
        /// Convert to api model
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static SupervisorEventApiModel ToApiModel(
            this SupervisorEventModel model) {
            return new SupervisorEventApiModel {
                EventType = (SupervisorEventType)model.EventType,
                Id = model.Id,
                Supervisor = model.Supervisor.ToApiModel()
            };
        }

        /// <summary>
        /// Convert to api model
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static GatewayEventApiModel ToApiModel(
            this GatewayEventModel model) {
            return new GatewayEventApiModel {
                EventType = (GatewayEventType)model.EventType,
                Id = model.Id,
                Gateway = model.Gateway.ToApiModel()
            };
        }

        /// <summary>
        /// Convert to api model
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static PublisherEventApiModel ToApiModel(
            this PublisherEventModel model) {
            return new PublisherEventApiModel {
                EventType = (PublisherEventType)model.EventType,
                Id = model.Id,
                Publisher = model.Publisher.ToApiModel()
            };
        }

        /// <summary>
        /// Convert to api model
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static EndpointEventApiModel ToApiModel(
            this EndpointEventModel model) {
            return new EndpointEventApiModel {
                EventType = (EndpointEventType)model.EventType,
                Id = model.Id,
                Endpoint = model.Endpoint.ToApiModel()
            };
        }
    }
}