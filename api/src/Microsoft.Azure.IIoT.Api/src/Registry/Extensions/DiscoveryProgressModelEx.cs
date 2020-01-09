// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Registry.Models {
    using Microsoft.Azure.IIoT.OpcUa.Registry.Models;
    using System.Collections.Generic;
    using System.Linq;

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
    }
}
