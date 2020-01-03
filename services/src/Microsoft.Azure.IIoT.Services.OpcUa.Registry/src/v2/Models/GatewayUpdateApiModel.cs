// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Services.OpcUa.Registry.v2.Models {
    using Microsoft.Azure.IIoT.OpcUa.Registry.Models;
    using Newtonsoft.Json;
    using System.ComponentModel;

    /// <summary>
    /// Gateway registration update request
    /// </summary>
    public class GatewayUpdateApiModel {

        /// <summary>
        /// Default constructor
        /// </summary>
        public GatewayUpdateApiModel() { }

        /// <summary>
        /// Create from service model
        /// </summary>
        /// <param name="model"></param>
        public GatewayUpdateApiModel(GatewayUpdateModel model) {
            SiteId = model.SiteId;
        }

        /// <summary>
        /// Convert back to service model
        /// </summary>
        /// <returns></returns>
        public GatewayUpdateModel ToServiceModel() {
            return new GatewayUpdateModel {
                SiteId = SiteId,
            };
        }

        /// <summary>
        /// Site of the Gateway
        /// </summary>
        [JsonProperty(PropertyName = "siteId",
            NullValueHandling = NullValueHandling.Ignore)]
        [DefaultValue(null)]
        public string SiteId { get; set; }
    }
}
