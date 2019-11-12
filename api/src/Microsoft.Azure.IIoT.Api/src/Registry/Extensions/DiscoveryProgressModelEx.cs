// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Registry.Models {
    using Microsoft.Azure.IIoT.OpcUa.Registry.Models;

    /// <summary>
    /// Discovery message model extensions
    /// </summary>
    public static class DiscoveryProgressModelEx {

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
                RequestDetails = model.RequestDetails,
                RequestId = model.Request?.Id,
                Result = model.Result,
                SupervisorId = model.SupervisorId,
                TimeStamp = model.TimeStamp,
                Workers = model.Workers
            };
        }
    }
}
