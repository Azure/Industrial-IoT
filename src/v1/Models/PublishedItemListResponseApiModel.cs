// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Modules.Twin.v1.Models {
    using Microsoft.Azure.IIoT.OpcUa.Twin.Models;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// List of published items
    /// </summary>
    public class PublishedItemListResponseApiModel {

        /// <summary>
        /// Default constructor
        /// </summary>
        public PublishedItemListResponseApiModel() { }

        /// <summary>
        /// Create api model from service model
        /// </summary>
        /// <param name="model"></param>
        public PublishedItemListResponseApiModel(PublishedItemListResultModel model) {
            ContinuationToken = model?.ContinuationToken;
            Items = model?.Items?
                .Select(n => new PublishedItemApiModel(n))
                .ToList();
        }

        /// <summary>
        /// Continuation or null if final
        /// </summary>
        public string ContinuationToken { get; set; }

        /// <summary>
        /// Monitored items
        /// </summary>
        public List<PublishedItemApiModel> Items { get; set; }
    }
}
