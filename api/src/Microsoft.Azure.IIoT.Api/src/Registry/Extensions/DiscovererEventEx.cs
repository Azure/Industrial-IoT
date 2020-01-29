// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Registry.Models {
    using Microsoft.Azure.IIoT.OpcUa.Registry.Events.v2.Models;
    using System;

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
                IsPatch = model.IsPatch,
                Discoverer = model.Discoverer.Map<DiscovererApiModel>()
            };
        }
    }
}