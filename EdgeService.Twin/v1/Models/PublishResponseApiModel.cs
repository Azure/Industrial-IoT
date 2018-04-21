// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcTwin.EdgeService.v1.Models {
    using Microsoft.Azure.IIoT.OpcTwin.Services.Models;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// publish response model for edge service api
    /// </summary>
    public class PublishResponseApiModel {
        /// <summary>
        /// Default constructor
        /// </summary>
        public PublishResponseApiModel() {}

        /// <summary>
        /// Create from service model
        /// </summary>
        /// <param name="model"></param>
        public PublishResponseApiModel(PublishResultModel model) {
            Diagnostics = model.Diagnostics;
        }

        /// <summary>
        /// Optional error diagnostics
        /// </summary>
        public JToken Diagnostics { get; set; }
    }
}
