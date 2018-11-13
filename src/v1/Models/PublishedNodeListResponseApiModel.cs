// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Modules.Twin.v1.Models {
    using Microsoft.Azure.IIoT.OpcUa.Twin.Models;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// List of published nodes
    /// </summary>
    public class PublishedNodeListResponseApiModel {

        /// <summary>
        /// Default constructor
        /// </summary>
        public PublishedNodeListResponseApiModel() { }

        /// <summary>
        /// Create api model from service model
        /// </summary>
        /// <param name="model"></param>
        public PublishedNodeListResponseApiModel(PublishedNodeListResultModel model) {
            ContinuationToken = model?.ContinuationToken;
            Items = model?.Items?
                .Select(n => new PublishedNodeApiModel(n))
                .ToList();
        }

        /// <summary>
        /// Continuation or null if final
        /// </summary>
        public string ContinuationToken { get; set; }

        /// <summary>
        /// Monitored items
        /// </summary>
        public List<PublishedNodeApiModel> Items { get; set; }
    }
}
