// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Modules.Twin.v1.Models {
    using Microsoft.Azure.IIoT.OpcUa.Twin.Models;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// History read results
    /// </summary>
    public class HistoryReadResponseApiModel {

        /// <summary>
        /// Default constructor
        /// </summary>
        public HistoryReadResponseApiModel() { }

        /// <summary>
        /// Create from service model
        /// </summary>
        /// <param name="model"></param>
        public HistoryReadResponseApiModel(HistoryReadResultModel model) {
            History = model.History;
            ContinuationToken = model.ContinuationToken;
            ErrorInfo = model.ErrorInfo == null ? null :
                new ServiceResultApiModel(model.ErrorInfo);
        }

        /// <summary>
        /// History as json encoded extension object
        /// </summary>
        public JToken History { get; set; }

        /// <summary>
        /// Continuation token if more results pending.
        /// </summary>
        public string ContinuationToken { get; set; }

        /// <summary>
        /// Service result in case of error
        /// </summary>
        public ServiceResultApiModel ErrorInfo { get; set; }
    }
}
