// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Services.OpcUa.Twin.v1.Models {
    using Microsoft.Azure.IIoT.OpcUa.Twin.Models;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using System;
    using System.ComponentModel;
    using System.ComponentModel.DataAnnotations;

    /// <summary>
    /// Request node history update
    /// </summary>
    public class HistoryUpdateRequestApiModel {

        /// <summary>
        /// Default constructor
        /// </summary>
        public HistoryUpdateRequestApiModel() { }

        /// <summary>
        /// Create from service model
        /// </summary>
        /// <param name="model"></param>
        public HistoryUpdateRequestApiModel(HistoryUpdateRequestModel model) {
            if (model == null) {
                throw new ArgumentNullException(nameof(model));
            }
            Request = model.Request;
            Header = model.Header == null ? null :
                new RequestHeaderApiModel(model.Header);
        }

        /// <summary>
        /// Convert back to service model
        /// </summary>
        /// <returns></returns>
        public HistoryUpdateRequestModel ToServiceModel() {
            return new HistoryUpdateRequestModel {
                Request = Request,
                Header = Header?.ToServiceModel()
            };
        }

        /// <summary>
        /// The HistoryUpdateDetailsType extension object
        /// encoded in json and containing the tunneled
        /// update request for the Historian server.
        /// </summary>
        [JsonProperty(PropertyName = "request")]
        [Required]
        public JToken Request { get; set; }

        /// <summary>
        /// Optional request header
        /// </summary>
        [JsonProperty(PropertyName = "header",
            NullValueHandling = NullValueHandling.Ignore)]
        [DefaultValue(null)]
        public RequestHeaderApiModel Header { get; set; }
    }
}
