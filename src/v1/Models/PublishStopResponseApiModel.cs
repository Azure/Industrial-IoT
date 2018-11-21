// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Modules.Twin.v1.Models {
    using Microsoft.Azure.IIoT.OpcUa.Twin.Models;

    /// <summary>
    /// Result of unpublish request
    /// </summary>
    public class PublishStopResponseApiModel {

        /// <summary>
        /// Default constructor
        /// </summary>
        public PublishStopResponseApiModel() { }

        /// <summary>
        /// Create api model from service model
        /// </summary>
        /// <param name="model"></param>
        public PublishStopResponseApiModel(PublishStopResultModel model) {
            ErrorInfo = model.ErrorInfo == null ? null :
                new ServiceResultApiModel(model.ErrorInfo);
        }

        /// <summary>
        /// Service result in case of error
        /// </summary>
        public ServiceResultApiModel ErrorInfo { get; set; }
    }
}
