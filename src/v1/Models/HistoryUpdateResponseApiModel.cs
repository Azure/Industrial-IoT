// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Modules.Twin.v1.Models {
    using Microsoft.Azure.IIoT.OpcUa.Twin.Models;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// History update results
    /// </summary>
    public class HistoryUpdateResponseApiModel {

        /// <summary>
        /// Default constructor
        /// </summary>
        public HistoryUpdateResponseApiModel() { }

        /// <summary>
        /// Create from service model
        /// </summary>
        /// <param name="model"></param>
        public HistoryUpdateResponseApiModel(HistoryUpdateResultModel model) {
            Results = model.Results?
                .Select(r => new ServiceResultApiModel(r)).ToList();
            ErrorInfo = model.ErrorInfo == null ? null :
                new ServiceResultApiModel(model.ErrorInfo);
        }

        /// <summary>
        /// List of results from the update operation
        /// </summary>
        public List<ServiceResultApiModel> Results { get; set; }

        /// <summary>
        /// Service result in case of service call error
        /// </summary>
        public ServiceResultApiModel ErrorInfo { get; set; }
    }
}
