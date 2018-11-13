// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Modules.Twin.v1.Models {
    using Microsoft.Azure.IIoT.OpcUa.Twin.Models;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Result of node browse continuation
    /// </summary>
    public class BrowsePathResponseApiModel {

        /// <summary>
        /// Default constructor
        /// </summary>
        public BrowsePathResponseApiModel() { }

        /// <summary>
        /// Create from service model
        /// </summary>
        /// <param name="model"></param>
        public BrowsePathResponseApiModel(BrowsePathResultModel model) {
            ErrorInfo = model?.ErrorInfo == null ? null :
                new ServiceResultApiModel(model.ErrorInfo);
            Targets = model?.Targets?
                .Select(r => new NodePathTargetApiModel(r))
                .ToList();
        }


        /// <summary>
        /// Targets
        /// </summary>
        public List<NodePathTargetApiModel> Targets { get; set; }

        /// <summary>
        /// Service result in case of error
        /// </summary>
        public ServiceResultApiModel ErrorInfo { get; set; }
    }
}
