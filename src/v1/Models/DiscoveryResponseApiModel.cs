// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Modules.Twin.v1.Models {
    using Microsoft.Azure.IIoT.OpcUa.Models;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// For manual discovery requests
    /// </summary>
    public class DiscoveryResponseApiModel {

        /// <summary>
        /// Default constructor
        /// </summary>
        public DiscoveryResponseApiModel() { }

        /// <summary>
        /// Create from service model
        /// </summary>
        /// <param name="model"></param>
        public DiscoveryResponseApiModel(DiscoveryResultModel model) {
            TimeStamp = model.TimeStamp;
            if (model?.Found == null) {
                Found = new List<ApplicationRegistrationApiModel>();
            }
            else {
                Found = model.Found
                    .Select(a => new ApplicationRegistrationApiModel(a))
                    .ToList();
            }
        }

        /// <summary>
        /// Discovered endpoint
        /// </summary>
        public List<ApplicationRegistrationApiModel> Found { get; set; }

        /// <summary>
        /// Timestamp of the discovery sweep
        /// </summary>
        public DateTime TimeStamp { get; set; }
    }
}
