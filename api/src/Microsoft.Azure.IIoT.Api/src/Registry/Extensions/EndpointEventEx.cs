// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Registry.Models {
    using Microsoft.Azure.IIoT.OpcUa.Registry.Events.v2.Models;
    using System;

    /// <summary>
    /// Endpoint event extensions
    /// </summary>
    public static class EndpointEventEx {

        /// <summary>
        /// Convert to api model
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static EndpointEventApiModel ToApiModel(
            this EndpointEventModel model) {
            return new EndpointEventApiModel {
                EventType = (EndpointEventType)model.EventType,
                IsPatch = model.IsPatch,
                Id = model.Id,
                Endpoint = model.Endpoint.Map<EndpointInfoApiModel>()
            };
        }
    }
}
