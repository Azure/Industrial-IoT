// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Services.OpcUa.Twin.History.v2.Models {
    using Microsoft.Azure.IIoT.OpcUa.Twin.Models;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// Event filter
    /// </summary>
    public class EventFilterApiModel : JObject {

        /// <inheritdoc/>
        public EventFilterApiModel() {
        }

        /// <summary>
        /// Create api model from service model
        /// </summary>
        /// <param name="model"></param>
        public EventFilterApiModel(EventFilterModel model) :
            base(model) {
        }

        /// <summary>
        /// Create service model from api model
        /// </summary>
        public EventFilterModel ToServiceModel() {
            return ToObject<EventFilterModel>();
        }
    }
}