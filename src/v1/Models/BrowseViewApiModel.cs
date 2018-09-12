// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Modules.Twin.v1.Models {
    using Microsoft.Azure.IIoT.OpcUa.Models;
    using Newtonsoft.Json;
    using System;
    using System.ComponentModel;
    using System.ComponentModel.DataAnnotations;

    /// <summary>
    /// browse view model for webservice api
    /// </summary>
    public class BrowseViewApiModel {

        /// <summary>
        /// Default constructor
        /// </summary>
        public BrowseViewApiModel() { }

        /// <summary>
        /// Create from service model
        /// </summary>
        /// <param name="model"></param>
        public BrowseViewApiModel(BrowseViewModel model) {
            ViewId = model.ViewId;
            Version = model.Version;
            Timestamp = model.Timestamp;
        }

        /// <summary>
        /// Convert back to service model
        /// </summary>
        /// <returns></returns>
        public BrowseViewModel ToServiceModel() {
            return new BrowseViewModel {
                ViewId = ViewId,
                Version = Version,
                Timestamp = Timestamp
            };
        }

        /// <summary>
        /// Node of the view to browse
        /// </summary>
        public string ViewId { get; set; }

        /// <summary>
        /// Browses specific version of the view.
        /// </summary>
        public uint? Version { get; set; }

        /// <summary>
        /// Browses at or before this timestamp.
        /// </summary>
        public DateTime? Timestamp { get; set; }
    }
}
