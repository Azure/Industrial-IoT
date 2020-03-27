// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Registry.Models {
    using Microsoft.Azure.IIoT.OpcUa.Registry.Events.v2.Models;
    using Microsoft.Azure.IIoT.OpcUa.Registry.Models;
    using System.Linq;

    /// <summary>
    /// Publisher event extensions
    /// </summary>
    public static class PublisherEventEx {

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
        /// Create api model
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        private static PublisherApiModel ToApiModel(
            this PublisherModel model) {
            if (model == null) {
                return null;
            }
            return new PublisherApiModel {
                Id = model.Id,
                SiteId = model.SiteId,
                LogLevel = (TraceLogLevel?)model.LogLevel,
                Configuration = model.Configuration.ToApiModel(),
                OutOfSync = model.OutOfSync,
                Connected = model.Connected
            };
        }

        /// <summary>
        /// Create api model
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        private static PublisherConfigApiModel ToApiModel(
            this PublisherConfigModel model) {
            if (model == null) {
                return null;
            }
            return new PublisherConfigApiModel {
                Capabilities = model.Capabilities?.ToDictionary(k => k.Key, v => v.Value),
                HeartbeatInterval = model.HeartbeatInterval,
                JobCheckInterval = model.JobCheckInterval,
                JobOrchestratorUrl = model.JobOrchestratorUrl,
                MaxWorkers = model.MaxWorkers
            };
        }
    }
}